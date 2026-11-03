using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 메인 화면 배경에 색 띠가 왼쪽으로 흐르게 합니다. (키보드 RGB 웨이브 같은 느낌)
    // 처음엔 무지개였으나 2026-11-03에 UI와 같은 남색 바탕 + 금색 띠로 바꿨습니다. 색은 Colors 그라디언트로 정합니다.
    // 빈 오브젝트에 붙이면 Play할 때 배경 전용 Canvas와 RawImage를 스스로 만듭니다.
    //   - Canvas는 Screen Space - Camera로 카메라 멀리(Far Clip의 90%)에 그려서
    //     3D 물체는 배경 앞에, 메뉴 UI(Overlay Canvas)는 맨 위에 보입니다.
    //   - 그라디언트로 256 × 128px 그림을 만들고, uvRect를 밀어서 흐르게 합니다. 그림이 반복되므로 그라디언트 처음과 끝 색을 같게 둡니다.
    //   - 띠는 높이에 따라 사인 곡선으로 좌우로 휘어 물결 모양이 되고(2026-11-03), 물결은 위아래로도 천천히 흘러 출렁입니다.
    //     Wave Amplitude를 0으로 두면 예전처럼 곧은 세로 띠입니다.
    // 일시정지와 상관없이 흐르도록 unscaledDeltaTime을 씁니다. 에디터(Play 전)에서는 카메라 배경색만 보입니다.
    public class RainbowBackground : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;                  // 비워 두면 Camera.main
        [SerializeField] private Gradient colors = CreateDefaultColors();
        [SerializeField] private float speed = 0.1f;                   // 1초에 흐르는 바퀴 수 (음수면 오른쪽으로)
        [SerializeField] private float repeat = 1f;                    // 화면 폭 안에 들어가는 바퀴 수

        [Header("Wave")]
        [Range(0f, 0.5f)]
        [SerializeField] private float waveAmplitude = 0.1f;           // 띠가 좌우로 휘는 폭 (한 바퀴 폭 기준 비율, 0이면 곧은 띠)
        [Min(1)]
        [SerializeField] private int waveCount = 1;                    // 화면 높이 안에 들어가는 물결 수 (정수라야 위아래로 흘러도 이음매가 없음)
        [SerializeField] private float waveSpeed = 0.2f;               // 물결이 위아래로 흐르는 속도 (1초에 바퀴 수, 0이면 멈춤)

        private const int TextureWidth = 256;
        private const int TextureHeight = 128;

        private RawImage image;
        private Texture2D texture;
        private float offset;
        private float waveOffset;

        // UI 색(남색 121628, 금색 E8BE5A)에 맞춘 기본값: 남색이 밝아졌다가 가운데에 흐린 금빛 띠가 지나가고 다시 남색으로.
        private static Gradient CreateDefaultColors()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(0x12, 0x16, 0x28, 255), 0f),
                    new GradientColorKey(new Color32(0x1E, 0x25, 0x45, 255), 0.35f),
                    new GradientColorKey(new Color32(0x3A, 0x36, 0x4A, 255), 0.45f),
                    new GradientColorKey(new Color32(0xA8, 0x8A, 0x45, 255), 0.5f),
                    new GradientColorKey(new Color32(0x3A, 0x36, 0x4A, 255), 0.55f),
                    new GradientColorKey(new Color32(0x1E, 0x25, 0x45, 255), 0.65f),
                    new GradientColorKey(new Color32(0x12, 0x16, 0x28, 255), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private void Reset()
        {
            colors = CreateDefaultColors();
        }

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning("[RainbowBackground] 카메라를 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            texture = CreateTexture();
            image = CreateImage();
        }

        private void Update()
        {
            offset = Mathf.Repeat(offset + speed * Time.unscaledDeltaTime, 1f);
            waveOffset = Mathf.Repeat(waveOffset + waveSpeed * Time.unscaledDeltaTime, 1f);
            image.uvRect = new Rect(offset, waveOffset, repeat, 1f);
        }

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        private Texture2D CreateTexture()
        {
            var result = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "Background Wave",
                wrapMode = TextureWrapMode.Repeat,   // 끝과 처음이 이어져 끊김 없이 흐름 (가로·세로 모두)
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < TextureHeight; y++)
            {
                // 높이에 따라 띠 위치를 좌우로 밀어 물결 모양을 만듭니다.
                float shift = waveAmplitude * Mathf.Sin(2f * Mathf.PI * waveCount * y / TextureHeight);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float t = Mathf.Repeat((float)x / TextureWidth + shift, 1f);
                    result.SetPixel(x, y, colors.Evaluate(t));
                }
            }
            result.Apply();
            return result;
        }

        private RawImage CreateImage()
        {
            var canvasObject = new GameObject("Background Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = targetCamera;
            canvas.planeDistance = targetCamera.farClipPlane * 0.9f;

            var imageObject = new GameObject("Background Wave", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;   // 화면 전체 (stretch-stretch, 값 0)
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var raw = imageObject.GetComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return raw;
        }
    }
}

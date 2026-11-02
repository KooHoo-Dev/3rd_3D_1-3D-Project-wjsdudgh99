using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 메인 화면 배경에 무지개 색이 왼쪽으로 흐르게 합니다. (키보드 RGB 웨이브 같은 느낌)
    // 빈 오브젝트에 붙이면 Play할 때 배경 전용 Canvas와 RawImage를 스스로 만듭니다.
    //   - Canvas는 Screen Space - Camera로 카메라 멀리(Far Clip의 90%)에 그려서
    //     3D 물체는 무지개 앞에, 메뉴 UI(Overlay Canvas)는 맨 위에 보입니다.
    //   - 무지개 그림(가로 256px)은 코드가 만들고, uvRect를 밀어서 흐르게 합니다.
    // 일시정지와 상관없이 흐르도록 unscaledDeltaTime을 씁니다. 에디터(Play 전)에서는 카메라 배경색만 보입니다.
    public class RainbowBackground : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;                  // 비워 두면 Camera.main
        [Range(0f, 1f)]
        [SerializeField] private float saturation = 0.45f;             // 낮을수록 파스텔
        [Range(0f, 1f)]
        [SerializeField] private float brightness = 0.85f;
        [SerializeField] private float speed = 0.1f;                   // 1초에 흐르는 무지개 바퀴 수 (음수면 오른쪽으로)
        [SerializeField] private float repeat = 1f;                    // 화면 폭 안에 들어가는 무지개 바퀴 수

        private const int TextureWidth = 256;

        private RawImage image;
        private Texture2D texture;
        private float offset;

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
            image.uvRect = new Rect(offset, 0f, repeat, 1f);
        }

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        private Texture2D CreateTexture()
        {
            var result = new Texture2D(TextureWidth, 1, TextureFormat.RGBA32, false)
            {
                name = "Rainbow",
                wrapMode = TextureWrapMode.Repeat,   // 끝과 처음이 이어져 끊김 없이 흐름
                filterMode = FilterMode.Bilinear,
            };
            for (int x = 0; x < TextureWidth; x++)
            {
                result.SetPixel(x, 0, Color.HSVToRGB((float)x / TextureWidth, saturation, brightness));
            }
            result.Apply();
            return result;
        }

        private RawImage CreateImage()
        {
            var canvasObject = new GameObject("Rainbow Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = targetCamera;
            canvas.planeDistance = targetCamera.farClipPlane * 0.9f;

            var imageObject = new GameObject("Rainbow", typeof(RectTransform), typeof(RawImage));
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

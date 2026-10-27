using UnityEngine;

namespace YoureRight
{
    // Directional Light를 해처럼 움직여 일출 → 한낮 → 일몰을 반복합니다.
    // 일몰 뒤에는 짧은 밤(빛이 꺼졌다 켜짐)을 지나 다시 일출로 돌아갑니다.
    // 해 하나가 두 섬을 똑같이 비추므로 양쪽 밝기·그림자가 항상 같습니다.
    //
    // Directional Light에 붙입니다. Time.deltaTime을 쓰므로 일시정지 중에는 해도 멈춥니다.
    // 에디터에서 값을 바꾸면 Start Time 시점의 모습을 바로 미리 볼 수 있습니다.
    [RequireComponent(typeof(Light))]
    public class SunCycle : MonoBehaviour
    {
        [Header("Cycle")]
        [SerializeField] private float dayDuration = 60f;            // 일출부터 일몰까지 걸리는 시간 (초)
        [SerializeField] private float nightDuration = 4f;           // 일몰 뒤 다시 일출까지의 밤 (0이면 바로 일출로)
        [SerializeField, Range(0f, 1f)] private float startTime = 0.2f;  // 시작 시점 (0 일출, 0.5 한낮, 1 일몰)

        [Header("Path")]
        // 카메라는 -Z 쪽에서 섬을 보므로, 한낮(0)에 해가 카메라 뒤쪽에서 비추어 앞면이 밝게 보입니다.
        [SerializeField] private float sunriseYaw = -90f;            // 일출 때 빛이 향하는 방향 (-90: 오른쪽에서 떠오름)
        [SerializeField] private float sunsetYaw = 90f;              // 일몰 때 빛이 향하는 방향 (90: 왼쪽으로 짐)
        [SerializeField] private float horizonElevation = 5f;        // 일출·일몰 때 해의 높이 (도). 낮을수록 그림자가 길어짐
        [SerializeField] private float noonElevation = 60f;          // 한낮 해의 높이 (도)

        [Header("Light")]
        [SerializeField] private Gradient sunColor;                  // 왼쪽 끝 일출 → 가운데 한낮 → 오른쪽 끝 일몰
        [SerializeField] private float horizonIntensity = 0.6f;
        [SerializeField] private float noonIntensity = 1.3f;

        [Header("Ambient (Lighting 창 Environment Lighting Source가 Color일 때만)")]
        [SerializeField] private bool controlAmbient = true;
        [SerializeField] private Gradient ambientColor;              // 낮 동안의 주변광 (일출 → 한낮 → 일몰)
        [SerializeField] private Color nightAmbient = new Color(0.12f, 0.12f, 0.2f);

        // 낮·밤에 따라 배경색을 바꿀 카메라 하나와 그 카메라의 낮·밤 색
        [System.Serializable]
        private class CameraBackground
        {
            public Camera camera;
            public Color dayColor;
            public Color nightColor;
        }

        [Header("Background (Play 중에만. 해가 밝을수록 Day Color 쪽, 카메라 Background Type은 Solid Color)")]
        [SerializeField] private CameraBackground[] backgrounds;

        private Light sun;
        private float elapsed;

        private float CycleLength => dayDuration + Mathf.Max(0f, nightDuration);

        private void Reset()
        {
            sunColor = MakeGradient(new Color(1f, 0.6f, 0.3f), new Color(1f, 0.96f, 0.88f), new Color(1f, 0.48f, 0.27f));
            ambientColor = MakeGradient(new Color(0.35f, 0.3f, 0.3f), new Color(0.55f, 0.55f, 0.58f), new Color(0.38f, 0.28f, 0.3f));
        }

        private void Awake()
        {
            sun = GetComponent<Light>();
            elapsed = startTime * dayDuration;
        }

        private void Update()
        {
            elapsed = (elapsed + Time.deltaTime) % CycleLength;
            Apply(elapsed);
        }

        // 한 바퀴 안의 시간(초)에 맞춰 해의 방향·색·밝기와 주변광을 정합니다.
        private void Apply(float time)
        {
            if (sun == null) sun = GetComponent<Light>();

            if (time < dayDuration)
            {
                float t = dayDuration > 0f ? time / dayDuration : 0f;   // 0 일출 → 1 일몰
                float height = Mathf.Sin(Mathf.PI * t);                  // 일출·일몰 0, 한낮 1

                float yaw = Mathf.Lerp(sunriseYaw, sunsetYaw, t);
                float elevation = Mathf.Lerp(horizonElevation, noonElevation, height);
                transform.rotation = Quaternion.Euler(elevation, yaw, 0f);

                if (sunColor != null) sun.color = sunColor.Evaluate(t);
                sun.intensity = Mathf.Lerp(horizonIntensity, noonIntensity, height);

                if (controlAmbient && ambientColor != null) RenderSettings.ambientLight = ambientColor.Evaluate(t);
            }
            else
            {
                // 밤: 해가 지평선 아래를 돌아 일출 자리로 가는 동안 빛이 꺼졌다가 다시 켜집니다.
                float n = (time - dayDuration) / nightDuration;          // 0 일몰 직후 → 1 일출 직전
                float dark = Mathf.Sin(Mathf.PI * n);                    // 한밤중 1

                float yaw = Mathf.Lerp(sunsetYaw, sunriseYaw + 360f, n);
                transform.rotation = Quaternion.Euler(horizonElevation, yaw, 0f);

                if (sunColor != null) sun.color = Color.Lerp(sunColor.Evaluate(1f), sunColor.Evaluate(0f), n);
                sun.intensity = horizonIntensity * (1f - dark);

                if (controlAmbient && ambientColor != null)
                {
                    Color dusk = Color.Lerp(ambientColor.Evaluate(1f), ambientColor.Evaluate(0f), n);
                    RenderSettings.ambientLight = Color.Lerp(dusk, nightAmbient, dark);
                }
            }

            // 에디터에서 바꾸면 씬에 저장된 배경색이 덮이므로 Play 중에만 바꿉니다(Play를 끄면 원래 색으로 돌아감).
            if (Application.isPlaying) ApplyBackgrounds();
        }

        // 해의 밝기(한낮 1 → 일출·일몰 약 0.5 → 한밤중 0)만큼 낮 색과 밤 색을 섞습니다.
        private void ApplyBackgrounds()
        {
            if (backgrounds == null) return;

            float daylight = noonIntensity > 0f ? Mathf.Clamp01(sun.intensity / noonIntensity) : 0f;
            foreach (CameraBackground background in backgrounds)
            {
                if (background.camera == null) continue;
                background.camera.backgroundColor = Color.Lerp(background.nightColor, background.dayColor, daylight);
            }
        }

        private void OnValidate()
        {
            dayDuration = Mathf.Max(1f, dayDuration);
            nightDuration = Mathf.Max(0f, nightDuration);

            // Play 중이 아니면 Start Time 시점의 모습을 바로 보여 줍니다.
            if (Application.isPlaying == false) Apply(startTime * dayDuration);
        }

        private static Gradient MakeGradient(Color start, Color middle, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.5f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}

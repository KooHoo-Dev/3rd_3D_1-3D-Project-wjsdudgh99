using UnityEngine;

namespace YoureRight
{
    // 가로등처럼 해가 지면 켜지고 해가 뜨면 꺼지는 불빛입니다. (씬의 SunCycle 밝기를 따라감)
    // 두 섬의 가로등이 같은 해를 읽으므로 양쪽이 항상 똑같이 켜집니다.
    //
    // 가로등 오브젝트에 하나씩 붙입니다. Play 중에만 동작하고, 에디터에서는 씬에 저장된 불빛이 그대로 보입니다.
    // Light의 Intensity는 다 켜졌을 때의 밝기로 씁니다. 그림자는 끄는 것(No Shadows)을 권합니다.
    public class NightLight : MonoBehaviour
    {
        [Header("Sun (비워 두면 씬에서 자동으로 찾음)")]
        [SerializeField] private SunCycle sunCycle;

        [Header("Light (비워 두면 자식의 Light를 모두 사용)")]
        [SerializeField] private Light[] lights;

        [Header("Glow (선택: 전구처럼 같이 빛날 렌더러)")]
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private int glowMaterialIndex = -1;                 // 빛날 머티리얼 번호 (-1이면 그 렌더러의 머티리얼 전부)
        [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(2f, 1.5f, 0.8f);

        [Header("On / Off (해 밝기: 한낮 1, 일출·일몰 약 0.46, 한밤중 0)")]
        [SerializeField, Range(0f, 1f)] private float offAbove = 0.5f;       // 해가 이보다 밝으면 꺼짐
        [SerializeField, Range(0f, 1f)] private float fullOnBelow = 0.25f;   // 해가 이보다 어두우면 다 켜짐

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private float[] maxIntensities;
        private Material[] glowMaterials;

        private void Awake()
        {
            if (sunCycle == null) sunCycle = FindAnyObjectByType<SunCycle>();
            if (sunCycle == null)
            {
                Debug.LogWarning($"[NightLight] {name}: 씬에 SunCycle이 없어서 불빛을 바꾸지 않습니다.", this);
                enabled = false;
                return;
            }

            if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>(true);
            maxIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null) maxIntensities[i] = lights[i].intensity;
            }

            glowMaterials = CreateGlowMaterials();
        }

        private void Update()
        {
            // 해가 offAbove보다 밝으면 0, fullOnBelow보다 어두우면 1
            float on = 1f - Mathf.InverseLerp(fullOnBelow, offAbove, sunCycle.Daylight);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = maxIntensities[i] * on;
                lights[i].enabled = on > 0f;
            }

            foreach (Material material in glowMaterials)
            {
                material.SetColor(EmissionColorId, glowColor * on);
            }
        }

        // 빛날 머티리얼의 복사본을 만들고 Emission을 켭니다. (원본 머티리얼은 다른 물체도 쓰므로 건드리지 않음)
        private Material[] CreateGlowMaterials()
        {
            var result = new System.Collections.Generic.List<Material>();
            if (glowRenderers == null) return result.ToArray();

            foreach (Renderer glowRenderer in glowRenderers)
            {
                if (glowRenderer == null) continue;

                Material[] materials = glowRenderer.materials;   // 이 렌더러 전용 복사본
                for (int i = 0; i < materials.Length; i++)
                {
                    if (glowMaterialIndex >= 0 && i != glowMaterialIndex) continue;
                    materials[i].EnableKeyword("_EMISSION");
                    materials[i].SetColor(EmissionColorId, Color.black);
                    result.Add(materials[i]);
                }
            }
            return result.ToArray();
        }

        private void OnDestroy()
        {
            if (glowMaterials == null) return;
            foreach (Material material in glowMaterials) Destroy(material);
        }

        private void OnValidate()
        {
            fullOnBelow = Mathf.Min(fullOnBelow, offAbove - 0.01f);
        }
    }
}

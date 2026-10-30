using UnityEditor;
using UnityEngine;

namespace YoureRight
{
    // BrokenVector LowPolyCarPack이 쓰는 자체 셰이더(LowPolyShaders/LowPolyPBRShader, LowPolyUnlitShader)를 URP 셰이더로 바꾸는 에디터 도구입니다.
    // Tools → You're Right → Convert LowPoly Shaders to URP
    //
    // 이 셰이더들은 Built-in 전용 Surface Shader라 URP에서는 분홍색으로 나오고, Render Pipeline Converter와
    // "Convert Selected Built-in Materials to URP"는 Standard 같은 기본 셰이더만 알아서 건너뜁니다.
    // 원래 셰이더는 팔레트 텍스처(_MainTex)를 꼭짓점 UV로 읽고 Tint(_Color)를 곱하는 방식이라,
    // URP Lit의 Base Map·Base Color에 그대로 옮기면 같은 색이 나옵니다(팔레트는 Point 필터라 칸 경계가 번지지 않음).
    // PBR → URP Lit(Smoothness·Metallic 옮김, 정답 반짝임용 Emission 검정), Unlit → URP Unlit.
    // 프로젝트 전체에서 두 셰이더를 쓰는 머티리얼을 찾아 바꾸므로 여러 번 실행해도 안전합니다.
    public static class LowPolyShaderConverter
    {
        private const string MenuPath = "Tools/You're Right/Convert LowPoly Shaders to URP";
        private const string Title = "Convert LowPoly Shaders";
        private const string PbrShaderName = "LowPolyShaders/LowPolyPBRShader";
        private const string UnlitShaderName = "LowPolyShaders/LowPolyUnlitShader";

        [MenuItem(MenuPath)]
        private static void Convert()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null)
            {
                EditorUtility.DisplayDialog(Title, "URP Lit/Unlit 셰이더를 찾지 못했습니다.", "OK");
                return;
            }

            int litCount = 0, unlitCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;

                bool isPbr = mat.shader.name == PbrShaderName;
                bool isUnlit = mat.shader.name == UnlitShaderName;
                if (!isPbr && !isUnlit) continue;

                // 셰이더를 바꾸기 전에 원래 값을 읽어 둡니다.
                Texture palette = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color tint = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

                Undo.RecordObject(mat, Title);
                mat.shader = isPbr ? lit : unlit;
                mat.SetTexture("_BaseMap", palette);
                mat.SetColor("_BaseColor", tint);
                if (isPbr)
                {
                    mat.SetFloat("_Smoothness", smoothness);
                    mat.SetFloat("_Metallic", metallic);
                    // EmissiveIsBlack으로 두면 URP가 저장할 때 _EMISSION을 꺼 버리므로 RealtimeEmissive로 둡니다.
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    litCount++;
                }
                else
                {
                    unlitCount++;
                }
                EditorUtility.SetDirty(mat);
                Debug.Log($"[{Title}] {path} → {mat.shader.name}", mat);
            }
            AssetDatabase.SaveAssets();

            string message = litCount + unlitCount == 0
                ? "바꿀 머티리얼이 없습니다."
                : $"URP Lit {litCount}개, URP Unlit {unlitCount}개로 바꿨습니다.";
            EditorUtility.DisplayDialog(Title, message, "OK");
        }
    }
}

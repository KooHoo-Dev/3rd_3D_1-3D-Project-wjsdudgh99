using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YoureRight
{
    // 3-1에 쓰는 Mnostva "Flying Sci-Fi Island City" 에셋을 게임 규칙에 맞게 정리하는 에디터 도구입니다. (여러 번 실행해도 결과는 같음)
    // Tools → You're Right → Setup Sci-Fi Island
    //
    // 먼저 머티리얼 2개를 URP로 바꿔야 합니다(머티리얼 선택 → Edit → Rendering → Materials → Convert Selected Built-in Materials to URP).
    //   - Island_Material: Emission 켬(검정, 정답 반짝임용)
    //   - 모든 프리팹: 메시가 있는 부품마다 MeshCollider, 메시 없는 오브젝트의 Collider(루트 1×1 상자)는 지움
    //   - Glass: Collider 없음(클릭이 유리를 뚫고 안쪽 건물에 맞게)
    //   - 섬 바닥(Island 1, Floor, Driveway, Driveway_2): Background 레이어(무반응)
    // 프리팹을 제자리에서 열어 고치므로 오브젝트 ID가 그대로라 씬에 놓인 섬과 정답 연결이 유지됩니다.
    public static class SciFiIslandSetup
    {
        private const string MenuPath = "Tools/You're Right/Setup Sci-Fi Island";
        private const string Title = "Setup Sci-Fi Island";
        private const string Root = "Assets/Mnostva_Art/Flying_Sci_Fi_Island_city";
        private const string BackgroundLayerName = "Background";

        private static readonly string[] PrefabFolders = { Root + "/Prefabs/island", Root + "/Prefabs/interiors" };
        private static readonly string[] MaterialPaths = { Root + "/Materials/Island_Material.mat", Root + "/Materials/Glass_Material.mat" };
        private const string EmissionMaterialPath = Root + "/Materials/Island_Material.mat";

        private static readonly HashSet<string> NoColliderParts = new HashSet<string> { "Glass" };
        private static readonly HashSet<string> GroundParts = new HashSet<string> { "Island 1", "Floor", "Driveway", "Driveway_2" };

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 실행할 수 없습니다.", "OK");
                return;
            }

            int backgroundLayer = LayerMask.NameToLayer(BackgroundLayerName);
            if (backgroundLayer < 0)
            {
                EditorUtility.DisplayDialog(Title, $"'{BackgroundLayerName}' 레이어가 없습니다.", "OK");
                return;
            }

            // Standard 셰이더가 남아 있으면 URP에서 분홍색이고 Emission 속성 이름도 달라서 먼저 변환하게 합니다.
            foreach (string path in MaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.shader.name.StartsWith("Universal Render Pipeline"))
                {
                    EditorUtility.DisplayDialog(Title,
                        $"{System.IO.Path.GetFileName(path)}가 아직 URP 셰이더가 아닙니다.\n" +
                        "머티리얼 2개를 선택하고 Edit → Rendering → Materials → Convert Selected Built-in Materials to URP를 먼저 실행해 주세요.", "OK");
                    return;
                }
            }

            EnableBlackEmission(AssetDatabase.LoadAssetAtPath<Material>(EmissionMaterialPath));

            var folders = new List<string>();
            foreach (string folder in PrefabFolders)
            {
                if (AssetDatabase.IsValidFolder(folder)) folders.Add(folder);
                else Debug.LogWarning($"[{Title}] 폴더가 없어 건너뜁니다: {folder}");
            }
            if (folders.Count == 0) return;

            int prefabCount = 0, added = 0, removed = 0, grounds = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FixParts(root, backgroundLayer, ref added, ref removed, ref grounds);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            string message = $"프리팹 {prefabCount}개 정리\nMeshCollider {added}개 추가, 다른 Collider {removed}개 삭제\n" +
                             $"바닥 {grounds}개 Background 레이어\nIsland_Material Emission 켬(검정)";
            Debug.Log($"[{Title}] {message.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        // 검정이어도 RealtimeEmissive로 둬야 URP가 저장할 때 _EMISSION을 끄지 않습니다.
        private static void EnableBlackEmission(Material material)
        {
            if (material == null) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
        }

        private static void FixParts(GameObject root, int backgroundLayer, ref int added, ref int removed, ref int grounds)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                var filter = collider.GetComponent<MeshFilter>();
                bool keep = collider is MeshCollider && filter != null && filter.sharedMesh != null
                            && !NoColliderParts.Contains(collider.gameObject.name);
                if (keep) continue;
                Object.DestroyImmediate(collider, true);
                removed++;
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || NoColliderParts.Contains(filter.gameObject.name)) continue;

                var meshCollider = filter.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                    added++;
                }
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                meshCollider.enabled = true;

                if (GroundParts.Contains(filter.gameObject.name) && filter.gameObject.layer != backgroundLayer)
                {
                    filter.gameObject.layer = backgroundLayer;
                    grounds++;
                }
            }
        }
    }
}

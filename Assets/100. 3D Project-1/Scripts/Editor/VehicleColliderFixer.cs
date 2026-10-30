using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YoureRight
{
    // 2-1 자동차 전시장에 쓰는 차 프리팹에 클릭용 Collider를 붙이는 에디터 도구입니다. (여러 번 실행해도 결과는 같음)
    // Tools → You're Right → Fix Vehicle Colliders
    //
    // 차 에셋은 대부분 Collider가 없어서 클릭이 차를 뚫고 받침대(무반응)에 맞고, LowPolyVehicle1·Drivable 픽업은 차 전체를 감싸는 상자가 있어서
    // 문·핸들·좌석을 눌러도 상자에 먼저 맞습니다. 그래서 기차역 소품과 같은 규칙으로 맞춥니다.
    //   - 메시가 있는 부품마다 그 모양의 MeshCollider 하나 (바퀴·문·스포일러 하나만 정답으로 둘 수 있음)
    //   - 상자·구·캡슐·바퀴 Collider와, 메시 없는 오브젝트(루트·그룹)의 Collider는 지움
    // 프리팹을 제자리에서 열어 고치므로 오브젝트 ID가 그대로라 씬에 놓인 차와 정답 연결이 유지됩니다.
    // ARCADE Racing Car는 Rigidbody·WheelCollider가 붙은 "With Colliders" 대신 "Meshes Only" 프리팹만 고칩니다(씬에서도 이쪽을 씀).
    public static class VehicleColliderFixer
    {
        private const string MenuPath = "Tools/You're Right/Fix Vehicle Colliders";
        private const string Title = "Fix Vehicle Colliders";

        private static readonly string[] PrefabFolders =
        {
            "Assets/Floreswa/Prefabs",
            "Assets/BrokenVector/LowPolyCarPack/Prefabs",
            "Assets/ARCADE - FREE Racing Car/Prefabs (Meshes Only)",
            "Assets/LowPolyVehicle1/Prefabs",
            "Assets/Drivable-Free Low Poly Cars/Prefabs/WithoutLod",   // LOD 버전(WithLod)은 쓰지 않음
        };

        // 차가 아닌 프리팹(데모용 바닥 등)은 건너뜁니다.
        private static readonly HashSet<string> Skip = new HashSet<string> { "ground" };

        [MenuItem(MenuPath)]
        private static void Fix()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 실행할 수 없습니다.", "OK");
                return;
            }

            var folders = new List<string>();
            foreach (string folder in PrefabFolders)
            {
                if (AssetDatabase.IsValidFolder(folder)) folders.Add(folder);
                else Debug.LogWarning($"[{Title}] 폴더가 없어 건너뜁니다: {folder}");
            }
            if (folders.Count == 0) return;

            // 원본 프리팹을 먼저 고쳐야 Variant가 물려받은 Collider를 다시 붙이지 않으므로, Variant는 나중에 처리합니다.
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Skip.Contains(Path.GetFileNameWithoutExtension(path))) continue;
                paths.Add(path);
            }
            paths.Sort((a, b) => IsVariant(a).CompareTo(IsVariant(b)));

            int added = 0, removed = 0;
            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FixParts(root, ref added, ref removed);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            string message = $"프리팹 {paths.Count}개 정리\nMeshCollider {added}개 추가, 다른 Collider {removed}개 삭제";
            Debug.Log($"[{Title}] {message.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        private static bool IsVariant(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null && PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant;
        }

        private static void FixParts(GameObject root, ref int added, ref int removed)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                var filter = collider.GetComponent<MeshFilter>();
                bool keep = collider is MeshCollider && filter != null && filter.sharedMesh != null;
                if (keep) continue;
                // Variant가 원본에서 물려받은 Collider는 원본에서 이미 지웠으므로 여기서는 Variant에서 새로 붙인 것만 남아 있습니다.
                Object.DestroyImmediate(collider, true);
                removed++;
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
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
            }
        }
    }
}

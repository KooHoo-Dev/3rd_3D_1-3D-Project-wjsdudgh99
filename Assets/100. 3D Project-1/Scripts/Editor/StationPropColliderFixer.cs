using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YoureRight
{
    // 기차역 소품 프리팹의 Collider를 정리하는 에디터 도구입니다. (한 번 쓰는 정리용, 여러 번 실행해도 결과는 같음)
    // Tools → You're Right → Fix Station Prop Colliders
    //
    // 루트에 상자 하나를 씌우면 상자 겉면에 먼저 맞아서, 자판기 버튼처럼 부품 하나를 정답으로 둔 소품은 클릭이 안 됩니다.
    // 루트를 손으로 MeshCollider로 바꾸면 루트에는 메시가 없어서 Mesh가 빈 채로 남고 아무것도 클릭되지 않습니다.
    // 그래서 모든 소품을 "루트 Collider 없음 + 메시가 있는 부품마다 그 모양의 MeshCollider"로 맞춥니다.
    //   - 소품 통째로 정답: 부품에 맞으면 부모의 AnswerTarget을 찾음 / 부품 하나가 정답: 그 부품에 바로 맞음 / 빈 곳: 무반응
    // Build Station Props를 다시 실행하지 않고 Collider만 고치므로 손으로 고친 모양은 그대로이고,
    // 프리팹을 제자리에서 열어 고치므로 오브젝트 ID가 그대로라 씬에 놓인 소품·정답 연결이 유지됩니다.
    public static class StationPropColliderFixer
    {
        private const string MenuPath = "Tools/You're Right/Fix Station Prop Colliders";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs/Station/Props";

        [MenuItem(MenuPath)]
        private static void Fix()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Fix Station Prop Colliders", "Play 모드에서는 실행할 수 없습니다.", "OK");
                return;
            }

            var done = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name == "Clock Pole") continue;   // 쓰지 않는 예전 프리팹
                Edit(path, FixParts);
                done.Add(name);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[StationPropColliderFixer] {done.Count}개 프리팹 정리 완료 (부품마다 MeshCollider)\n{string.Join("\n", done)}");
        }

        // 프리팹을 열어 고친 뒤 같은 자리에 저장합니다.
        private static void Edit(string path, System.Action<GameObject> fix)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                fix(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 루트의 Collider를 모두 지우고, 메시가 있는 부품 중 Collider가 없는 것에 MeshCollider를 붙입니다.
        private static void FixParts(GameObject root)
        {
            foreach (Collider collider in root.GetComponents<Collider>()) Object.DestroyImmediate(collider, true);

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.gameObject == root || filter.sharedMesh == null) continue;
                if (filter.GetComponent<Collider>() != null) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }
    }
}

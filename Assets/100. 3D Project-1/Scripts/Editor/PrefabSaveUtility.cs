using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace YoureRight
{
    // 코드로 조립한 프리팹을 저장하는 에디터 도우미입니다. 이미 있는 프리팹은 "덮어쓰지 않고 맞춰서" 저장합니다.
    //
    // SaveAsPrefabAsset으로 새 오브젝트를 덮어쓰면 자식 오브젝트의 내부 ID(fileID)가 일부 새로 바뀝니다.
    // 그러면 씬에서 그 자식에 붙인 컴포넌트·고친 값이 사라집니다(2026-10-29: 2-2 스크린도어 24개의 Housing Back/Body에
    // 씬에서 붙인 MeshCollider가 Build Station Kit을 다시 실행하자 모두 사라짐).
    // 그래서 기존 프리팹을 열어 이름 경로가 같은 오브젝트는 그대로 두고 값만 새것으로 바꾸고, 없는 것만 만들고, 남는 것만 지웁니다.
    // 같은 부모 아래 이름이 같은 자식은 순서대로 짝짓습니다.
    internal static class PrefabSaveUtility
    {
        public static void SaveKeepingIds(GameObject source, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                PrefabUtility.SaveAsPrefabAsset(source, path);
                return;
            }

            GameObject target = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Sync(source, target);
                PrefabUtility.SaveAsPrefabAsset(target, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(target);
            }
        }

        private static void Sync(GameObject source, GameObject target)
        {
            target.name = source.name;
            target.layer = source.layer;
            target.SetActive(source.activeSelf);
            target.transform.localPosition = source.transform.localPosition;
            target.transform.localRotation = source.transform.localRotation;
            target.transform.localScale = source.transform.localScale;
            SyncComponents(source, target);

            // 자식: 같은 이름끼리 순서대로 짝짓고, 남는 기존 자식은 지웁니다.
            List<Transform> unused = target.transform.Cast<Transform>().ToList();
            var matched = new List<Transform>();
            foreach (Transform child in source.transform)
            {
                Transform match = unused.FirstOrDefault(t => t.name == child.name);
                if (match != null) unused.Remove(match);
                else
                {
                    match = new GameObject(child.name).transform;
                    match.SetParent(target.transform, false);
                }
                Sync(child.gameObject, match.gameObject);
                matched.Add(match);
            }

            foreach (Transform extra in unused) Object.DestroyImmediate(extra.gameObject);
            for (int i = 0; i < matched.Count; i++) matched[i].SetSiblingIndex(i);
        }

        // Transform 말고는 새것과 같은 종류·개수로 맞추고 값을 복사합니다(같은 종류 컴포넌트는 순서대로 짝지음).
        private static void SyncComponents(GameObject source, GameObject target)
        {
            List<Component> unused = target.GetComponents<Component>().Where(c => c is not Transform).ToList();
            foreach (Component component in source.GetComponents<Component>())
            {
                if (component is Transform) continue;
                Component match = unused.FirstOrDefault(c => c.GetType() == component.GetType());
                if (match != null) unused.Remove(match);
                else match = target.AddComponent(component.GetType());

                ComponentUtility.CopyComponent(component);
                ComponentUtility.PasteComponentValues(match);
            }

            // MeshRenderer가 MeshFilter를 필요로 하는 등 순서 문제를 피하려고 뒤에서부터 지웁니다.
            for (int i = unused.Count - 1; i >= 0; i--) Object.DestroyImmediate(unused[i]);
        }
    }
}

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 이동을 꺼 둔 스테이지용 안내 문구 UI 프리팹(Prefabs/Move Lock Notice)을 만드는 에디터 도구입니다.
    // Tools → You're Right → Build Move Lock Notice
    //
    // 화면 아래 가운데에 붙는 반투명 띠 + 글자 하나입니다(앵커·Pivot 모두 아래 가운데, 여백 120).
    // 스테이지 Canvas에 놓고 TMP 글자를 한국어로 고쳐 씁니다(글꼴은 Noto Sans KR로 넣어 둠).
    // 이미 프리팹이 있으면 덮어쓰지 않습니다. 씬·프리팹에서 고친 문구가 사라지지 않게 하기 위해서입니다.
    // UI라서 PivotUtility는 쓰지 않습니다(3D 조각 그룹용이라 RectTransform 배치를 흐트러뜨림).
    public static class MoveLockNoticeBuilder
    {
        private const string MenuPath = "Tools/You're Right/Build Move Lock Notice";
        private const string Title = "Build Move Lock Notice";
        private const string PrefabPath = "Assets/100. 3D Project-1/Prefabs/Move Lock Notice.prefab";
        private const string FontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                EditorUtility.DisplayDialog(Title, $"이미 프리팹이 있어서 만들지 않았습니다.\n{PrefabPath}", "OK");
                return;
            }

            var root = new GameObject("Move Lock Notice", typeof(RectTransform));
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.anchoredPosition = new Vector2(0f, 120f);
                rootRect.sizeDelta = new Vector2(900f, 70f);

                var visual = new GameObject("Visual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                visual.transform.SetParent(root.transform, false);
                Stretch((RectTransform)visual.transform);
                var background = visual.GetComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.65f);
                background.raycastTarget = false;

                var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(visual.transform, false);
                Stretch((RectTransform)text.transform);
                var label = text.GetComponent<TextMeshProUGUI>();
                label.text = "You can't move in this stage. Right-drag to rotate, wheel to zoom.";
                label.fontSize = 30f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font != null) label.font = font;
                else Debug.LogWarning($"[{Title}] 한글 글꼴을 찾지 못했습니다: {FontPath}");

                var notice = root.AddComponent<MoveLockNotice>();
                var serialized = new SerializedObject(notice);
                serialized.FindProperty("visual").objectReferenceValue = visual.GetComponent<CanvasGroup>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[{Title}] 프리팹 생성: {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabPath));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

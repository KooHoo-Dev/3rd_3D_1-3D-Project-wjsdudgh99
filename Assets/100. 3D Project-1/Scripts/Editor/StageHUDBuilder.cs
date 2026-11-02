using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YoureRight
{
    // A안 UI(반투명 둥근 패널 + 금색 포인트)로 스테이지 상단 HUD 프리팹을 만들고, 스테이지 씬에 바꿔 넣는 에디터 도구입니다.
    //
    // Tools → You're Right → Build Stage HUD
    //   Prefabs/Stage HUD 를 만듭니다. 화면 전체를 덮는 빈 루트(StageHUD 컴포넌트) 아래에
    //   왼쪽 위 스테이지 이름 알약, 가운데 위 타이머 알약, 오른쪽 위 진행도 알약(점 템플릿)을 둡니다. 세 알약 모두 불투명 + 금색 테두리.
    //   이미 있으면 확인 후 덮어씁니다(프리팹에서 직접 고친 값은 사라짐).
    //
    // Tools → You're Right → Apply Stage HUD To Scenes
    //   스테이지 씬 6개와 Template을 차례로 열어, Canvas의 예전 HUD(StageNameText·TimerBackground·TimerText·ProgressText와
    //   Canvas에 붙은 StageHUD)를 지우고 그 자리에 Stage HUD 프리팹을 넣은 뒤 저장합니다. 이미 들어 있는 씬은 건너뜁니다.
    //
    // UI라서 PivotUtility는 쓰지 않습니다(3D 조각 그룹용이라 RectTransform 배치를 흐트러뜨림).
    // 글꼴은 게임 UI 글꼴(UIFontApplier.FontAssetPath, 누림보 고딕)을 씁니다.
    public static class StageHUDBuilder
    {
        private const string Title = "Stage HUD";
        private const string PrefabPath = "Assets/100. 3D Project-1/Prefabs/Stage HUD.prefab";
        private const string SpriteFolder = "Assets/100. 3D Project-1/UI/Sprites";
        private const string FontPath = UIFontApplier.FontAssetPath;   // 게임 UI 글꼴 (Apply UI Font가 만듦)

        private static readonly string[] ScenePaths =
        {
            "Assets/100. 3D Project-1/Scenes/10. Stage Tutorial.unity",
            "Assets/100. 3D Project-1/Scenes/11. Stage 1-1.unity",
            "Assets/100. 3D Project-1/Scenes/12. Stage 1-2.unity",
            "Assets/100. 3D Project-1/Scenes/21. Stage 2-1.unity",
            "Assets/100. 3D Project-1/Scenes/22. Stage 2-2.unity",
            "Assets/100. 3D Project-1/Scenes/31. Stage 3-1.unity",
            "Assets/100. 3D Project-1/Scenes/Template/Stage Template.unity",
        };

        private static readonly string[] OldHudNames = { "StageNameText", "TimerBackground", "TimerText", "ProgressText" };

        // A안 색
        // HUD 알약은 모두 불투명입니다(2026-11-02, 사용자 결정). 반투명이면 타이머 뒤로 세로 분할선이 비치고, 세 알약을 같은 모양으로 맞춤.
        private static readonly Color Panel = new Color32(0x12, 0x16, 0x28, 255);
        private static readonly Color Gold = new Color32(0xE8, 0xBE, 0x5A, 255);
        private static readonly Color GoldSoft = new Color32(0xE8, 0xBE, 0x5A, 150);
        private static readonly Color Cream = new Color32(0xF3, 0xEA, 0xD7, 255);
        private static readonly Color CreamSoft = new Color32(0xF3, 0xEA, 0xD7, 153);

        #region 프리팹 만들기

        [MenuItem("Tools/You're Right/Build Stage HUD")]
        private static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                EditorUtility.DisplayDialog(Title, "이미 Stage HUD 프리팹이 있습니다. 덮어쓸까요?\n(프리팹에서 직접 고친 값은 사라집니다)", "덮어쓰기", "취소") == false)
            {
                return;
            }

            Sprite round = LoadSprite("T_UI_RoundRect");
            Sprite outline = LoadSprite("T_UI_RoundRectOutline");
            Sprite dot = LoadSprite("T_UI_Dot");
            Sprite ring = LoadSprite("T_UI_DotRing");
            if (round == null || outline == null || dot == null || ring == null) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = new GameObject("Stage HUD", typeof(RectTransform));
            try
            {
                Stretch((RectTransform)root.transform, 0f);

                // 왼쪽 위: 스테이지 이름
                Image namePill = Pill(root, "Stage Name", new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(220f, 56f), round, Panel, 56f);
                AddOutline(namePill, outline, 56f);
                TMP_Text nameText = Label(namePill.gameObject, 26f, font, 20f);
                nameText.fontStyle = FontStyles.UpperCase;   // 화면에는 STAGE 1처럼 대문자로 (씬의 Stage Name과 결과 기록은 그대로)

                // 가운데 위: 타이머
                Image timerPill = Pill(root, "Timer", new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(260f, 64f), round, Panel, 64f);
                AddOutline(timerPill, outline, 64f);
                TMP_Text timerText = Label(timerPill.gameObject, 34f, font, 16f);

                // 오른쪽 위: 진행도 점 (점 개수에 맞춰 폭이 늘어남)
                Image progressPill = Pill(root, "Progress", new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(160f, 56f), round, Panel, 56f);
                AddOutline(progressPill, outline, 56f);
                var layout = progressPill.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(20, 20, 17, 17);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                var fitter = progressPill.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                Image template = Child(progressPill.gameObject, "Dot Template", ring, CreamSoft, 0f);
                var element = template.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.preferredHeight = 22f;
                template.preserveAspect = true;

                var hud = root.AddComponent<StageHUD>();
                var so = new SerializedObject(hud);
                so.FindProperty("stageNameText").objectReferenceValue = nameText;
                so.FindProperty("timerText").objectReferenceValue = timerText;
                so.FindProperty("progressDots").objectReferenceValue = progressPill.rectTransform;
                so.FindProperty("dotTemplate").objectReferenceValue = template;
                so.FindProperty("foundSprite").objectReferenceValue = dot;
                so.FindProperty("emptySprite").objectReferenceValue = ring;
                so.FindProperty("foundColor").colorValue = Gold;
                so.FindProperty("emptyColor").colorValue = CreamSoft;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 미리보기용 글자 (Play하면 StageHUD가 덮어씀)
                nameText.text = "Stage 1";
                timerText.text = "<mspace=0.6em>00:00.00</mspace>";

                foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[{Title}] 프리팹 생성: {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabPath));
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");
            if (sprite == null) EditorUtility.DisplayDialog(Title, $"그림을 찾지 못했습니다: {SpriteFolder}/{name}.png\n(Texture Type이 Sprite인지 확인해 주세요)", "OK");
            return sprite;
        }

        // 모서리 앵커·Pivot을 같은 자리로 맞춘 알약 하나 (UI 규칙: 모서리에 붙는 UI는 앵커와 Pivot을 둘 다 그 모서리로)
        private static Image Pill(GameObject parent, string name, Vector2 corner, Vector2 position, Vector2 size, Sprite sprite, Color color, float height)
        {
            Image image = Child(parent, name, sprite, color, height);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        // 알약과 같은 크기의 금색 테두리. 진행도 알약의 Horizontal Layout Group이 점처럼 늘어놓지 않도록 Ignore Layout을 켭니다.
        private static void AddOutline(Image pill, Sprite outline, float height)
        {
            Image line = Child(pill.gameObject, "Outline", outline, GoldSoft, height);
            Stretch(line.rectTransform, 0f);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        // height를 주면 모서리가 높이의 절반(완전한 알약)이 되도록 Pixels Per Unit Multiplier를 맞춥니다. (그림의 모서리 반지름 32)
        private static Image Child(GameObject parent, string name, Sprite sprite, Color color, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (height > 0f)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 32f / (height * 0.5f);
            }
            return image;
        }

        private static TMP_Text Label(GameObject parent, float size, TMP_FontAsset font, float sidePadding)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            Stretch((RectTransform)go.transform, sidePadding);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = Cream;
            text.alignment = TextAlignmentOptions.Center;
            if (font != null) text.font = font;
            return text;
        }

        private static void Stretch(RectTransform rect, float sidePadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(sidePadding, 0f);
            rect.offsetMax = new Vector2(-sidePadding, 0f);
        }

        #endregion

        #region 씬에 넣기

        [MenuItem("Tools/You're Right/Apply Stage HUD To Scenes")]
        private static void Apply()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(Title, "Stage HUD 프리팹이 없습니다. 먼저 Build Stage HUD를 실행해 주세요.", "OK");
                return;
            }
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
            if (EditorUtility.DisplayDialog(Title, $"스테이지 씬 {ScenePaths.Length}개의 예전 HUD를 지우고 Stage HUD 프리팹으로 바꾼 뒤 저장합니다.\n되돌리려면 git으로 씬을 되돌려야 합니다.", "바꾸기", "취소") == false)
            {
                return;
            }

            string startScene = SceneManager.GetActiveScene().path;
            var report = new List<string>();
            foreach (string path in ScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { report.Add($"없음: {path}"); continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                report.Add($"{scene.name}: {ApplyToScene(scene, prefab)}");
                EditorSceneManager.SaveScene(scene);
            }
            if (string.IsNullOrEmpty(startScene) == false) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            string message = string.Join("\n", report);
            Debug.Log($"[{Title}] 씬에 적용\n{message}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        private static string ApplyToScene(Scene scene, GameObject prefab)
        {
            Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c => c.isRootCanvas);
            if (canvas == null) return "Canvas 없음, 건너뜀";

            Transform parent = canvas.transform;
            foreach (Transform child in parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab) return "이미 있음, 건너뜀";
            }

            // 예전 HUD 글자들이 있던 자리(가장 앞 순서)에 새 HUD를 넣습니다. 없으면 분할선 바로 뒤.
            int index = -1;
            foreach (string name in OldHudNames)
            {
                Transform old = parent.Find(name);
                if (old == null) continue;
                index = index < 0 ? old.GetSiblingIndex() : Mathf.Min(index, old.GetSiblingIndex());
            }
            if (index < 0)
            {
                Transform divider = parent.Find("Divider");
                index = divider != null ? divider.GetSiblingIndex() + 1 : 0;
            }

            int removed = 0;
            foreach (string name in OldHudNames)
            {
                Transform old = parent.Find(name);
                if (old == null) continue;
                Object.DestroyImmediate(old.gameObject);
                removed++;
            }
            StageHUD oldHud = canvas.GetComponent<StageHUD>();
            if (oldHud != null) Object.DestroyImmediate(oldHud);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent, false);
            instance.transform.SetSiblingIndex(Mathf.Min(index, parent.childCount - 1));
            return $"예전 HUD {removed}개{(oldHud != null ? " + StageHUD" : "")} 지우고 넣음";
        }

        #endregion
    }
}

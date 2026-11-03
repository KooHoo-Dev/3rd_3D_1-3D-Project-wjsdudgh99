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
    // A안 UI(반투명 둥근 패널 + 금색 포인트)를 기존 UI에 입히는 에디터 도구입니다.
    // Tools → You're Right → Apply UI Theme
    //
    // 프리팹(Pause Menu, Stage Clear Panel, Tutorial Guide)은 제자리에서 열어 고치고,
    // 메뉴·결과 씬은 열어서 Canvas 아래를 고친 뒤 저장합니다. 오브젝트를 새로 만들지 않고 모양·색만 바꾸므로
    // 스크립트 연결과 씬에서 고친 문구(한국어)는 그대로입니다. 여러 번 실행해도 결과가 같습니다.
    //
    // 규칙 (이름이 아니라 생김새로 판단)
    //   - Button이 붙은 Image: 알약 버튼. 이름에 Continue·Next·Start가 들어가면 금색 주 버튼(짙은 글자), 그 밖은 남색 버튼 + 크림 테두리.
    //     Color Tint(평소 0.9 → 올리면 1)와 UIButtonHover(1.05배)를 붙임
    //   - 화면 전체를 덮는(앵커 0~1) Image: 어둡게 덮는 막. 검정, 알파는 그대로(없으면 0.5)
    //   - 그 밖의 Image: 창. 둥근 패널(모서리 16) 남색 불투명 + 금색 테두리 자식 "Theme Outline"
    //   - TMP 글자: 크림색, 금색 버튼 안은 짙은 갈색, 이름에 Timer·Total이 들어가면 금색(클리어 기록·결과 합계)
    //   - 건드리지 않음: Stage HUD·Click Lock Indicator 안, Divider, 이미 그림이 있는 아이콘(T_UI_* 제외)
    public static class UIThemeApplier
    {
        private const string Title = "Apply UI Theme";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs";
        private const string SpriteFolder = "Assets/100. 3D Project-1/UI/Sprites";
        private const string OutlineName = "Theme Outline";

        private static readonly string[] PrefabNames = { "Pause Menu", "Stage Clear Panel", "Tutorial Guide" };
        private static readonly string[] ScenePaths =
        {
            "Assets/100. 3D Project-1/Scenes/00. Menu.unity",
            "Assets/100. 3D Project-1/Scenes/90. Result.unity",
        };
        private static readonly string[] SkipRoots = { "Stage HUD", "Click Lock Indicator", "Divider" };
        private static readonly string[] PrimaryWords = { "Continue", "Next", "Start" };

        private static readonly Color Panel = new Color32(0x12, 0x16, 0x28, 255);       // 불투명 (HUD와 맞춤, 2026-11-02 사용자 결정)
        private static readonly Color ButtonNavy = new Color32(0x24, 0x2B, 0x48, 255);
        private static readonly Color Gold = new Color32(0xE8, 0xBE, 0x5A, 255);
        private static readonly Color GoldSoft = new Color32(0xE8, 0xBE, 0x5A, 128);
        private static readonly Color Cream = new Color32(0xF3, 0xEA, 0xD7, 255);
        private static readonly Color CreamSoft = new Color32(0xF3, 0xEA, 0xD7, 102);
        private static readonly Color OnGold = new Color32(0x3A, 0x2A, 0x08, 255);

        private static Sprite round;
        private static Sprite outline;

        [MenuItem("Tools/You're Right/Apply UI Theme")]
        private static void Apply()
        {
            if (EditorApplication.isPlaying) return;
            round = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/T_UI_RoundRect.png");
            outline = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/T_UI_RoundRectOutline.png");
            if (round == null || outline == null)
            {
                EditorUtility.DisplayDialog(Title, $"그림을 찾지 못했습니다: {SpriteFolder}", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
            if (EditorUtility.DisplayDialog(Title, $"프리팹 {PrefabNames.Length}개와 메뉴·결과 씬의 UI 모양을 바꾸고 저장합니다.\n되돌리려면 git으로 되돌려야 합니다.", "바꾸기", "취소") == false)
            {
                return;
            }

            var report = new List<string>();
            foreach (string name in PrefabNames)
            {
                string path = $"{PrefabFolder}/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { report.Add($"없음: {name}"); continue; }
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    report.Add($"{name}: {Theme(root.transform)}");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string startScene = SceneManager.GetActiveScene().path;
            foreach (string path in ScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { report.Add($"없음: {path}"); continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas);
                // 씬에 놓인 프리팹 인스턴스(일시정지 팝업 등)는 프리팹에서 이미 고쳤으므로 건너뜁니다.
                string result = string.Join(", ", canvases.Select(c => Theme(c.transform, skipPrefabInstances: true)));
                report.Add($"{scene.name}: {result}");
                EditorSceneManager.SaveScene(scene);
            }
            if (string.IsNullOrEmpty(startScene) == false) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            string message = string.Join("\n", report);
            Debug.Log($"[{Title}]\n{message}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        private static string Theme(Transform root, bool skipPrefabInstances = false)
        {
            int panels = 0, dims = 0, buttons = 0, texts = 0;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (ShouldSkip(image.transform, root, skipPrefabInstances)) continue;
                if (image.gameObject.name == OutlineName) continue;
                if (image.sprite != null && image.sprite != round && image.sprite.name.StartsWith("T_UI_") == false && image.sprite.name != "UISprite" && image.sprite.name != "Background") continue;   // 아이콘 그림

                Button button = image.GetComponent<Button>();
                if (button != null) { StyleButton(image, button); buttons++; }
                else if (IsFullScreen(image.rectTransform, root)) { StyleDim(image); dims++; }
                else { StylePanel(image); panels++; }
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ShouldSkip(text.transform, root, skipPrefabInstances)) continue;
                Button button = text.GetComponentInParent<Button>(true);
                Color color = Cream;
                if (button != null && IsPrimary(button)) color = OnGold;
                else if (text.gameObject.name.Contains("Timer") || text.gameObject.name.Contains("Total")) color = Gold;
                Undo.RecordObject(text, Title);
                text.color = color;
                // 느림보 고딕은 Ascent/Descent가 위로 치우쳐 Middle이면 글자가 살짝 위로 뜸 → 버튼 글자는 실제 글자 모양 기준 가운데(Midline)
                if (button != null) text.verticalAlignment = VerticalAlignmentOptions.Geometry;
                EditorUtility.SetDirty(text);
                texts++;
            }
            return $"창 {panels}, 덮는 막 {dims}, 버튼 {buttons}, 글자 {texts}";
        }

        private static bool ShouldSkip(Transform t, Transform root, bool skipPrefabInstances)
        {
            for (Transform p = t; p != null && p != root.parent; p = p.parent)
            {
                if (SkipRoots.Contains(p.name)) return true;
                if (skipPrefabInstances && p != root && PrefabUtility.IsOutermostPrefabInstanceRoot(p.gameObject)) return true;
            }
            return false;
        }

        // 자신부터 위로 Canvas(또는 프리팹 루트)까지 모두 앵커가 0~1이어야 화면 전체입니다.
        // 자기 부모만 꽉 채우는 것(예: 고정 크기 띠 안을 꽉 채운 그림)은 창으로 봅니다.
        private static bool IsFullScreen(RectTransform rect, Transform root)
        {
            for (Transform t = rect; t != null; t = t.parent)
            {
                if (t.GetComponent<Canvas>() != null && t.GetComponent<Canvas>().isRootCanvas) return true;
                var r = t as RectTransform;
                if (r == null || r.anchorMin != Vector2.zero || r.anchorMax != Vector2.one) return false;
                if (t == root) return true;
            }
            return false;
        }

        private static bool IsPrimary(Button button) => PrimaryWords.Any(w => button.name.Contains(w));

        private static void StylePanel(Image image)
        {
            SetImage(image, round, Panel, 2f);   // 모서리 16
            Image line = EnsureOutline(image);
            SetImage(line, outline, GoldSoft, 2f);
        }

        private static void StyleDim(Image image)
        {
            float alpha = image.color.a > 0f ? image.color.a : 0.5f;
            Undo.RecordObject(image, Title);
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(0f, 0f, 0f, alpha);
            EditorUtility.SetDirty(image);
        }

        private static void StyleButton(Image image, Button button)
        {
            bool primary = IsPrimary(button);
            float height = Mathf.Max(1f, image.rectTransform.rect.height);
            float pill = 32f / (height * 0.5f);   // 높이에 맞춰 완전한 반원 모서리
            SetImage(image, round, primary ? Gold : ButtonNavy, pill);

            Image line = EnsureOutline(image);
            SetImage(line, outline, primary ? new Color(0f, 0f, 0f, 0f) : CreamSoft, pill);

            Undo.RecordObject(button, Title);
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = colors.normalColor;   // 누른 뒤 밝은 채로 남지 않게
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.targetGraphic = image;
            EditorUtility.SetDirty(button);

            if (button.GetComponent<UIButtonHover>() == null) Undo.AddComponent<UIButtonHover>(button.gameObject);
        }

        private static void SetImage(Image image, Sprite sprite, Color color, float pixelsPerUnitMultiplier)
        {
            Undo.RecordObject(image, Title);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        // 창·버튼과 같은 크기의 테두리 자식을 맨 앞(바탕 바로 위, 내용 아래)에 둡니다. 이미 있으면 그대로 씁니다.
        private static Image EnsureOutline(Image owner)
        {
            Transform existing = owner.transform.Find(OutlineName);
            Image line;
            if (existing != null)
            {
                line = existing.GetComponent<Image>();
            }
            else
            {
                var go = new GameObject(OutlineName, typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, Title);
                go.layer = owner.gameObject.layer;
                go.transform.SetParent(owner.transform, false);
                line = go.GetComponent<Image>();
            }
            var rect = line.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(0);
            line.raycastTarget = false;
            return line;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace YoureRight
{
    // 게임 UI 글꼴을 한 번에 바꾸는 에디터 도구입니다.
    // Tools → You're Right → Apply UI Font
    //
    // 1) 글꼴 파일(SourceFontPath)로 TMP 글꼴 에셋을 만듭니다(이미 있으면 그대로 씀).
    //    Dynamic 방식이라 처음 쓰는 글자는 그때그때 아틀라스에 추가되고(아틀라스가 차면 새 장을 만듦),
    //    이 글꼴에 없는 글자는 Noto Sans KR로 대신 그립니다(Fallback).
    // 2) UI 프리팹과 모든 게임 씬의 TMP 글자 글꼴을 새 글꼴로 바꾸고 저장합니다.
    //    씬에 놓인 프리팹 인스턴스는 프리팹에서 이미 바뀌므로 따로 덮어쓰지 않습니다.
    // 3) 지금 쓰는 글자(영문·숫자·문장부호 + 씬·프리팹 글자)를 미리 아틀라스에 넣고,
    //    TMP Settings의 기본 글꼴도 바꿔 새로 만드는 TMP 글자가 이 글꼴로 시작하게 합니다.
    // 여러 번 실행해도 결과가 같습니다. 글꼴을 바꾸려면 SourceFontPath만 고칩니다.
    public static class UIFontApplier
    {
        private const string Title = "Apply UI Font";
        public const string SourceFontPath = "Assets/99. External Assets/Font/Y_spotlight_FONT/YSpotlight-Regular.otf";
        public const string FontAssetPath = "Assets/99. External Assets/Font/Y_spotlight_FONT/YSpotlight-Regular SDF.asset";
        private const string FallbackPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs";
        private const string SceneFolder = "Assets/100. 3D Project-1/Scenes";

        private static readonly string[] PrefabNames =
        {
            "Stage HUD", "Pause Menu", "Stage Clear Panel", "Tutorial Guide", "Wrong Marker", "Click Lock Indicator",
        };

        [MenuItem("Tools/You're Right/Apply UI Font")]
        private static void Apply()
        {
            if (EditorApplication.isPlaying) return;
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
            {
                EditorUtility.DisplayDialog(Title, $"글꼴 파일을 찾지 못했습니다.\n{SourceFontPath}", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
            if (EditorUtility.DisplayDialog(Title, "UI 프리팹과 게임 씬의 TMP 글꼴을 바꾸고 저장합니다.\n되돌리려면 git으로 되돌려야 합니다.", "바꾸기", "취소") == false)
            {
                return;
            }

            TMP_FontAsset font = GetOrCreateFontAsset(source);
            var used = new HashSet<char>();
            var report = new List<string>();

            foreach (string name in PrefabNames)
            {
                string path = $"{PrefabFolder}/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int count = SetFont(root.GetComponentsInChildren<TMP_Text>(true), font, used, skipPrefabInstances: false);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.Add($"{name}: {count}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string startScene = SceneManager.GetActiveScene().path;
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var texts = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true));
                int count = SetFont(texts, font, used, skipPrefabInstances: true);
                if (count > 0) EditorSceneManager.SaveScene(scene);
                report.Add($"{scene.name}: {count}");
            }
            if (string.IsNullOrEmpty(startScene) == false) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            // 영문·숫자·문장부호와 지금 쓰는 글자를 미리 그려 둡니다. (없는 글자는 Fallback이 그림)
            for (char c = (char)32; c < 127; c++) used.Add(c);
            font.TryAddCharacters(new string(used.ToArray()), out string missing);
            EditorUtility.SetDirty(font);

            SetDefaultFont(font);
            AssetDatabase.SaveAssets();

            string message = $"글꼴: {font.name}\n바꾼 TMP 글자 수\n{string.Join("\n", report)}" +
                             (string.IsNullOrEmpty(missing) ? "" : $"\n\n이 글꼴에 없어 Noto Sans KR로 그리는 글자: {missing}");
            Debug.Log($"[{Title}] {message.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        private static int SetFont(IEnumerable<TMP_Text> texts, TMP_FontAsset font, HashSet<char> used, bool skipPrefabInstances)
        {
            int count = 0;
            foreach (TMP_Text text in texts)
            {
                foreach (char c in text.text) used.Add(c);
                if (skipPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(text)) continue;
                if (text.font == font) continue;
                Undo.RecordObject(text, Title);
                text.font = font;
                text.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(text);
                count++;
            }
            return count;
        }

        // TMP의 "Create Font Asset" 메뉴와 같은 방식으로 아틀라스 텍스처와 머티리얼을 글꼴 에셋 안에 함께 저장합니다.
        private static TMP_FontAsset GetOrCreateFontAsset(Font source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = System.IO.Path.GetFileNameWithoutExtension(FontAssetPath);
            AssetDatabase.CreateAsset(font, FontAssetPath);

            Texture2D atlas = font.atlasTextures[0];
            atlas.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            if (fallback != null) font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null) return;
            var so = new SerializedObject(settings);
            SerializedProperty property = so.FindProperty("m_defaultFontAsset");
            if (property == null) return;
            property.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

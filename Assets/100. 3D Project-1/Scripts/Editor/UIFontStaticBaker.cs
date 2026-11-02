using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 게임 UI 글꼴(느림보 고딕)을 Static 방식으로 굽는 에디터 도구입니다.
    // Tools → You're Right → Bake UI Font (Static)
    //
    // Dynamic 글꼴은 빌드에 원본 글꼴 파일(.otf)이 함께 들어가 게임 안에서 글자를 그립니다.
    // Static으로 바꾸면 미리 그린 글자 그림(아틀라스)만 들어가고 원본 파일은 빌드에서 빠집니다.
    //   1) 영문·숫자·문장부호 + 게임 씬·프리팹에 있는 모든 TMP 글자를 모읍니다.
    //   2) 아틀라스를 비우고 그 글자만 다시 그린 뒤 Static으로 바꿉니다.
    //   3) 이 글꼴에 없는 글자는 Fallback(Noto Sans KR)이 그리고, 거기에도 없으면 창에 알려 줍니다.
    // Static에서는 새 글자가 자동으로 추가되지 않으므로, 씬·프리팹에 새 한국어 문구를 넣으면 다시 실행합니다.
    // 여러 번 실행해도 결과가 같습니다. 다시 Dynamic으로 쓰려면 글꼴 에셋 Inspector에서 Atlas Population Mode를 바꿉니다.
    public static class UIFontStaticBaker
    {
        private const string Title = "Bake UI Font (Static)";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs";
        private const string SceneFolder = "Assets/100. 3D Project-1/Scenes";

        [MenuItem("Tools/You're Right/Bake UI Font (Static)")]
        private static void Bake()
        {
            if (EditorApplication.isPlaying) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontApplier.FontAssetPath);
            var source = AssetDatabase.LoadAssetAtPath<Font>(UIFontApplier.SourceFontPath);
            if (font == null || source == null)
            {
                EditorUtility.DisplayDialog(Title, $"글꼴 에셋이나 원본 글꼴 파일을 찾지 못했습니다.\n{UIFontApplier.FontAssetPath}", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            var used = new HashSet<char>();
            for (char c = (char)32; c < 127; c++) used.Add(c);
            CollectFromPrefabs(used);
            CollectFromScenes(used);
            used.RemoveWhere(char.IsControl);
            string characters = new string(used.OrderBy(c => c).ToArray());

            // 원본 글꼴에서 다시 그려야 하므로 잠깐 Dynamic으로 돌린 뒤 비우고 채웁니다.
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.ClearFontAssetData(true);
            font.TryAddCharacters(characters, out string missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            string notInFallback = FindNotInFallbacks(font, missing);
            string message = $"{font.name}: 글자 {characters.Length - (missing?.Length ?? 0)}개를 Static으로 구웠습니다." +
                             $" (아틀라스 {font.atlasTextures.Length}장)" +
                             (string.IsNullOrEmpty(missing) ? "" : $"\n\n이 글꼴에 없어 Fallback이 그리는 글자: {missing}") +
                             (string.IsNullOrEmpty(notInFallback) ? "" : $"\n\n어느 글꼴에도 없어 네모로 보이는 글자: {notInFallback}");
            Debug.Log($"[{Title}] {message.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        private static void CollectFromPrefabs(HashSet<char> used)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true)) Add(used, text.text);
            }
        }

        // 씬마다 덮어쓴 글자(스테이지 이름, 튜토리얼 안내 등)까지 읽으려고 씬을 하나씩 엽니다.
        private static void CollectFromScenes(HashSet<char> used)
        {
            string startScene = SceneManager.GetActiveScene().path;
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder }))
            {
                Scene scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) Add(used, text.text);
                    // 코드가 Play 중에 TMP에 넣는 글자(스테이지 이름, 마지막 스테이지 버튼 글자)도 모읍니다.
                    foreach (StageClearPanel panel in root.GetComponentsInChildren<StageClearPanel>(true))
                    {
                        Add(used, new SerializedObject(panel).FindProperty("lastStageNextText")?.stringValue);
                    }
                    foreach (StageManager manager in root.GetComponentsInChildren<StageManager>(true))
                    {
                        Add(used, new SerializedObject(manager).FindProperty("stageName")?.stringValue);
                    }
                }
            }
            if (string.IsNullOrEmpty(startScene) == false) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
        }

        private static void Add(HashSet<char> used, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (char c in text) used.Add(c);
        }

        private static string FindNotInFallbacks(TMP_FontAsset font, string missing)
        {
            if (string.IsNullOrEmpty(missing)) return "";
            var result = new List<char>();
            foreach (char c in missing)
            {
                bool found = font.fallbackFontAssetTable != null &&
                             font.fallbackFontAssetTable.Any(f => f != null && f.HasCharacter(c, searchFallbacks: true, tryAddCharacter: false));
                if (found == false) result.Add(c);
            }
            return new string(result.ToArray());
        }
    }
}

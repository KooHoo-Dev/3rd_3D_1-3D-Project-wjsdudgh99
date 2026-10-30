using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // Floreswa 세단 FBX 안에 들어 있는(읽기 전용) 부위별 머티리얼을 .mat 파일로 꺼내는 에디터 도구입니다.
    // Tools → You're Right → Extract Floreswa Materials
    //
    // Unity의 Materials 탭 "Extract Materials"를 그냥 쓰면 FBX 안의 머티리얼이 사라지는데, Floreswa 프리팹 8개는 그 머티리얼을
    // FBX 안의 ID로 직접 가리키고 있어서 분홍색(Missing)이 됩니다. 그래서 이 도구는
    //   1) 부위 이름마다 .mat 복사본을 Parts 폴더에 만들고(세단과 낮은 세단이 같은 부위는 같은 파일을 함께 씀),
    //   2) 프리팹·씬에서 FBX 안 머티리얼을 가리키는 참조를 새 .mat으로 바꾸고,
    //   3) 두 FBX가 새 .mat을 쓰도록 연결(Remap)합니다.
    // 정답 반짝임용으로 Parts와 Floreswa/Materials의 색 머티리얼 모두 Emission을 켜고 검정으로 둡니다.
    // 다시 실행해도 이미 만든 .mat은 그대로 쓰고, 바꿀 참조가 없으면 아무것도 바꾸지 않습니다.
    public static class FloreswaMaterialExtractor
    {
        private const string MenuPath = "Tools/You're Right/Extract Floreswa Materials";
        private const string Title = "Extract Floreswa Materials";
        private const string ModelFolder = "Assets/Floreswa/Models";
        private const string ColorFolder = "Assets/Floreswa/Materials";
        private const string PartsFolder = "Assets/Floreswa/Materials/Parts";
        private const string SlotReportPrefab = "Assets/Floreswa/Prefabs/free_sedan_red.prefab";

        [MenuItem(MenuPath)]
        private static void Extract()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 실행할 수 없습니다.", "OK");
                return;
            }
            // 열린 씬 파일을 바깥에서 고치므로, 저장하지 않은 변경이 있으면 멈춥니다.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    EditorUtility.DisplayDialog(Title, "저장하지 않은 씬이 있습니다. 먼저 저장해 주세요.", "OK");
                    return;
                }
            }

            string[] models = Directory.GetFiles(ModelFolder, "*.fbx").Select(p => p.Replace('\\', '/')).ToArray();
            if (models.Length == 0)
            {
                EditorUtility.DisplayDialog(Title, $"FBX가 없습니다.\n{ModelFolder}", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(PartsFolder)) AssetDatabase.CreateFolder(ColorFolder, "Parts");

            // FBX 안 머티리얼 참조 문자열 → 새 .mat 참조 문자열
            var replacements = new Dictionary<string, string>();
            var parts = new Dictionary<string, Material>();
            var remaps = new List<(ModelImporter importer, string name, Material mat)>();

            foreach (string modelPath in models)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
                foreach (Material src in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
                {
                    Material part = GetOrCreatePart(src, parts);
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(src, out string srcGuid, out long srcId) &&
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(part, out string partGuid, out long partId))
                    {
                        replacements[$"{{fileID: {srcId}, guid: {srcGuid}, type: 3}}"] = $"{{fileID: {partId}, guid: {partGuid}, type: 2}}";
                    }
                    remaps.Add((importer, src.name, part));
                }
            }

            string slotReport = DescribeSlots();
            int changedFiles = ReplaceReferences(replacements);

            foreach (var group in remaps.GroupBy(r => r.importer))
            {
                foreach (var (importer, name, mat) in group)
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                }
                group.Key.SaveAndReimport();
            }

            int colors = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ColorFolder }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat != null && EnableBlackEmission(mat)) colors++;
            }
            AssetDatabase.SaveAssets();

            ReopenScenes();

            string message = $"부위 머티리얼 {parts.Count}개 ({PartsFolder})\n참조를 바꾼 파일 {changedFiles}개\nEmission을 새로 켠 머티리얼 {colors}개\n\n{slotReport}";
            Debug.Log($"[{Title}] {message.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }

        // 부위 이름마다 .mat 하나. 이미 있으면 그대로 쓰고, 같은 이름인데 색이 다르면 경고만 합니다.
        private static Material GetOrCreatePart(Material src, Dictionary<string, Material> parts)
        {
            if (parts.TryGetValue(src.name, out Material existing))
            {
                if (existing.HasProperty("_BaseColor") && src.HasProperty("_BaseColor") && existing.GetColor("_BaseColor") != src.GetColor("_BaseColor"))
                {
                    Debug.LogWarning($"[{Title}] '{src.name}' 색이 FBX마다 다릅니다. 처음 것({AssetDatabase.GetAssetPath(existing)})을 함께 씁니다.");
                }
                return existing;
            }

            string path = $"{PartsFolder}/{src.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(src) { name = src.name };
                EnableBlackEmission(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            parts[src.name] = mat;
            return mat;
        }

        // Emission을 켜고 검정으로 둡니다. 이미 켜져 있으면 색은 건드리지 않습니다.
        // EmissiveIsBlack으로 두면 URP가 저장할 때 _EMISSION을 꺼 버리므로 RealtimeEmissive로 둡니다.
        private static bool EnableBlackEmission(Material mat)
        {
            if (!mat.HasProperty("_EmissionColor") || mat.IsKeywordEnabled("_EMISSION")) return false;
            mat.SetColor("_EmissionColor", Color.black);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return true;
        }

        // 바꾸기 전에 세단 Body·바퀴의 머티리얼 칸 순서를 적어 둡니다(차이 만들 때 몇 번 칸이 어느 부위인지 보려고).
        private static string DescribeSlots()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotReportPrefab);
            if (prefab == null) return "";
            var sb = new StringBuilder("칸 순서 (free_sedan_red)\n");
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name != "Body" && renderer.name != "FL") continue;
                string label = renderer.name == "FL" ? "바퀴" : "Body";
                sb.Append(label).Append(": ");
                sb.Append(string.Join(", ", renderer.sharedMaterials.Select((m, i) => $"{i} {(m != null ? m.name : "None")}")));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static int ReplaceReferences(Dictionary<string, string> replacements)
        {
            if (replacements.Count == 0) return 0;
            int changed = 0;
            var files = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories));
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                string updated = text;
                foreach (var pair in replacements) updated = updated.Replace(pair.Key, pair.Value);
                if (updated == text) continue;
                File.WriteAllText(file, updated);
                AssetDatabase.ImportAsset(file.Replace('\\', '/'));
                changed++;
                Debug.Log($"[{Title}] 참조 변경: {file}");
            }
            return changed;
        }

        // 파일을 바깥에서 고쳤으므로 열린 씬을 다시 엽니다.
        private static void ReopenScenes()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string path = SceneManager.GetSceneAt(i).path;
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
            for (int i = 0; i < paths.Count; i++)
            {
                EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
            }
        }
    }
}

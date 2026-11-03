using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YoureRight
{
    // Windows용 빌드를 만들고 결과를 정리해 보여 주는 에디터 도구입니다. (13단계 빌드 테스트)
    // Tools → You're Right → Build Windows
    //
    // Build Settings에 켜 둔 씬을 순서대로 넣어 Builds/Windows/(productName).exe로 빌드합니다. (Builds 폴더는 git에서 빠짐)
    // 끝나면 성공 여부·시간·크기·오류/경고 수, 용량이 큰 에셋 10개, 그리고 빌드에 들어간 글꼴 원본(.otf/.ttf)을 확인해
    // 창과 Builds/Windows/build_report.txt에 남깁니다. 성공하면 UI 글꼴(Pretendard, SIL OFL 1.1) 라이선스 문서와 크레딧(Sounds/Credits.txt)도 빌드 폴더에 복사합니다.
    public static class GameBuilder
    {
        private const string Title = "Build Windows";
        private const string OutputFolder = "Builds/Windows";
        private const string CreditsPath = "Assets/100. 3D Project-1/Sounds/Credits.txt";   // 음악·글꼴 크레딧 전체 문구 (곡을 바꾸면 같이 고침)

        [MenuItem("Tools/You're Right/Build Windows")]
        private static void Build()
        {
            if (EditorApplication.isPlaying) return;
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(Title, "Build Settings에 켜진 씬이 없습니다.", "OK");
                return;
            }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            string exe = $"{OutputFolder}/{PlayerSettings.productName}.exe";
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            string text = Describe(report, scenes);

            Directory.CreateDirectory(OutputFolder);
            // UI 글꼴(SIL OFL 1.1)은 배포할 때 라이선스 문서를, BGM(CC BY 3.0)은 크레딧을 같이 넣어야 합니다.
            if (report.summary.result == BuildResult.Succeeded)
            {
                text += CopyNextToBuild(UIFontApplier.LicensePath, "Font License (Pretendard, OFL).txt");
                text += CopyNextToBuild(CreditsPath, "Credits.txt");
            }
            File.WriteAllText($"{OutputFolder}/build_report.txt", text, Encoding.UTF8);
            Debug.Log($"[{Title}]\n{text}");
            EditorUtility.DisplayDialog(Title, text.Length > 1500 ? text.Substring(0, 1500) + "\n…" : text, "OK");
            if (report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(exe);
        }

        private static string CopyNextToBuild(string source, string fileName)
        {
            if (File.Exists(source) == false) return $"\n⚠ 빌드 폴더에 넣을 파일이 없음: {source}\n";
            File.Copy(source, $"{OutputFolder}/{fileName}", true);
            return $"\n빌드 폴더에 복사함: {fileName}\n";
        }

        private static string Describe(BuildReport report, string[] scenes)
        {
            BuildSummary summary = report.summary;
            var sb = new StringBuilder();
            sb.AppendLine($"결과: {summary.result}");
            sb.AppendLine($"시간: {summary.totalTime.TotalSeconds:0}초, 크기: {summary.totalSize / (1024f * 1024f):0.0} MB");
            sb.AppendLine($"오류 {summary.totalErrors}개, 경고 {summary.totalWarnings}개");
            sb.AppendLine($"씬 {scenes.Length}개: {string.Join(", ", scenes.Select(Path.GetFileNameWithoutExtension))}");

            var assets = report.packedAssets.SelectMany(p => p.contents).ToList();
            var fonts = assets.Where(a => a.sourceAssetPath.EndsWith(".otf") || a.sourceAssetPath.EndsWith(".ttf"))
                              .Select(a => a.sourceAssetPath).Distinct().ToList();
            sb.AppendLine();
            sb.AppendLine(fonts.Count == 0 ? "글꼴 원본(.otf/.ttf): 빌드에 없음" : "글꼴 원본(.otf/.ttf)이 빌드에 들어감:");
            foreach (string font in fonts) sb.AppendLine($"  - {font}");

            sb.AppendLine();
            sb.AppendLine("용량이 큰 에셋 10개:");
            foreach (var group in assets.GroupBy(a => a.sourceAssetPath).Select(g => (path: g.Key, size: g.Sum(a => (long)a.packedSize)))
                                        .OrderByDescending(g => g.size).Take(10))
            {
                sb.AppendLine($"  {group.size / (1024f * 1024f),6:0.0} MB  {group.path}");
            }

            if (summary.totalErrors > 0)
            {
                sb.AppendLine();
                sb.AppendLine("오류:");
                foreach (var message in report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Take(10))
                {
                    sb.AppendLine($"  - {message.content.Split('\n')[0]}");
                }
            }
            return sb.ToString();
        }
    }
}

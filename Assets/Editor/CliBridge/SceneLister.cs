using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CliBridge
{
    // UnityCli의 scenes 명령이 배치 모드로 호출하는 Editor 전용 스크립트.
    // 씬 목록을 읽어서 JSON 파일로 저장할 뿐, 씬이나 에셋을 수정하지 않는다.
    public static class SceneLister
    {
        [Serializable]
        private class Result
        {
            public string[] scenes;
        }

        // Unity.exe ... -executeMethod CliBridge.SceneLister.Run -cliOutput <파일 경로>
        public static void Run()
        {
            try
            {
                string outputPath = GetArg("-cliOutput");
                if (string.IsNullOrEmpty(outputPath))
                {
                    Debug.LogError("[CliBridge] -cliOutput 인자가 없습니다.");
                    EditorApplication.Exit(2);
                    return;
                }

                string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(p => p)
                    .ToArray();

                var result = new Result { scenes = scenePaths };
                File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
                Debug.Log($"[CliBridge] 씬 {scenePaths.Length}개를 {outputPath}에 저장했습니다.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CliBridge] 오류: {e}");
                EditorApplication.Exit(1);
            }
        }

        // 명령줄 인자에서 "-이름 값" 형태의 값을 찾는다.
        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}

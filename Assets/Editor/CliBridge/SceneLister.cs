using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CliBridge
{
    // 씬 목록을 찾는 Editor 전용 스크립트. 배치 모드(Run)와 HTTP 서버(FindScenes)가 함께 쓴다.
    // 씬 목록을 읽기만 할 뿐, 씬이나 에셋을 수정하지 않는다.
    public static class SceneLister
    {
        [Serializable]
        public class Result
        {
            public string[] scenes;
        }

        // Unity.exe ... -executeMethod CliBridge.SceneLister.Run -cliOutput <파일 경로>
        public static void Run()
        {
            CliRunner.Execute(() =>
            {
                Result result = FindScenes();
                Debug.Log($"[CliBridge] 씬 {result.scenes.Length}개를 찾았습니다.");
                return result;
            });
        }

        // Assets 폴더 아래의 모든 씬 경로를 정렬해서 반환한다 (읽기 전용)
        public static Result FindScenes()
        {
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .ToArray();

            return new Result { scenes = scenePaths };
        }
    }
}

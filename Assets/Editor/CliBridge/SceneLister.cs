using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CliBridge
{
    // UnityCli의 scenes 명령이 배치 모드로 호출하는 Editor 전용 스크립트.
    // 씬 목록을 읽어서 결과로 돌려줄 뿐, 씬이나 에셋을 수정하지 않는다.
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
            CliRunner.Execute(() =>
            {
                string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(p => p)
                    .ToArray();

                Debug.Log($"[CliBridge] 씬 {scenePaths.Length}개를 찾았습니다.");
                return new Result { scenes = scenePaths };
            });
        }
    }
}

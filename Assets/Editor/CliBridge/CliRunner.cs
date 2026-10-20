using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CliBridge
{
    // UnityCli가 배치 모드로 호출하는 Editor 메서드들이 함께 쓰는 실행 도우미.
    // -cliOutput 확인 → 작업 실행 → 결과를 JSON으로 저장 → Unity 종료 순서를 한 곳에서 처리한다.
    public static class CliRunner
    {
        // work: 결과 객체를 반환하는 작업. 결과 클래스에는 [Serializable]과 public 필드가 있어야 한다.
        // 종료 코드: 0 = 성공, 1 = 작업 중 오류, 2 = -cliOutput 인자 없음
        public static void Execute(Func<object> work)
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

                object result = work();
                File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
                Debug.Log($"[CliBridge] 결과를 {outputPath}에 저장했습니다.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CliBridge] 오류: {e}");
                EditorApplication.Exit(1);
            }
        }

        // 명령줄 인자에서 "-이름 값" 형태의 값을 찾는다. 없으면 null.
        public static string GetArg(string name)
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

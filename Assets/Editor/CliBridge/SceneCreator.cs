using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CliBridge
{
    // 새 씬을 만드는 Editor 전용 스크립트. 배치 모드(Run)와 HTTP 서버(Create)가 함께 쓴다.
    // 안전장치: Assets/CliScenes 폴더에만 만들고, 이름을 검사하며, 같은 이름의 파일이 있으면 만들지 않는다.
    // 열려 있는 씬은 바꾸지 않고, Build Settings에도 추가하지 않는다.
    public static class SceneCreator
    {
        public const string Folder = "Assets/CliScenes";

        // 글자(한글 포함)·숫자·공백·_·- 만 허용, 1~64자, 앞뒤 공백 불가 (UnityCli와 같은 규칙)
        private static readonly Regex NamePattern =
            new Regex(@"^[\p{L}\p{N}_\-]([\p{L}\p{N}_\- ]{0,62}[\p{L}\p{N}_\-])?$");

        [Serializable]
        public class Result
        {
            public string path;
        }

        // 만들 수 없는 상황을 알리는 예외. Code는 HTTP 상태 코드로도 쓴다.
        public class CreateException : Exception
        {
            public int Code { get; }

            public CreateException(int code, string message) : base(message)
            {
                Code = code;
            }
        }

        // Unity.exe ... -executeMethod CliBridge.SceneCreator.Run -cliSceneName <이름> -cliOutput <파일 경로>
        public static void Run()
        {
            CliRunner.Execute(() =>
            {
                Result result = Create(CliRunner.GetArg("-cliSceneName"));
                Debug.Log($"[CliBridge] 새 씬을 만들었습니다: {result.path}");
                return result;
            });
        }

        public static Result Create(string name)
        {
            // 1. 이름 검사 (경로 문자, 마침표 등은 여기서 모두 걸러진다)
            if (string.IsNullOrEmpty(name) || !NamePattern.IsMatch(name))
            {
                throw new CreateException(400, $"씬 이름이 올바르지 않습니다: '{name}' (글자·숫자·공백·_·- 만, 1~64자)");
            }

            // 2. Editor 상태 검사
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new CreateException(503, "Play 모드에서는 씬을 만들 수 없습니다. Play를 멈추고 다시 시도하세요.");
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                throw new CreateException(503, "Unity가 컴파일 또는 에셋 갱신 중입니다. 잠시 후 다시 시도하세요.");
            }

            // 3. 덮어쓰기 금지 (Unity의 현재 폴더는 프로젝트 루트이므로 상대 경로로 확인할 수 있다)
            string path = $"{Folder}/{name}.unity";
            if (File.Exists(path))
            {
                throw new CreateException(409, $"이미 같은 이름의 씬이 있습니다: {path} (덮어쓰지 않습니다)");
            }

            // 4. Editor에 저장 안 된 새 씬(Untitled)이 있으면 Additive 생성이 실패할 수 있으므로 미리 거부
            if (!Application.isBatchMode)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    {
                        throw new CreateException(409, "저장되지 않은 새 씬(Untitled)이 열려 있어 만들 수 없습니다. 그 씬을 먼저 저장하거나 닫으세요.");
                    }
                }
            }

            // 5. 폴더 준비 (없을 때만 Assets/CliScenes 하나를 만든다)
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "CliScenes");
            }

            // 6. 씬 생성
            if (Application.isBatchMode)
            {
                // 배치 모드에는 사용자가 작업 중인 씬이 없으므로 단독(Single)으로 만든다
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                Save(scene, path);
            }
            else
            {
                // Editor에서는 열린 씬을 건드리지 않도록 추가(Additive)로 만들고, 저장 후 바로 닫는다
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                try
                {
                    Save(scene, path);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return new Result { path = path };
        }

        private static void Save(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new CreateException(500, $"씬을 저장하지 못했습니다: {path}");
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CliBridge
{
    // Editor가 열려 있는 동안 127.0.0.1:17890에서 UnityCli의 요청을 받는 작은 HTTP 서버.
    // 읽기(GET /ping, GET /scenes)와 씬 생성(POST /scenes/new, X-UnityCli 헤더 필수)을 제공한다.
    // Unity API는 메인 스레드에서만 쓸 수 있으므로, 요청은 큐에 넣고 EditorApplication.update에서 처리한다.
    [InitializeOnLoad]
    public static class CliHttpServer
    {
        public const int Port = 17890;

        private static HttpListener listener;
        private static readonly ConcurrentQueue<HttpListenerContext> pending = new ConcurrentQueue<HttpListenerContext>();

        [Serializable]
        private class PingResult
        {
            public string projectPath;
            public string unityVersion;
        }

        [Serializable]
        private class ErrorResult
        {
            public string error;
        }

        [Serializable]
        private class NewSceneRequest
        {
            public string name;
        }

        // Editor가 켜지거나 스크립트가 다시 로드될 때마다 자동으로 실행된다.
        static CliHttpServer()
        {
            // 배치 모드(scenes 명령의 배치 실행)에서는 서버를 켜지 않는다
            if (Application.isBatchMode) return;

            Start();
            EditorApplication.update += ProcessPending;
            AssemblyReloadEvents.beforeAssemblyReload += Stop; // 스크립트 리로드 전에 포트를 반납
            EditorApplication.quitting += Stop;
        }

        private static void Start()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                listener.Start();
                listener.BeginGetContext(OnRequest, listener);
                Debug.Log($"[CliBridge] HTTP 서버 시작: http://127.0.0.1:{Port}/");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CliBridge] HTTP 서버를 시작하지 못했습니다 (포트 {Port}가 사용 중일 수 있음): {e.Message}");
                listener = null;
            }
        }

        private static void Stop()
        {
            if (listener == null) return;
            try
            {
                listener.Stop();
                listener.Close();
            }
            catch (Exception)
            {
                // 이미 멈춘 경우 무시
            }
            listener = null;
        }

        // 백그라운드 스레드: 요청을 받아 큐에 넣기만 한다
        private static void OnRequest(IAsyncResult ar)
        {
            var current = (HttpListener)ar.AsyncState;
            try
            {
                pending.Enqueue(current.EndGetContext(ar));
            }
            catch (Exception)
            {
                // 서버를 멈추는 중이면 예외가 난다 — 무시
            }

            if (current.IsListening)
            {
                try { current.BeginGetContext(OnRequest, current); }
                catch (Exception) { }
            }
        }

        // 메인 스레드: 큐에 쌓인 요청을 처리한다
        private static void ProcessPending()
        {
            while (pending.TryDequeue(out HttpListenerContext context))
            {
                Handle(context);
            }
        }

        private static void Handle(HttpListenerContext context)
        {
            int status = 200;
            string body;
            try
            {
                string path = context.Request.Url.AbsolutePath;
                string method = context.Request.HttpMethod;
                if (method == "GET" && path == "/ping")
                {
                    var ping = new PingResult
                    {
                        projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                        unityVersion = Application.unityVersion,
                    };
                    body = JsonUtility.ToJson(ping);
                }
                else if (method == "GET" && path == "/scenes")
                {
                    body = JsonUtility.ToJson(SceneLister.FindScenes());
                }
                else if (method == "POST" && path == "/scenes/new")
                {
                    body = NewScene(context.Request, out status);
                }
                else
                {
                    status = 404;
                    body = JsonUtility.ToJson(new ErrorResult { error = $"지원하지 않는 요청입니다: {method} {path}" });
                }
            }
            catch (Exception e)
            {
                status = 500;
                body = JsonUtility.ToJson(new ErrorResult { error = e.Message });
                Debug.LogError($"[CliBridge] 요청 처리 오류: {e}");
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception)
            {
                // 클라이언트가 먼저 연결을 끊은 경우 무시
            }
        }

        // POST /scenes/new  본문: {"name":"씬이름"}  헤더: X-UnityCli: 1
        private static string NewScene(HttpListenerRequest request, out int status)
        {
            // 웹페이지는 이 헤더를 붙여 보낼 수 없으므로, 브라우저를 통한 몰래 요청을 막는다
            if (request.Headers["X-UnityCli"] != "1")
            {
                status = 403;
                return JsonUtility.ToJson(new ErrorResult { error = "X-UnityCli 헤더가 필요합니다." });
            }

            NewSceneRequest req;
            try
            {
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    req = JsonUtility.FromJson<NewSceneRequest>(reader.ReadToEnd());
                }
            }
            catch (Exception)
            {
                status = 400;
                return JsonUtility.ToJson(new ErrorResult { error = "요청 본문이 올바른 JSON이 아닙니다." });
            }

            try
            {
                SceneCreator.Result result = SceneCreator.Create(req?.name);
                Debug.Log($"[CliBridge] 새 씬을 만들었습니다: {result.path}");
                status = 200;
                return JsonUtility.ToJson(result);
            }
            catch (SceneCreator.CreateException e)
            {
                status = e.Code;
                return JsonUtility.ToJson(new ErrorResult { error = e.Message });
            }
        }
    }
}

namespace YoureRight
{
    // 스테이지 클리어 시간을 재는 스톱워치 입니다.
    // MonoBehaviour가 아니므로 StageManager가 Update에서 Tick을 호출해 줘야 합니다.
    // Time.deltaTime을 넘겨받기 때문에 일시정지(timeScale = 0) 중에는 자동으로 멈춥니다.
    public class StageTimer
    {
        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; }

        public void Start()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (IsRunning == false) return;
            Elapsed += deltaTime;
        }
    }
}

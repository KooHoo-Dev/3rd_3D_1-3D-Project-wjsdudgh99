using System;
using System.Collections;
using UnityEngine;

namespace YoureRight
{
    // 한 스테이지의 진행 상황을 관리합니다.
    // 씬에 있는 AnswerPair를 모두 모아 찾은 개수를 세고, 전부 찾으면 클리어를 알립니다.
    // 스테이지 시작과 동시에 스톱워치를 돌리고, 클리어 순간에 멈춥니다.
    // 클리어 후 잠시 기다렸다가 GameManager에 기록을 넘기면 다음 씬으로 이동합니다.
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private string stageName = "Tutorial";   // HUD에 그대로 표시됩니다 (예: Stage 2 - Snowy Night)
        [SerializeField] private float nextStageDelay = 1.5f;     // 클리어 후 다음 씬으로 넘어가기 전 대기 시간 (연출을 보여줄 시간)
        [SerializeField] private bool addToRecords = true;        // 끄면 시간은 HUD에 보여주기만 하고 결과 기록에는 넣지 않습니다 (튜토리얼)

        private AnswerPair[] pairs;
        private int foundCount;
        private readonly StageTimer timer = new StageTimer();

        public string StageName => stageName;
        public int FoundCount => foundCount;
        public int TotalCount => pairs.Length;
        public bool IsCleared { get; private set; }
        public float ElapsedTime => timer.Elapsed;

        // (찾은 개수, 전체 개수) - HUD 진행도 표시용
        public event Action<int, int> OnProgressChanged;
        public event Action OnStageCleared;

        private void Awake()
        {
            // 씬에 배치된 정답 쌍을 자동으로 모읍니다. 하나하나 연결할 필요가 없습니다.
            pairs = FindObjectsByType<AnswerPair>(FindObjectsSortMode.None);
            if (pairs.Length == 0)
            {
                Debug.LogWarning("[StageManager] 씬에 AnswerPair가 하나도 없습니다.", this);
            }
        }

        private void OnEnable()
        {
            foreach (AnswerPair pair in pairs) pair.OnFound += HandlePairFound;
        }

        private void OnDisable()
        {
            foreach (AnswerPair pair in pairs) pair.OnFound -= HandlePairFound;
        }

        private void Start()
        {
            // 구독자들이 Awake/OnEnable에서 이벤트를 연결한 뒤 초기 진행도(0 / N)를 알려줍니다.
            OnProgressChanged?.Invoke(foundCount, TotalCount);
            timer.Start();
        }

        private void Update()
        {
            timer.Tick(Time.deltaTime);
        }

        private void HandlePairFound(AnswerPair pair)
        {
            if (IsCleared) return;

            foundCount++;
            Debug.Log($"[StageManager] 정답! {pair.name} ({foundCount} / {TotalCount})");
            OnProgressChanged?.Invoke(foundCount, TotalCount);

            if (foundCount >= TotalCount)
            {
                IsCleared = true;
                timer.Stop();
                Debug.Log($"[StageManager] {stageName} 클리어! 기록: {TimeFormatter.Format(timer.Elapsed)}");
                OnStageCleared?.Invoke();
                StartCoroutine(ReportClearRoutine());
            }
        }

        private IEnumerator ReportClearRoutine()
        {
            // 기록은 클리어 순간에 이미 멈췄으므로, 대기 시간은 기록에 포함되지 않습니다.
            yield return new WaitForSeconds(nextStageDelay);
            GameManager.Instance.ReportStageClear(stageName, timer.Elapsed, addToRecords);
        }
    }
}

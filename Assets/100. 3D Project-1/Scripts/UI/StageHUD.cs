using TMPro;
using UnityEngine;

namespace YoureRight
{
    // 플레이 화면 상단 HUD 입니다.
    // 좌측: 스테이지 이름 / 중앙: 스톱워치 (00:00.00) / 우측: 정답 진행도 ( 2 / 5 )
    // StageManager의 값을 읽어 표시만 하고, 게임 진행에는 관여하지 않습니다.
    public class StageHUD : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text progressText;

        [Header("Timer")]
        // 숫자마다 글자 폭이 달라 시간이 흐를 때 글자가 좌우로 흔들리는 것을 막습니다. (0이면 사용 안 함)
        [SerializeField] private float timerMonospaceEm = 0.6f;

        private void Awake()
        {
            if (stageManager == null) stageManager = FindAnyObjectByType<StageManager>();
        }

        private void OnEnable()
        {
            stageManager.OnProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            stageManager.OnProgressChanged -= HandleProgressChanged;
        }

        private void Start()
        {
            stageNameText.text = stageManager.StageName;
        }

        private void Update()
        {
            string time = TimeFormatter.Format(stageManager.ElapsedTime);
            timerText.text = timerMonospaceEm > 0f ? $"<mspace={timerMonospaceEm}em>{time}</mspace>" : time;
        }

        private void HandleProgressChanged(int found, int total)
        {
            progressText.text = $"( {found} / {total} )";
        }
    }
}

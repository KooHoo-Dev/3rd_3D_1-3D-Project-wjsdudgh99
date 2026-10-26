using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 스테이지를 클리어하면 잠시 뒤 화면 중앙에 나타나는 클리어 창 입니다.
    // 이번 기록을 보여 주고, Next 버튼을 눌러야 다음 스테이지(마지막이면 결과 화면)로 넘어갑니다.
    // 창이 떠 있는 동안에도 카메라는 돌릴 수 있어서, 찾은 정답을 다시 둘러보며 쉬어 갈 수 있습니다.
    //
    // 항상 켜져 있는 루트에 붙이고, 보였다 숨겨지는 것은 popup 자식입니다. (PauseController와 같은 구조)
    public class StageClearPanel : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private CanvasGroup popup;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonText;   // 마지막 스테이지에서는 아래 문구로 바뀝니다 (비워 두면 그대로)
        [SerializeField] private Button mainMenuButton;

        [Header("Text")]
        // 다음이 결과 화면일 때 Next 버튼에 쓸 문구 입니다. 씬에서 한국어로 바꿔 써도 됩니다.
        [SerializeField] private string lastStageNextText = "Results";

        [Header("Appear")]
        [SerializeField] private float appearDelay = 1f;    // 반짝임 연출을 먼저 보여 줄 시간
        [SerializeField] private float appearDuration = 0.4f;
        [SerializeField] private float startScale = 0.8f;

        private StageManager stageManager;

        private void Awake()
        {
            stageManager = FindAnyObjectByType<StageManager>();
            nextButton.onClick.AddListener(() => stageManager.ProceedToNext());
            mainMenuButton.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
            popup.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (stageManager != null) stageManager.OnStageCleared += HandleStageCleared;
        }

        private void OnDisable()
        {
            if (stageManager != null) stageManager.OnStageCleared -= HandleStageCleared;
        }

        private void HandleStageCleared()
        {
            timeText.text = TimeFormatter.Format(stageManager.ElapsedTime);
            if (nextButtonText != null && GameManager.Instance.HasNextStage == false) nextButtonText.text = lastStageNextText;

            StartCoroutine(AppearRoutine());
        }

        private IEnumerator AppearRoutine()
        {
            yield return new WaitForSeconds(appearDelay);

            Transform popupTransform = popup.transform;
            popup.alpha = 0f;
            popup.interactable = false;
            popupTransform.localScale = Vector3.one * startScale;
            popup.gameObject.SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / appearDuration;
                // 끝으로 갈수록 천천히 멈추는 EaseOut 곡선 (ResultUI와 같은 연출)
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                popup.alpha = eased;
                popupTransform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
                yield return null;
            }

            popup.interactable = true;
        }
    }
}

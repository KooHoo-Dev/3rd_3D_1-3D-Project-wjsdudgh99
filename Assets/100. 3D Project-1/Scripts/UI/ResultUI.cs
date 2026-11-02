using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 결과 화면 입니다.
    // GameManager에 모아 둔 스테이지별 기록과 총 시간을 보여주고,
    // 결과 패널이 화면 중앙에 부드럽게 나타나도록 연출합니다.
    public class ResultUI : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text recordsText;
        [SerializeField] private TMP_Text totalTimeText;
        [SerializeField] private Button mainMenuButton;

        [Header("Appear")]
        [SerializeField] private float appearDelay = 0.3f;
        [SerializeField] private float appearDuration = 0.5f;
        [SerializeField] private float startScale = 0.8f;

        private void Awake()
        {
            mainMenuButton.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
        }

        private void Start()
        {
            ShowRecords();
            StartCoroutine(AppearRoutine());
        }

        private void ShowRecords()
        {
            var records = GameManager.Instance.Records;
            if (records.Count == 0)
            {
                // TMP 기본 폰트에는 한글이 없어서 영어로 표시합니다.
                recordsText.text = "NO RECORDS";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (GameManager.StageRecord record in records)
                {
                    // HUD처럼 STAGE 1 대문자로 보여 줍니다. (기록에 저장된 이름은 씬의 Stage Name 그대로)
                    sb.AppendLine($"{record.stageName.ToUpperInvariant()}   {TimeFormatter.Format(record.time)}");
                }
                recordsText.text = sb.ToString();
            }

            totalTimeText.text = $"TOTAL TIME: {TimeFormatter.Format(GameManager.Instance.TotalTime)}";
        }

        private IEnumerator AppearRoutine()
        {
            Transform panelTransform = panel.transform;
            panel.alpha = 0f;
            panel.interactable = false;
            panelTransform.localScale = Vector3.one * startScale;

            yield return new WaitForSecondsRealtime(appearDelay);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / appearDuration;
                // 끝으로 갈수록 천천히 멈추는 EaseOut 곡선
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                panel.alpha = eased;
                panelTransform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
                yield return null;
            }

            panel.interactable = true;
        }
    }
}

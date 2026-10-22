using System.Collections;
using UnityEngine;

namespace YoureRight
{
    // 오답 클릭 시 마우스 커서 위치에 작은 붉은 X 표시를 잠깐 띄웁니다. (기획서 5.1)
    // 타격감만 주고 패널티는 없습니다.
    // Screen Space - Overlay Canvas에 붙입니다. X 표시는 프리팹으로 만들어 연결합니다.
    public class WrongClickMarker : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private ClickInteractor clickInteractor;
        [SerializeField] private RectTransform markerPrefab;   // 붉은 X (Image 또는 TMP). Raycast Target은 꺼 둡니다.

        [Header("Animation")]
        [SerializeField] private float lifetime = 0.5f;
        [SerializeField] private float popScale = 1.4f;         // 처음에 살짝 크게 나타났다가 원래 크기로 줄어듭니다
        [SerializeField] private float popTime = 0.1f;

        private RectTransform canvasRect;

        private void Awake()
        {
            if (clickInteractor == null) clickInteractor = FindAnyObjectByType<ClickInteractor>();
            canvasRect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            clickInteractor.OnWrongClicked += HandleWrong;
        }

        private void OnDisable()
        {
            clickInteractor.OnWrongClicked -= HandleWrong;
        }

        private void HandleWrong(Vector2 screenPos, Collider hitCollider)
        {
            RectTransform marker = Instantiate(markerPrefab, canvasRect);
            marker.position = screenPos;   // Overlay Canvas에서는 화면 좌표가 곧 UI 위치입니다

            // 투명도를 한 번에 조절하려고 CanvasGroup을 사용합니다. (프리팹에 없으면 추가)
            CanvasGroup group = marker.GetComponent<CanvasGroup>();
            if (group == null) group = marker.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            StartCoroutine(MarkerRoutine(marker, group));
        }

        private IEnumerator MarkerRoutine(RectTransform marker, CanvasGroup group)
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                // 처음 popTime 동안 popScale → 1로 줄어들고, 뒤쪽 절반 동안 서서히 투명해집니다.
                float pop = Mathf.Clamp01(elapsed / popTime);
                marker.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, pop);

                float fadeStart = lifetime * 0.5f;
                group.alpha = elapsed < fadeStart ? 1f : 1f - (elapsed - fadeStart) / (lifetime - fadeStart);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(marker.gameObject);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 오답 클릭 제한 중에 마우스 커서를 빨간 원으로 바꿔 보여 줍니다.
    // 12시에서 출발한 반지름(바늘)이 제한 시간 동안 시계 방향으로 한 바퀴 돌며 지나간 자리의 원을 지웁니다.
    // 다 지워지면 원래 커서로 돌아옵니다. 일시정지 중에는 팝업 버튼을 누를 수 있도록 원래 커서를 보여 줍니다.
    //
    // 항상 켜져 있는 오브젝트에 붙이고, 보였다 숨겨지는 것은 visual 자식입니다. (PauseController와 같은 구조)
    // Screen Space - Overlay Canvas의 맨 아래(가장 위에 그려지는 자리)에 둡니다.
    public class ClickLockIndicator : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private ClickInteractor clickInteractor;
        [SerializeField] private RectTransform visual;   // 커서를 따라다니는 부모 (원과 바늘을 담음)
        [SerializeField] private Image ring;             // 빨간 원. Image Type은 코드에서 Filled로 맞춥니다.
        [SerializeField] private RectTransform hand;     // 반지름 바늘 (없어도 됨). Pivot을 아래 가운데(0.5, 0)로 둡니다.

        private bool cursorHidden;

        private void Awake()
        {
            if (clickInteractor == null) clickInteractor = FindAnyObjectByType<ClickInteractor>();

            // 12시에서 시작해, 남은 양이 줄수록 지워진 자리가 시계 방향으로 넓어지게 합니다.
            // (반시계 방향으로 채우고 fillAmount를 1 → 0으로 줄이면 경계가 시계 방향으로 움직임)
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;

            // 커서 아래의 버튼이나 물체 클릭을 가로막지 않도록 합니다.
            foreach (Graphic graphic in visual.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            visual.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            bool show = clickInteractor != null && clickInteractor.IsLocked && Time.timeScale > 0f;
            if (show != visual.gameObject.activeSelf) visual.gameObject.SetActive(show);
            SetCursorHidden(show);
            if (show == false) return;

            // Overlay Canvas에서는 화면 좌표가 곧 UI 위치입니다.
            visual.position = SimpleInput.GetMousePosition();

            float remaining = clickInteractor.LockDuration > 0f ? clickInteractor.LockRemaining / clickInteractor.LockDuration : 0f;
            ring.fillAmount = remaining;

            // UI에서 Z 회전 +는 반시계 방향이므로, 시계 방향으로 돌리려면 음수로 줍니다.
            if (hand != null) hand.localEulerAngles = new Vector3(0f, 0f, -360f * (1f - remaining));
        }

        private void SetCursorHidden(bool hidden)
        {
            if (cursorHidden == hidden) return;
            cursorHidden = hidden;
            Cursor.visible = hidden == false;
        }

        private void OnDisable()
        {
            // 제한 중에 씬이 바뀌거나 Play가 끝나도 커서가 사라진 채로 남지 않게 합니다.
            SetCursorHidden(false);
        }
    }
}

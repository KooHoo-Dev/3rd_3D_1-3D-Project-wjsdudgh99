using UnityEngine;

namespace YoureRight
{
    // 이동을 꺼 둔 스테이지(OrbitCameraController의 Horizontal Move / Vertical Move)에서 WASD·Q/E를 누르면
    // "이 스테이지에서는 이동할 수 없다"는 안내 문구를 화면 아래에 잠깐 띄웁니다.
    // 누르고 있는 동안은 계속 보이고, 손을 떼면 Hold Time 뒤에 Fade Time 동안 사라집니다.
    // 일시정지 중에는 카메라가 입력을 받지 않으므로 뜨지 않고, 스테이지 클리어 뒤에도 띄우지 않습니다.
    //
    // 항상 켜져 있는 오브젝트에 붙이고, 보였다 숨겨지는 것은 visual(CanvasGroup) 자식입니다. (ClickLockIndicator와 같은 구조)
    // 문구는 프리팹(Prefabs/Move Lock Notice)의 TMP 글자를 씬에서 직접 고칩니다.
    public class MoveLockNotice : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private OrbitCameraController orbitCamera;   // 비워 두면 씬에서 찾습니다
        [SerializeField] private CanvasGroup visual;

        [Header("Timing")]
        [SerializeField] private float holdTime = 1.5f;   // 손을 뗀 뒤 그대로 보이는 시간
        [SerializeField] private float fadeTime = 0.3f;

        private StageManager stageManager;
        private float lastInputTime = float.NegativeInfinity;

        private void Awake()
        {
            if (orbitCamera == null) orbitCamera = FindAnyObjectByType<OrbitCameraController>();
            stageManager = FindAnyObjectByType<StageManager>();

            visual.blocksRaycasts = false;
            visual.interactable = false;
            visual.alpha = 0f;
            visual.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (orbitCamera != null) orbitCamera.OnLockedMoveInput += HandleLockedMoveInput;
        }

        private void OnDisable()
        {
            if (orbitCamera != null) orbitCamera.OnLockedMoveInput -= HandleLockedMoveInput;
        }

        private void HandleLockedMoveInput()
        {
            if (stageManager != null && stageManager.IsCleared) return;
            lastInputTime = Time.unscaledTime;
        }

        private void Update()
        {
            float since = Time.unscaledTime - lastInputTime;
            float alpha = since <= holdTime ? 1f : 1f - Mathf.Clamp01((since - holdTime) / Mathf.Max(0.01f, fadeTime));

            bool show = alpha > 0f;
            if (show != visual.gameObject.activeSelf) visual.gameObject.SetActive(show);
            visual.alpha = alpha;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YoureRight
{
    // 마우스 좌클릭 시 커서가 있는 화면의 카메라에서 Raycast를 발사해 정답/오답을 판정합니다.
    // 맞은 Collider에 AnswerTarget이 있으면 정답, 다른 물체면 오답입니다.
    // 빈 곳(아무 Collider도 맞지 않음)을 클릭하면 아무 반응도 하지 않습니다.
    public class ClickInteractor : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private Camera[] targetCameras;           // 분할 화면의 카메라들 (비워 두면 Main Camera 하나만 사용)

        [Header("Raycast")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask clickableLayers = ~0;   // 지형 레이어를 빼면 지형이 클릭을 막지 않습니다

        // 정답 쌍을 새로 찾았을 때 (클릭한 오브젝트)
        public event Action<AnswerTarget> OnAnswerClicked;

        // 오답일 때 (커서 화면 좌표, 맞은 Collider)
        public event Action<Vector2, Collider> OnWrongClicked;

        private StageManager stageManager;

        private void Awake()
        {
            if (targetCameras == null || targetCameras.Length == 0) targetCameras = new[] { Camera.main };
            stageManager = FindAnyObjectByType<StageManager>();
        }

        private void Update()
        {
            // 일시정지 중이거나, 스테이지를 클리어했거나, UI 위를 클릭한 경우에는 판정하지 않습니다.
            if (Time.timeScale == 0f) return;
            if (stageManager != null && stageManager.IsCleared) return;
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left) == false) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screenPos = SimpleInput.GetMousePosition();
            Camera cam = FindCameraAt(screenPos);
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);

            // 빈 곳을 클릭했으면 정답도 오답도 아니므로 아무것도 하지 않습니다.
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayers, QueryTriggerInteraction.Ignore) == false) return;

            HandleHit(hit, screenPos);
        }

        // 커서가 어느 카메라의 화면 영역(Viewport Rect) 안에 있는지 찾습니다.
        private Camera FindCameraAt(Vector2 screenPos)
        {
            foreach (Camera cam in targetCameras)
            {
                if (cam != null && cam.pixelRect.Contains(screenPos)) return cam;
            }
            return null;
        }

        private void HandleHit(RaycastHit hit, Vector2 screenPos)
        {
            // 자식 Collider를 클릭해도 부모의 AnswerTarget을 찾을 수 있게 합니다.
            AnswerTarget target = hit.collider.GetComponentInParent<AnswerTarget>();
            if (target == null || target.Pair == null)
            {
                HandleWrong(screenPos, hit.collider);
                return;
            }

            // 이미 찾은 쌍을 다시 누른 경우는 정답도 오답도 아니므로 무시합니다.
            if (target.Pair.TryFind() == false) return;

            OnAnswerClicked?.Invoke(target);
        }

        private void HandleWrong(Vector2 screenPos, Collider hitCollider)
        {
            Debug.Log($"[ClickInteractor] 오답: {hitCollider.name}");
            OnWrongClicked?.Invoke(screenPos, hitCollider);
        }
    }
}

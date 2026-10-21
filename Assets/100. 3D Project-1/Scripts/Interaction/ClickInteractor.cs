using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YoureRight
{
    // 마우스 좌클릭 시 카메라에서 Raycast를 발사해 정답/오답을 판정합니다.
    // 맞은 Collider에 AnswerTarget이 있으면 정답, 그 외(빈 곳 포함)는 오답입니다.
    public class ClickInteractor : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private Camera targetCamera;

        [Header("Raycast")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask clickableLayers = ~0;   // 지형 레이어를 빼면 지형이 클릭을 막지 않습니다

        // 정답 쌍을 새로 찾았을 때 (클릭한 오브젝트)
        public event Action<AnswerTarget> OnAnswerClicked;

        // 오답일 때 (커서 화면 좌표, 맞은 Collider - 빈 곳이면 null)
        public event Action<Vector2, Collider> OnWrongClicked;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            // 일시정지 중이거나 UI 위를 클릭한 경우에는 판정하지 않습니다.
            if (Time.timeScale == 0f) return;
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left) == false) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screenPos = SimpleInput.GetMousePosition();
            Ray ray = targetCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayers, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit, screenPos);
            }
            else
            {
                HandleWrong(screenPos, null);
            }
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
            string hitName = hitCollider != null ? hitCollider.name : "빈 곳";
            Debug.Log($"[ClickInteractor] 오답: {hitName}");
            OnWrongClicked?.Invoke(screenPos, hitCollider);
        }
    }
}

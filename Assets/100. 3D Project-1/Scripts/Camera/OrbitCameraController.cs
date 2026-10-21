using Study.Utilities;
using UnityEngine;

namespace YoureRight
{
    // 두 디오라마의 중앙(pivot)을 축으로 공전하는 오빗 카메라 입니다.
    // 회전: WASD 키 또는 마우스 우클릭 드래그
    // 줌: 마우스 휠
    // 입력은 목표값(target)에 쌓고, 실제 카메라는 목표값을 부드럽게 따라갑니다.
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private Transform pivot;

        [Header("Rotate")]
        [SerializeField] private float keyRotateSpeed = 90f;     // WASD 입력시 초당 회전 각도
        [SerializeField] private float degreesPerPixel = 0.2f;   // 드래그 1픽셀당 회전 각도
        [SerializeField] private float pitchMin = -80f;          // 음수면 디오라마 아래쪽에서도 볼 수 있습니다
        [SerializeField] private float pitchMax = 80f;           // ±90도에 가까우면 화면이 뒤집히니 80도 정도로 제한합니다

        [Header("Zoom")]
        [SerializeField] private float zoomStep = 1f;            // 휠 한 칸당 거리 변화량
        [SerializeField] private float distanceMin = 5f;
        [SerializeField] private float distanceMax = 25f;

        [Header("Smoothing")]
        [SerializeField] private float sharpness = 10f;          // 클수록 목표값을 빨리 따라갑니다

        // 입력이 쌓이는 목표값
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;

        // 실제 카메라에 적용되는 현재값
        private float yaw;
        private float pitch;
        private float distance;

        private void Awake()
        {
            // 씬에 배치해 둔 카메라 위치에서 시작하도록 현재 각도와 거리를 역으로 계산합니다.
            Vector3 offset = transform.position - pivot.position;
            distance = Mathf.Clamp(offset.magnitude, distanceMin, distanceMax);
            pitch = Mathf.Clamp(Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg, pitchMin, pitchMax);
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;

            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
        }

        private void Update()
        {
            // 일시정지(timeScale = 0) 중에는 카메라를 조작하지 않습니다.
            if (Time.timeScale == 0f) return;

            UpdateRotateInput();
            UpdateZoomInput();
        }

        private void LateUpdate()
        {
            // 프레임 속도와 상관없이 같은 느낌으로 따라가도록 지수 감쇠 비율을 사용합니다.
            float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);
            distance = Mathf.Lerp(distance, targetDistance, t);

            ApplyTransform();
        }

        private void UpdateRotateInput()
        {
            // A/D: 좌우 공전, W/S: 위아래 공전
            Vector2 axis = SimpleInput.GetMoveAxisRaw();
            targetYaw -= axis.x * keyRotateSpeed * Time.deltaTime;
            targetPitch += axis.y * keyRotateSpeed * Time.deltaTime;

            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right))
            {
                Vector2 delta = SimpleInput.GetMouseDelta();
                targetYaw += delta.x * degreesPerPixel;
                targetPitch -= delta.y * degreesPerPixel;
            }

            targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
        }

        private void UpdateZoomInput()
        {
            float scroll = SimpleInput.GetMouseScrollDelta();
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomStep, distanceMin, distanceMax);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = pivot.position + rotation * new Vector3(0f, 0f, -distance);
            transform.rotation = rotation;
        }

        private void OnValidate()
        {
            pitchMax = Mathf.Max(pitchMin, pitchMax);
            distanceMax = Mathf.Max(distanceMin, distanceMax);
        }

        private void OnDrawGizmosSelected()
        {
            if (pivot == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivot.position, 0.3f);
            Gizmos.DrawLine(transform.position, pivot.position);
        }
    }
}

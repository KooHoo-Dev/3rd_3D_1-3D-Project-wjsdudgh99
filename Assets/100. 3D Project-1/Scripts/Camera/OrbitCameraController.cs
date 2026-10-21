using UnityEngine;

namespace YoureRight
{
    // 분할 화면의 카메라들을 각자의 디오라마 중앙(pivot) 기준으로 똑같이 공전시키는 오빗 카메라 입니다.
    // 회전: WASD 키 또는 마우스 우클릭 드래그
    // 줌: 마우스 휠
    // 입력은 목표값(target)에 쌓고, 실제 카메라는 목표값을 부드럽게 따라갑니다.
    // 모든 카메라가 같은 각도·거리를 쓰므로 두 화면이 항상 같은 시점으로 보입니다.
    public class OrbitCameraController : MonoBehaviour
    {
        // 카메라 한 대와 그 카메라가 바라볼 중심점 한 쌍
        [System.Serializable]
        private class OrbitView
        {
            public Transform camera;
            public Transform pivot;
        }

        [Header("Ref")]
        [SerializeField] private OrbitView[] views;              // 0번 카메라의 배치 위치가 시작 시점이 됩니다

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
            if (views == null || views.Length == 0 || views[0].camera == null || views[0].pivot == null)
            {
                Debug.LogError("[OrbitCameraController] Views에 카메라와 Pivot을 지정해 주세요.", this);
                enabled = false;
                return;
            }

            // 0번 카메라를 씬에 배치해 둔 위치에서 시작하도록 현재 각도와 거리를 역으로 계산합니다.
            Vector3 offset = views[0].camera.position - views[0].pivot.position;
            distance = Mathf.Clamp(offset.magnitude, distanceMin, distanceMax);
            pitch = Mathf.Clamp(Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg, pitchMin, pitchMax);
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;

            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;

            // 첫 프레임부터 모든 카메라가 같은 시점을 갖도록 바로 맞춥니다.
            ApplyTransform();
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
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            foreach (OrbitView view in views)
            {
                if (view.camera == null || view.pivot == null) continue;

                view.camera.position = view.pivot.position + offset;
                view.camera.rotation = rotation;
            }
        }

        private void OnValidate()
        {
            pitchMax = Mathf.Max(pitchMin, pitchMax);
            distanceMax = Mathf.Max(distanceMin, distanceMax);
        }

        private void OnDrawGizmosSelected()
        {
            if (views == null) return;

            Gizmos.color = Color.yellow;
            foreach (OrbitView view in views)
            {
                if (view.camera == null || view.pivot == null) continue;

                Gizmos.DrawWireSphere(view.pivot.position, 0.3f);
                Gizmos.DrawLine(view.camera.position, view.pivot.position);
            }
        }
    }
}

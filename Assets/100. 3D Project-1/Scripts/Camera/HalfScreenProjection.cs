using UnityEngine;

namespace YoureRight
{
    // 월드 3용: 카메라가 화면 전체 기준으로 그린 그림 중 자기 반쪽만 보여 줍니다.
    // Viewport만 반으로 나누면 Pivot의 물체가 각 반쪽 가운데에 그려지지만, 이 컴포넌트를 붙이면 화면 정중앙(분할선)에 그려져
    // 왼쪽 화면에는 버전 A의 왼쪽 반, 오른쪽 화면에는 버전 B의 오른쪽 반이 이어져 하나처럼 보입니다.
    // 두 카메라에 하나씩 붙이고 Side만 다르게 둡니다. Viewport Rect도 여기서 맞춥니다.
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class HalfScreenProjection : MonoBehaviour
    {
        public enum ScreenSide { Left, Right }

        [SerializeField] private ScreenSide side = ScreenSide.Left;

        private Camera cam;

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            ApplyRect();
        }

        private void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>();
            ApplyRect();
        }

        private void OnDisable()
        {
            if (cam != null) cam.ResetProjectionMatrix();
        }

        // 해상도나 Field of View가 바뀌어도 맞도록 매 프레임 다시 계산합니다.
        private void LateUpdate()
        {
            Rect pixels = cam.pixelRect;
            if (pixels.height <= 0f) return;

            // 화면 전체(반쪽 2개) 기준 시야를 만들고, 그중 자기 쪽 절반만 남깁니다.
            float near = cam.nearClipPlane;
            float top = near * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = top * (pixels.width * 2f / pixels.height);
            float left = side == ScreenSide.Left ? -halfWidth : 0f;
            float right = side == ScreenSide.Left ? 0f : halfWidth;

            cam.projectionMatrix = Matrix4x4.Frustum(left, right, -top, top, near, cam.farClipPlane);
        }

        // 값이 다를 때만 바꿔서, 씬을 열 때마다 저장 안 됨(*) 표시가 생기지 않게 합니다.
        private void ApplyRect()
        {
            if (cam == null) return;
            Rect rect = side == ScreenSide.Left ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0.5f, 0f, 0.5f, 1f);
            if (cam.rect != rect) cam.rect = rect;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

namespace YoureRight
{
    // A안 UI 버튼 연출: 마우스를 올리면 살짝 커지고, 떼면 원래 크기로 돌아옵니다. (밝아지는 것은 Button의 Color Tint가 맡음)
    // 일시정지 팝업에서도 움직이도록 unscaledDeltaTime을 씁니다. UIThemeApplier가 버튼마다 붙입니다.
    public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float sharpness = 18f;   // 클수록 빨리 커짐

        private Vector3 baseScale = Vector3.one;
        private bool hovered;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            // 팝업이 닫힐 때 커진 채로 남지 않게 합니다.
            hovered = false;
            transform.localScale = baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void Update()
        {
            Vector3 target = hovered ? baseScale * hoverScale : baseScale;
            float t = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, target, t);
        }
    }
}

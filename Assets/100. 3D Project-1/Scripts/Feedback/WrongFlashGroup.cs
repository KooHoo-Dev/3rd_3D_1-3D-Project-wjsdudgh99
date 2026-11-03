using UnityEngine;

namespace YoureRight
{
    // 오답 빨간 깜빡임을 이 오브젝트 전체에 적용하라는 표시입니다.
    // 부품마다 Collider가 있는 소품(예: Info Stand)의 루트에 붙이면, 어느 부품을 눌러도 소품 전체가 빨개집니다.
    // 붙이지 않은 물체는 맞은 Collider의 물체만 빨개집니다. (ClickFeedback 참고)
    public class WrongFlashGroup : MonoBehaviour
    {
    }
}

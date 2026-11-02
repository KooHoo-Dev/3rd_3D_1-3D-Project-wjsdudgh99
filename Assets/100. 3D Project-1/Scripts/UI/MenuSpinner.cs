using UnityEngine;

namespace YoureRight
{
    // 메인 화면의 쌍둥이 물체를 제자리에서 천천히 돌립니다.
    // 두 물체에 같은 값으로 붙이면 똑같이 돌아서 "쌍둥이"로 보입니다.
    // 일시정지와 상관없이 돌도록 unscaledDeltaTime을 씁니다.
    public class MenuSpinner : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 20f;     // 음수면 반대로 돕니다
        [SerializeField] private Vector3 axis = Vector3.up;        // 월드 기준 회전 축

        private void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.unscaledDeltaTime, Space.World);
        }
    }
}

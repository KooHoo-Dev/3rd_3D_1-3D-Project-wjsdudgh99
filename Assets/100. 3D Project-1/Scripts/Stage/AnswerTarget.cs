using UnityEngine;

namespace YoureRight
{
    // 정답 쌍둥이 오브젝트 하나하나에 붙이는 컴포넌트 입니다.
    // 마우스 Raycast에 잡혀야 하므로 Collider가 반드시 필요합니다.
    // 자신이 어느 쌍(AnswerPair)에 속하는지는 AnswerPair가 Awake에서 알려줍니다.
    [RequireComponent(typeof(Collider))]
    public class AnswerTarget : MonoBehaviour
    {
        public AnswerPair Pair { get; private set; }

        public void SetPair(AnswerPair pair)
        {
            if (Pair != null && Pair != pair)
            {
                Debug.LogWarning($"[AnswerTarget] {name}이(가) 두 개의 AnswerPair({Pair.name}, {pair.name})에 등록되어 있습니다.", this);
            }
            Pair = pair;
        }
    }
}

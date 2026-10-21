using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 메인 메뉴 화면의 버튼을 처리합니다.
    // 버튼의 OnClick은 Inspector에서 연결하지 않고 여기서 코드로 연결합니다.
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            startButton.onClick.AddListener(() => GameManager.Instance.StartNewRun());
            exitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace YoureRight
{
    // P 키로 게임을 일시정지하고 팝업(계속하기 / 메인 메뉴로)을 띄웁니다.
    // Time.timeScale = 0이 되면 스톱워치, 카메라, 클릭 판정이 모두 알아서 멈춥니다.
    //
    // 이 컴포넌트는 항상 켜져 있는 루트 오브젝트에 붙이고, 실제로 보였다 숨겨지는 것은 popup 자식입니다.
    // (컴포넌트가 붙은 오브젝트를 끄면 Update가 멈춰 P 키를 다시 받을 수 없기 때문)
    // 자식만 참조하므로 프리팹으로 만들어 스테이지 씬마다 그대로 넣을 수 있습니다.
    public class PauseController : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private GameObject popup;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Input")]
        [SerializeField] private Key pauseKey = Key.P;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            continueButton.onClick.AddListener(Resume);
            mainMenuButton.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
            popup.SetActive(false);
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(pauseKey) == false) return;

            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            popup.SetActive(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            popup.SetActive(false);
        }

        private void OnDestroy()
        {
            // 일시정지 상태로 씬이 바뀌거나 Play가 끝나도 timeScale이 0으로 남지 않도록 되돌립니다.
            if (IsPaused) Time.timeScale = 1f;
        }
    }
}

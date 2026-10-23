using System;
using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 스테이지 시작 전에 규칙과 조작법을 여러 페이지로 보여주는 안내 패널 입니다.
    // 패널이 떠 있는 동안 Time.timeScale = 0이 되어 스톱워치, 카메라, 클릭 판정이 모두 멈추고,
    // 마지막 페이지의 시작 버튼을 누르면 패널을 닫고 시간을 다시 흐르게 합니다.
    // 페이지 내용(글자, 이미지)은 씬에서 직접 꾸미고, 이 스크립트는 페이지 넘김만 맡습니다.
    public class TutorialGuide : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private GameObject[] pages;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button startButton;

        public bool IsOpen { get; private set; }

        // 시작 버튼으로 안내를 닫았을 때 (StageManager가 이때 스톱워치를 켭니다)
        public event Action OnClosed;

        private int pageIndex;

        private void Awake()
        {
            prevButton.onClick.AddListener(() => ShowPage(pageIndex - 1));
            nextButton.onClick.AddListener(() => ShowPage(pageIndex + 1));
            startButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            Open();
        }

        public void Open()
        {
            IsOpen = true;
            Time.timeScale = 0f;
            gameObject.SetActive(true);
            ShowPage(0);
        }

        private void Close()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }

        private void ShowPage(int index)
        {
            pageIndex = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == pageIndex);

            // 첫 페이지에서는 이전 버튼을, 마지막 페이지에서는 다음 버튼 대신 시작 버튼을 보여줍니다.
            bool isLast = pageIndex == pages.Length - 1;
            prevButton.gameObject.SetActive(pageIndex > 0);
            nextButton.gameObject.SetActive(isLast == false);
            startButton.gameObject.SetActive(isLast);
        }

        private void OnDestroy()
        {
            // 안내 중에 씬이 바뀌거나 Play가 끝나도 timeScale이 0으로 남지 않도록 되돌립니다.
            if (IsOpen) Time.timeScale = 1f;
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YoureRight
{
    // 게임 전체의 소리를 담당합니다. (기획서 5.2)
    // BGM: 씬이 바뀌어도 끊기지 않고 이어서 재생됩니다. (DontDestroyOnLoad)
    // SFX: 정답 / 오답 / 버튼 클릭 소리
    //
    // 프리팹(Prefabs/SoundManager)으로 만들어 모든 씬에 넣어 둡니다.
    // 처음 만들어진 것 하나만 살아남고, 다음 씬에 있는 것은 스스로 사라집니다.
    // 그래서 메뉴부터 시작해도, 스테이지 씬을 바로 Play해도 소리가 납니다.
    //
    // 씬이 바뀔 때마다 그 씬의 ClickInteractor 이벤트와 모든 버튼에 소리를 자동으로 연결하므로
    // 다른 스크립트나 Inspector에서 따로 연결할 필요가 없습니다.
    public class SoundManager : SingletonBase<SoundManager>
    {
        [Header("AudioSource")]
        [SerializeField] private AudioSource bgmSource;   // 비워 두면 자동으로 만듭니다
        [SerializeField] private AudioSource sfxSource;   // 비워 두면 자동으로 만듭니다

        [Header("BGM")]
        [SerializeField] private AudioClip bgm;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float pausedBgmVolumeRatio = 0.3f;   // 일시정지 중에는 BGM을 이 비율만큼 줄입니다
        [SerializeField] private float bgmFadeSpeed = 2f;              // 볼륨이 바뀌는 속도 (초당 변화량)

        [Header("SFX")]
        [SerializeField] private AudioClip correctAnswerClip;
        [SerializeField] private AudioClip wrongAnswerClip;
        [SerializeField] private AudioClip buttonClickClip;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        // 지금 씬에서 구독 중인 ClickInteractor (씬이 바뀌면 새 씬의 것으로 바꿉니다)
        private ClickInteractor clickInteractor;

        protected override void Awake()
        {
            // 이미 살아 있는 SoundManager가 있으면 (이전 씬에서 넘어온 것) 이 복제본은 조용히 사라집니다.
            // (SingletonBase의 중복 경고 로그가 씬을 넘길 때마다 뜨지 않도록 먼저 처리합니다)
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // DontDestroyOnLoad는 하이어라키 맨 위(루트) 오브젝트에만 동작합니다.
            // 씬에서는 Managers 그룹 안에 두고, 살아남을 때만 그룹에서 빼냅니다.
            transform.SetParent(null);

            base.Awake();
        }

        protected override void OnInitialize()
        {
            SetupSources();

            // 이 씬을 포함해 앞으로 열리는 씬마다 소리를 연결합니다.
            // (sceneLoaded는 씬 오브젝트들의 Awake 이후에 호출되므로 지금 씬도 여기서 처리됩니다)
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDispose()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnbindClickInteractor();
        }

        private void Start()
        {
            PlayBgm();
        }

        private void Update()
        {
            if (bgmSource == null) return;

            // 일시정지(timeScale = 0) 중에는 BGM을 부드럽게 줄이고, 풀리면 다시 올립니다.
            // timeScale이 0이어도 움직이도록 unscaledDeltaTime을 사용합니다.
            float target = Time.timeScale == 0f ? bgmVolume * pausedBgmVolumeRatio : bgmVolume;
            bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, target, bgmFadeSpeed * Time.unscaledDeltaTime);
        }

        private void SetupSources()
        {
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        #region 씬마다 연결

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindClickInteractor();
            BindButtons();
        }

        private void BindClickInteractor()
        {
            UnbindClickInteractor();

            // 메뉴나 결과 화면처럼 ClickInteractor가 없는 씬도 있습니다.
            clickInteractor = FindAnyObjectByType<ClickInteractor>();
            if (clickInteractor == null) return;

            clickInteractor.OnAnswerClicked += HandleAnswerClicked;
            clickInteractor.OnWrongClicked += HandleWrongClicked;
        }

        private void UnbindClickInteractor()
        {
            // 이전 씬의 ClickInteractor는 이미 파괴됐을 수 있지만, C# 이벤트 해제는 그대로 해도 안전합니다.
            if (ReferenceEquals(clickInteractor, null)) return;

            clickInteractor.OnAnswerClicked -= HandleAnswerClicked;
            clickInteractor.OnWrongClicked -= HandleWrongClicked;
            clickInteractor = null;
        }

        private void BindButtons()
        {
            // 꺼져 있는 일시정지 팝업의 버튼까지 포함해서 씬의 모든 버튼에 클릭 소리를 연결합니다.
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                button.onClick.AddListener(PlayButtonSfx);
            }
        }

        private void HandleAnswerClicked(AnswerTarget target) => PlayCorrectSfx();

        private void HandleWrongClicked(Vector2 screenPos, Collider hitCollider) => PlayWrongSfx();

        #endregion

        #region 재생

        public void PlayBgm()
        {
            if (bgm == null) return;

            // 같은 곡이 이미 재생 중이면 처음부터 다시 틀지 않습니다.
            if (bgmSource.isPlaying && bgmSource.clip == bgm) return;

            bgmSource.clip = bgm;
            bgmSource.Play();
        }

        public void PlayCorrectSfx()
        {
            PlaySfx(correctAnswerClip);
        }

        public void PlayWrongSfx()
        {
            PlaySfx(wrongAnswerClip);
        }

        public void PlayButtonSfx()
        {
            PlaySfx(buttonClickClip);
        }

        private void PlaySfx(AudioClip clip)
        {
            // 사운드 파일을 아직 넣지 않았으면 아무 일도 하지 않습니다.
            if (clip == null) return;

            // PlayOneShot은 이전 효과음을 끊지 않고 겹쳐서 재생합니다.
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        #endregion
    }
}

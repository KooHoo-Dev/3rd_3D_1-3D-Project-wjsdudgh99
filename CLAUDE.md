# CLAUDE.md

이 저장소는 Unity 3D 코스 프로젝트입니다. 지금 진행 중인 메인 작업은 **'You're Right' 게임**입니다.
사용자는 학원 컴퓨터와 집 컴퓨터를 오가며 작업하므로, 작업 규칙과 진행 상황은 이 파일에 기록합니다.

## 프로젝트 개요

- **게임**: 'You're Right'. 좌우 분할 화면에 나란히 보이는 두 3D 디오라마를 오빗 카메라로 함께 돌려 보며 **완전히 똑같은 쌍둥이 오브젝트**를 찾는 관찰형 퍼즐 게임입니다. 스테이지는 튜토리얼 1개와 메인 3개이고, 스톱워치로 기록을 잽니다.
- **기획서**: `Assets/01. Docs/3D 프로젝트 기획서 - 1 수정본.md`. 게임 작업 전에 먼저 읽습니다.
- **작업 폴더**: `Assets/100. 3D Project-1/` (Scenes, Scripts, Prefabs, Materials, Audio, UI)
- **환경**: Unity 6000.3.15f1, URP 17, Cinemachine 3.1.7(사용 안 함), Test Framework 1.6.0
  - **새 Input System만 켜져 있습니다**(activeInputHandler 1). `Input.GetKey` 같은 예전 API는 쓰지 않고 `SimpleInput`을 사용합니다.
  - asmdef가 없어서 모든 스크립트가 Assembly-CSharp 하나로 컴파일됩니다.
- 나머지 폴더(`00. Study`, `10. Scripts`, `19. Legacy`)는 수업 학습 코드이고, 게임과는 분리되어 있습니다.

## 작업 규칙

### 1. 학습 코드는 복사해서 쓴다

게임 코드에서 `Assets/10. Scripts`나 `Assets/19. Legacy` 등 `100. 3D Project-1` 바깥의 스크립트가 필요하면, **참조하지 말고 복사합니다.** 사용자에게 따로 묻지 않습니다.

- `Assets/100. 3D Project-1/Scripts/` 안에 복사합니다. 공용 도구는 `Utils/`에, 그 밖의 파일은 해당 기능 폴더에 둡니다.
- 네임스페이스를 `YoureRight`로 바꿉니다. 같은 어셈블리 안에서 클래스 이름이 겹치면 컴파일 오류가 나기 때문입니다.
- 게임 코드에서 예전 `using`을 지웁니다. 원본은 건드리지 않고, `.meta`는 복사하지 않습니다.
- 복사한 다음에는 무엇을 복사했는지 사용자에게 알립니다.

### 2. 씬 하이어라키 구조

모든 씬은 아래 루트 그룹을 사용합니다. 그룹 오브젝트는 빈 Transform이고 원점에 둡니다(위치·회전 0, 스케일 1). 쓰지 않는 그룹도 빈 채로 둡니다.

```
Cameras   — Main Camera(왼쪽 화면, OrbitCameraController), Right Camera(오른쪽 화면)
Lights    — Directional Light 등 조명
Stage     — 디오라마 지형, CameraPivot Left(x -100), CameraPivot Right(x 100)
Props     — 정답 쌍(AnswerPair/AnswerTarget), 오답 미끼 오브젝트
UI        — Canvas, EventSystem
Managers  — 화면에 안 보이는 관리 스크립트를 하나씩 빈 자식 오브젝트로 (StageManager, ClickInteractor, ClickFeedback, SoundManager 프리팹)
```

새 루트 그룹을 추가하려면 먼저 사용자에게 묻습니다.

### 3. 씬을 확인할 때는 GUID도 확인한다

사용자가 씬 파일을 복사해서 기존 씬을 덮어쓰면 `.unity.meta`의 GUID가 새로 만들어집니다. 그러면 `ProjectSettings/EditorBuildSettings.asset`이 예전 GUID를 가리키게 됩니다. 실제로 두 번 있었던 일입니다.

- 씬을 확인할 때마다 `git diff`로 `.meta`의 GUID가 바뀌었는지 봅니다. 바뀌었고 새 GUID를 참조하는 곳이 없으면 `git checkout -- <meta>`로 되돌리고 Unity를 갱신합니다.
- 스테이지가 아닌 씬(Menu, Result)에 스테이지 전용 컴포넌트가 남아 있는지도 확인합니다. Views가 빈 `OrbitCameraController`는 Play하자마자 "Views에 카메라와 Pivot을 지정해 주세요" 오류를 내고 꺼집니다.
- 사용자가 저장했다고 해도 씬 파일 수정 시각이 바뀌지 않았다면, 저장하지 않았거나 Play 모드 중에 수정한 것입니다.

### 4. UI 규칙

- Canvas Scaler는 Scale With Screen Size, 1920 × 1080으로 둡니다.
- 모서리에 붙는 UI는 앵커와 **Pivot**을 둘 다 그 모서리로 맞춥니다(Shift + Alt 프리셋). Pos는 여백만큼만 줍니다.
- 화면 전체를 덮는 요소는 stretch-stretch, 값은 모두 0으로 둡니다. 고정 크기로 두면 16:9가 아닌 화면에서 빈틈이 생깁니다.
- 프리팹 루트의 RectTransform은 씬마다 따로 저장됩니다. 그래서 프리팹을 고치면 이미 배치된 씬 인스턴스도 확인해야 합니다.
- TMP 기본 폰트(LiberationSans)에는 한글이 없습니다. 한글 텍스트는 `Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF`를 씁니다. 코드에서 넣는 문구는 영어로 씁니다.
- UI 크기는 Scale이 아니라 Width/Height와 글자 크기로 조절하는 것을 권합니다.

### 5. Git

- 커밋 메시지는 한국어로 씁니다. 커밋과 푸시는 사용자가 요청할 때만 합니다.
- **커밋하지 않는 파일**: `ProjectSettings/Packages/com.unity.probuilder/Settings.json`(ProBuilder 창을 열면 자동으로 바뀜), `.idea/`(Rider 설정), 빈 폴더의 `.meta`(다른 PC에서 폴더 없이 `.meta`만 남아 Unity가 지움).
- `Assets/CliScenes`는 `SceneCreator`가 처음 실행될 때 생기는 폴더라 PC마다 있을 수도, 없을 수도 있습니다.

## Unity Editor 연동 (CliBridge)

`Assets/Editor/CliBridge/`는 Editor가 열려 있을 때 `http://127.0.0.1:17890/`에서 HTTP 요청을 받습니다.

- `GET /ping`: 프로젝트 경로와 Unity 버전을 알려 줍니다.
- `GET /scenes`: `AssetDatabase.Refresh()`를 한 뒤 씬 목록을 돌려줍니다. **바깥에서 스크립트를 고친 뒤 컴파일을 일으킬 때** 이 요청을 씁니다.
- `POST /scenes/new`: `Assets/CliScenes`에 새 씬을 만듭니다.

이 브리지는 GitHub에 올라가 있어서 학원과 집 어디서나 동작합니다. 브리지를 호출하는 명령줄 프로그램 `C:\VR_9_Unity\UnityCli`는 원격 저장소가 없는 별도 저장소라 집 컴퓨터에만 있습니다. 게임 작업에는 필요 없습니다.

컴파일 결과는 `%LOCALAPPDATA%/Unity/Editor/Editor.log`에서 `Tundra build success`나 `error CS`를 찾아 확인합니다. 학습 코드에서 나오는 기존 경고(CollisionSensor, B_Blacksmith 등)는 무시합니다.

## 게임 코드 구조 (`Assets/100. 3D Project-1/Scripts`, namespace `YoureRight`)

| 폴더 | 스크립트 | 역할 |
|---|---|---|
| Core | `GameManager` | `SingletonBase`를 상속하고 DontDestroyOnLoad로 유지됩니다. 씬에 배치하지 않아도 처음 호출될 때 자동으로 만들어집니다. 스테이지별 기록을 모으고, 다음 씬 이동·메인 메뉴·종료를 맡습니다. 씬을 바꿀 때 `timeScale`을 1로 되돌립니다. |
| Core | `SceneNames` | Build Settings에 등록된 씬 이름 상수입니다. 씬 파일 이름을 바꾸면 여기도 고칩니다. |
| Core | `TimeFormatter` | 시간을 `00:00.00` 형식으로 바꿉니다. 반올림하지 않고 내림합니다. |
| Camera | `OrbitCameraController` | 분할 화면의 카메라 2대를 각자의 Pivot 기준으로 같은 각도·거리로 공전시킵니다. `Views`(카메라 + Pivot 쌍)를 씬에서 연결하고, 0번 카메라의 배치 위치가 시작 시점입니다. WASD 또는 우클릭 드래그로 회전하고 휠로 줌하며, Pitch는 -80 ~ 80입니다. `timeScale` 0이면 입력을 무시합니다. |
| Interaction | `ClickInteractor` | 좌클릭한 쪽 화면의 카메라(`Target Cameras` 중 커서가 Viewport 안에 있는 것)에서 Raycast를 쏴서 `AnswerTarget`이면 정답, 다른 물체면 오답으로 판정합니다. **빈 곳(Collider 없음) 클릭은 아무 반응도 하지 않습니다**(이벤트·로그 없음, 사용자 요청). 이벤트는 `OnAnswerClicked`, `OnWrongClicked`(맞은 Collider는 항상 null이 아님)입니다. 일시정지 중, 스테이지 클리어 후(`StageManager.IsCleared`), UI 위 클릭은 무시합니다. |
| Feedback | `ClickFeedback` | `Managers/ClickFeedback`에 붙입니다. 정답일 때 쌍둥이 둘의 머티리얼 복사본에 황금빛 Emission을 올렸다가(0.25초) 은은하게 남기고(0.6초 동안 `Glow Settle Intensity`까지), 기본 색도 금색 쪽으로 섞습니다(Emission 없는 셰이더 대비). 렌더러 윗면에 `Sparkle` 파티클을 만듭니다. **오답 흔들림은 보류 중**이라 `[보류]` 표시와 함께 `//` 주석으로 막혀 있고, 주석을 풀면 다시 동작합니다. |
| Feedback | `WrongClickMarker` | 스테이지 `Canvas`에 붙입니다. 오답 물체를 클릭하면 커서 위치에 `Prefabs/Wrong Marker`(빨간 TMP `X`, Raycast Target 끔)를 띄우고, 살짝 튀었다가 0.5초 안에 사라지게 합니다. |
| Stage | `AnswerTarget` / `AnswerPair` | 쌍둥이 2개를 묶어서 한쪽만 클릭해도 쌍 전체를 정답 처리합니다. 이벤트는 `OnFound`입니다. |
| Stage | `StageManager` | 씬의 `AnswerPair`를 자동으로 모읍니다. 진행도·클리어 이벤트를 보내고 `StageTimer`를 돌립니다. 클리어 후 1.5초 뒤 `GameManager.ReportStageClear`를 부릅니다. |
| Stage | `StageTimer` | 일반 C# 클래스로 된 스톱워치입니다. `Time.deltaTime`을 쓰므로 일시정지 중에는 멈춥니다. |
| UI | `StageHUD` | 스테이지 이름, 스톱워치(`<mspace>`로 숫자 폭 고정), `( 2 / 5 )` 진행도를 표시합니다. |
| UI | `PauseController` | Esc 키로 일시정지합니다(키 값은 프리팹에 저장된 `pauseKey`). 클리어 후에는 새로 일시정지하지 않습니다. 항상 켜져 있는 루트에 붙이고, 자식 Popup만 켜고 끕니다. `Prefabs/Pause Menu.prefab`에 들어 있습니다. |
| UI | `MainMenuUI` / `ResultUI` | 메뉴의 Start/Exit 버튼과, 결과 화면의 기록 목록·Total Time·패널 등장 연출입니다. 버튼 OnClick은 코드에서 연결합니다. |
| Sound | `SoundManager` | 사용자가 뼈대(필드·함수 이름)를 쓰고 Claude가 보완했습니다. `SingletonBase`를 상속하고, `Prefabs/SoundManager.prefab`을 **모든 씬의 `Managers` 아래**에 둡니다. 처음 것만 살아남고(이때 `SetParent(null)`로 루트로 빠진 뒤 DontDestroyOnLoad), 다음 씬의 복제본은 경고 없이 사라집니다. AudioSource 2개(BGM 반복 재생 / SFX)는 칸이 비어 있으면 자동으로 만듭니다. 같은 BGM이 재생 중이면 다시 틀지 않습니다. `timeScale` 0이면 BGM을 `pausedBgmVolumeRatio`(30%)로 부드럽게 줄입니다. `sceneLoaded`마다 그 씬의 `ClickInteractor`(정답·오답 SFX)와 모든 `Button`(꺼진 것 포함, 클릭 SFX)에 자동으로 연결합니다. 클립이 null이면 재생하지 않습니다. |
| Utils | `SimpleInput`, `SingletonBase` | `19. Legacy/9. Utils`에서 복사해 네임스페이스를 바꾼 파일입니다. |

**씬** (Build Settings 순서): `00. Menu` → `10. Stage Tutorial` → `11. Stage 1` → `12. Stage 2` → `13. Stage 3` → `90. Result`
- 스테이지 씬 4개는 같은 구조이고, `StageNameText`의 글자와 `StageManager`의 Stage Name만 다릅니다. 지금은 큐브로 된 테스트용 정답 쌍(`AnswerPair_01`: 왼쪽 섬 `Cube`, 오른쪽 섬 `Cube (2)`)과 섬마다 미끼 큐브 하나가 배치되어 있습니다.
- 분할 화면 구성: `Main Camera`는 Viewport X 0 / W 0.5, `Right Camera`는 X 0.5 / W 0.5이고, 둘 다 Far Clip 60입니다. `MainCamera` 태그와 Audio Listener는 `Main Camera`에만 있습니다. Canvas 첫 자식인 `Divider`(Width 4, 위아래 stretch, Raycast Target 끔)가 화면을 가르고, `TimerText` 바로 위의 `TimerBackground`가 분할선을 가립니다.
- **예비 씬**: `Scenes/Template/Stage Template.unity`. 스테이지 씬과 같은 구성이고 이름만 `New Stage`입니다. Build Settings와 `SceneNames`에는 넣지 않습니다. 스테이지를 늘릴 때는 다음 순서로 합니다.
  1. Project 창에서 Ctrl + D로 복제합니다(새 GUID가 생김). `Scenes`로 옮기고 이름을 `14. Stage 4`처럼 바꿉니다.
  2. `StageNameText`와 `StageManager`의 이름을 바꿉니다.
  3. Build Settings에서 마지막 스테이지 다음, `90. Result` 앞에 추가합니다.
  4. `SceneNames.Stages` 배열에 씬 이름을 추가합니다. 빠뜨리면 그 씬을 건너뛰고 Result로 넘어갑니다.
- 씬 구성을 한꺼번에 바꿀 때는 한 씬을 고친 뒤 `.unity` 파일 내용만 다른 씬에 복사하고 이름 2줄을 고치는 방법을 씁니다. `.meta`는 건드리지 않으므로 GUID가 유지됩니다.

## 진행 상황

| 단계 | 내용 | 상태 |
|---|---|---|
| 1 | 폴더, 씬 6개, Build Settings | ✅ |
| 2 | OrbitCameraController | ✅ |
| 3 | 클릭 판정 (ClickInteractor, AnswerTarget, AnswerPair, StageManager) | ✅ |
| 4 | StageTimer + StageHUD | ✅ |
| 5 | GameManager + 씬 흐름 (Menu → Stage 4개 → Result) | ✅ |
| 6 | PauseController + Pause Menu 프리팹 | ✅ |
| 6.5 | 좌우 분할 화면 전환(카메라 2대 동기화, 클릭 카메라 선택, 분할선), 일시정지 키 Esc, 클리어 후 입력 차단, 예비 씬 | ✅ |
| 7 | 시각 피드백: 정답 Emission(황금빛) + 반짝임 파티클, 오답 물체 클릭 시 커서 위치 붉은 X(0.5초). 빈 곳 클릭은 무반응, 오답 흔들림은 보류 | ✅ |
| 8 | SoundManager: BGM(씬을 넘어가도 이어짐, 일시정지 때 볼륨 낮춤), 정답·오답·버튼 SFX | ✅ 코드·배치 완료 / ⏳ 사운드 파일 대기 |
| 9 | 스테이지별 조명 연출: 형광등 깜빡임(10초마다, 1초 이내), 일출·일몰 Directional Light, 네온 Emission | **다음** |
| 10 | 실제 디오라마 제작 (Synty 등), 스테이지마다 정답 3~5쌍 배치, 마무리 | |

**7단계 메모 (완료)**
- 반짝임 파티클 `Prefabs/Sparkle`: Burst 25, Sphere 0.3, Start Speed 7~9, Lifetime 0.6~1, Limit Velocity Dampen 0.2, Start Size 0.2~0.4, Billboard, Rotation over Lifetime 180°/s, Size over Lifetime 1→0. **퍼지는 거리 ≈ Start Speed × Lifetime**(Dampen을 켜면 그보다 짧아짐)이고, 거리는 Start Speed로 조절합니다.
- 기본 파티클 텍스처는 가장자리가 부드러워서 흐릿하게 보입니다. 그래서 `Materials/Textures/T_SparkleStar.png`(PowerShell System.Drawing으로 만든 128px 4갈래 별, 흰색)와 `Materials/M_Sparkle.mat`(URP `ParticlesUnlit.mat`을 복사해 텍스처만 교체)를 씁니다. Stretched Billboard는 별 모양을 찌그러뜨리므로 쓰지 않습니다.
- 빌드에서는 쓰는 머티리얼이 없는 `_EMISSION` 셰이더 변형이 빠질 수 있습니다. 그래서 10단계에서 **정답 오브젝트 머티리얼은 Emission을 켜고 색을 검정으로** 둡니다. 그래도 빠지면 기본 색 섞기만 보입니다.

**8단계 메모**
- 일시정지 중 BGM은 **볼륨 낮추기**로 정했습니다(사용자 결정).
- **사운드 파일은 아직 없습니다.** 파일을 구하면 `Assets/100. 3D Project-1/Audio/`에 넣고, **`Prefabs/SoundManager.prefab`**(씬 인스턴스가 아님)의 `bgm`, `correctAnswerClip`, `wrongAnswerClip`, `buttonClickClip`에 연결합니다. 그러면 모든 씬에 한 번에 들어갑니다. 이때 `Audio.meta`도 같이 커밋합니다.
- 기획서 기준 소리: BGM은 잔잔한 Lofi나 어쿠스틱 루프, 정답은 맑은 실로폰·종소리, 오답은 불쾌하지 않은 '뾱'이나 '삐빅', 버튼은 깔끔한 클릭음입니다.

## 검토 중: 10스테이지 확장 (2026-10-22 제안, 아직 확정 아님)

사용자가 9단계에 들어가기 전에 스테이지를 4개에서 **10개**로 늘리는 방안을 제안했습니다. Claude가 가능하다고 검토했고, **사용자의 답을 기다리는 중**입니다. 확정되기 전에는 기획서와 진행 상황 표를 바꾸지 않습니다.

**제안 구성**: Tutorial / 1-1, 1-2, 1-3 / 2-1, 2-2, 2-3 / 3-1, 3-2, 3-3
- **월드 1**: 지금처럼 좌우(세로선) 분할입니다.
- **월드 2**: 화면을 가로(위아래)나 대각선으로 나눕니다.
  - 가로 분할은 카메라 Viewport만 바꾸면 되고, 지금 `ClickInteractor`(pixelRect 판정)가 그대로 동작합니다.
  - 대각선은 Viewport로 만들 수 없습니다. 두 카메라를 각각 화면 크기의 RenderTexture에 그리고, UI(RawImage)에서 한쪽을 삼각형으로 가려 겹칩니다. 클릭은 커서가 대각선의 어느 쪽인지로 카메라를 고른 뒤 `ScreenPointToRay`를 씁니다(RT가 화면 크기와 같으면 좌표가 그대로 맞음).
- **월드 3**: 화면은 반으로 나눈 채, 카메라는 고정하고 **각 화면 가운데의 물체를 같은 각도로 돌립니다**(오빗의 반대). 조명이 고정이라 돌릴 때 음영이 바뀝니다. 섬 대신 물체 하나라 콘텐츠 제작이 빠릅니다.

**Claude의 검토 결과**
- 시스템은 약 3~4일입니다(10씬 흐름과 결과 화면 10줄 반나절, 가로 분할 30분, 대각선 1~1.5일, 물체 회전 반나절~1일, 조명 연출 1일).
- 콘텐츠는 스테이지당 약 3~4시간으로, 9개에 약 4~5일입니다. 같은 월드 안에서 바탕 디오라마를 재사용하면 줄일 수 있습니다.
- UI·사운드·빌드·버그 수정에 2~3일이 들어서, 합계는 약 9~12일입니다. 하루 종일 작업하면 2주 안에 되지만, 수업과 병행하면 빠듯합니다.
- **추천 순서**:
  1. 1차: Tutorial + **1-1, 2-1, 3-1** (지금 Stage 1·2·3을 바꾸고 메커니즘 3개를 모두 완성)
  2. 2차: 1-2, 2-2, 3-2
  3. 3차: 1-3, 2-3, 3-3

  어느 단계에서 멈춰도 완성된 게임으로 제출할 수 있습니다.
- 결과 화면은 기록이 10줄이 되므로 두 줄로 나누거나 월드별 소계로 보여 주게 바꿔야 합니다.

**사용자에게 확인할 것**
1. 월드 2의 분할 방식을 정합니다(예: 2-1 가로, 2-2 대각선↘, 2-3 대각선↙ 또는 대각선 하나로 통일).
2. 월드 3의 회전 방식을 정합니다. 위아래까지 완전히 뒤집을 수 있게 할지, ±80도로 제한할지, 입력은 WASD·우클릭 드래그·휠 줌을 그대로 쓸지입니다.
3. 9단계 조명 연출을 월드와 어떻게 짝지을지 정합니다(예: 월드 1 형광등, 월드 2 일출·일몰, 월드 3 네온).

확정되면 기획서(2.1 카메라, 3.1 스테이지 구조)와 이 파일의 진행 상황 표를 고친 뒤 작업을 시작합니다.

**나중에 할 일**
- Play 모드 자동 테스트를 만듭니다(`Assets/100. 3D Project-1/Tests/PlayMode/` + asmdef). 확인할 흐름: Menu → Start → 스테이지 4개 정답 처리 → Result 기록 4개와 Total 합계 → 메인 메뉴 → 재시작 시 기록 초기화, 오답·중복 클릭은 무시. 사용자가 집 컴퓨터에서 확인을 마친 뒤 진행하기로 했습니다. CliBridge에 `/tests/run` 엔드포인트를 추가해 Claude가 직접 실행하는 방식을 제안해 둔 상태입니다.
- 디오라마 아래쪽에도 문제를 둘 예정입니다. 섬 바닥은 두께가 있는 메시로 만들고(Plane은 아래에서 투명하게 보임), 아랫면이 어둡지 않게 조명을 보강합니다.
- 지형에 Collider가 생기면 `Terrain` 레이어로 옮기고 `ClickInteractor`의 Clickable Layers에서 뺍니다. 그래야 지형 클릭도 빈 곳처럼 무반응이 되고 X가 뜨지 않습니다.

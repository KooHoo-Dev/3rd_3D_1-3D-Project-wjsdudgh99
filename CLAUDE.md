# CLAUDE.md

이 저장소는 Unity 3D 코스 프로젝트입니다. 지금 진행 중인 메인 작업은 **'You're Right' 게임**입니다.
사용자는 학원 컴퓨터와 집 컴퓨터를 오가며 작업하므로, 작업 규칙과 진행 상황은 이 파일에 기록합니다.

## 프로젝트 개요

- **게임**: 'You're Right'. 좌우 분할 화면에 나란히 보이는 두 3D 디오라마를 오빗 카메라로 함께 돌려 보며 **완전히 똑같은 쌍둥이 오브젝트**를 찾는 관찰형 퍼즐 게임입니다. 스테이지는 튜토리얼 1개와 월드 3개 × 2스테이지(모두 필수, 시간이 모자라면 뒤에서부터 뺌)이고, 스톱워치로 기록을 잽니다.
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

### 6. 기획서

- 기획서는 제작하면서 고쳐도 됩니다. 원본은 사용자가 따로 보관하고 있고, git 커밋 `fa39065`에도 있습니다.
- 기획을 바꾸면 본문을 고치고, 기획서 맨 아래 **"6. 기획 변경 이력"** 표에 날짜·바뀐 내용·원래 기획·이유를 한 줄 추가합니다.
- 원본과 비교할 때는 `git diff fa39065 -- "Assets/01. Docs/3D 프로젝트 기획서 - 1 수정본.md"`를 씁니다.

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
| Core | `GameManager` | `SingletonBase`를 상속하고 DontDestroyOnLoad로 유지됩니다. 씬에 배치하지 않아도 처음 호출될 때 자동으로 만들어집니다. 스테이지별 기록을 모으고, 다음 씬 이동·메인 메뉴·종료를 맡습니다. 씬을 바꿀 때 `timeScale`을 1로 되돌리고 `[GameManager] 씬 이동: A → B (기록 N개)` 로그를 남깁니다(Editor.log로 흐름 확인용. Unity의 `Loaded scene` 줄은 Menu로 돌아갈 때 남지 않는 경우가 있었음). |
| Core | `SceneNames` | Build Settings에 등록된 씬 이름 상수입니다. 씬 파일 이름을 바꾸면 여기도 고칩니다. |
| Core | `TimeFormatter` | 시간을 `00:00.00` 형식으로 바꿉니다. 반올림하지 않고 내림합니다. |
| Camera | `OrbitCameraController` | 분할 화면의 카메라 2대를 각자의 Pivot 기준으로 같은 각도·거리로 공전시킵니다. `Views`(카메라 + Pivot 쌍)를 씬에서 연결하고, 0번 카메라의 배치 위치가 시작 시점입니다. **우클릭 드래그로 회전**, 휠로 줌하며, Pitch는 -80 ~ 80입니다. **WASD는 이동**(2026-10-27 변경, 전에는 회전): 현재 Yaw 기준 앞뒤좌우로 바닥과 평행하게 `Move Speed`(10/초, 사용자 결정)만큼 움직이고, Pivot을 옮기지 않고 모든 Pivot에 같은 이동량을 더해 두 카메라가 똑같이 움직입니다(검사 도구가 Pivot을 섬 중심으로 쓰기 때문). 이동 범위는 0번 Pivot에서 가장 가까운 `PlayArea`의 XZ 크기 안(없으면 `Move Limit` 10)이고, Awake에서 한 번 계산합니다. `timeScale` 0이면 입력을 무시합니다. 튜토리얼 안내 2페이지(조작 방법, `Prefabs/Tutorial Guide.prefab`) 글자도 이동/회전을 나눠 고쳐야 합니다. **Q 하강 / E 상승**(2026-10-28, 사용자 결정): `Vertical Move`로 끌 수 있고 키는 `Down Key`/`Up Key`, 범위는 PlayArea 높이(없으면 ±`Move Limit`). 튜토리얼 안내 2페이지에 "Q / E: 아래 / 위로 이동" 한 줄을 사용자가 추가해야 합니다. **카메라 충돌**(2026-10-28): `Block Layers`(비우면 끔, 지금은 2-2만 사용) + `Collision Radius` 0.3. Pivot에서 카메라 쪽으로 SphereCast를 쏴서 막히면 그 앞까지 바로 당기고(부드럽게 따라가면 몇 프레임 속이 보임), 두 화면 중 더 짧은 거리를 같이 써서 시점을 맞춥니다. 휠로 정한 거리는 기억해서 벗어나면 돌아갑니다. Pivot 이동은 축마다 CheckSphere로 막는 물체 안에 들어가지 못하게 합니다(벽을 따라 미끄러짐). **처음 Pivot 자리가 막는 Collider와 겹치면 아예 움직이지 못하니** 그때는 양쪽 Pivot을 같은 값만큼 옮깁니다. |
| Camera | `HalfScreenProjection` | 월드 3용(2026-10-31). 두 카메라에 하나씩 붙이고 `Side`(Left/Right)만 다르게 둡니다. Viewport Rect를 반쪽(X 0 또는 0.5, W 0.5)으로 맞추고, 매 프레임 **화면 전체 기준 시야**(세로 Field of View 그대로, 가로는 반쪽 2개 폭)를 만들어 자기 쪽 절반만 남기는 off-center 투영(`Matrix4x4.Frustum`)을 넣습니다. 그래서 Pivot의 물체가 각 반쪽 가운데가 아니라 화면 정중앙(분할선)에 그려져 왼쪽 반은 버전 A, 오른쪽 반은 버전 B로 이어져 보입니다. 에디터에서도 동작하고(`ExecuteAlways`), 끄면 투영을 원래대로 되돌립니다. Rect는 값이 다를 때만 바꿉니다(씬 저장 안 됨 표시 방지). |
| Interaction | `ClickInteractor` | 좌클릭한 쪽 화면의 카메라(`Target Cameras` 중 커서가 Viewport 안에 있는 것)에서 Raycast를 쏴서 `AnswerTarget`이면 정답, 다른 물체면 오답으로 판정합니다. **빈 곳(Collider 없음) 클릭은 아무 반응도 하지 않습니다**(이벤트·로그 없음, 사용자 요청). 이벤트는 `OnAnswerClicked`, `OnWrongClicked`(맞은 Collider는 항상 null이 아님)입니다. 일시정지 중, 스테이지 클리어 후(`StageManager.IsCleared`), UI 위 클릭은 무시합니다. **무반응 레이어(`Silent Layers`, 비워 두면 `Background` 레이어 자동 사용)**에 맞으면 Raycast는 막히되(뒤쪽 물체가 대신 맞지 않음) 정답·오답 모두 처리하지 않습니다. 정답 오브젝트라도 Background에 있으면 반응하지 않습니다. 씬에 `PlayArea`가 있으면 맞은 지점(`hit.point`)이 어느 PlayArea에도 없을 때도 무반응입니다(판정 순서: PlayArea → 무반응 레이어 → 정답/오답). `Clickable Layers`는 보통 Everything이고, 여기서 뺀 레이어는 클릭이 뚫고 지나갑니다. **오답 클릭 제한**: 오답(X가 뜨는 클릭) 뒤 `Wrong Click Lock Time`(3초, 0이면 끔) 동안 좌클릭을 정답·오답 모두 무시합니다(`IsLocked`, `LockRemaining`, `LockDuration`). `Time.deltaTime`으로 줄어서 일시정지 중에는 멈추고, 카메라 조작은 막지 않습니다. |
| Feedback | `ClickFeedback` | `Managers/ClickFeedback`에 붙입니다. 정답일 때 쌍둥이 둘의 머티리얼 복사본에 황금빛 Emission을 올렸다가(0.25초) 은은하게 남기고(0.6초 동안 `Glow Settle Intensity`까지), 기본 색도 금색 쪽으로 섞습니다(Emission 없는 셰이더 대비). 렌더러 윗면에 `Sparkle` 파티클을 만듭니다. **오답 흔들림은 보류 중**이라 `[보류]` 표시와 함께 `//` 주석으로 막혀 있고, 주석을 풀면 다시 동작합니다. |
| Feedback | `WrongClickMarker` | 스테이지 `Canvas`에 붙입니다. 오답 물체를 클릭하면 커서 위치에 `Prefabs/Wrong Marker`(빨간 TMP `X`, Raycast Target 끔)를 띄우고, 살짝 튀었다가 0.5초 안에 사라지게 합니다. |
| Feedback | `ClickLockIndicator` | 오답 클릭 제한 중에 커서를 숨기고 빨간 원(`Materials/Textures/T_LockRing.png`, 흰 링을 Image 색으로 빨갛게)을 커서 위치에 띄웁니다. 원은 코드에서 Filled·Radial360·Top·반시계로 맞추고 `fillAmount`를 남은 비율로 줄여서, 12시에서 출발한 경계가 시계 방향으로 돌며 원을 지웁니다. 선택 항목인 `Hand`(반지름 바늘, Pivot (0.5, 0))도 같이 돕니다. 일시정지 중에는 원을 숨기고 커서를 보여 줍니다. 항상 켜진 루트에 붙이고 `Visual` 자식만 켜고 끕니다. Canvas 맨 아래에 둡니다. |
| Stage | `AnswerTarget` / `AnswerPair` | 쌍둥이 2개를 묶어서 한쪽만 클릭해도 쌍 전체를 정답 처리합니다. 이벤트는 `OnFound`입니다. AnswerTarget은 Collider가 자신이나 자식에 있으면 됩니다(2026-10-29, RequireComponent 뺌: 거울처럼 부품마다 Collider를 붙인 물체도 정답으로 쓰기 위함. 클릭은 맞은 Collider의 부모에서 AnswerTarget을 찾음). |
| Stage | `StageManager` | 씬의 `AnswerPair`를 자동으로 모읍니다. 진행도·클리어 이벤트를 보내고 `StageTimer`를 돌립니다. 씬에 `TutorialGuide`가 있으면 안내를 닫는 순간(`OnClosed`) 타이머를 켜고, 없으면 `Start`에서 켭니다. 켠 뒤 **첫 번째 `Update`는 건너뜁니다**(로딩 직후 큰 deltaTime 제외. 에디터 첫 Play에서는 Start와 첫 Update가 다른 프레임일 수 있어 프레임 번호 비교로는 막지 못했음). 클리어하면 `OnStageCleared`를 보내고 기다립니다. 클리어 창의 Next가 `ProceedToNext()`(한 번만 동작)로 `GameManager.ReportStageClear`를 부릅니다. 씬에 `StageClearPanel`이 없으면 경고를 남기고 `Next Stage Delay`(1.5초) 뒤 자동으로 넘어갑니다. `Add To Records`를 끄면(튜토리얼) 시간은 HUD에만 보이고 결과 기록·Total에는 들어가지 않습니다(`GameManager`가 "기록 제외" 로그만 남김). |
| Stage | `PlayArea` | 섬마다 하나씩 두는 보이지 않는 구역 상자입니다(`Stage/PlayArea Left`·`PlayArea Right`, Pivot과 같은 x ±100). 물리 Collider 없이 `Contains(점)`으로 계산하며, Transform의 위치·회전·스케일을 따릅니다. `Center`(로컬)·`Size`(기본 20×15×20)를 쓰고, **`Mirror Of`를 지정하면 그 상자의 Center/Size를 그대로 따라갑니다**(오른쪽 → 왼쪽 지정, 한 단계만). 빨간 선 기즈모는 항상 보이고, 선택하면 반투명 면도 칠합니다. 씬에 PlayArea가 하나도 없으면 모든 곳이 반응합니다. |
| Stage | `StageTimer` | 일반 C# 클래스로 된 스톱워치입니다. `Time.deltaTime`을 쓰므로 일시정지 중에는 멈춥니다. |
| UI | `StageHUD` | 스테이지 이름, 스톱워치(`<mspace>`로 숫자 폭 고정), `( 2 / 5 )` 진행도를 표시합니다. |
| UI | `StageClearPanel` | 클리어 창입니다. `OnStageCleared` 후 `Appear Delay`(1초) 뒤 popup이 커지며 나타나고(ResultUI와 같은 EaseOut), 이번 기록을 보여 줍니다. Next는 `StageManager.ProceedToNext()`, Main Menu는 `GameManager.LoadMainMenu()`입니다. 다음이 결과 화면이면(`GameManager.HasNextStage` false) Next 글자를 `Last Stage Next Text`(기본 `Results`, 씬에서 한국어로 바꿀 수 있음)로 바꿉니다. 1-1의 창 글자는 한국어("스테이지 클리어!", "다음 스테이지")입니다. 창이 떠 있어도 카메라는 돌릴 수 있습니다. 항상 켜진 루트에 붙이고 popup(CanvasGroup) 자식만 켜고 끕니다. |
| UI | `PauseController` | Esc 키로 일시정지합니다(키 값은 프리팹에 저장된 `pauseKey`). 클리어 후와 `TutorialGuide`가 열려 있는 동안에는 새로 일시정지하지 않습니다. 항상 켜져 있는 루트에 붙이고, 자식 Popup만 켜고 끕니다. `Prefabs/Pause Menu.prefab`에 들어 있습니다. |
| UI | `TutorialGuide` | 스테이지 시작 전 안내 패널입니다(`Prefabs/Tutorial Guide.prefab`, 튜토리얼 Canvas 맨 아래). `Start`에서 열리며 `timeScale` 0으로 카메라·클릭을 멈추고, `pages` 배열을 이전/다음 버튼으로 넘깁니다. 버튼은 페이지 바깥(Window 바로 아래)에 한 벌만 둡니다(페이지 안에 넣으면 페이지를 끌 때 같이 사라짐). 마지막 페이지의 시작 버튼을 누르면 패널을 끄고 `timeScale`을 1로 되돌린 뒤 `OnClosed`를 보냅니다. 페이지 글자는 씬에서 직접 씁니다(한국어, Noto Sans KR). 패널 오브젝트는 씬 시작 때 켜져 있어야 합니다. 안내 중에는 SoundManager가 BGM을 30%로 낮춥니다(그대로 두기로 함). |
| UI | `MainMenuUI` / `ResultUI` | 메뉴의 Start/Exit 버튼과, 결과 화면의 기록 목록·Total Time·패널 등장 연출입니다. 버튼 OnClick은 코드에서 연결합니다. |
| Sound | `SoundManager` | 사용자가 뼈대(필드·함수 이름)를 쓰고 Claude가 보완했습니다. `SingletonBase`를 상속하고, `Prefabs/SoundManager.prefab`을 **모든 씬의 `Managers` 아래**에 둡니다. 처음 것만 살아남고(이때 `SetParent(null)`로 루트로 빠진 뒤 DontDestroyOnLoad), 다음 씬의 복제본은 경고 없이 사라집니다. AudioSource 2개(BGM 반복 재생 / SFX)는 칸이 비어 있으면 자동으로 만듭니다. 같은 BGM이 재생 중이면 다시 틀지 않습니다. `timeScale` 0이면 BGM을 `pausedBgmVolumeRatio`(30%)로 부드럽게 줄입니다. `sceneLoaded`마다 그 씬의 `ClickInteractor`(정답·오답 SFX)와 모든 `Button`(꺼진 것 포함, 클릭 SFX)에 자동으로 연결합니다. 클립이 null이면 재생하지 않습니다. |
| Editor | `AnswerPairCreator` | 에디터 전용(빌드 제외). **Tools → You're Right → Create Answer Pair**: 왼쪽·오른쪽 섬 물체를 하나씩 선택해 실행하면, 두 루트에 Collider와 `AnswerTarget`을 붙이고 `Props/AnswerPair_NN`(가장 큰 번호 + 1)을 만들어 연결합니다(PlayArea 기준 왼쪽이 A). Collider는 루트에 이미 있거나 자식에 클릭되는 Collider가 있으면 그대로 두고(Trigger·꺼짐이면 고침), 없으면 루트 메시가 있을 때 MeshCollider, 메시가 자식에만 있으면 자식 렌더러 전체를 감싸는 BoxCollider를 붙입니다. 2개가 아님·프리팹 에셋·Play 모드·같은 섬·PlayArea 밖·이미 등록(부모·자식 포함)·Background 레이어(자식 Collider 포함)·`Props` 없음이면 거부합니다. 메시·머티리얼 구성 차이와 Emission 꺼짐은 경고만 합니다. Ctrl + Z 한 번으로 전부 되돌립니다. |
| Editor | `StageValidator` | 에디터 전용(빌드 제외). **Tools → You're Right → Validate Stage**: 열린 씬을 읽기만 해서 검사하고 Console(❌ 오류 / ⚠ 경고 / ℹ 참고)과 요약 창으로 알려 줍니다. 섬 중심은 `OrbitCameraController` Views의 Pivot 2개(없으면 이름)이고, 위치는 Pivot 기준 상대 위치, 회전은 월드 회전으로 비교합니다(돌려 둔 `Props/Right`도 화면 그대로 비교). 단위는 프리팹 루트, 오차는 위치 0.01·회전 1°·크기 1%. 검사: Pivot·Props, 정답 쌍 개수(0 오류, 3~5 밖 경고), 쌍 연결·같은 섬·쌍 없는 AnswerTarget, 정답 Collider(자신·자식 포함, 없음·꺼짐·Trigger·Mesh가 빈 MeshCollider)·Background 레이어·PlayArea 밖, **쌍둥이 동일성**(위치·크기·메시/머티리얼/자식 구성·회전), 정답 머티리얼 Emission, **의도하지 않은 쌍둥이**(정답이 아닌데 똑같아 보임 → 오류), **함정 후보**(같은 물체가 0.5 안에서 조금만 다름 → 참고), `Stage` 아래 Background가 아닌 Collider, 두 카메라 설정, 섬마다 둔 Point/Spot 조명 대칭. 회전만 다르면 **방향별 광선**으로 **회전 대칭**을 판단합니다: 루트 기준점에서 4096방향(피보나치 구)으로 광선을 쏴서 가장 바깥 면까지의 거리와 그 자리 텍스처 색(서브메시별 머티리얼의 `mainTexture`, UV 보간, Tiling·Offset 반영)을 구하고, 모든 방향 d에서 d와 Δd(Δ = 두 물체의 회전 차이)의 결과가 같으면(거리 크기의 1%, 색 채널 0.05) 똑같아 보인다고 봅니다. 메시 삼각형 나눔과 상관없이 겉모습만으로 판단하기 위한 방식이고, 꼭짓점 위치·꼭짓점 UV·꼭짓점 색·면 안쪽 격자 점 방식은 1-1 폰(진짜 대칭)과 튜토리얼 8번 당구공(반대편 숫자 크기가 다름)을 동시에 맞히지 못해서 버렸습니다(2026-10-26). Read/Write 꺼진 FBX 메시는 `Mesh.AcquireReadOnlyMeshData`로, 텍스처는 `Graphics.Blit` 임시 복사본으로 읽고(검사 끝에 지움), 메시를 못 읽으면 "판단 불가"입니다. 삼각형이 1만 개 넘는 물체는 몇 초 걸릴 수 있습니다. 두 카메라의 **배경색 차이는 참고(ℹ)**로만 알립니다(1-2처럼 테마마다 일부러 다르게 둘 수 있음, 2026-10-27). Background Type 차이는 그대로 경고입니다. |
| Editor | `StationKitBuilder` | 에디터 전용. **Tools → You're Right → Build Station Kit**: 2-2 기차역용 스타일라이즈드 키트(Cube 조각 + 단색 URP Lit)를 `Materials/Station/`(`M_Station_*`)과 `Prefabs/Station/`에 만듭니다(다시 실행하면 덮어씀, 확인 창). 프리팹: `Platform Modern 4m`·`Platform Rural 4m`(윗면 1.2, 폭 4.8, Background 레이어 + 루트 BoxCollider. 2026-10-29: 몸체 아래에 선로 바닥 밑면까지 채우는 `Foundation`(y -1.85 ~ 0, 가장자리 처마 0.25~0.3 안쪽부터)을 붙이고 Collider도 -1.85 ~ 1.2로 늘림), `Screen Door Gate`(길이 2.4, 문짝 2장은 따로 움직일 수 있음)·`Screen Door Panel`(길이 1), `Fence Modern 2m`·`Fence Rural 2m`(뒤쪽 가장자리, 기둥은 로컬 z -1 끝에만), `Fence Modern Post`·`Fence Rural Post`(기둥만, 기준점은 울타리 한 칸과 같음. 줄 끝은 마지막 칸과 같은 위치에 Y 180으로 놓음, 2026-10-28). 스크린도어는 양쪽 하우징 `Housing Front/Back/Body`에만 MeshCollider(Default, 클릭하면 오답, 2026-10-29 사용자 결정. 전에는 씬에서 Back에만 붙였다가 재실행으로 사라짐), 문짝·유리 패널·울타리는 Collider 없음(무반응, 클릭 통과). 플랫폼 기준점은 가장자리 선·바닥 y 0·길이 가운데(몸체는 로컬 -X, 가장자리는 +X 선로 쪽)이고, 왼쪽 플랫폼은 회전 0·x = 선로 중심 - 1.45, 오른쪽은 회전 180·x = 선로 중심 + 1.45. 문·울타리 기준점은 자기 밑면 가운데이고, 플랫폼과 같은 회전·y 1.2에 둡니다(문은 가장자리에서 0.5, 울타리는 4.65 안쪽). |
| Editor | `StationPropsBuilder` | 에디터 전용(2026-10-28). **Tools → You're Right → Build Station Props**: 기차역 소품 15종을 Cube·Cylinder·Sphere 조각 + 단색 URP Lit(`Materials/Station/M_Prop_*` 26개, 쓰지 않는 색도 변형용으로 만듦)으로 조립해 `Prefabs/Station/Props/`에 저장합니다(다시 실행하면 덮어씀, 지운 소품의 예전 프리팹은 남으니 직접 지움). 소품: `Bench Modern`·`Bench Rural`, `Trash Bin Modern`(캔·병·종이 칸 묶음)·`Trash Bin Rural`, `LED Departure Board`(주황·초록 LED, 7세그먼트 시각 + 한자 모양 행선지(두 줄 모두 역명판 Modern과 같은 `日山口`) + 량수(두 줄 모두 3), 1줄 행선지와 량수 사이에 빨간 `急行` 종별 `Row 1/Type`, 앞뒤 양면 `Front`/`Back`(뒷면은 Y 180° 그룹에 다시 만듦, 면마다 따로 고칠 수 있음), 2026-10-29), `Digital Clock Hanging`(흰 LED "10:42", 앞뒤 양면: 뒷면은 같은 글자를 Y 180° 그룹에 다시 만듦), `Station Name Sign Modern`·`Rural`, `Vending Machine`(음료 3줄 묶음), `Ticket Gate`(자동 개찰구 3통로, 2026-10-29: 통 4개 `Cabinet 1~4`(길이 1.4, 높이 1, 폭 0.22) + 통로 0.6 × 3, 전체 폭 2.68. 사람은 X로 지나가므로 플랫폼 길이 방향으로 지나가게 하려면 Y 90°. 통마다 파란 IC 단말(`M_Prop_LED_Blue`)·표 넣는/나오는 곳·양 끝 표시등(+X 초록, -X 빨강)·옆 파란 띠, 통로마다 닫힌 주황 문짝 `Lane N Flaps`(끄면 열린 상태). Collider는 부품마다), `Platform Mirror`, `Stop Position Marker`(숫자 "6"), `Exit Sign Hanging`(위쪽 화살표 + "出口" 모양), `Fire Extinguisher Box`, `Post Box Rural`. 기준점은 밑면 가운데·앞면 +X(앞에서 볼 때 오른쪽 +Z)이고, **매다는 소품 3종(Exit Sign, LED Board, Digital Clock)은 맨 위 매다는 점이 기준점**. **모든 소품은 루트에 Collider 없이 부품마다 MeshCollider**(Default 레이어, 클릭하면 오답, 2026-10-29 확정). 루트에 상자를 씌우면 상자 겉면에 먼저 맞아서 자판기 버튼처럼 부품 하나를 정답으로 둘 수 없고, 루트를 MeshCollider로 바꾸면 루트에 메시가 없어 Mesh가 빈 채로 아무것도 클릭되지 않습니다. 소품 통째로 정답일 때는 맞은 부품의 부모에서 AnswerTarget을 찾습니다. 머티리얼은 모두 Emission 켬(검정, LED·자판기 창은 빛나는 색). **Emission을 코드로 켤 때 `globalIlluminationFlags`를 `EmissiveIsBlack`으로 두면 URP가 저장할 때 `_EMISSION`을 꺼 버리므로 검정이어도 `RealtimeEmissive`로 둡니다.** 글자는 진짜 글자가 아니라 막대 조합(K 口, H 日, E 工, J 十, M 山, Q 急, G 行)과 7세그먼트 숫자이고, 숫자·글자 조각을 켜고 끄거나 바꿔 미끼를 만듭니다. |
| Editor | `StationPropColliderFixer` | 에디터 전용(2026-10-29). **Tools → You're Right → Fix Station Prop Colliders**: `Prefabs/Station/Props`의 모든 프리팹(`Clock Pole` 제외)을 제자리에서 열어 루트 Collider를 지우고, 메시가 있는 부품 중 Collider가 없는 것마다 MeshCollider를 붙입니다. Build Station Props를 다시 실행하지 않고 Collider만 고치므로 손으로 고친 프리팹 모양과 씬 연결(오브젝트 ID)이 그대로입니다. 여러 번 실행해도 결과가 같습니다. |
| Editor | `PivotUtility` | 에디터 전용 도우미(2026-10-29). `CenterGroupPivots(root)`: 코드로 조립한 프리팹 안의 **그룹(렌더러 없이 자식만 있는 오브젝트)의 기준점을 자기 밑면 가운데**로 옮깁니다(자식 월드 위치는 그대로, 돌려 둔 그룹은 자기 축 기준). 그룹을 원점에 만들고 조각을 붙이면 기준점이 프리팹 가운데에 남아 통 하나만 돌려도 프리팹 가운데를 중심으로 돌았기 때문입니다. 루트 기준점(밑면 가운데·매다는 점·플랫폼 가장자리)과 조각(Cube 등, 자기 가운데)은 그대로입니다. `StationKitBuilder`·`StationPropsBuilder`가 저장 직전에 부르고, **앞으로 만드는 조립 도구도 저장 직전에 반드시 부릅니다**(사용자 요청). |
| Editor | `PrefabSaveUtility` | 에디터 전용 도우미(2026-10-29). `SaveKeepingIds(source, path)`: 이미 있는 프리팹을 덮어쓰지 않고 열어서, 이름 경로가 같은 오브젝트는 그대로 두고 값(Transform·컴포넌트)만 바꾸고 없는 것만 만들고 남는 것만 지웁니다(같은 부모 아래 같은 이름은 순서대로 짝지음). `SaveAsPrefabAsset`으로 덮어쓰면 자식의 내부 ID가 일부 새로 바뀌어 **씬에서 그 자식에 붙인 컴포넌트·고친 값이 사라지기 때문**입니다(2026-10-29: 스크린도어 24개의 `Housing Back/Body`에 씬에서 붙인 MeshCollider가 Build Station Kit 재실행으로 모두 사라짐). 두 조립 도구가 이걸로 저장하고, **앞으로 만드는 조립 도구도 이걸로 저장합니다.** 부품 이름을 바꾸면 그 부품은 새 ID가 됩니다. |
| Editor | `RailTerminalBuilder` | 에디터 전용(2026-10-29). **Tools → You're Right → Build Terminal Rail**: Polyeler `Straight rail`(메시 좌표 z 0 ~ -101.76, 복선, 선로 중심 x ±2.375, 레일 윗면 y 0.554, 바닥 y -1.832 ~ 0.16·폭 ±7.02, 전차선 기둥 z -90.85·-69.97·-49.47·-29.69·-11.66)을 평면으로 잘라 **두단식(막다른) 선로** `Prefabs/Station/Straight rail Terminal`을 만듭니다. -101.76 끝부터 기둥 2개까지만 남기고, 선로·바닥은 z -67.4, 전차선은 -69.5(두 번째 기둥 바로 뒤)에서 자릅니다(UV 보간, 전차선은 원본 메시 그대로라 위치가 같음). 잘린 바닥 끝에 뚜껑, 선로마다 차막이(`Buffer Stop Left/Right`: 레일 위 기둥 + 뒤 버팀대 + 노랑·검정 가로대 + 빨간 등, `M_Prop_*` 머티리얼이라 Build Station Props를 먼저 실행해야 함)를 붙입니다. 잘린 메시는 `Prefabs/Station/Meshes/Rail Terminal *.asset`(다시 실행하면 내용만 바꿔 GUID 유지). **기준점은 원본과 같은 자리**라 씬의 `Straight rail (1)`과 같은 Transform(그룹 아래 z 5.08, Scale 0.05 × 0.5 × 0.05)에 두면 선로 끝이 월드 z 34.2(플랫폼 끝)입니다. Collider 없음. |
| Editor | `FloreswaMaterialExtractor` | 에디터 전용(2026-10-30, 2-1). **Tools → You're Right → Extract Floreswa Materials**: Floreswa 세단 FBX 안의 읽기 전용 부위 머티리얼(Body: car·tail light·window·headlight·underbody·fog·grille·side mirror·exhaust, 바퀴: tire·rim·gap)을 `Assets/Floreswa/Materials/Parts/*.mat`으로 꺼냅니다. Unity의 Extract Materials를 그냥 쓰면 **FBX 안 머티리얼을 직접 가리키는 Floreswa 프리팹 8개가 분홍색(Missing)이 되므로**, 프리팹·씬의 참조를 새 .mat으로 바꾼 뒤 두 FBX(`free_sedan`·`free_sedan_low`, 같은 부위는 같은 파일을 함께 씀)를 Remap합니다. Parts와 `Floreswa/Materials`의 색 머티리얼 16개 모두 Emission(검정, RealtimeEmissive)을 켭니다. 저장 안 한 씬이 있으면 멈추고, 끝나면 열린 씬을 다시 열고 Body·바퀴 칸 순서를 창에 보여 줍니다. 다시 실행해도 안전합니다. |
| Editor | `LowPolyShaderConverter` | 에디터 전용(2026-10-30, 2-1). **Tools → You're Right → Convert LowPoly Shaders to URP**: BrokenVector `LowPolyCarPack`의 자체 셰이더(`LowPolyShaders/LowPolyPBRShader`·`LowPolyUnlitShader`, Built-in Surface Shader라 URP에서 분홍색)를 쓰는 머티리얼을 프로젝트 전체에서 찾아 URP Lit / URP Unlit으로 바꿉니다(팔레트 `_MainTex` → `_BaseMap`, Tint → `_BaseColor`, Smoothness·Metallic, Lit은 Emission 검정). Render Pipeline Converter와 Convert Selected Built-in Materials는 Standard 같은 기본 셰이더만 바꿔서 이 머티리얼들을 건너뜁니다. 다시 실행해도 안전합니다. **Render Pipeline Converter는 한 번 실행하면 결과를 기억해 다음 에셋에서 Convert 버튼이 꺼지므로**, 새 에셋은 머티리얼을 선택하고 Edit → Rendering → Materials → Convert Selected Built-in Materials to URP를 씁니다. |
| Editor | `ShowroomKitBuilder` | 에디터 전용(2026-10-30, 2-1). **Tools → You're Right → Build Showroom Kit**: 자동차 전시장 키트를 `Materials/Showroom/`(`M_Showroom_*`, 모두 Emission 켬)과 `Prefabs/Showroom/`에 만듭니다(다시 실행하면 덮어씀). 바닥 윗면 y 0, 차는 X로 한 줄, 카메라는 -Z, 벽은 +Z 뒤. 프리팹: `Showroom Floor 4x4`(두께 0.5, 기준점 윗면 가운데, 앞·왼쪽 줄눈), `Showroom Floor Edge 4m`(바닥 가장자리 마감 + 파란 빛줄, 기준점 가장자리 선·윗면, 몸체 -Z 0.15, 네 변에 두름: 앞 0·뒤 180·왼쪽 90·오른쪽 -90), `Showroom Floor Edge Corner`(모서리 0.15 × 0.15 조각, 기준점 바닥 모서리 점, 앞 왼쪽 0·앞 오른쪽 -90·뒤 오른쪽 180·뒤 왼쪽 90), `Showroom Wall 4m`(높이 6, 가운데 흰 세로 빛줄기. **2-1에서는 쓰지 않음**: 360도 도는 카메라를 가려서 지움, 2026-10-30 사용자 결정), `Car Pedestal`(지름 5.6, 윗면 0.12, 밑동 빛 테두리), `Info Stand`(기둥 + 25° 기운 파란 화면, 글자 줄 3개 `Head/Lines/Line 1~3`, 미끼용). 바닥·가장자리·벽·받침대는 Background 레이어 + Collider(무반응), 안내판은 부품마다 MeshCollider(Default). `PivotUtility`·`PrefabSaveUtility` 사용. |
| Editor | `VehicleColliderFixer` | 에디터 전용(2026-10-30, 2-1). **Tools → You're Right → Fix Vehicle Colliders**: 차 프리팹(`Floreswa/Prefabs`, `BrokenVector/LowPolyCarPack/Prefabs`, `ARCADE - FREE Racing Car/Prefabs (Meshes Only)`, `LowPolyVehicle1/Prefabs`, 데모용 `ground` 제외)을 제자리에서 열어 **메시가 있는 부품마다 MeshCollider**를 붙이고, 상자·바퀴 Collider와 메시 없는 오브젝트의 Collider는 지웁니다(LowPolyVehicle1 Body의 차 전체 상자 때문에 문·핸들이 눌리지 않았음). 원본 프리팹을 먼저, Variant를 나중에 고칩니다. 오브젝트 ID가 그대로라 씬의 차·정답 연결이 유지되고, 여러 번 실행해도 같습니다. ARCADE는 Rigidbody·WheelCollider가 붙은 `With Colliders` 대신 `Meshes Only`를 씁니다. 새 차 에셋을 쓰면 `PrefabFolders`에 폴더를 추가합니다. |
| Editor | `SciFiIslandSetup` | 에디터 전용(2026-10-31, 3-1). **Tools → You're Right → Setup Sci-Fi Island**: Mnostva `Flying_Sci_Fi_Island_city`(`Assets/Mnostva_Art`, 7.4MB) 정리. 머티리얼 2개(`Island_Material`·`Glass_Material`, Standard)를 **먼저 Convert Selected Built-in Materials to URP로 바꿔야** 하고, 아니면 멈춥니다. `Island_Material` Emission(검정, RealtimeEmissive)을 켜고, `Prefabs/island`·`Prefabs/interiors`의 모든 프리팹에서 메시가 있는 부품마다 MeshCollider를 붙이고 메시 없는 오브젝트의 Collider(루트 1×1 상자)는 지웁니다. `Glass`는 Collider 없음(클릭이 유리를 뚫음), 바닥 `Island 1`·`Floor`·`Driveway`·`Driveway_2`는 Background 레이어. 제자리에서 고쳐 오브젝트 ID 유지, 여러 번 실행해도 같음. 에셋 메모: 섬 프리팹 `Sci_Fi_Island`(루트 → `Island` 그룹 → 바닥 `Island 1` + 소품 50여 개가 각각 따로, 루트 바로 아래 반투명 `Glass`)는 가로세로 약 47 × 53, 도시 바닥 높이 약 46으로 커서 씬에서 Scale 0.4 정도로 씀. 색은 8 × 8 팔레트 텍스처 한 장(`Texture_*.png`)을 모든 물체가 같이 써서, 색 차이는 팔레트 복사본의 한 칸만 바꾼 머티리얼을 소품 하나에 넣어 만듦. |
| Lighting | `SunCycle` | Directional Light에 붙여 해를 일출 → 한낮 → 일몰로 움직이고, 짧은 밤(빛이 꺼졌다 켜짐)을 지나 다시 일출을 반복합니다(1-2용, 2026-10-27). 해 하나가 두 섬을 똑같이 비추므로 양쪽이 항상 같습니다. 값: `Day Duration`(60초), `Night Duration`(4초, 0이면 바로 일출), `Start Time`(0 일출~1 일몰, 기본 0.2), 빛이 향하는 Yaw `Sunrise -90`→`Sunset 90`(한낮 0이라 카메라 뒤에서 비춤, 해는 오른쪽에서 떠서 왼쪽으로 짐), 높이 `Horizon 5°`→`Noon 60°`, `Sun Color` Gradient(일출 주황 → 한낮 흰빛 → 일몰 붉은 주황), `Horizon/Noon Intensity` 0.6/1.3. `Control Ambient`를 켜면 `RenderSettings.ambientLight`도 Gradient로 바꿉니다(Lighting 창 Environment Lighting Source가 Color일 때만 보임, 밤은 `Night Ambient`). 기본 Gradient는 컴포넌트를 처음 붙일 때(`Reset`) 채워집니다. `Backgrounds`(카메라 + Day/Night Color)를 채우면 **Play 중에만** 해 밝기(`intensity / Noon Intensity`, 한낮 1·일출 일몰 약 0.46·한밤중 0)만큼 두 색을 섞어 카메라 배경색을 바꿉니다(2026-10-27, 1-2: 사막은 지금 색이 낮, 설원은 지금 색이 밤). 에디터에서는 바꾸지 않아 씬에 저장된 배경색이 그대로입니다. Play 중이 아니면 값을 바꿀 때 `Start Time` 모습을 바로 보여 줍니다. `Time.deltaTime`을 써서 일시정지 중에는 멈춥니다. 해 밝기는 `Daylight` 속성(한낮 1 → 한밤중 0)으로 바깥에서 읽습니다. |
| Lighting | `NightLight` | 가로등마다 하나씩 붙여 해가 지면 켜고 해가 뜨면 끕니다(2026-10-27, 1-2). 씬의 `SunCycle.Daylight`를 읽어 `Off Above`(0.5)보다 밝으면 꺼짐, `Full On Below`(0.25)보다 어두우면 다 켜짐, 그 사이는 서서히 밝아집니다. `Lights`를 비우면 자식 Light를 모두 쓰고, 씬에 저장된 Intensity가 최대 밝기입니다. 선택 항목 `Glow Renderers`(+ `Glow Material Index`, -1이면 전부)를 넣으면 그 머티리얼 복사본에 `Glow Color` Emission을 같이 켭니다. **Play 중에만** 동작하고, SunCycle이 없으면 경고 후 꺼집니다. 양쪽 불빛은 같은 값으로 두고, 가로등은 정답 쌍으로 쓰지 않습니다(정답 반짝임과 머티리얼 복사본이 겹침). |
| Utils | `SimpleInput`, `SingletonBase` | `19. Legacy/9. Utils`에서 복사해 네임스페이스를 바꾼 파일입니다. |

**씬** (Build Settings 순서): `00. Menu` → `10. Stage Tutorial` → `11. Stage 1-1` → `12. Stage 1-2` → `21. Stage 2-1` → `22. Stage 2-2` → `31. Stage 3-1` → `32. Stage 3-2` → `90. Result` (2026-10-27 계획 변경. `21. Stage 2-1`은 예전 `13. Stage 1-3`의 이름을 바꾼 것이라 GUID가 같고, 22·31·32는 Template을 복제해 만듦)
- 스테이지 씬 4개는 같은 구조이고, `StageNameText`의 글자와 `StageManager`의 Stage Name만 다릅니다. `Stage` 아래에는 임시 섬 바닥 `Stage Left`·`Stage Right`(큐브 20 × 2 × 20, y -2, Background 레이어)와 `PlayArea Left`·`PlayArea Right`(y 6.5)가 있습니다. 지금은 큐브로 된 테스트용 정답 쌍(`AnswerPair_01`: 왼쪽 섬 `Cube`, 오른쪽 섬 `Cube (2)`)과 섬마다 미끼 큐브 하나가 배치되어 있습니다.
- 분할 화면 구성: `Main Camera`는 Viewport X 0 / W 0.5, `Right Camera`는 X 0.5 / W 0.5이고, 둘 다 Far Clip 60입니다. `MainCamera` 태그와 Audio Listener는 `Main Camera`에만 있습니다. Canvas 첫 자식인 `Divider`(Width 4, 위아래 stretch, Raycast Target 끔)가 화면을 가르고, `TimerText` 바로 위의 `TimerBackground`가 분할선을 가립니다.
- **예비 씬**: `Scenes/Template/Stage Template.unity`. 스테이지 씬과 같은 구성이고 이름만 `New Stage`입니다. Build Settings와 `SceneNames`에는 넣지 않습니다. 스테이지를 늘릴 때는 다음 순서로 합니다.
  1. Project 창에서 Ctrl + D로 복제합니다(새 GUID가 생김). `Scenes`로 옮기고 이름을 `21. Stage 2-1`처럼 바꿉니다(월드 2는 21·22, 월드 3은 31·32).
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
| 9 | 월드 1 씬 정리: Stage 1·2·3 → 1-1·1-2·1-3 (파일 이름은 Unity에서 바꿔 GUID 유지, SceneNames·HUD 이름) | ✅ |
| 10 | 월드 1 디오라마: Tutorial ✅, 1-1 ✅, **1-2 사막 ↔ 설원 + 일출·일몰 반복** (정답 5쌍, `SunCycle` 코드 ✅) | **진행 중** |
| 10.5 | 씬 정리: 1-3 → 2-1 이름 변경, 22·31·32 생성, SceneNames·Build Settings 7개 | ✅ |
| 11 | 월드 2: 가로 분할, 2-1·2-2 디오라마, 맵별 조명 연출 | ✅ (2-2 기차역, 2-1 자동차 전시장 모두 정답 5쌍·검사 통과) |
| 12 | 월드 3: 반반 투영 코드, 3-1·3-2 물체, 맵별 조명 연출 | **진행 중** (3-1 공중 SF 섬 도시 ✅ 정답 5쌍·검사 통과, 3-2 남음) |
| 13 | 마무리: 사운드 파일, 결과 화면 6줄 확인, 빌드 테스트 | |

**7단계 메모 (완료)**
- 반짝임 파티클 `Prefabs/Sparkle`: Burst 25, Sphere 0.3, Start Speed 7~9, Lifetime 0.6~1, Limit Velocity Dampen 0.2, Start Size 0.2~0.4, Billboard, Rotation over Lifetime 180°/s, Size over Lifetime 1→0. **퍼지는 거리 ≈ Start Speed × Lifetime**(Dampen을 켜면 그보다 짧아짐)이고, 거리는 Start Speed로 조절합니다.
- 기본 파티클 텍스처는 가장자리가 부드러워서 흐릿하게 보입니다. 그래서 `Materials/Textures/T_SparkleStar.png`(PowerShell System.Drawing으로 만든 128px 4갈래 별, 흰색)와 `Materials/M_Sparkle.mat`(URP `ParticlesUnlit.mat`을 복사해 텍스처만 교체)를 씁니다. Stretched Billboard는 별 모양을 찌그러뜨리므로 쓰지 않습니다.
- 빌드에서는 쓰는 머티리얼이 없는 `_EMISSION` 셰이더 변형이 빠질 수 있습니다. 그래서 10단계에서 **정답 오브젝트 머티리얼은 Emission을 켜고 색을 검정으로** 둡니다. 그래도 빠지면 기본 색 섞기만 보입니다.

**8단계 메모**
- 일시정지 중 BGM은 **볼륨 낮추기**로 정했습니다(사용자 결정).
- **사운드 파일은 아직 없습니다.** 파일을 구하면 `Assets/100. 3D Project-1/Audio/`에 넣고, **`Prefabs/SoundManager.prefab`**(씬 인스턴스가 아님)의 `bgm`, `correctAnswerClip`, `wrongAnswerClip`, `buttonClickClip`에 연결합니다. 그러면 모든 씬에 한 번에 들어갑니다. 이때 `Audio.meta`도 같이 커밋합니다.
- 기획서 기준 소리: BGM은 잔잔한 Lofi나 어쿠스틱 루프, 정답은 맑은 실로폰·종소리, 오답은 불쾌하지 않은 '뾱'이나 '삐빅', 버튼은 깔끔한 클릭음입니다.

**10단계 메모 (진행 중, 2026-10-22~23)**
- 결정: 튜토리얼 정답 3개. 튜토리얼 시작 전 **안내 패널(A안: 규칙 → 조작법 → 시작하기, 2~3페이지)**을 튜토리얼에 들어올 때마다 보여 줍니다(한국어). **튜토리얼 시간은 HUD에만 보이고 기록에서 뺍니다.**
- **에셋과 디오라마는 사용자가 직접 구해서 만듭니다.** Claude는 코드, 도구, 단계별 안내를 맡습니다. 디오라마용 에셋은 Synty POLYGON Apocalypse(`99. External Assets/Synty`)와 **Palmov Island `Low Poly Atmospheric Locations Pack`**(`Assets/Palmov Island`, 약 14MB, 2026-10-23 추가, `99. External Assets`가 아니라 Assets 바로 아래에 있음)입니다. 둘 다 git에 있습니다.
- 2026-10-26: **GeeZyyGames `Desert`**(`Assets/GeeZyyGames`, 약 22MB) 추가, 머티리얼 80개 URP Lit으로 변환. 에셋을 지우고 다시 임포트하면 Render Pipeline Converter가 예전 결과("Complete")를 기억해 Convert 버튼이 꺼지므로, 창을 닫았다 열고 Initialize Converters부터 다시 합니다. 변환 여부는 `.mat`에 `m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000}`(Standard)가 남았는지로 확인합니다.
- 2026-10-23에 Render Pipeline Converter로 Built-in Standard 머티리얼(Synty, Parazite, Paper Plane 등 20개)을 URP Lit으로 바꿨습니다.
- 지금 섬 바닥과 테스트 큐브가 쓰는 `Lit` 머티리얼은 **URP 패키지 기본 머티리얼**(guid `31321ba1…`, 읽기 전용)입니다. Emission을 켤 수 없으므로, 정답 오브젝트에는 프로젝트 안의 머티리얼(`100. 3D Project-1/Materials`)을 씁니다.
- **디오라마 형태 (사용자 결정, 2026-10-24)**: 공중에 뜬 문제 구역 하나만 둡니다(네모 블록 또는 공중 섬, 맵마다 고름). 바깥 풍경·장식 섬은 없고, 배경은 두 카메라 모두 Solid Color(스테이지마다 다른 색)입니다. 아래쪽 문제 계획은 유지합니다.
- **다음 할 일: 튜토리얼 디오라마 제작 순서**
  1. 준비: 테스트 큐브 4개와 `AnswerPair_01`을 지우고, `Props/Left` 그룹을 만듭니다(원점).
  2. 문제 구역: 네모 블록이나 공중 섬 하나를 `Stage` 아래에 둡니다. x -100, 윗면 y 0 근처, 가로세로 20~30(Far Clip 60), **Background 레이어**(자식 포함), Collider를 붙입니다. 임시 큐브 `Stage Left`·`Right`는 지웁니다.
  3. 배경색: `Main Camera`·`Right Camera`의 Background Type을 Solid Color로 하고 같은 색을 고릅니다.
  4. 물체 배치: `Props/Left` 아래에 8~12개를 놓고, 그중 정답 3개(크고 잘 보이는 것)를 정합니다.
  5. 복제: 구역과 `Props/Left`를 Ctrl + D로 복제하고 x를 +200 합니다(`Props/Right`).
  6. 차이 만들기: 정답 3개는 그대로 두고 나머지는 전부 다르게 바꿉니다.
  7. `PlayArea Left` 크기를 구역에 맞춥니다(Right는 Mirror Of로 따라감).
  8. Create Answer Pair로 3쌍을 등록하고, Emission 경고가 나오면 머티리얼에서 Emission을 켜고 색을 검정으로 둡니다.
  9. Play 확인 → 저장 → Claude에게 씬 확인을 요청합니다.
- **튜토리얼 = 공 비교 맵 (2026-10-26 결정)**: 야구·농구·축구·테니스공을 두고 비교합니다. 프로젝트에는 공 모델이 없어(Palmov의 크리스마스 장식 공뿐) Asset Store(`low poly sports balls`)에서 구하거나 임시로 색 입힌 Sphere를 씁니다. 섬 바닥 후보는 Palmov `Models/Lands/land soccer field`(Background 레이어), 장식은 `soccer goal`·`mesh fence`. **뼈대 완료 (2026-10-26)**: 공은 `Assets/Saritasa/Models/Sport_Balls`(7종 프리팹, MeshCollider 포함, 약 62MB, 머티리얼 URP 변환 완료). 이 에셋을 임포트할 때 Post Processing v2 패키지가 같이 들어와 변환 창이 깨져서 패키지를 제거함(Player의 Scripting Define에 `UNITY_POST_PROCESSING_STACK_V2`가 남아 있으면 지울 것). 섬은 `Stage/land soccer field Left`·`Right` (∓100, -5, 0), 공 7개(Scale 10)는 `Props/Left`·`Right`(회전 없음), 정답은 미식축구공·골프공·배구공. 카메라 Pitch Min 5, Distance Max 25. **2026-10-26 집에서 한 일**: ✅ 하늘 — `Materials/M_Sky_Tutorial.mat`(Skybox/Procedural: Sun High Quality, Size 0.04, Convergence 5, Thickness 0.8, Sky Tint (110,160,220), Ground (90,110,80), Exposure 1.2)을 Lighting 창 Skybox Material로, Sun Source는 Directional Light, 두 카메라 Background Type은 Skybox(튜토리얼만 단색 대신 하늘). ✅ 축구장 바닥 — 씬이 아니라 프리팹 에셋 `land soccer field.prefab` 자체를 Background 레이어로 바꿈(튜토리얼에서만 씀). ✅ 정답 머티리얼 Emission(검정) 켬: `endesga64`(체스 전체), `Football`·`Golf`·`Volleyball`. ✅ 축구공 색 차이 — 오른쪽 축구공만 `Saritasa/.../Materials/Soccer Navy.mat`(Base Map `Soccer_A Navy.png`: 원본 `Soccer_A.png`의 검은 무늬를 남색 (25,40,110)으로 바꾼 것. 텍스처의 검은 부분은 Base Color 곱하기로는 안 바뀌어서 PowerShell + 인라인 C#(System.Drawing, 밝기 0.5 미만을 새 색으로 섞음)으로 만듦), 크기는 양쪽 10. 테니스공은 양쪽 Scale 10으로 맞춤.
  ✅ **오답 차이 완료 (2026-10-26, 공 9개)**: 공 자리는 x -6/0/6 × z -6/0/6이고, 모두 Y 2입니다.
  | 자리 (x, z) | 왼쪽 | 오른쪽 | 차이 |
  |---|---|---|---|
  | (6, 6) / (-6, -6) / (0, 0) | 미식축구공 / 배구공 / 골프공 | 같음 | **정답** |
  | (-6, 6) | 축구공 | 남색 축구공(`Soccer Navy`) | 색 |
  | (6, 0) | 야구공 Scale 10 | 야구공 Scale 12 | 크기 |
  | (-6, 0) | 테니스공 | 야구공 | 종류 |
  | (6, -6) | 농구공 | 농구공, 솔기 각도가 다름 | 회전 |
  | (0, -6) | 8번 당구공 | 8번, 180° (`Eight-ball Rack`, 반대편 숫자가 보여 숫자 크기가 다름) | 회전 |
  | (0, 6) | 10번 당구공 | 10번, 120° | 회전 |
  검사 도구에서는 오류 0, 함정 후보(ℹ) 4개(야구공 크기, 농구공·8번·10번 회전)가 나오는 것이 정상입니다. 튜토리얼 원칙: 공 8~10개·정답 3쌍, 차이는 종류·색·크기·빼기로 눈에 띄게, 크기는 과장(큰 공 1.2, 작은 공 0.6 정도). **회전 차이는 미세하면(5° 등) 쓰지 않지만, 농구공 솔기나 당구공 숫자처럼 한눈에 보이면 허용합니다(사용자 결정).** 야구공은 1.2배 그대로 둡니다(사용자 결정). 작은 공(야구·테니스·골프)이 Y 2로 떠 있는 것은 의도한 것입니다. 3초 제한 문구는 안내 3페이지 본문에 들어가 있습니다. **튜토리얼 디오라마 완료 (2026-10-26).**
- **1-2 = 사막 ↔ 설원 (2026-10-27 결정, 사용자 아이디어)**: 왼쪽 GeeZyy `Desert`, 오른쪽 GeeZyy `Winter`로 **테마를 완전히 다르게** 하고, 똑같은 물체 **5쌍**을 찾습니다. 같은 제작자 키트라 화풍·블록 구조가 같습니다. Forest와 나머지 Palmov 섬은 월드 2에 남깁니다.
  - **설계 원칙** (정답이 "양쪽에 다 있는 물체"라서 바로 티 나는 것을 막음): ① **같은 뼈대, 다른 옷**: 블록 모양·길·물체 자리를 양쪽 같게 두고 같은 자리에 테마만 바꾼 짝(선인장 ↔ 소나무, 텐트 ↔ 이글루, 모래 바위 ↔ 눈 바위). ② 정답은 **양쪽 테마에 다 어울리는 중립 소품**(Palmov `Barrels`·`Box`·`Benchs`·`chest`·`lamppost`·`information post`·`trash can` 등). ③ 같은 종류의 **미끼**(색·자리·크기가 조금 다른 상자 등)를 섞음.
  - **조명**: 양쪽 공용 Directional Light 하나에 `SunCycle`을 붙여 일출·일몰 반복(사용자 결정). 해가 하나라 양쪽이 항상 같음. 섬마다 따로 두는 조명(Point·Spot)은 넣으면 양쪽 같은 값으로(검사 도구가 확인). **배경색은 양쪽 다르게**(사막 주황빛 / 설원 남색, 검사 도구는 참고로만 표시). Lighting 창 Environment Lighting Source는 Color로 두어야 `Control Ambient`가 보입니다. PC_RPAsset 그림자 거리 50이라 카메라 거리 25~30에서 그림자가 충분함.
  - 기획서 2.2(1-2 예외), 3.1(연출), 변경 이력 갱신.
  - **정답 5쌍 완료 (2026-10-27)**: `_01` 울타리 `fence (12)`, `_02` 텐트 안 `sheet and bread`(텐트 위치만 일부러 다름, 착시), `_03` 꽃병의 `flower`(**일부러 아주 작게, Collider도 작게 둠, 사용자 결정**), `_04` 장작 `wood (3)`(나머지 장작은 머티리얼이 다름), `_05` 풀 `grass (18)`(오른쪽은 낙엽 사이에 풀 하나, 쉬운 문제). PlayArea 26 × 17 × 26, 그 밖의 선인장·나무·앞쪽 소나무·산은 Collider 없음(무반응이라 괜찮음).
  - ✅ **배경·가로등·검사 (2026-10-27)**: SunCycle `Backgrounds` 설정(Main Camera 사막 Day `E8A06E` / Night `2A2033`, Right Camera 설원 Day `B4D2EB` / Night `28345A`). 정답 머티리얼 Emission(검정) 켬: GeeZyy `Desert` `Material.006`·`Material.011`·`rocks`, `Forest` `Material.010`·`011`·`014`·`015`, `Winter` `Material.015`·`wood`(같은 이름이 Desert·Forest·Winter 폴더에 따로 있으니 Console 경고를 눌러 실제 머티리얼을 찾음). **가로등**: Winter `lamp` 프리팹을 섬마다 2개(`lamp`, `lamp (1)`), 각각 Point Light 자식 + `NightLight` + MeshCollider. 양쪽이 똑같으면 "의도하지 않은 쌍둥이" 오류가 나서, **오른쪽(설원) 가로등 2개만 전구 머티리얼을 `Winter/materials/White Lamp.mat`(흰색, 주황 Emission이라 낮에도 빛남)**으로 바꿔 미끼로 둠. 불빛(Light) 값은 양쪽 같음. Validate Stage: 오류 0·경고 0·참고 1(배경색 차이).
  - ⏳ SunCycle이 지금 씬에 **낮 10초 / 밤 10초**로 저장되어 있습니다(기본 60/4). 테스트용인지 실제 값인지 정해야 합니다.
- **1-1 진행 상황 (2026-10-24, 사용자가 튜토리얼보다 1-1을 먼저 만들기로 함)**
  - **1-1 = 체스판 맵**입니다. 에셋은 `Assets/SmallBigSquare/LowPolyChessPieces`(git에 있음)이고, 나이트는 `Horse` 프리팹입니다. 기물 프리팹은 기준점이 바닥 가운데입니다.
  - **왼쪽 판 `Stage/BW_board Left`**: Position (-109.23, -2.63, -9.81), Rotation Y 90, Scale 20, Background 레이어(자식 포함). 에셋의 기준점이 판 모서리에 있어서 Box Collider Center를 (-0.4906, 0.0658, 0.4616)로 고쳤습니다(Size는 원래 값). 판 가운데가 (-100, 0, 0)이고, Y 90으로 돌려 h1(오른쪽 아래)이 밝은 칸이 되게 했습니다.
  - **`Props/Left`는 (-100, 0, 0)**에 있어서, 기물 Position에 판 가운데 기준 칸 좌표를 그대로 씁니다.
    - a~h → X: -9.23, -6.59, -3.96, -1.32, 1.32, 3.96, 6.59, 9.23
    - 1~8 → Z: -9.81, -7.01, -4.21, -1.40, 1.40, 4.21, 7.01, 9.81 (1줄이 카메라 쪽, 흰색)
  - **기물 Y는 -0.01**이고, 나이트만 **-0.26**입니다(모델 탓에 -0.01이면 떠 보여서 눈으로 맞춤). 판 윗면의 정확한 높이는 -0.044입니다. 기물 32개 기본 배치를 마쳤고(퀸 d, 킹 e), 검은 기물은 Rotation Y 180입니다.
  - **2026-10-26 진행**: 오른쪽 판 `BW_board Right` (90.77, -2.63, -9.81)와 `Props/Right`(100, 0, 0)를 만들었습니다. **`Props/Right`는 일부러 Rotation Y -180**입니다(사용자 결정: 그대로 복제해 돌리면 같은 칸에 색만 반대인 기물이 와서 전부 오답이 되고, 정답 칸만 손보면 됨). 그래서 `Props/Right` 안의 좌표는 칸 좌표의 X·Z 부호가 반대이고, 실제 방향은 로컬 Rotation Y + 180입니다. 중반 배치로 기물을 섞었고 PlayArea는 22 × 15 × 23입니다.
  - **정답 3쌍 (확인 완료)**: `AnswerPair_01` a5 흰 룩, `_02` c4 검은 나이트(양쪽 실제 방향 -120°), `_03` f2 흰 폰. AnswerPair 3개는 `Props/Right Answer` 그룹 안에 있습니다(이름과 달리 정답 기물이 아니라 쌍 오브젝트만 들어 있음).
  - **e4 흰 나이트는 의도한 함정**입니다(양쪽 같은 칸, 방향만 85° vs 95°로 10° 다름). 정답으로 등록하지 않습니다.
  - 기물 프리팹에는 MeshCollider가 원래 들어 있습니다.
  - **1-1 카메라 (2026-10-26)**: `Main Camera` (-100, 15, -30), `Pitch Min` 15(체스판이라 아래를 볼 필요 없음), `Distance Max` 30. 배치 거리(약 33.5)가 Distance Max보다 커서 시작 거리는 30으로 잘리고 시작 각도는 약 26.6°입니다. 스테이지마다 시작 거리·각도는 `Main Camera` 위치로 정합니다.
  - **1-1 조명 (2026-10-26, 취조실 느낌)**: 두 카메라 Solid Color `0B0C0F`, Lighting 창 Environment Lighting Source Color (26, 26, 31), Reflections Intensity 0.2(이 값들은 씬마다 저장됨), Directional Light 0.05. `Lights/Spot Light Left`·`Right`: (∓100, 20, 0), Rotation X 90, 색 `FFE6B8`, Intensity 1000, Range 25, Spot 70 / Inner 30, Soft Shadows. **양쪽 조명은 반드시 같은 값**이어야 합니다(그림자·밝기 차이가 오답 단서처럼 보임). 빛기둥 원뿔·전등 모델은 넣지 않음. HDR 색상 창(Ambient Color 등)은 Hex 칸이 없어서 RGB로 넣습니다. PC_RPAsset은 추가 조명 그림자·Soft Shadows를 지원하지만 Mobile_RPAsset은 지원하지 않습니다.
- **오답 클릭 제한 + 클리어 창 (2026-10-26, 사용자 결정)**: 제한 3초, 제한 중 커서는 빨간 원 타이머(12시에서 시계 방향으로 지워짐), 클리어 창에는 이번 기록 + Next·Main Menu만 넣음(누적 시간·Retry는 넣지 않음). 기획서 4.2·5.1과 변경 이력 갱신.
  - ✅ 코드·`T_LockRing.png` 완료, 컴파일 확인.
  - ✅ 1-1에 UI 생성(2026-10-26): `Click Lock Indicator`(바늘 없이 Ring만, 사용자가 이 모양으로 결정), `Stage Clear Pannel`(Pause Menu를 본떠 만듦). `T_LockRing.png`는 Sprite Mode가 Multiple이고 128px 전체가 `T_LockRing_1` 하나라 Single과 같습니다(이 때문에 `com.unity.2d.sprite` 패키지가 추가됨). Pause Menu 프리팹 버튼 순서를 바꿈(Main Menu 왼쪽, Continue 오른쪽, 클리어 창과 맞춤).
  - ✅ 프리팹 `Prefabs/Click Lock Indicator`·`Prefabs/Stage Clear Panel`을 튜토리얼·1-1~1-3·Template에 배치하고 Play 확인 완료(2026-10-26). 1-3의 `Last Stage Next Text`는 "스테이지 1 결과"입니다. 결과 씬 `Result Panel`은 1000 × 750으로 키웠습니다. 2026-10-27: 기록이 6줄이 되면서 6번째 줄이 하늘의 흰 지평선과 겹쳐 안 보여서, 결과 씬 카메라 배경을 Skybox에서 Solid Color(진한 남색 0, 0, 30)로 바꿈(창이 반투명이라 뒤 배경이 비침).
  - (완료된 계획) UI 배치: 1-1에서 `Click Lock Indicator`·`Stage Clear Panel` 오브젝트를 만들어 `Prefabs/`에 프리팹으로 저장 → 튜토리얼·1-2·1-3·Template Canvas에 배치(Canvas 자식 순서: … `Stage Clear Panel` → `Pause Menu` → `Tutorial Guide` → `Click Lock Indicator`). `T_LockRing.png`는 Texture Type을 Sprite (2D and UI)로 바꿔야 함. 튜토리얼 안내 1페이지에 "틀리면 3초 동안 클릭할 수 없음" 문구 추가 권장.
  - 카메라(`OrbitCameraController`)는 UI 위에서도 돌아가므로, 클리어 창은 화면 전체를 어둡게 덮지 않고 가운데 창만 두는 것을 권함(쉼터에서 디오라마를 보게).
  - 튜토리얼 씬은 디오라마 작업 중이라 정답 쌍이 없습니다. Menu에서 시작하면 튜토리얼에서 막히므로, 1-1은 씬을 직접 열어 테스트합니다.
- ✅ 코드: `TutorialGuide`(새로 추가, `OnClosed`), `GameManager.ReportStageClear(..., addRecord)`, `StageManager.addToRecords`와 타이머 시작 시점, `PauseController`(안내 중 Esc 무시).
- ✅ 튜토리얼 안내 패널 UI(3페이지, 임시 흰 창 + 검정 반투명 배경, 나중에 UI 에셋으로 꾸밈)와 `Prefabs/Tutorial Guide.prefab`, Add To Records 끔, 안내 중 타이머 00:00.00 확인(에디터 첫 Play 포함), 기획서 3.1·4.2와 변경 이력 갱신 (2026-10-23).
- ✅ 무반응 레이어 (2026-10-23): `Background` 레이어(User Layer 10)와 `ClickInteractor.silentLayers`를 추가했고, 미끼 큐브로 테스트해 X·로그가 없는 것을 확인했습니다.
- **클릭 반응 규칙 (사용자 결정, 2026-10-23)**: 참고 게임 스크린샷 기준입니다.
  - 플랫폼 **밖**의 주변 장식(바위, 바깥 차량, 사막 바닥)은 무반응입니다. → **구역 상자(PlayArea)**로 처리합니다(✅ 2026-10-23 완료. 오른쪽은 Mirror Of로 왼쪽을 따라가는 B안. 스테이지 4개와 Template에 배치·테스트 완료).
  - **플랫폼 자체(윗면 바닥, 옆면 두께, 길)**도 무반응입니다. → 이 오브젝트들을 `Background` 레이어로 둡니다(✅ 코드 완료).
  - 플랫폼 **위에 올린 물체**만 반응합니다(정답 반짝임 / 오답 X). 바닥의 차이점(맨홀, 도로 표시, 웅덩이 등)은 바닥에서 0.01~0.02 띄운 **별도 물체 + Collider**로 만듭니다.
- ⏳ **다음 작업 후보** (사용자가 하나씩 고름):
  1. ~~**PlayArea**~~ (✅ 완료 2026-10-23): 섬마다 보이지 않는 상자 하나(`Stage` 아래, 왼쪽·오른쪽 같은 크기, 플랫폼 위아래로 넉넉히, 빨간 선 기즈모). 물리 Collider 없이 클릭 지점이 상자 안인지만 계산합니다. `ClickInteractor`는 맞은 지점이 어느 PlayArea에도 없으면 무반응으로 처리하고, PlayArea가 없는 씬은 지금처럼 동작합니다. 예상 1시간 이내입니다.
  2. ~~**정답 쌍 만들기**~~ (코드 완료 2026-10-23, `Scripts/Editor/AnswerPairCreator.cs`, 루트 메시는 MeshCollider로 결정, ✅ Editor 테스트 완료. 레이어 검사를 위치 검사보다 먼저 함) 에디터 도구: 두 물체 선택 → AnswerTarget·Collider(Mesh/Box)·`Props/AnswerPair_NN` 자동 생성, 같은 섬·이미 등록·Background 레이어면 거부, Undo 지원. 예상 1시간입니다.
  3. ~~**스테이지 검사**~~ (✅ 완료 2026-10-26, `Scripts/Editor/StageValidator.cs`. 1-1·튜토리얼에서 테스트함. 1-1의 흰 룩 쌍은 룩 모델이 180° 대칭이 아니라서 오류로 잡혔고, 사용자가 회전을 맞춰 고침. 저장할 때 자동 검사 토글은 필요 없다고 결정) 에디터 도구 (계획 당시 추천 구성: Console 로그 + 열린 씬만 + 회전 대칭 꼭짓점 자동 판단, 약 2시간 30분. 전용 창 +1시간, 전체 스테이지 검사 +30분은 선택. 비교 기준은 Pivot 기준 상대 위치와 월드 회전(그래서 회전된 `Props/Right`도 정확히 비교), 단위는 프리팹 루트, 오차 위치 0.01·회전 1°. 추가 항목: 함정 후보(거의 같은 물체, 예: 1-1 e4 나이트) 참고 표시, ±100 조명·두 카메라 설정 대칭 검사. 1-1에서 e4는 함정 후보, a5 룩·f2 폰은 대칭이라 통과가 기대 결과): 쌍둥이 동일성(메시·머티리얼·크기·회전·섬 중심 기준 위치·자식 구성), **의도하지 않은 쌍둥이**(등록 안 했는데 양쪽 같은 위치에 같은 물체), 연결·Collider(없음·꺼짐·Trigger), 레이어(정답이 Background, 큰 바닥이 Default), PlayArea 밖의 정답, 정답 개수 3~5, 정답 머티리얼 Emission을 봅니다. 로그를 클릭하면 오브젝트가 선택되고, 씬은 고치지 않습니다. 예상 2시간입니다.
  - 도구는 `Scripts/Editor/`에 두고(빌드 제외), 메뉴는 `Tools → You're Right`로 제안해 두었습니다.
  - Synty Apocalypse 프리팹은 약 80%에 Collider가 있고 모두 Default 레이어입니다. 그래서 플랫폼·길 조각은 Background로 바꿔야 합니다.

**11단계 메모 (진행 중, 2026-10-27)**
- **2-1 가로 분할 완료**: `Main Camera`(왼쪽 섬 x -100)는 Viewport X 0 / Y 0.5 / W 1 / H 0.5로 **위 화면**, `Right Camera`(오른쪽 섬 x +100)는 Y 0 / H 0.5로 **아래 화면**입니다. 코드는 그대로입니다(`ClickInteractor`가 `pixelRect`로 카메라를 고름). `Divider`는 가운데 가로 stretch, Height 4.
  - 화면 하나가 1920 × 540이라 **FOV를 두 카메라 모두 32**로 둡니다(60이면 물체가 반 크기, 가로 시야 약 128°로 휨). FOV 32, 거리 25에서 보이는 범위는 약 가로 51 × 세로 14라서 **가로로 넓고 낮은 디오라마**가 어울립니다.
- **2-1 = 자동차 전시장 (2026-10-29 결정)**: 어두운 전시장, 뒤쪽 세로 빛줄기, 세단·SUV 4~5대를 한 줄로 두고 두 섬의 차 배치를 다르게 합니다. 씬에 Floreswa 세단 2대(`free_sedan_red`, `free_sedan_gray`)를 놓아 둔 상태입니다(예전 `1995_stock_train`도 아직 씬에 있으면 정리).
  - **주력 에셋 `Assets/Floreswa`** (0.6MB, git에 있음): 세단 1종 × 차체 2종(`free_sedan`, 차체가 낮은 `free_sedan_low`) × 4색(기본·black·gray·red) = 프리팹 8개, URP Lit. 구조는 빈 루트 아래 `Body`(머티리얼 칸 9개, 부위별) + 바퀴 `FL`·`FR`·`RL`·`RR`(따로, 칸 3개씩). 색 버전은 Body의 차체 칸 하나만 `Floreswa/Materials/*.mat`(16색)으로 바꾼 것이라 **칸만 바꿔서 부위별 차이**를 만들 수 있습니다. Collider가 없으니 씬에서 `Body`와 바퀴에 MeshCollider를 붙입니다.
  - **해야 할 일**: ① **Tools → You're Right → Extract Floreswa Materials** 실행(기본 머티리얼이 FBX 안에 있어 읽기 전용이라 Emission을 켤 수 없음. Unity Extract는 프리팹을 깨뜨리므로 쓰지 않음) ✅ 2026-10-30 실행 완료(부위 12개 `Floreswa/Materials/Parts`, 프리팹 8개·SampleScene 참조 변경, 색 머티리얼 16개 Emission). `Dark Gray`·`Brown`은 원본이 없는 Material Variant라 경고가 나서 `Red.mat` 설정에 원래 색만 넣은 일반 머티리얼로 바꿈 ② **Body 칸**: 0 차체 칠(`car`, 색 버전은 Red·Gray 등), 1 tail light, 2 headlight, 3 window, 4 underbody, 5 fog, 6 grille, 7 side mirror, 8 exhaust. **바퀴 칸**: 0 tire, 1 rim, 2 gap ③ 세단만 있어 실루엣이 비슷하므로 **SUV 등 다른 차 에셋 추가**(고르는 기준: 부위별 머티리얼 칸이 나뉘어 있는가) ④ 전시장 바닥·조명.
  - **조명·환경 추천 값**(아직 적용 안 함): 두 카메라 Solid Color `0A0B0E`, Emission 켠 세로 빛줄기 판 + 양쪽 같은 값의 Spot Light, 헤드라이트 Emission, 광택 바닥(Smoothness 0.85~0.9) + 섬마다 Reflection Probe.
  - **차이 만드는 방법**: 대부분 같은 자리에 두고 세부(바퀴 하나의 림 색·각도, 라이트 색, 차체 칸 색, 번호판)를 바꾸고, 1~2대는 차 자체를 바꿈. `free_sedan` ↔ `free_sedan_low`는 미묘한 함정 후보.
  - 써 보고 뺀 에셋: `Low poly vehicle pack lite`(색마다 텍스처 한 장 + Rigidbody·WheelCollider가 붙어 있음), `Awbmecreations`(모든 차가 텍스처 `Color.png` 한 장을 같이 씀). 둘 다 칠이 텍스처에 그려져 있어 부위별 차이를 만들기 어려워 지웠습니다.
  - **에셋 정리 (2026-10-30 학원)**: 쓰는 차 에셋은 **BrokenVector `LowPolyCarPack`**(3.8MB, 승용차 6종 `Car_1~6` + 트럭 2·버스·경찰차, 6색, 차체 칸이 칠·라이트·유리·고무로 나뉨, 자체 셰이더라 `LowPolyShaderConverter`로 URP 변환), **ARCADE - FREE Racing Car**(5.5MB, Body·Spoiler·바퀴 4, 색은 512px 텍스처 한 장, `Prefabs (Meshes Only)`만 씀: `With Colliders`는 Rigidbody·WheelCollider 있음. 하늘 텍스처·홍보 이미지 약 4.9MB는 안 씀), **LowPolyVehicle1**(274KB, 차 1대, 문·핸들·좌석·유리가 따로, 차체 색은 8×8 팔레트), Floreswa. 모든 차 프리팹은 `VehicleColliderFixer`로 부품마다 MeshCollider. ARCADE·LowPolyVehicle1 머티리얼도 Emission(검정) 켬. **YelScrypt**는 전용 Shader Graph에 `_EmissionColor`·`_BaseColor`가 없어 정답 반짝임이 안 나와서 지움(그때 `Packages/manifest.json`에 `com.unity.shadergraph`가 추가됨, URP가 이미 포함하는 패키지라 되돌려도 됨). **Render Pipeline Converter는 결과를 기억해 두 번째 에셋부터 Convert가 꺼지므로** 머티리얼을 선택하고 Edit → Rendering → Materials → Convert Selected Built-in Materials to URP를 씀(자체 셰이더는 이것도 건너뜀).
  - **전시장 배치 (2026-10-30)**: `Stage/Showroom Left`·`Right`(바닥 4×4 24장, 32 × 12, **윗면 y -1**), `Stage/Left`·`Right`의 `Edge`(네 변 가장자리 22개 + 모서리 4개, 하위 그룹 Front/Behind/Left/Right를 돌려 배치)·`Pedestal`(5개, x -12/-6/0/6/12, 씬에서 Scale 0.8 × 2 × 0.8이라 지름 4.56·윗면 -0.76). **벽은 360도 카메라를 가려서 지움**(사용자 결정). `Props/Left`·`Right`의 `Cars`(왼쪽부터 free_sedan, vehicle1-red, Free Racing Car, Car_3_Blue, Car_4_Green, 받침대 윗면 -0.76에 올림)·`Stands`(Info Stand 5개, 차 앞 z -4, `Head` X 25°). 조명: 두 카메라 `0A0B0E`, Ambient Color (20,22,26), Directional 0.3·`CFD8E6`·(50,-30,0), 차마다 Spot Light (차 x, 8, -3)·X 70°·150·Range 15·50/30·`FFF4E5`·Soft Shadows(양쪽 10개), Reflection Probe 섬마다 (y 3, 34 × 8 × 14, Baked). Spot 10개 그림자 때문에 `PC_RPAsset` 추가 조명 Shadow Atlas를 2048 → **4096**으로 올림. PlayArea 34 × 15 × 14.
  - **검사 (2026-10-30 13:40)**: 정답 2쌍(`_01` 레이싱카 `RearLeftWheel`, `_02` LowPolyVehicle1 `Windshield`), 오류 7개(의도하지 않은 쌍둥이: `Car_3_Blue`·`Car_4_Green`·Info Stand 5쌍), 경고 3개(정답 2쌍 + Emission 2개, Emission은 머티리얼에서 켜서 해결). **다음 할 일**: 정답 1~3쌍 추가, 나머지 차·스탠드에 차이 만들기, 벽을 지운 뒤 Reflection Probe 다시 굽기(Generate Lighting).
  - ✅ **정답 5쌍·검사 통과 (2026-10-30 집)**: `_03` 픽업 `Pick Up_7NL`의 `Tachometer`(회전수 계기판), `_04` `Car_3_Blue` 차 전체, `_05` `Info Stand`(맨 왼쪽). 나머지 Info Stand 4개와 차는 차이를 줘서 미끼로 둠. **Drivable-Free Low Poly Cars**(`Assets/Drivable-Free Low Poly Cars`, 1.1MB, 픽업 2종, 부품마다 문·핸들·계기판·타이어가 따로)를 추가하고 `WithoutLod`(`*NL`) 프리팹만 씀(`VehicleColliderFixer`에 폴더 추가). 칠은 텍스처 한 장(`Color.png`)이라 색 차이는 못 냄. `Glass`는 Emission이 꺼져 있으니 유리를 정답으로 쓰려면 켬. Validate Stage 오류 0·경고 0·참고 0, Play에서 5쌍 모두 찾아 클리어 확인. `LowPolyVehicle1/Materials/WindowsMaterial2.mat`의 색 알파를 0 → 1로 바꿈.
- 2026-10-27 추가 에셋(git에 있음, 모두 작음): `Assets/Polyeler/Simple Aircraft Pack`(1.7MB), `Assets/Alstra Infinite/Planes LowPoly`(1.4MB), `Assets/Cobra Games Studio/Low Poly Subway Pack`(0.4MB).
- **2-2 = 기차역 (2026-10-28 결정)**: 한쪽은 **일본 시골 간이역**, 한쪽은 **스크린도어가 있는 최신 역**(1-2처럼 같은 뼈대, 다른 옷). 열차는 Polyeler `Simple Train Pack`(`Assets/Polyeler/Simple Train Pack`, 6.3MB, URP 변환 완료) `Train_Type B`(위, 왼쪽 섬)·`Train_Type A`(아래, 오른쪽 섬), **둘 다 3량으로 줄였고 일부러 다른 열차**입니다. 스크린도어는 오른쪽 섬(`Props/Right/PSD Left`·`PSD Right`, Train_Type B의 문 12개 위치)에 있고, 그 섬의 `Train_Type A`는 **역을 통과하는 특급 열차라 문(차량마다 1개, z -25.26 / 9.22 / 25.26)이 스크린도어와 맞지 않는 것이 의도**입니다(사용자 결정). **위(왼쪽 섬)는 종착역, 아래(오른쪽 섬)는 중간역인 것도 의도**입니다(사용자 결정, 2026-10-29): `Stage Left/Straight Rails`의 `Straight rail (1)` 자리(z 5.08)에 `Straight rail Terminal`을 두고, `Stage Right`는 원본 선로 2개 그대로 둡니다. 울타리는 기둥이 한쪽 끝(로컬 z -1)에만 있어서, 줄 끝은 `Fence ... Post`를 마지막 칸과 같은 자리에 180° 돌려 놓아 기둥을 채웁니다(전에는 돌린 울타리를 겹쳤음). 열차는 Collider 없음, 폭 2.7(x -3.7 ~ -1.0), 지붕 약 4.4(팬터그래프 5.1). 레일 `Straight rail`은 복선(선로 중심 x ±2.375). 카메라 FOV 32, Pitch Min 5, Distance 5~10. 처음 배치한 `Assets/Japanese_Station` 플랫폼은 사실적이라 다른 스테이지와 안 어울려서 `StationKitBuilder` 키트로 바꿉니다. 아래 비행기 메모는 다른 스테이지에서 쓸 수 있어 남겨 둡니다.
  - **카메라 충돌 설정 (2026-10-28)**: 열차 대차도 문제로 쓸 예정이라 Pivot을 올리지 않고 충돌 코드로 해결함. 레이어 **User Layer 11 = `CameraBlock`**(ProjectSettings/TagManager), `Managers/ClickInteractor`의 `Clickable Layers`에서 빼서 클릭이 통과합니다. `Train_Type B` 아래 자식 `Camera Block`(CameraBlock 레이어, BoxCollider, 위치 y 2.25, Scale 3 × 3 × 60이라 높이 0.75~3.75, 대차는 상자 밖). 막는 상자는 열차의 자식으로 둡니다(`Props` 바로 아래면 양쪽 같은 빈 상자가 의도하지 않은 쌍둥이로 잡힘). 아래 `Train_Type A`에는 상자가 없지만 코드가 두 Pivot을 모두 검사하고 짧은 거리를 같이 써서 위 상자가 함께 막습니다(Type A가 더 길거나 높은 부분은 막히지 않음). 지붕(약 4.4) 근처에서 속이 보이면 상자 Scale Y 3.65·위치 Y 2.575로 올립니다. `Main Camera`의 **Block Layers는 Background + CameraBlock**이라 Background 플랫폼·바닥도 카메라를 막고 Q로 바닥 아래로 내려가지 않습니다. **플랫폼은 Background 그대로** 둡니다(CameraBlock으로 바꾸면 클릭이 뚫고 지나감). PlayArea 길이는 70으로 늘림. 정답은 `AnswerPair_01` 매표기(`SM_Ticket_Machine`)부터 진행 중.
  - **열차 변경 (2026-10-28, 사용자 결정)**: 두 열차의 차량 위치가 하나도 맞지 않아서 **양쪽 모두 `Train_Type B` 3량**(앞차 + `Pantograph carType B` + 앞차(1))으로 바꾸고, **위(왼쪽 섬) 열차 전체를 제자리에서 Y 180°** 돌려 차이를 줌(`Train_Type A`는 아래 섬에 꺼진 채 남아 있음). **판토그래프차 모델은 몸통 중심이 기준점에서 -0.22 치우쳐 있어서**, 객차 위치를 앞차 20.43 / 판토그래프차 **0.22** / 앞차(1) -20.43으로 둬야 이음매 간격이 원래대로 맞고 180° 돌려도 세 객차가 정확히 겹칩니다(원래 값 20.21 / 0 / -20.65는 간격만 맞고 대칭이 아님). 머티리얼 차이: 위 열차는 대차·바퀴 전부 `M_Train_Bogie_Dirty`(갈색 곱하기, Smoothness 0.1) + 띠 `Color2`, 아래 열차는 대부분 `M_Train_Bogie_Clean`(단색 `5C6168`, Smoothness 0.6, Metallic 0.2) + 띠 `Color3` + `Window2`(창문 불 켜짐) + `Body2`(10% 어두움). 머티리얼은 모두 `Materials/Station/`. **`AnswerPair_02` = 아래 열차에서 하나만 Dirty인 바퀴(`Wheel.001`) ↔ 위 열차 같은 자리 바퀴**(180° 회전이지만 회전 대칭으로 통과). 검사 도구는 열차를 루트(통째로) 단위로 비교해서 대차·창문 같은 자식 부품끼리의 "의도하지 않은 쌍둥이"는 잡지 못하니 눈으로 확인합니다. 열차 라이트(앞차 `Body`의 Element 3 = `LightWhite`/`LightRed`)를 바꿀 때는 `LightWhite`를 복사해 Emission 색만 바꾸고 Inspector의 Element 3에만 넣습니다(Scene 창에 끌어다 놓으면 다른 칸에 들어가 테두리가 까맣게 됨).
  - Validate Stage (2026-10-28): 오류 0, 경고 1(정답 2쌍). **다음 할 일**: 소품 키트(`Build Station Props`)로 역 꾸미기와 정답 1~3쌍 추가, 매다는 소품을 위한 지붕(캐노피)이 필요한지 정하기, 열차 객차 `Body`에 MeshCollider(Default)를 붙일지 정하기(지금은 클릭이 열차를 뚫고 뒤 물체에 맞을 수 있음), `Prefabs/Station/Props/Clock Pole` 지우기(아날로그 시계는 디지털 시계로 바꿈).
  - ✅ **소품 Collider·정답 5쌍 정리 (2026-10-29 집)**: 학원에서 정답 5쌍 배치 후 검사 오류 12개가 나왔던 원인은 ① 소품 루트를 손으로 MeshCollider로 바꿔 Mesh가 빈 채로 남은 것(루트는 메시 없는 빈 오브젝트), ② `AnswerPair_05`의 B가 Unpack된 예전 구조 복사본이었던 것. 처음엔 꽉 찬 소품만 루트 BoxCollider로 하려 했으나, **자판기 버튼 하나를 정답으로 두면 상자 겉면에 먼저 맞아 클릭이 안 돼서**(사용자가 MeshCollider로 바꿨던 이유) **모든 소품을 부품마다 MeshCollider로 확정**. `StationPropColliderFixer`로 16개 프리팹을 정리하고 `StationPropsBuilder`도 같은 방식으로 바꿈. `_05`는 Unpack본을 지우고 프리팹을 새로 놓아 `Middle`을 지운 뒤 다시 만듦. 정답: `_01` 거울, `_02` 정지 위치 표지, `_03`·`_04`(학원에서 배치), `_05` 출구 표지판(자판기 버튼 정답 포함 Play 확인). **Validate Stage: 오류 0·경고 0·참고 0.** 검사 도구는 정답 쌍의 Collider만 보므로 미끼 소품 클릭은 Play로 확인합니다. `Prefabs/Station/Props/Clock Pole`(git에 없음)은 아직 지워야 함.
  - ✅ **배경·승강장 마무리 (2026-10-29 집)**: **흐린 날씨**로 정함(번개는 나중에 결정, 넣는다면 해 하나의 밝기·배경색을 0.1~0.2초 번쩍이는 스크립트로 20~40초에 한 번). 추천 값: 두 카메라 Solid Color `9BA3AD`(양쪽 같음), Lighting 창 Environment Lighting Source **Gradient**(Sky 167,174,183 / Equator 133,140,149 / Ground 92,97,104, HDR 창이라 RGB로 넣음), Reflections Intensity 0.5, Directional Light Intensity 0.6·색 `D9DEE6`·Shadow Strength 0.45, 안개는 선택(Linear 30~80, 배경과 같은 색). 플랫폼 `Foundation`으로 승강장 밑의 빈 공간을 채움(선로 바닥은 y -1.832 ~ 0.16·폭 ±7.02라 승강장 바깥 1.6m(x 7.02~8.625) 밑이 비어 있었음). **위 섬(종착역) 선로 끝**은 `Platform Rural 4m`을 Rotation Y 90·Scale (1, 1, 4.3125)로 돌려 두 승강장을 잇는 가로 승강장으로 메움. 위치는 **z 33.9**(34.2에 두면 가장자리 처마 0.3 아래가 비어 틈이 생김). 전등은 넣지 않기로 함(사용자 결정. 넣는다면 Light 없이 Emission 전등 소품 추천: 승강장이 길이 68.4짜리 물체 하나라 URP 물체당 추가 조명 4개 제한에 걸림). Validate Stage 오류 0·경고 0·참고 0.
- **비행기 메모 (예전 2-2 계획, 미룸)**. Polyeler `Passenger Aircraft V1`에서 알아낸 것:
  - 구조: 빈 루트 아래 `Body`(몸통·날개·꼬리가 메시 하나), `PropellerL/R`, `LandingGear`(`Front/Back Landing gear`, `Wheels/Back Wheel L·R`, `Front Wheel`). **Collider가 없으므로** 씬 인스턴스의 `Body`에 Mesh Collider를 붙입니다(프리팹은 고치지 않음). `Front Wheel`만 머티리얼이 `Tex1`(원래 에셋이 그럼).
  - 창문·문·줄무늬·꼬리는 모두 `Tex.png` 한 장에 그려져 있어 머티리얼을 바꾸면 몸통 전체가 바뀝니다(`Tex1`·`Tex2`는 색만 다른 버전). 차이를 만드는 방법: ① `Tex.png` 복사본의 한 구역만 PowerShell로 다시 칠함(꼬리 날개=아래 가운데 파란 사다리꼴, 몸통 줄무늬, 조종석 유리, 엔진 빨간 띠. 축구공 남색과 같은 방식) ② 부품 끄기·회전(검사 도구는 프리팹 루트 단위라 자식 차이는 눈으로 확인) ③ 작은 물체 덧대기.
  - **창문 하나만 정답**: Collider만 두면 판정은 되지만 렌더러가 없어 `ClickFeedback` 연출이 안 나옵니다. 창문 모양 얇은 Quad를 몸통 위 0.01 정도 띄워 `Props/Left` 바로 아래(비행기 자식 아님)에 두고, 자기 머티리얼(Emission 켜고 검정)로 정답 등록합니다. 양쪽 비행기 몸통은 어딘가 달라야 합니다(아니면 의도하지 않은 쌍둥이).
  - `Tex illumination.mat`은 `Tex`와 같은 그림에 Emission(맵 `Tex_illumination.png`: 조종석 앞유리와 객실 창문 줄만 밝음, 세기 1.58)을 켠 **밤 버전**입니다. 창문 불 켜짐/꺼짐을 미끼로 쓰고, 밤 섬 비행기가 전부 불이 켜지면 티가 나니 일부는 양쪽 다 `Tex illumination`으로 둡니다.
  - **주의**: `Tex`·`Tex illumination` 모두 Emission Map이 있어서 정답 반짝임(Emission 색 × 맵)이 창문에만 나옵니다. 비행기 자체를 정답으로 쓰려면 Emission Map을 비운 정답 전용 머티리얼 복사본을 씁니다.
**12단계 메모 (진행 중, 2026-10-31~)**
- ✅ **3-1 = 공중 SF 섬 도시 완료 (2026-11-01)**: Mnostva `Sci_Fi_Island`(Scale 0.4)를 두 Pivot에 두고 `HalfScreenProjection`으로 화면 정중앙에서 반반으로 보이게 함. 정답 5쌍(`AnswerPair_01~05`), 사용자가 Validate Stage 통과 확인.
  - 조명: 우주 배경이라 햇빛 대신 **푸른 달빛** Directional Light 하나(Intensity 0.8, 색 `C8D4FF`). 두 카메라 Solid Color `0B0D1A`(양쪽 같음), FOV 60, Environment Lighting Source Color (70, 80, 140).
  - **미끼 머티리얼 `Materials/Island/M_Island_Hue120.mat`**: 원래 팔레트 `Texture_2048.png`의 색상(Hue)만 +120° 돌린 `T_Island_Hue120.png`를 씀(초록 → 보라, 하늘색 → 분홍, 흰색·회색은 그대로). Emission(검정) 켬. 씬에서 미끼 소품에만 넣고(약 46곳), 정답 물체는 양쪽 모두 원래 `Island_Material`.
  - 가로등 `Streetlight 1`·`2`: 오른쪽 섬은 1·2를 바꿔 놓음(줄 간격·개수가 달라 그룹 회전으로는 못 맞춰 Prefab Replace 권장).
- **낮/밤 화면 나누기는 하지 않습니다**(2026-10-27 논의): 조명이 다르면 정답 쌍둥이도 달라 보여 공정성이 깨집니다. 밤 분위기는 1-2처럼 **배경색·Emission 소품(불 켜진 창문, 별, 달 등, Collider 없음)**으로만 내고, 물체를 비추는 해·주변광은 양쪽 같게 둡니다.

## 확정: 7스테이지 (2026-10-27)

- 구성: Tutorial / 1-1·1-2 / 2-1·2-2 / 3-1·3-2. 씬 번호는 월드 1이 11·12, 월드 2가 21·22, 월드 3이 31·32입니다. (2026-10-22에 10스테이지로 정했으나, 2026-10-27에 시간이 모자라 각 월드의 3번 맵을 취소함, 사용자 결정)
- **월드 1·2·3 모두 필수입니다.** 시간이 모자라면 **뒤에서부터(3-2 → 3-1 → …) 뺍니다.** 월드 3은 투영 코드가 따로 필요해 가장 위험하므로 월드 2까지 먼저 확실히 끝냅니다.
- 월드 1: 지금의 좌우 분할 그대로입니다. **튜토리얼에는 조명 연출을 넣지 않고, 1-1·1-2에는 맵에 어울리는 연출을 넣습니다**(처음에는 1-1도 기본 조명이었으나 2026-10-26에 1-1 취조실 조명으로 바꿈, 사용자 결정).
- 월드 2: 2-1·2-2 모두 가로(위아래) 분할입니다. 카메라 Viewport만 바꾸면 되고 `ClickInteractor`가 그대로 동작합니다. 대각선 분할은 하지 않습니다.
- 월드 3: 좌우 분할 + 월드 1과 같은 오빗 카메라(±80도, 입력 동일)입니다. 물체 하나를 두 버전(A/B)으로 만들어 두 Pivot에 두고, 두 카메라가 **화면 전체 기준 투영**으로 그린 뒤 각자 반쪽만 보여 줍니다. 그래서 물체가 화면 정중앙에서 반반으로 보입니다. Viewport만 반으로 나누면 물체가 각 반쪽 가운데로 옮겨져 월드 1처럼 보이므로, 투영을 조정하는 코드가 필요합니다.
- 조명 연출: 맵마다 어울리는 것을 그때그때 고릅니다.
- 결과 화면: 기록은 최대 6줄(튜토리얼 제외)이라 지금 `Result Panel`(1000 × 750)에 들어갈 것으로 보입니다. 마무리 때 확인합니다.

**나중에 할 일**
- Play 모드 자동 테스트를 만듭니다(`Assets/100. 3D Project-1/Tests/PlayMode/` + asmdef). 지금은 Claude가 Unity에서 Play·클릭을 할 수 없어서 흐름 테스트는 사용자가 직접 하고, Claude는 `[GameManager] 씬 이동` 로그로 확인합니다. **언제 만들지는 미정**입니다(디오라마를 먼저 만들고 13단계 마무리 때 만드는 것을 추천, 2026-10-27).
  - **확인할 흐름 (7스테이지 기준)**: Menu → Start → 튜토리얼 안내 닫기 → 각 스테이지 정답 쌍을 코드로 찾음 처리(`AnswerPair.TryFind`) → 클리어 창 Next → … 순서 7개와 HUD 이름, 튜토리얼 기록 제외, Result 기록 6개와 Total 합계, 3-2 클리어 버튼 "최종 결과", 이미 찾은 쌍 다시 누르기 무시, Result → 메뉴 → 재시작 시 기록 초기화, 일시정지 → 메뉴.
  - **확인하지 못하는 것**: 실제 마우스 클릭(Raycast·PlayArea·Background 레이어·3초 제한)과 화면 모습. 이 부분은 직접 Play와 검사 도구로 봅니다.
  - **예상 시간 약 3~4.5시간**: 테스트 어셈블리 준비 30~45분, 테스트용 연결 20분, 테스트 작성 1~1.5시간, CliBridge `/tests/run`(실행·결과 조회 두 단계) 45분~1시간, 디버깅 30분~1시간(씬을 넘어 살아남는 `GameManager`·`SoundManager` 정리, 씬 로딩 대기, `timeScale`). `/tests/run`을 빼면 약 2~3시간(사용자가 Test Runner 창에서 실행).
  - **asmdef: A안으로 정함(Claude 추천, 사용자 동의 2026-10-27)**. 테스트 asmdef는 Assembly-CSharp을 참조할 수 없으므로, 게임 코드(`Assets/100. 3D Project-1/Scripts`)에 `YoureRight.asmdef`를 추가하고 Editor 도구(`AnswerPairCreator`, `StageValidator`)는 Editor 전용 asmdef로 나눕니다. 처음 한 번 컴파일 오류 정리가 필요합니다. (B안 "Enable playmode tests for all assemblies"는 테스트 코드가 Assembly-CSharp에 섞이고 `#if UNITY_INCLUDE_TESTS`로 감싸야 해서 쓰지 않음)
- 디오라마 아래쪽에도 문제를 둘 예정입니다. 섬 바닥은 두께가 있는 메시로 만들고(Plane은 아래에서 투명하게 보임), 아랫면이 어둡지 않게 조명을 보강합니다.
- 플랫폼·바닥·길 오브젝트는 `Background` 레이어로 둡니다(Collider는 남겨 둬서 클릭을 막게 함). 예전 계획(Terrain을 Clickable Layers에서 빼기)은 클릭이 바닥을 뚫고 지나가는 문제가 있어서 쓰지 않습니다.

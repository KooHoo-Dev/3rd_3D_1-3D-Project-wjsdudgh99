# CLAUDE.md

이 저장소는 Unity 3D 코스 프로젝트입니다. 지금 진행 중인 메인 작업은 **'You're Right' 게임**입니다.
사용자는 학원 컴퓨터와 집 컴퓨터를 오가며 작업하므로, 작업 규칙과 진행 상황은 이 파일에 기록합니다.

## 프로젝트 개요

- **게임**: 'You're Right'. 좌우 분할 화면에 나란히 보이는 두 3D 디오라마를 오빗 카메라로 함께 돌려 보며 **완전히 똑같은 쌍둥이 오브젝트**를 찾는 관찰형 퍼즐 게임입니다. 스테이지는 튜토리얼 1개와 월드 3개 × 3스테이지(월드 1 우선)이고, 스톱워치로 기록을 잽니다.
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
| Core | `GameManager` | `SingletonBase`를 상속하고 DontDestroyOnLoad로 유지됩니다. 씬에 배치하지 않아도 처음 호출될 때 자동으로 만들어집니다. 스테이지별 기록을 모으고, 다음 씬 이동·메인 메뉴·종료를 맡습니다. 씬을 바꿀 때 `timeScale`을 1로 되돌립니다. |
| Core | `SceneNames` | Build Settings에 등록된 씬 이름 상수입니다. 씬 파일 이름을 바꾸면 여기도 고칩니다. |
| Core | `TimeFormatter` | 시간을 `00:00.00` 형식으로 바꿉니다. 반올림하지 않고 내림합니다. |
| Camera | `OrbitCameraController` | 분할 화면의 카메라 2대를 각자의 Pivot 기준으로 같은 각도·거리로 공전시킵니다. `Views`(카메라 + Pivot 쌍)를 씬에서 연결하고, 0번 카메라의 배치 위치가 시작 시점입니다. WASD 또는 우클릭 드래그로 회전하고 휠로 줌하며, Pitch는 -80 ~ 80입니다. `timeScale` 0이면 입력을 무시합니다. |
| Interaction | `ClickInteractor` | 좌클릭한 쪽 화면의 카메라(`Target Cameras` 중 커서가 Viewport 안에 있는 것)에서 Raycast를 쏴서 `AnswerTarget`이면 정답, 다른 물체면 오답으로 판정합니다. **빈 곳(Collider 없음) 클릭은 아무 반응도 하지 않습니다**(이벤트·로그 없음, 사용자 요청). 이벤트는 `OnAnswerClicked`, `OnWrongClicked`(맞은 Collider는 항상 null이 아님)입니다. 일시정지 중, 스테이지 클리어 후(`StageManager.IsCleared`), UI 위 클릭은 무시합니다. **무반응 레이어(`Silent Layers`, 비워 두면 `Background` 레이어 자동 사용)**에 맞으면 Raycast는 막히되(뒤쪽 물체가 대신 맞지 않음) 정답·오답 모두 처리하지 않습니다. 정답 오브젝트라도 Background에 있으면 반응하지 않습니다. 씬에 `PlayArea`가 있으면 맞은 지점(`hit.point`)이 어느 PlayArea에도 없을 때도 무반응입니다(판정 순서: PlayArea → 무반응 레이어 → 정답/오답). `Clickable Layers`는 보통 Everything이고, 여기서 뺀 레이어는 클릭이 뚫고 지나갑니다. |
| Feedback | `ClickFeedback` | `Managers/ClickFeedback`에 붙입니다. 정답일 때 쌍둥이 둘의 머티리얼 복사본에 황금빛 Emission을 올렸다가(0.25초) 은은하게 남기고(0.6초 동안 `Glow Settle Intensity`까지), 기본 색도 금색 쪽으로 섞습니다(Emission 없는 셰이더 대비). 렌더러 윗면에 `Sparkle` 파티클을 만듭니다. **오답 흔들림은 보류 중**이라 `[보류]` 표시와 함께 `//` 주석으로 막혀 있고, 주석을 풀면 다시 동작합니다. |
| Feedback | `WrongClickMarker` | 스테이지 `Canvas`에 붙입니다. 오답 물체를 클릭하면 커서 위치에 `Prefabs/Wrong Marker`(빨간 TMP `X`, Raycast Target 끔)를 띄우고, 살짝 튀었다가 0.5초 안에 사라지게 합니다. |
| Stage | `AnswerTarget` / `AnswerPair` | 쌍둥이 2개를 묶어서 한쪽만 클릭해도 쌍 전체를 정답 처리합니다. 이벤트는 `OnFound`입니다. |
| Stage | `StageManager` | 씬의 `AnswerPair`를 자동으로 모읍니다. 진행도·클리어 이벤트를 보내고 `StageTimer`를 돌립니다. 씬에 `TutorialGuide`가 있으면 안내를 닫는 순간(`OnClosed`) 타이머를 켜고, 없으면 `Start`에서 켭니다. 켠 뒤 **첫 번째 `Update`는 건너뜁니다**(로딩 직후 큰 deltaTime 제외. 에디터 첫 Play에서는 Start와 첫 Update가 다른 프레임일 수 있어 프레임 번호 비교로는 막지 못했음). 클리어 후 1.5초 뒤 `GameManager.ReportStageClear`를 부릅니다. `Add To Records`를 끄면(튜토리얼) 시간은 HUD에만 보이고 결과 기록·Total에는 들어가지 않습니다(`GameManager`가 "기록 제외" 로그만 남김). |
| Stage | `PlayArea` | 섬마다 하나씩 두는 보이지 않는 구역 상자입니다(`Stage/PlayArea Left`·`PlayArea Right`, Pivot과 같은 x ±100). 물리 Collider 없이 `Contains(점)`으로 계산하며, Transform의 위치·회전·스케일을 따릅니다. `Center`(로컬)·`Size`(기본 20×15×20)를 쓰고, **`Mirror Of`를 지정하면 그 상자의 Center/Size를 그대로 따라갑니다**(오른쪽 → 왼쪽 지정, 한 단계만). 빨간 선 기즈모는 항상 보이고, 선택하면 반투명 면도 칠합니다. 씬에 PlayArea가 하나도 없으면 모든 곳이 반응합니다. |
| Stage | `StageTimer` | 일반 C# 클래스로 된 스톱워치입니다. `Time.deltaTime`을 쓰므로 일시정지 중에는 멈춥니다. |
| UI | `StageHUD` | 스테이지 이름, 스톱워치(`<mspace>`로 숫자 폭 고정), `( 2 / 5 )` 진행도를 표시합니다. |
| UI | `PauseController` | Esc 키로 일시정지합니다(키 값은 프리팹에 저장된 `pauseKey`). 클리어 후와 `TutorialGuide`가 열려 있는 동안에는 새로 일시정지하지 않습니다. 항상 켜져 있는 루트에 붙이고, 자식 Popup만 켜고 끕니다. `Prefabs/Pause Menu.prefab`에 들어 있습니다. |
| UI | `TutorialGuide` | 스테이지 시작 전 안내 패널입니다(`Prefabs/Tutorial Guide.prefab`, 튜토리얼 Canvas 맨 아래). `Start`에서 열리며 `timeScale` 0으로 카메라·클릭을 멈추고, `pages` 배열을 이전/다음 버튼으로 넘깁니다. 버튼은 페이지 바깥(Window 바로 아래)에 한 벌만 둡니다(페이지 안에 넣으면 페이지를 끌 때 같이 사라짐). 마지막 페이지의 시작 버튼을 누르면 패널을 끄고 `timeScale`을 1로 되돌린 뒤 `OnClosed`를 보냅니다. 페이지 글자는 씬에서 직접 씁니다(한국어, Noto Sans KR). 패널 오브젝트는 씬 시작 때 켜져 있어야 합니다. 안내 중에는 SoundManager가 BGM을 30%로 낮춥니다(그대로 두기로 함). |
| UI | `MainMenuUI` / `ResultUI` | 메뉴의 Start/Exit 버튼과, 결과 화면의 기록 목록·Total Time·패널 등장 연출입니다. 버튼 OnClick은 코드에서 연결합니다. |
| Sound | `SoundManager` | 사용자가 뼈대(필드·함수 이름)를 쓰고 Claude가 보완했습니다. `SingletonBase`를 상속하고, `Prefabs/SoundManager.prefab`을 **모든 씬의 `Managers` 아래**에 둡니다. 처음 것만 살아남고(이때 `SetParent(null)`로 루트로 빠진 뒤 DontDestroyOnLoad), 다음 씬의 복제본은 경고 없이 사라집니다. AudioSource 2개(BGM 반복 재생 / SFX)는 칸이 비어 있으면 자동으로 만듭니다. 같은 BGM이 재생 중이면 다시 틀지 않습니다. `timeScale` 0이면 BGM을 `pausedBgmVolumeRatio`(30%)로 부드럽게 줄입니다. `sceneLoaded`마다 그 씬의 `ClickInteractor`(정답·오답 SFX)와 모든 `Button`(꺼진 것 포함, 클릭 SFX)에 자동으로 연결합니다. 클립이 null이면 재생하지 않습니다. |
| Editor | `AnswerPairCreator` | 에디터 전용(빌드 제외). **Tools → You're Right → Create Answer Pair**: 왼쪽·오른쪽 섬 물체를 하나씩 선택해 실행하면, 두 루트에 Collider와 `AnswerTarget`을 붙이고 `Props/AnswerPair_NN`(가장 큰 번호 + 1)을 만들어 연결합니다(PlayArea 기준 왼쪽이 A). Collider는 루트에 이미 있으면 그대로 두고(Trigger·꺼짐이면 고침), 없으면 루트 메시가 있을 때 MeshCollider, 메시가 자식에만 있으면 자식 렌더러 전체를 감싸는 BoxCollider를 붙입니다. 2개가 아님·프리팹 에셋·Play 모드·같은 섬·PlayArea 밖·이미 등록(부모·자식 포함)·Background 레이어(자식 Collider 포함)·`Props` 없음이면 거부합니다. 메시·머티리얼 구성 차이와 Emission 꺼짐은 경고만 합니다. Ctrl + Z 한 번으로 전부 되돌립니다. |
| Utils | `SimpleInput`, `SingletonBase` | `19. Legacy/9. Utils`에서 복사해 네임스페이스를 바꾼 파일입니다. |

**씬** (Build Settings 순서): `00. Menu` → `10. Stage Tutorial` → `11. Stage 1-1` → `12. Stage 1-2` → `13. Stage 1-3` → `90. Result`
- 스테이지 씬 4개는 같은 구조이고, `StageNameText`의 글자와 `StageManager`의 Stage Name만 다릅니다. `Stage` 아래에는 임시 섬 바닥 `Stage Left`·`Stage Right`(큐브 20 × 2 × 20, y -2, Background 레이어)와 `PlayArea Left`·`PlayArea Right`(y 6.5)가 있습니다. 지금은 큐브로 된 테스트용 정답 쌍(`AnswerPair_01`: 왼쪽 섬 `Cube`, 오른쪽 섬 `Cube (2)`)과 섬마다 미끼 큐브 하나가 배치되어 있습니다.
- 분할 화면 구성: `Main Camera`는 Viewport X 0 / W 0.5, `Right Camera`는 X 0.5 / W 0.5이고, 둘 다 Far Clip 60입니다. `MainCamera` 태그와 Audio Listener는 `Main Camera`에만 있습니다. Canvas 첫 자식인 `Divider`(Width 4, 위아래 stretch, Raycast Target 끔)가 화면을 가르고, `TimerText` 바로 위의 `TimerBackground`가 분할선을 가립니다.
- **예비 씬**: `Scenes/Template/Stage Template.unity`. 스테이지 씬과 같은 구성이고 이름만 `New Stage`입니다. Build Settings와 `SceneNames`에는 넣지 않습니다. 스테이지를 늘릴 때는 다음 순서로 합니다.
  1. Project 창에서 Ctrl + D로 복제합니다(새 GUID가 생김). `Scenes`로 옮기고 이름을 `21. Stage 2-1`처럼 바꿉니다(월드 2는 21~23, 월드 3은 31~33).
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
| 10 | 월드 1 디오라마 제작 (Synty 등): Tutorial + 1-1~1-3, 스테이지마다 정답 3~5쌍 배치 | **다음** |
| 10.5 | 1-2·1-3 조명 연출 (디오라마에 맞춰 고름), 월드 1 마무리 | |
| 11 | (여유가 있으면) 월드 2: 가로 분할, 2-1~2-3 디오라마, 결과 화면 레이아웃, 맵별 조명 연출 | |
| 12 | (여유가 있으면) 월드 3: 반반 투영 코드, 3-1~3-3 물체, 맵별 조명 연출 | |

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
  3. **스테이지 검사** 에디터 도구: 쌍둥이 동일성(메시·머티리얼·크기·회전·섬 중심 기준 위치·자식 구성), **의도하지 않은 쌍둥이**(등록 안 했는데 양쪽 같은 위치에 같은 물체), 연결·Collider(없음·꺼짐·Trigger), 레이어(정답이 Background, 큰 바닥이 Default), PlayArea 밖의 정답, 정답 개수 3~5, 정답 머티리얼 Emission을 봅니다. 로그를 클릭하면 오브젝트가 선택되고, 씬은 고치지 않습니다. 예상 2시간입니다.
  - 도구는 `Scripts/Editor/`에 두고(빌드 제외), 메뉴는 `Tools → You're Right`로 제안해 두었습니다.
  - Synty Apocalypse 프리팹은 약 80%에 Collider가 있고 모두 Default 레이어입니다. 그래서 플랫폼·길 조각은 Background로 바꿔야 합니다.

## 확정: 10스테이지 확장 (2026-10-22)

- 구성: Tutorial / 1-1~1-3 / 2-1~2-3 / 3-1~3-3. 씬 번호는 월드 1이 11~13, 월드 2가 21~23, 월드 3이 31~33입니다.
- **월드 1에 먼저 집중합니다.** 시간이 모자라면 월드 1까지만 완성해서 제출합니다.
- 월드 1: 지금의 좌우 분할 그대로입니다. **튜토리얼과 1-1에는 조명 연출을 넣지 않고, 1-2·1-3에는 맵에 어울리는 연출을 넣습니다**(사용자 결정: 기본에 충실한 뒤 커스텀).
- 월드 2: 2-1~2-3 모두 가로(위아래) 분할입니다. 카메라 Viewport만 바꾸면 되고 `ClickInteractor`가 그대로 동작합니다. 대각선 분할은 하지 않습니다.
- 월드 3: 좌우 분할 + 월드 1과 같은 오빗 카메라(±80도, 입력 동일)입니다. 물체 하나를 두 버전(A/B)으로 만들어 두 Pivot에 두고, 두 카메라가 **화면 전체 기준 투영**으로 그린 뒤 각자 반쪽만 보여 줍니다. 그래서 물체가 화면 정중앙에서 반반으로 보입니다. Viewport만 반으로 나누면 물체가 각 반쪽 가운데로 옮겨져 월드 1처럼 보이므로, 투영을 조정하는 코드가 필요합니다.
- 조명 연출: 1-2부터 맵마다 어울리는 것을 그때그때 고릅니다.
- 결과 화면은 스테이지가 7개 이상이 되면(월드 2 추가 때) 두 줄 또는 월드별 소계로 바꿉니다.

**나중에 할 일**
- Play 모드 자동 테스트를 만듭니다(`Assets/100. 3D Project-1/Tests/PlayMode/` + asmdef). 확인할 흐름: Menu → Start → 스테이지 4개 정답 처리 → Result 기록 4개와 Total 합계 → 메인 메뉴 → 재시작 시 기록 초기화, 오답·중복 클릭은 무시. 사용자가 집 컴퓨터에서 확인을 마친 뒤 진행하기로 했습니다. CliBridge에 `/tests/run` 엔드포인트를 추가해 Claude가 직접 실행하는 방식을 제안해 둔 상태입니다.
- 디오라마 아래쪽에도 문제를 둘 예정입니다. 섬 바닥은 두께가 있는 메시로 만들고(Plane은 아래에서 투명하게 보임), 아랫면이 어둡지 않게 조명을 보강합니다.
- 플랫폼·바닥·길 오브젝트는 `Background` 레이어로 둡니다(Collider는 남겨 둬서 클릭을 막게 함). 예전 계획(Terrain을 Clickable Layers에서 빼기)은 클릭이 바닥을 뚫고 지나가는 문제가 있어서 쓰지 않습니다.

# 씬 연결과 분할 화면 설정

## 실행 흐름

빌드의 첫 씬은 `CoreScene`입니다. 기본 흐름은 **코어 → 게임 제너럴 → 배틀 + 아이템**입니다. 코어를 통해 시작했을 때는 설정 Canvas·로딩 Canvas·CurrencyManager·GameSession을 유지하고 나머지 화면을 Additive로 교체합니다.

```text
CoreScene (항상 유지)
 ├─ GameGeneralScene (기본 시작 화면)
 │   ├─ GameBattleScene
 │   └─ GameItemScene
 └─ UpgradeScene → GameGeneralScene
```

`CoreScene → Core Scene Bootstrap`에서 다음을 설정합니다.

| 필드 | 의미 |
| --- | --- |
| Auto Load Startup Scene | 기본 켜짐. 끄면 코어에서 다른 씬으로 자동 이동하지 않음 |
| Startup Scene | 기본 `GameGeneralScene`. `MainScene`, `GameBattleScene`, `GameItemScene` 등 원하는 목적지 입력 가능 |
| Only When Core Is Active | 기본 켜짐. 다른 활성 씬 옆에 코어만 추가했을 때 시작 화면으로 이동하는 것을 방지 |

`GameGeneralScene`은 통합 실행용입니다. **Load Core Scene**은 기본 켜짐이며 설정 창·재화·세션이 있는 코어를 먼저 불러옵니다. 이미 코어가 있으면 다시 불러오지 않습니다. **Auto Load Children**도 기본 켜짐이며 배틀과 아이템 씬을 함께 엽니다. 각 옵션을 끄면 해당 자동 로딩을 생략합니다. 배틀/아이템 씬을 직접 실행할 때는 이 옵션이 적용되지 않습니다.

## 각 씬을 독립 테스트하기

아이템 씬의 조명은 **Sorting Layer `GameItemWorld`**로 분리되어 있습니다. 배틀의 Global Light 2D는 Default/Enemy, 아이템의 Global Light 2D는 GameItemWorld만 비춥니다. URP의 Global Light는 Additive 씬 사이에서도 같은 Sorting Layer를 공유하므로 카메라의 Culling Mask만 분리하면 중복 조명이 생길 수 있습니다.

아이템 프리팹의 SpriteRenderer·SortingGroup·SpriteMask와 아이템 씬 Canvas도 GameItemWorld를 사용합니다. 기존 Order in Layer는 유지하며, GameItemWorld는 Default보다 아래에 두어 코어 설정/로딩 UI가 아이템 화면 위에 표시되도록 합니다. 새 머지 아이템은 기존 MergeItemObject의 Prefab Variant로 만들면 이 설정을 상속합니다. 독립 프리팹을 직접 만들 경우 해당 컴포넌트들의 Sorting Layer도 GameItemWorld로 설정하세요. 여기서 Sorting Layer는 제너럴의 카메라 분리용 GameObject Layer `GameItem`과 별개입니다.

기본 Editor 설정은 **Current Scene Only**입니다. 배틀 씬을 열고 Play하면 배틀만, 아이템 씬을 열고 Play하면 아이템만 실행합니다. 다른 씬을 편집 중 함께 열어 뒀어도 현재 활성 씬 에셋만 실행합니다. 제너럴을 선택한 경우에는 제너럴의 Load Core Scene / Auto Load Children 설정을 따릅니다. 종료하면 원래 편집 상태로 돌아갑니다.

**Catnip → Scenes → Select Play Settings**를 누르거나 `Assets/Data/ScenePlaySettings.asset`을 선택하고 **Play Mode**를 바꾸세요.

| Play Mode | 배틀 씬에서 Play했을 때 |
| --- | --- |
| Current Scene Only (기본) | 배틀 씬만 실행. 코어·제너럴·아이템을 자동으로 불러오지 않음 |
| Current Scene With Core | 공용 매니저를 먼저 초기화한 뒤 **코어 + 배틀만** 실행. 코어의 Startup Scene으로 이동하지 않음 |
| Core Startup Scene | 코어부터 시작하고 코어의 Auto Load Startup Scene / Startup Scene 설정을 사용 |

Current Scene Only에서도 코어 씬을 직접 실행하면 코어의 시작 설정을 따릅니다. 따라서 기본 상태에서 코어 Play는 제너럴로, 배틀 Play는 배틀 단독으로, 아이템 Play는 아이템 단독으로 시작합니다.

설정 창·오디오·재화·게임 세션까지 함께 테스트하려면 **Current Scene With Core**를 선택하세요. 코어 없이 실행하면 이 공용 기능은 제공되지 않으며, 상점 구매처럼 재화가 필요한 동작은 기존 매니저의 연결 확인을 따릅니다. 전투 결과 확정은 코어 재화 매니저가 없어도 예외 없이 처리합니다. 없는 매니저나 UI를 코드로 새로 만들지는 않습니다.

각 씬에는 독립 실행용 EventSystem과 씬 이동 연결이 있습니다. **EventSystem과 Input System UI Input Module 컴포넌트는 편집 모드에서 꺼 두고, Scene Event System Owner만 켜 둡니다.** Owner의 **Input Modules**에는 같은 오브젝트의 입력 모듈을 연결합니다. 꺼진 초기 상태는 입력을 사용하지 않는다는 뜻이 아니라, 씬 로딩 시 Unity가 먼저 중복 등록하는 것을 막기 위한 설정입니다.

`SceneEventSystemOwner`는 OnEnable/OnDisable에서 직접 등록·해제하며, 전역 객체 검색 없이 코어 우선으로 하나만 활성화합니다. 이전 소유자를 먼저 끈 뒤 새 소유자를 켜므로 교체 중에도 두 EventSystem이 겹치지 않습니다. 코어가 없으면 해당 씬의 입력이 켜지고, 코어가 제거되면 남아 있는 씬이 입력을 이어받습니다. `EventSystem.current`는 Unity의 자체 등록 과정에 맡깁니다. 편집 중 코어 UI를 함께 확인하려면 **Catnip → Scenes → Open Core Additively**를 사용하세요.

오디오도 같은 방식으로 **Audio Listener는 편집 모드에서 꺼 두고 Scene Audio Listener Owner만 켭니다.** 실행하면 Owner가 자동으로 하나를 켭니다. 코어는 Priority 0의 예비 리스너이며 코어 카메라가 꺼져 있어도 동작합니다. 배틀·메인·스토리·업그레이드 카메라는 100, 아이템 카메라는 50으로 화면을 담당하는 씬을 우선합니다. 제너럴에는 카메라가 없어도 코어 로딩 전/자동 로딩을 끈 단독 실행에서 사용할 예비 리스너(-50)가 컨트롤러에 있습니다.

일반 씬의 **Require Enabled Camera**는 켜 둡니다. General에서 Battle Only / Item Only로 바꾸면 켜진 화면 카메라로 오디오를 넘기고, 화면 씬이 모두 내려가면 코어로 돌려줍니다. 넘길 때 이전 리스너를 먼저 끄므로 중복 활성화하지 않습니다. 새 씬에서는 기존 카메라의 Audio Listener에 SceneAudioListenerOwner를 붙이고 같은 초기 상태를 사용하세요. UI를 생성할 필요는 없습니다.

기존 UI의 모양과 배치는 직접 편집합니다. UI를 생성하는 코드나 생성 메뉴는 추가하지 않았습니다. Main의 중복 설정·로딩 Canvas와 씬별 중복 EventSystem은 코어 쪽으로 통일했고, 기존 스토리 초기화 버튼은 코어 설정 Canvas로 옮겼습니다.

## 게임 화면 비율

`GameGeneralScene`을 열고 Hierarchy에서 **Game General Controller**를 선택합니다. `GameGeneralSceneController`의 Inspector에서 조절합니다.

| 필드 | 의미 |
| --- | --- |
| Layout | Side By Side: 좌우 / Top And Bottom: 상하 / Battle Only / Item Only |
| Battle Ratio | 전투 화면이 차지하는 비율. 기본 0.5, 0.7은 전투 70%·아이템 30% |
| Battle First | 켜면 전투가 왼쪽/위쪽. 끄면 오른쪽/아래쪽 |
| Gap | 두 화면 사이 간격. 해당 축의 전체 화면 대비 비율이며 기본 0 |
| Fit Scene Canvases | 기존 배틀·아이템 Canvas를 각 카메라에 연결해 해당 영역에 표시. 코어 Canvas는 전체 화면 유지 |
| Battle Scene / Item Scene | 함께 불러올 씬 이름 |
| Load Core Scene | 기본 켜짐. 제너럴에서 시작해도 코어의 설정 창·재화·세션 사용 |
| Auto Load Children | 기본 켜짐. 제너럴에서 배틀·아이템 씬 자동 로딩 |
| Item Layer | 아이템 씬의 오브젝트를 표시할 전용 레이어. 기본 31번 `GameItem` |
| Isolate Item Physics | 아이템 씬에 별도 PhysicsScene2D 사용. 제너럴 컨트롤러가 FixedUpdate에서 시뮬레이션 |

`Battle Ratio` 등은 Play 중에도 바꿀 수 있습니다. Play 중 바꾼 값은 종료 후 저장되지 않으므로 최종값은 편집 모드에서 지정하세요. 직접 만든 Slider의 **On Value Changed → Dynamic float → SetBattleRatio**로 연결해도 됩니다. 새 UI를 코드에서 만들지는 않습니다.

기존 Overlay Canvas는 실행 중 각 씬의 카메라에 연결됩니다. Canvas 자식·앵커·글꼴·색상은 다시 만들거나 재배치하지 않습니다. 이 자동 연결을 끄려면 `Fit Scene Canvases`를 끄고 직접 만든 Canvas의 Render Mode를 Screen Space - Camera로 설정해 해당 씬의 카메라를 지정하세요. UI에 카메라를 연결할 때 캔버스 정렬 순서와 Plane Distance는 Unity Play에서 확인하세요.

현재 `GameItemScene`에는 카메라만 있으므로 오른쪽에는 배경만 표시됩니다. 서브 게임 오브젝트와 UI는 이 씬에 직접 만드세요. 카메라 Orthographic Size로 각 영역에 보이는 월드 범위를 조절할 수 있습니다.

## 설정 버튼 연결

기존 Main·배틀·업그레이드 설정 버튼은 같은 `SettingBtn` 프리팹의 `SettingUIButton`을 통해 코어 설정 창을 엽니다. 다른 씬의 설정 Canvas를 버튼 필드에 직접 끌어 넣을 필요가 없습니다.

새 버튼은 직접 만든 후 다음과 같이 연결합니다.

1. 버튼에 `SettingUIButton` 컴포넌트를 붙입니다.
2. Button → **On Click()**의 `+`를 누르고 그 버튼 오브젝트를 끌어 넣습니다.
3. **SettingUIButton → OpenSetting()**을 선택합니다.
4. 닫기 버튼은 **SettingUIButton → CloseSetting()**에 연결합니다. 코어 설정 Canvas의 기존 닫기 버튼은 이미 연결되어 있습니다.

기존 `Is Time Stop` 필드는 기존 동작을 유지합니다. **꺼져 있으면** 설정 창을 열 때 일시정지하고, **켜면** 시간 진행을 유지합니다. 배틀의 Esc도 코어 설정 창을 열고 닫습니다. 설정을 닫으면 열기 전 시간 배율과 전투 입력 차단 상태를 복원하므로 결과 창 등에서 이미 멈춘 게임을 재개하지 않습니다.

스토리·아이템처럼 설정 버튼이 없는 씬에 버튼이 필요하면 위 방식으로 직접 만들어 연결하면 됩니다. 코어가 로드되어 있으면 설정 창을 모든 화면에서 사용할 수 있습니다. 튜토리얼·스토리 초기화 버튼도 현재 씬의 오브젝트에 대한 직접 참조 없이 작동합니다.

## 씬 이동과 공용 데이터

기존 버튼의 로컬 SceneMoveManager 연결은 코어 로더로 전달됩니다. 새 버튼은 기존 연결 컴포넌트의 다음 메서드에 연결하세요. 씬 사이에 코어 오브젝트를 직접 직렬화 참조하지 않습니다.

| 메서드 | 이동 대상 |
| --- | --- |
| GoGameScene() | GameGeneralScene + 배틀 + 아이템 |
| GoUpgradeScene() | UpgradeScene |
| GoMainScene() | MainScene |
| GoStoryScene() | StoryScene |
| LoadScene(string) | 이름에 맞는 화면 |
| ReloadCurrentScene() | 현재 화면. 제너럴이면 두 하위 씬도 재생성 |

코드에서는 `SceneMoveManager.instance.LoadScene(...)`을 사용합니다. 코어가 있으면 코어를 유지하는 Additive 전환을, 없으면 독립 실행용 Single 전환을 사용합니다. 예전 데이터의 `GameScene`, `Main` 이름만 새 진입점으로 변환하며 `GameBattleScene`과 `GameItemScene`은 그대로 유지합니다.

재화 표시는 직접 만든 UI에 `CurrencyUIGroup`을 붙이고 **Currency UIs → Type / Text Obj / Text UI**를 연결합니다. 활성화되면 코어 CurrencyManager에 등록되어 현재 값을 표시하며 씬이 내려가면 해제됩니다. 코어 매니저에 다른 씬의 텍스트를 직접 연결할 필요가 없습니다.

아이템 씬 오브젝트는 실행 시 `GameItem` 레이어로 분리되어 전투 카메라에 섞이지 않습니다. 런타임 생성 오브젝트는 `GameGeneralSceneController.RegisterItemObject(instance)`로 해당 씬과 레이어에 등록할 수 있습니다. 별도 물리를 사용할 때 검색·레이캐스트는 컨트롤러의 `ItemPhysics`를 사용하세요. 정적 `Physics2D` 검색은 기본 전투 물리 공간을 검색합니다.

## 확인 방법

```bash
python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation
```

클라우드에서는 관련 C# 컴파일, 실제 씬/프리팹 참조, 순수 화면 비율·씬 이름 변환 계산을 검증합니다. Unity 렌더링·버튼 입력·Additive 비동기 로딩을 실행하는 검사는 아닙니다.

Unity에서는 Test Runner → EditMode → `SceneFlowTests`를 실행하세요. 기본 모드에서 배틀·아이템·메인·스토리·업그레이드를 각각 Play해 해당 씬만 열리는지 확인합니다. 코어에서 Play했을 때는 기본 제너럴 진입을, With Core 모드에서는 코어 + 선택 씬만 열리는지 확인합니다. Auto Load Startup Scene을 끄거나 Startup Scene을 변경하는 경우도 확인 대상입니다. 제너럴 통합 실행에서는 설정·재화·분할 비율과 UI 입력을 확인하며 오른쪽 클릭이 배틀 입력을 발생시키지 않아야 합니다.

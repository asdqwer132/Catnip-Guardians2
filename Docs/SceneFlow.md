# 씬 연결과 분할 화면 설정

## 실행 흐름

빌드의 첫 씬은 `CoreScene`입니다. 코어의 설정 Canvas·로딩 Canvas·EventSystem·CurrencyManager·GameSession은 유지하고 나머지 화면을 Additive로 교체합니다.

```text
CoreScene (항상 유지)
 ├─ MainScene → StoryScene → GameGeneralScene
 ├─ GameGeneralScene
 │   ├─ GameBattleScene
 │   └─ GameItemScene
 └─ UpgradeScene → GameGeneralScene
```

`Core Scene Bootstrap → Startup Scene`의 기본값은 `MainScene`입니다. `GameGeneralScene`은 두 하위 씬을 불러오는 진입점입니다. 업그레이드 진입 시 세 게임 씬을 함께 내리고, 전투 복귀 시 다시 함께 엽니다. 재화와 GameSession 데이터는 코어에 남습니다. 여기서 유지는 씬 사이의 유지를 뜻하며 새 디스크 저장 기능은 아닙니다.

Unity에서 어느 씬을 열고 Play해도 `CoreScenePlayBootstrap`이 코어부터 실행한 뒤 열어 둔 화면으로 이동합니다. 배틀/아이템 씬에서 Play하면 제너럴을 통해 두 게임 씬이 함께 열립니다. Play를 종료하면 원래 편집 씬으로 돌아옵니다. 편집 중 코어 UI를 함께 확인하려면 **Catnip → Scenes → Open Core Additively**를 사용하세요.

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

스토리·아이템처럼 설정 버튼이 없는 씬에 버튼이 필요하면 위 방식으로 직접 만들어 연결하면 됩니다. 코어의 설정 창 자체는 모든 화면에서 사용할 수 있습니다. 튜토리얼·스토리 초기화 버튼도 현재 씬의 오브젝트에 대한 직접 참조 없이 작동합니다.

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

코드에서는 `SceneMoveManager.instance.LoadScene(...)`을 사용합니다. `SceneManager.LoadScene(..., Single)`을 직접 호출하면 코어도 내려가므로 화면 전환은 공통 로더를 거칩니다. 예전 데이터에 남은 `GameScene`, `Main` 이름은 새 진입점으로 변환합니다.

재화 표시는 직접 만든 UI에 `CurrencyUIGroup`을 붙이고 **Currency UIs → Type / Text Obj / Text UI**를 연결합니다. 활성화되면 코어 CurrencyManager에 등록되어 현재 값을 표시하며 씬이 내려가면 해제됩니다. 코어 매니저에 다른 씬의 텍스트를 직접 연결할 필요가 없습니다.

아이템 씬 오브젝트는 실행 시 `GameItem` 레이어로 분리되어 전투 카메라에 섞이지 않습니다. 런타임 생성 오브젝트는 `GameGeneralSceneController.RegisterItemObject(instance)`로 해당 씬과 레이어에 등록할 수 있습니다. 별도 물리를 사용할 때 검색·레이캐스트는 컨트롤러의 `ItemPhysics`를 사용하세요. 정적 `Physics2D` 검색은 기본 전투 물리 공간을 검색합니다.

## 확인 방법

```bash
python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation
```

클라우드에서는 관련 C# 컴파일, 실제 씬/프리팹 참조, 순수 화면 비율·씬 이름 변환 계산을 검증합니다. Unity 렌더링·버튼 입력·Additive 비동기 로딩을 실행하는 검사는 아닙니다.

Unity에서는 Test Runner → EditMode → `SceneFlowTests`를 실행하세요. 이후 각 씬에서 Play하여 코어가 하나만 유지되는지, Main → Story → 제너럴 → 결과 → 업그레이드 → 제너럴 흐름, 설정 열기/닫기와 재화 갱신, 좌우·상하 비율 변경을 확인하세요. 오른쪽 영역을 클릭했을 때 배틀의 이동/아이템 사용 입력이 발생하지 않아야 합니다.

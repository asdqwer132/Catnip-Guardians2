# 게임 설정 가이드

Unity Inspector와 프로젝트 데이터에서 자주 바꾸는 설정을 모았다. 씬 배치와 컴포넌트 연결은 씬에 저장하고, 여러 씬 또는 슬롯에서 공유하는 값은 해당 ScriptableObject·프리팹·JSON을 수정한다.

## 1. 실행 씬과 화면 비율

| 확인할 화면 | 열 씬 |
| --- | --- |
| 실제 전투 화면 분할과 씬 조합 | `Assets/Scenes/GameGeneralScene.unity` |
| 배틀 카메라와 전투 UI | `Assets/Scenes/GameBattleScene.unity` |
| 핀볼 보드와 투입 UI | `Assets/Scenes/GameItemScene.unity` |
| 스킬트리, 상점, 장착, 포션, 노트 | `Assets/Scenes/UpgradeScene.unity` |
| 비주얼 노벨 | `Assets/Scenes/StoryScene.unity` |

1. 제너럴 씬의 `Game General Controller`에서 `Viewport Source`가 `UI Regions`인지 확인한다.
2. Overlay Canvas에서 `Battle Area`와 `Item Area`로 연결된 RectTransform의 앵커, 위치, 크기를 조정한다. 제너럴 씬에서 부팅하면 이 두 영역을 직접 읽어 카메라에 적용한다.
3. 제너럴 씬을 저장하면 공유 화면 배치 프로필이 자동으로 갱신된다. 배틀·아이템 씬을 각각 열어 Game 뷰에서 해당 영역을 확인한다. 프로필만 다시 갱신해야 할 때는 컨트롤러 메뉴 `⋮ > Save UI Regions To Screen Profile`을 실행한다. [화면 배치 프로필](../Assets/Data/Scriptable/Scene/GameScreenLayoutProfile.asset)
4. 게임 전체 월드의 픽셀 밀도와 기준 높이는 프로필의 `World Pixels Per Unit`, `World Reference Height`에서 관리한다. 화면 분할 폭은 2단계에서 조정하고, Canvas UI 스케일은 공유 `SceneUIScaleProfile`에서 조정한다. [UI 스케일 프로필](../Assets/Data/Scriptable/Scene/SceneUIScaleProfile.asset)

## 2. 아이템 씬의 핀볼 보드

- **벽과 투입구** — `MergeWallUIFitter`의 `Frame`과 `Inlet Anchor`가 기준이다. 각 벽의 두께를 개별로 설정할 수 있고, 별도의 벽 RectTransform을 연결하면 그 영역이 우선한다.
- **핀과 배출구** — `Shared Merge Box > Pinball Physics`에서 `Pin 01`부터 `Pin 28`, 좌우 경사로, 배출구 트리거를 찾는다. `Pinball Board > Fit To UI`를 켜면 현재 화면의 UI 영역에 맞춰 핀·경사로·배출구가 배치된다. 이 상태에서는 `Pin Size`, `Ramp Rise`, `Outlet Width`, `Maximum Item Radius`를 조정한다. 오브젝트의 Transform과 콜라이더를 직접 편집하려면 `Fit To UI`를 끈다.
- **핀 배치 원본** — 업그레이드 씬의 백 패널에서 `Equip Board > Pins` 배치를 수정한다. 보드가 에디터 Hierarchy에서 활성화된 상태에서 `BoardEquipmentPanelUI > ⋮ > Capture Pinball Layout`을 실행하고 에셋을 저장한다. 핀 배열 순서는 장착 슬롯 인덱스와 일치해야 한다. 캡처는 위치와 크기를 저장하며, 슬롯 수를 바꿨다면 아이템 씬의 `Scene Pins` 배열도 같은 순서와 개수로 맞춘다. [핀 배치 에셋](<../Assets/Data/Scriptable/Items/BoardPins/Pinball Board Layout.asset>)
- **핀 효과** — `BoardPinData > Hit Effects`에 튕김·합성 등의 효과 에셋을 순서대로 연결한다. `World Prefab`은 선택 사항이다. 핀을 교체할 프리팹은 루트에 `PinballPin`과 `CircleCollider2D`, 하위 또는 루트에 `SpriteRenderer`가 있어야 한다. 빈 슬롯에는 `Pinball Board > Default Pin`이 들어간다. [핀과 효과 에셋](../Assets/Data/Scriptable/Items/BoardPins)
- **기즈모 읽기** — 노란 사각형은 `MergeWallUIFitter`가 투영한 UI 기준 영역, 초록 사각형은 배경 스프라이트 범위다. 핀의 청록 원과 경사로·배출구 표시는 실제 콜라이더를 그린다.

## 3. 장착, 시작 재고, 저장

| 설정 | 위치 |
| --- | --- |
| 일반 새 게임의 시작 아이템 | 업그레이드 씬의 `ItemInitManager > Items`, `Set Amount` |
| 일반 시작 재화 | 사용 중인 `CurrencyManager > Initial Amounts` |
| 디버그 모드 | 배틀의 `GameManager > Debug Mode`, 업그레이드 단독 실행은 `UpgradeManager > Debug Mode` |
| 디버그 시작 수량 | [UpgradeDebugStart.json](../Assets/Resources/Data/UpgradeDebugStart.json) |
| 저장·복원에 쓰는 에셋 목록 | [UpgradeProgressCatalog.asset](../Assets/Resources/UpgradeProgressCatalog.asset) |

디버그 JSON의 `defaultItemAmount`와 `defaultCurrencyAmount`는 전체 기본 수량이다. `items`에 `{"itemId":"WeaponMaterial1","amount":5}`를 넣으면 해당 아이템만 덮어쓴다. `currencies`에는 `{"currencyType":0,"amount":500}`처럼 넣는다. `0`은 메인 재화다. 새 아이템 에셋이 저장 카탈로그에 누락됐으면 **Tools > Save Data > Rebuild Upgrade Asset Catalog**를 실행한다.

일반 모드의 실제 보유량과 업그레이드 진행은 `Application.persistentDataPath/upgrade-progress.json`, 장착은 `equipment.json`에 저장된다. 아이템 씬의 투입은 라운드 복사본에서만 차감한다. 디버그 진행과 장착 변경은 실행 중 메모리에 유지된다.

백 패널의 종류 드롭다운은 `전체 / 재료 / 핀`을 선택한다. 이름·시리즈·등급 조건과 목록 컴포넌트의 `Search Mask`를 함께 사용한다. 마스크 배열에는 목록에서 제외할 값을 넣는다. 핀 전용 필터를 고르면 재료 시리즈 조건이 해제된다. 보드에 장착한 재료와 핀은 다음 라운드에 전달된다.

## 4. 스킬트리와 기믹 해금

- 스킬 노드는 **Tools > Skill > Tree Graph**에서 연결과 배치를 수정한다. 현재 업그레이드 씬의 스킬 패널은 하나의 스킬맵을 사용한다.
- 1번 상자는 기본 해금이다. 2~8번 상자를 여는 노드의 `Rewards`에 `UnlockSkillReward`를 붙이고 `Unlock Type = MergeBox`, `Unlock Id = 2`처럼 상자 번호를 적는다.
- 상자를 열면 첫 번째 스킬도 열린다. 이후 스킬은 `Unlock Type = BoardSkill`, `Unlock Id = 2:3`처럼 `상자번호:스킬번호`로 연다. 투입 보드에 상자 기믹을 0~3개 활성화할 수 있다.
- 장착 화면의 스킬 아이콘과 설명은 각 [MergeBoxEquipmentData](../Assets/Data/Scriptable/MergeBoxEquipment)의 `Skills` 배열에서 설정한다. 배열 순서를 아이템 씬 기믹의 `Skills` 순서와 맞춘다. 실제 스킬의 발동·쿨타임·지속시간은 아이템 씬의 해당 기믹 컴포넌트에서 설정한다. 표시 데이터의 쿨타임·지속시간도 함께 맞춘다.

## 5. 상점, 포션, 툴팁

- **상자 데이터** — `ItemBoxData`의 `Background`은 상점 슬롯 배경, 기본 아이콘은 닫힌 상자, `Open Clip`은 열 때 재생할 클립이다. `Gacha Items`의 아이템별 `Probability`는 0~1의 독립 확률이다. 당첨된 항목의 개수는 해당 항목 안의 개수별 `Weight`로 결정한다. 재료상자 전용 등급 그림은 [MaterialChestGrades.asset](<../Assets/Data/Scriptable/General/Utility/DisplayData/MaterialChestGrades.asset>)에서 관리한다.
- **포션** — 제조·장착·레시피 탭은 하나의 목록을 공유한다. 장착 탭에서 슬롯을 선택한 후 보유 포션을 선택하면 해당 슬롯을 채우거나 교체한다. `Slot Clear`는 포션이 든 선택 슬롯을, 전체 비우기는 채워진 장착 슬롯을 비운다.
- **툴팁** — 트리거의 `Is Pointer`는 마우스를 올렸을 때 표시하는 옵션이다. `Show On Selection`은 선택 고정 옵션이며, 토글 슬롯은 토글을 끌 때 고정이 풀린다. 포션 목록은 호버 방식으로 표시한다. 슬롯별 위치는 `Tooltip Provider > Anchor Rect`와 `Tooltip Offset`에서 조정한다. `Tooltip UI > Fixed Position`을 켜면 프로바이더 오프셋만, 끄면 툴팁의 `Offset`도 합산한다. 씬에 고정 배치한 정보 패널은 `Position At Anchor`를 끈다.

## 6. 엑셀, 레시피, 로컬라이제이션

| 작업 | 설정 위치 |
| --- | --- |
| ScriptableObject 생성·갱신 | **Tools > Data > Excel Manager** |
| 스킬트리 그래프 | **Tools > Skill > Tree Graph** |
| 머지 레시피 그래프 | **Tools > Item > Merge > Recipe Book Graph** |
| 레시피 배치 툴 | **Tools > Item > Recipe Tree Builder** |
| 스프라이트 시트 → 애니메이션 클립 | **Tools > Utility > Animation > Sprite Sheet Clips** |
| 나무 단계별 클립 연결 | **Tools > Utility > Animation > Inject Tree Growth Clips** |

엑셀에서는 [Code.xlsx](../Assets/Data/Excel/Code.xlsx)의 시트·루트 경로·클래스 매핑이 기준이다. 적용할 시트만 체크한 뒤 **검증 / 미리보기**를 실행한다. `BasicDataIdentify.xlsx`는 ID로 루트 아래를 검색하고, 없는 항목은 `Generated`에 생성한다. 언어별 `BasicDataDisplayer*.xlsx`와 `ExtraData.xlsx`는 일치하는 기존 ID를 갱신한다. **빈칸과 `-`는 유지**, **`Null`은 비우기**다. 스토리 탭과 [VisualNovelExample.xlsx](../Assets/Data/Excel/VisualNovelExample.xlsx)에는 대화 행별 배경·캐릭터·대사·화자와 연출을 설정한다.

UI 문구는 씬의 `Scene Language Mediator > Translations`에서 연결한다. 여러 씬이 쓰는 문자열 묶음은 공통 테이블로 연결한다. 플레이 중 언어는 설정창에서 바꾼다. 씬을 단독으로 확인할 때는 언어 매니저가 있으면 그 언어를, 매니저가 없으면 메디에이터의 `Standalone Language`를 사용한다.

## 7. 변경 후 확인 순서

- **화면 배치 변경** — 제너럴 씬에서 부팅한 뒤, 배틀과 아이템 씬의 단독 미리보기도 확인한다.
- **핀·장착 변경** — 백 패널에서 재료 슬롯과 핀 슬롯을 장착하고, 다음 라운드에서 배치와 재고가 전달되는지 확인한다.
- **스킬·데이터 변경** — 일반 모드에서는 실제 해금 조건과 보상을, 디버그 모드에서는 표시·연결을 확인한다.
- **UI 클릭이 안 될 때** — 버튼 위의 장식 `Image`가 포인터를 받고 있는지 본다. 포션과 상점에서는 장식 프레임이 클릭을 가로챈 사례가 있었다.

더 자세한 설계는 [씬 흐름](SceneFlow.md), [머지·아이템 게임](MergeGame.md), [플랜트와 전투 비주얼](PlantBattleDataAndGauges.md), [프리팹 비주얼](PrefabVisuals.md), [전투 효과](CombatEffects.md), [추가 효과 사용법](AdditionalEffectsUsage.md)을 참고한다.

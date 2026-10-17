# 게임아이템씬 머지 연결

`9c7c20ee`에 만든 GameItemScene, MergegameManager, MergeItem, MergeItemObject, MergeTree와 기존 프리팹/재료/조합법을 기준으로 연결했다.

## 실행

1. `Assets/Scenes/GameItemScene.unity`를 연다. 단독 테스트는 Catnip → Scenes → Select Play Settings → Current Scene Only에서 Play한다.
2. Game 뷰에 포커스를 두고 **Space**를 한 번 누르면 `ItemspawnPoint`에 선택 재료 1개를 생성해 낙하시킨다. 기본 대기 시간은 0.5초다. 누르고 있는 동안 자동 투입하는 방식은 아니다.
3. **Q**를 누르면 `mergeMaterials` 순서대로 다음 사용 가능한 재료를 선택한다. 잔량 0, 비어 있는 설정은 건너뛰며 끝에서는 처음으로 순환한다. 마지막 재료를 사용하면 다음 남은 재료를 자동 선택한다.
4. 닿은 두 아이템과 **정확히 일치하는 등록 조합법**이 있으면 두 개를 풀에 반환하고 결과 1개를 생성한다. A+B와 B+A는 동일하다. A+A는 A 2개의 조합법을 등록했을 때만 합성한다.

`MergeManager (1)`이 입력을 받는다. `MergeManager (2)`는 현재 재료/조합법이 비어 있으므로 새 필드 **Accept Keyboard Input**을 꺼 두었다. 다른 매니저도 함께 키를 받게 하려면 켜고 Drop Binding / Next Material Binding을 지정한다. 입력은 최신 Input System의 InputAction 콜백이며 프레임 폴링이나 구형 Input API를 쓰지 않는다.

## 유지한 사용자 설정과 추가 연결

벽/바닥의 위치·크기·Rigidbody2D·Collider2D, 카메라, 지급량·레시피·반지름은 사용자가 만든 씬/에셋 설정을 사용한다. UI 오브젝트는 생성하지 않는다.

추가 연결/구현은 아래와 같다.

- **Object Pool**: 현재 씬의 `Pool` 오브젝트를 사용한다. 실제 사용하는 `MergeManager (1)`이 연결되어 있고, 빈 `MergeManager (2)`는 입력을 끈 채 사용자가 연결할 수 있도록 남겨두었다. `PooledObject`의 기존 MergeItem 그룹으로 `Active` / `Inactive` 부모에 분류하고 같은 공용 `MergeItemObject.prefab` 하나를 키로 재사용한다.
- **Register As Default Pool = false**: 아이템씬 전용 풀은 `ObjectPoolManager.instance`를 변경하지 않는다. 제너럴에서 전투씬의 적/이펙트 풀이 그대로 동작하며 아이템씬도 단독 실행할 수 있다.
- **Spawn Point**: 기존 `ItemspawnPoints / ItemspawnPoint`를 명시적으로 연결했다. 카메라 위치/줌/뷰포트가 낙하 위치·반지름·충돌 판정을 바꾸지 않는다. 생성 위치는 이 Transform으로 조절한다.
- **Merge Items**: 기존 정의 5개를 연결했다. 재료에 없는 합성 결과도 어떤 반지름/ItemData를 쓰는지 찾기 위한 목록이다. 새로운 레시피 결과를 만들 때 결과의 MergeItem도 이 목록에 넣는다.
- **Drop Interval / Maximum Active Items / Prewarm Count / Result Merge Delay**: 기본값은 0.5초 / 동시 활성 50개 / 사전 준비 4개 / 합성 후 재합성 대기 0.1초다. 최대 개수는 누적 사용 횟수가 아니라 통 안의 활성 개수다. 합성은 2개가 1개로 줄기 때문에 상한에 도달해도 가능하다.
- **Visual 자식**: 아이콘은 ItemData.icon, 콜라이더는 MergeItem.radius를 사용한다. 사용자가 구성한 `CircleVisual / Visual` 구조와 `visualSize` 참조를 사용하며 물리 루트의 스케일·프리팹 중력·질량은 유지한다.
- **풀 초기화**: 반환/재대여마다 이전 ItemData, 매니저 참조, 속도·각속도, 합성 예약, 충돌 수명 번호를 초기화한다. 같은 충돌이 양쪽에서 보고되어도 한 쌍만 소비하고 이전 대여분의 충돌은 무시한다.
- **제너럴 연결**: 생성한 루트/Visual은 해당 매니저의 레이어를 따른다. 아이템씬 안의 풀/부모와 `gameObject.scene.GetPhysicsScene2D()`를 사용하여 제너럴의 로컬 물리 공간에서도 동작한다.
- **ItemRecipeData 안전 처리**: 빈 레시피를 만드는 순간 OnValidate에서 resultItem이 null이면 건너뛴다. 사용하지 않던 NUnit 참조도 제거했다.
- **단독 실행 이름 표시**: 코어의 LanguageManager나 번역 데이터가 없어도 재료 선택/낙하 로그가 중단되지 않도록 ItemData 에셋 이름을 대신 사용한다.

생성 위치가 막힘, 쿨타임, 재고 부족, 최대 개수, 연결 누락으로 생성하지 못하면 재료는 줄지 않는다. 풀 생성 중 취소되어도 예약한 재료를 돌려준다. 합성 결과 정의가 없으면 원래 두 아이템을 유지한다.

## 현재 선택과 잔량 확인 / 직접 만든 UI 연결

Play 중 `MergeManager (1)`의 **Current Material**은 실제 Merge Materials 재고 항목을 가리킨다. **Last Message** 및 Console의 `[MergeGame]` 로그에서 재료 변경·낙하·합성·생성 거부 이유를 확인한다. Log Actions로 Console 출력을 조절한다. Canvas의 사용자 배치는 그대로 두었으며 새 표시 오브젝트를 만들지 않았다.

직접 만든 UI는 다음 메서드/정보에 연결한다.

| 용도 | 연결 |
| --- | --- |
| 사용/낙하 버튼 | `DropCurrent()` |
| 다음 재료 버튼 | `NextMaterial()` |
| 특정 재료 선택 | `SetMaterial(int)` — Merge Materials의 0부터 시작하는 인덱스 |
| 키보드 입력 허용 | `SetKeyboardInputEnabled(bool)` |
| 현재 재료 표시 | `CurrentMaterialIcon`, `CurrentMaterialName`, `CurrentMaterialCount`를 UI 스크립트에서 읽기 |
| 표시 갱신 알림 | On Current Material Changed / On Materials Changed |
| 통 내부 변화 / 합성 알림 | On Board Changed / On Item Merged |
| 통 비우기 | `ClearItems()` — 활성 피스를 반환하며 이미 쓴 재료를 지급하지 않는다. |

반환값이 필요한 코드에서는 `TryDropCurrent()`, `SelectNextMaterial()`, `SelectMaterial(index)`로 성공 여부를 받는다. 생성/합성은 ItemData의 공격·소환 이펙트나 실제 아이템 사용을 발동시키지 않는다.

## 마우스 드래그 / 클릭 선택·제거

`GameItemScene`의 **MergeManager (1) → Merge Item Selection**에서 설정한다. Board, 해당 씬의 Camera, 사용자가 만든 `CragtingBox / Drag`의 SpriteRenderer를 연결했다. 새 UI나 표시 오브젝트를 생성하지 않는다. `Drag`는 실행 시 드래그할 때만 보인다. 색·투명도·스프라이트·Sorting Order는 기존 SpriteRenderer에서 직접 바꿀 수 있다.

Drag의 Draw Mode가 **Tiled / Sliced**이면 Transform 스케일을 유지하고 **SpriteRenderer.size**를 선택 영역에 맞춘다. 부모와 Drag 자체의 스케일도 크기 계산에 반영한다. 타일 크기는 스프라이트의 Pixels Per Unit과 Transform 스케일로 정하고, Tile Mode / Adaptive Threshold / 테두리 설정은 직접 설정한 값을 사용한다. Simple은 기존처럼 스케일로 맞춘다. 중앙이 아닌 스프라이트 피벗과 Flip X/Y도 반영하며, 같은 크기로 드래그하는 도중 Draw Mode나 스프라이트를 바꿔도 갱신한다.

- **좌클릭**: 누르고 놓은 지점의 아이템 한 개를 선택한다. 겹쳐 있으면 중심이 가장 가까운 한 개를 고른다.
- **좌클릭 드래그 → 놓기**: 시작점과 끝점의 사각형에 콜라이더가 조금이라도 걸친 아이템들을 선택한다. 드래그 도중에는 아이템을 제거하지 않는다.
- **X**: 선택을 취소하고 영역 표시를 숨긴다. 이후 마우스 버튼을 놓아도 클릭/드래그 삭제가 실행되지 않는다. 새로 클릭해야 다시 선택할 수 있다.
- 선택이 확정되면 아이템별 이름·월드 위치를 Console의 **`[MergeSelection]`** 로그에 남기고 기존 풀에 반환한다. 재료 재고를 돌려주거나 전투 아이템 사용 이펙트를 실행하지 않는다.
- 클릭과 드래그는 한 번에 하나만 실행된다. 한 번 드래그 기준 거리를 넘으면 시작점으로 돌아와도 클릭으로 바뀌지 않는다.
- 다른 카메라 영역이나 UI 위에서 선택을 시작하지 않는다. 진행 도중 허용 영역 또는 아이템 카메라 영역을 벗어나거나 일시정지/입력 차단/포커스 상실/컴포넌트 비활성화가 발생하면 취소한다. UI 위에서 놓아도 취소한다.

| Inspector 옵션 | 의미 / 기본값 |
| --- | --- |
| Drag Allowed Area | 클릭·드래그를 허용하는 RectTransform. 현재 `UICanvas / BoxArea / Frame / DragArea`에 연결했다. |
| Maximum Drag Width | 선택 사각형의 최대 가로 길이 **3**. 월드 단위. |
| Maximum Drag Height | 선택 사각형의 최대 세로 길이 **3**. 가로와 독립적으로 제한. |
| Drag Threshold | 클릭 대신 드래그로 판정하는 월드 이동 거리 **0.1** |
| Drag Cooldown | 드래그로 아이템을 지운 뒤 다음 선택까지 **0.5초** |
| Click Cooldown | 클릭으로 아이템을 지운 뒤 다음 선택까지 **0.2초** |
| Log Selections | 아이템별 선택 로그와 취소 로그 출력 |

클릭과 드래그는 다음 선택 가능 시각을 공유하므로 번갈아 써서 쿨타임을 우회할 수 없다. 빈 영역 선택이나 X 취소에는 쿨타임을 쓰지 않는다. 선택 사각형의 크기와 아이템 판정은 월드 좌표다. `Drag Allowed Area`는 Canvas의 화면 위치로 검사하여 UI 크기·앵커·피벗·회전과 카메라 viewport를 따른다. `IsSelecting`, `IsDragging`, `SelectionBounds`, `CooldownRemaining`, `LastRemovedCount`, `LastMessage`를 직접 만든 UI 표시에서 읽을 수 있다. 다른 통에도 같은 컴포넌트를 붙이고 해당 Board/Camera/Drag Visual을 연결하면 된다.

### 드래그 허용 영역 변경

`MergeManager (1) → Merge Item Selection → Drag Allowed Area`에 직접 만든 UI RectTransform을 연결한다. 현재 연결한 `UICanvas / BoxArea / Frame / DragArea`의 RectTransform 크기와 앵커를 조절하면 허용 범위도 따라 바뀐다. 더 작은 영역을 쓰려면 그 안에 빈 UI RectTransform을 직접 만들고 대신 연결하면 된다. Image나 Collider를 붙일 필요는 없으며 영역 표시가 필요하면 직접 만든 Image의 Raycast Target을 끈다. 비워 두면 기존 카메라 viewport 제한만 적용한다.

영역 안에서만 선택을 시작할 수 있다. 드래그 중 **실제 마우스 위치**가 밖으로 나가면 선택을 취소하고 표시를 숨긴다. 최대 드래그 가로·세로 제한으로 표시 사각형이 작게 고정되어 있어도 실제 마우스가 밖이면 취소한다. 정확한 경계선 위까지는 허용한다. 취소 후 안으로 돌아와 놓아도 아이템이 삭제되지 않으며, 새로 클릭해야 다시 선택할 수 있다. 영역을 벗어나면서 버튼을 바로 놓는 경우와 한 프레임 안에 밖으로 나갔다 돌아오는 입력도 검사한다. 영역 이탈 취소에는 쿨타임을 쓰지 않는다. 연결한 영역/Canvas가 비활성화되거나 크기가 0이면 진행 중 선택도 취소된다.

Canvas 카메라가 필요한 Screen Space Camera / World Space 모드에서는 해당 Canvas의 Render Camera를 연결한다. Overlay Canvas는 카메라 없이 검사한다. 허용 영역은 입력 범위만 정하며 아이템 물리 이동·통 모양·선택 사각형의 최대 크기는 바꾸지 않는다.

## 검사

`python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation`에서 머지 코드·풀 코드·Unity 테스트의 보조 API 컴파일, 실제 등록 조합 판정/재료 순환 관리 코드, 씬/프리팹 참조와 결과 정의 연결을 검사한다. 코드 검사와 Unity 실행은 다르다.

Unity Test Runner의 **MergeGameTests**에는 Sprite/반지름·재고 소비·막힌 입구·최대 개수·쿨타임·중복 합성·결과 정의 누락·풀 재사용/이전 충돌 무시·재진입/취소 환불·재료 순환에 더해 드래그/클릭 제거·취소·범위 제한·재대여·다른 통 격리 검사가 있다. 관리 코드 검사에서는 실제 선택 상태 코드의 클릭/드래그 중복 방지, X 취소 후 해제, 공유 쿨타임, 범위·원형 콜라이더 경계 판정을 실행한다. 접촉/선택 메서드를 직접 호출하므로 실제 마우스·키보드 이벤트와 낙하는 GameItemScene의 Play에서 확인한다. 이 클라우드에는 Unity Editor가 없어 해당 엔진 테스트와 Play 실행은 수행하지 않는다.

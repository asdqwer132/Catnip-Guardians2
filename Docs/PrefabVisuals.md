# 프리팹 자체 연출과 실제 범위 보기

클립 없이 크기·회전·흔들림·투명도를 조합하려면 **Prefab Visual Animator**를 프리팹에 붙인다. 데미지 영역, 일반/소비 영역, 소환물 모두 같은 컴포넌트를 사용한다. 스프라이트 그림 자체가 바뀌는 프레임 애니메이션은 기존 Animator를 사용하면 된다. 두 컴포넌트가 같은 Transform이나 SpriteRenderer.color를 동시에 수정하지 않도록 대상을 나눈다.

## 1. 프리팹 구성

```text
GameplayRoot              ← 판정·이동·수명 스크립트와 Collider2D
└─ RangeVisual            ← 반경에 따른 크기 조절용 Transform
   └─ Visual              ← SpriteRenderer, 순수 시각 연출
```

1. 루트 또는 비주얼 프리팹 루트에 **Add Component → Catnip → Visuals → Prefab Visual Animator**를 추가한다.
2. **Visual Target**에 순수 비주얼 자식을 연결한다. 비어 있으면 바로 아래의 `Visual` 자식을 찾는다.
3. **Entrance**는 등장 시간·시작 크기·투명도, **Loop**는 회전·맥동·흔들림, **Exit**는 종료 시간·최종 크기·투명도를 설정한다. 각 항목은 별도로 켜고 끌 수 있다.
4. **Lifetime Source**는 비워 두면 부모의 DamageArea / ReactiveGroundArea / SummonActor를 찾는다. 일반 프리팹은 **Manual Lifetime**으로 시각 연출 시간을 정한다. 0이면 유지 연출을 계속 재생한다.

`Visual Target`으로 루트, 콜라이더를 포함한 자식, 범위 크기를 제어하는 Transform을 지정하면 Inspector에 이유가 표시되고 재생하지 않는다. 애니메이션이 판정 반경을 바꾸지 않도록 `RangeVisual`의 **자식**을 움직인다. 크기·위치·회전·알파는 항상 재생 시작 상태를 기준으로 계산한다.

이미 연결해 둔 예제:

- `Assets/Prefab/Item/Attack/DamageArea.prefab`: RangeVisual 아래의 스프라이트를 등장/종료 시 작게 확대·축소하고 페이드한다.
- `Assets/AreaPrefab.prefab`: 같은 연출을 쓰는 영역 비주얼이다. AreaDefinition의 Visual Prefab에 연결한다.

소환물은 기존 스프라이트를 자식 `Visual`로 옮겨 그 Transform을 연결하면 된다. 스프라이트가 없는 소환물은 원하는 비주얼 자식을 추가한다. 여러 스프라이트를 가진 Visual도 하위 SpriteRenderer의 원래 알파를 유지하면서 함께 페이드한다.

## 2. 영역 데이터 설정은 한 곳에서

**AreaDefinition**에서 `Stat`, `Visual Prefab`, `Scale Visual By Radius`, 영역 생명주기 이벤트를 설정한다. **ReactiveGroundEffect / AreaEffectData**에는 필수 `Area Definition`을 연결한다. 생성 효과에 별도의 Ground Stat / Override Definition Stat / Area Visual Prefab / Scale Visual By Radius는 없다.

`Scale Visual By Radius`를 켜면 비주얼 프리팹 루트의 XY 크기에 현재 반경의 두 배를 곱한다. 따라서 지름 1의 그림으로 제작한다. 일반 크기의 소환물처럼 반경과 무관한 비주얼이면 끈다. 기존 광물 영역은 기존에 사용하던 정의의 스탯과 비주얼을 유지하도록 이전했다.

생성 효과의 **Visual Data**는 실행 시 한 번 재생할 연출, **End Visual Data**는 효과와 하위 효과가 완료된 뒤 한 번 재생할 연출이다. **AreaDefinition.Visual Prefab**은 영역 수명 동안 붙어 있는 비주얼이므로 역할이 다르다. 착지 반응의 Default/Special Effects와 필터는 기존 생성 효과에서 계속 설정한다.

## 3. 실제 범위 기즈모

Scene 뷰의 **Gizmos**를 켜고 영역이나 그 비주얼을 선택한다. 기본 범위는 컴포넌트를 추가하지 않아도 표시된다.

| 대상 | 표시하는 실제 값 |
| --- | --- |
| DamageArea | 현재 CircleCollider2D의 반경·오프셋·월드 크기 |
| SectorDamageArea | 현재 반경·방향·각도. 기본 중심 오프셋 0인 부채꼴 |
| DirectionalRectangleDamageArea | BoxCollider2D의 크기·오프셋·회전을 반영한 사각형 |
| 일반/소비 영역 | ReactiveGroundArea의 현재 런타임 월드 반경 |
| 소환물 | 접촉 피해/접촉 효과, 포탑/아이템 투척/끌어당김, 오라 모듈의 현재 런타임 반경 |

소환물 프리팹을 편집할 때는 모듈의 기본값을 표시하고, 실행 중에는 버프 적용값을 표시한다. HP·궤도·주기 소환처럼 공격/감지 반경이 없는 모듈에 범위를 임의로 만들지 않는다. 일반/소비 영역과 소환물의 조회는 월드 반경을 사용하므로 Visual의 크기를 다시 곱하지 않는다.

색·라벨·선택 여부를 바꾸려면 **Catnip → Debug → Prefab Range Gizmo**를 붙인다. 별도 프리팹에서도 부모 범위 제공자를 자동으로 찾는다. 제공자가 없는 일반 프리팹은 Circle/Box Collider2D를 표시한다. 콜라이더도 없으면 **Show Manual Preview**를 켜고 월드 단위 Preview Radius를 입력한다. 이 원은 `Preview (수동)`으로 표시되며 게임 판정은 만들지 않는다.

## 4. 수명과 풀 재사용

첫 LateUpdate에서 재생을 시작하므로 생성 코드가 설정한 반경과 색을 먼저 반영한다. 실제 남은 수명을 매 프레임 읽어 자연 종료 직전에 Exit를 재생한다. 실행 중 수명이 늘어나면 새 남은 시간에 맞춰 종료 페이드를 되돌린다.

기존 ObjectPoolManager의 IPoolable 콜백과 OnDisable에서 원래 위치·회전·크기·색을 복구한다. 반환된 오브젝트를 다시 소환하면 등장부터 시작하며 별도의 애니메이션 오브젝트 풀을 만들지 않는다. 이 컴포넌트는 연출만 관리한다. 게임 루트의 삭제·반환은 기존 런타임이 담당한다.

소비·취소로 즉시 삭제된 오브젝트는 그 뒤에 종료 연출을 재생할 수 없다. 삭제 전에 연출을 기다리는 별도 흐름이 필요하면 `PlayExit()`을 호출하고 **On Exit Completed**에 후속 처리를 연결한다. 이 이벤트는 명시적 PlayExit의 완료에만 한 번 실행된다. 자연 수명 종료의 완료 처리는 기존 런타임을 그대로 사용한다. 코드로 `Play()` / `Stop()`을 호출할 수도 있다.

추가 프리팹 런타임은 `IPrefabVisualLifetime`으로 수명, `IPrefabRangeProvider`로 범위를 제공하면 같은 연출·기즈모에 연결된다. 새 유지 연출은 `PrefabVisualLoop`와 `PrefabVisualAnimationMath`에 설정과 프레임 계산을 추가하면 된다.

## 5. 검증

Unity Test Runner → EditMode의 **PrefabVisualTests**는 판정/연출 분리, 색 복구, 실제 ObjectPoolManager 재사용, 사각형·부채꼴·소비 영역 범위, 정의의 단일 설정을 검사한다. **SummonModuleTests.SummonRangesFollowRuntimeBuffsWithoutScalingTheVisualOrChangingModuleDefaults**는 반경 버프 추가·제거 시 기즈모 값을 검사한다.

클라우드의 `python3 Tools/ValidateEffects/check.py`는 코드와 테스트를 컴파일하고 순수 연출 계산 테스트를 실행한다. Unity 렌더링·Scene 뷰·물리·풀 콜백 테스트는 실제 Unity Editor의 Test Runner에서 실행해야 한다.

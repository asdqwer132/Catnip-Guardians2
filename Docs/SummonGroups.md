# 소환 무리와 조건 달성 아이템

`SummonDefinition`은 붉은 슬라임·푸른 슬라임 같은 **종류의 식별 키**이고, `SummonGroupDefinition`은 이 종류들이 함께 속하는 **무리**다. 각 종류 정의의 **Summon Group**에 같은 무리 에셋을 연결한다. 이름이 같아도 다른 종류 정의 에셋이면 별개의 종류다.

## 1. 무리 만들기

1. **Create → 04.Items → Effects → Direct Effects → Summons → Group**으로 `SlimeGroup`을 만든다.
2. 붉은·푸른·녹색 슬라임의 `SummonDefinition`을 각각 만들고 세 정의의 **Summon Group**에 `SlimeGroup`을 연결한다.
3. 각 소환 프리팹의 `SummonActor → Definition`에 해당 종류 정의를 연결한다.
4. 각 프리팹을 소환하는 기존 `SummonSpawnEffect`를 아이템의 Effect Datas에 연결한다.

종류와 무리는 프리팹의 정의로 식별한다. 소환 효과는 기존대로 생성 수·지속시간·배치만 담당하고, 공격/체력 스탯과 기능은 소환물의 모듈에서 설정한다. 별도 매니저 컴포넌트를 씬에 추가할 필요는 없다.

## 2. 종류 수·개체 수 조건

무리 에셋의 **Item Rules**에 규칙을 추가한다.

| 설정 | 의미 |
| --- | --- |
| Distinct Kinds | 필드에 있는 서로 다른 종류 정의가 Required Kinds 이상 |
| Total Instances | 필드에 있는 무리 소환물의 전체 개체 수가 Required Instances 이상 |
| Both | 종류 수와 개체 수 조건을 모두 만족 |
| Comparison = At Least | 설정한 수 이상. 기본 설정 |
| Comparison = Exactly | 설정한 수와 정확히 같을 때 |
| Owner Scope = Same Owner | 같은 소유자의 소환물만 집계. 기본 설정 |
| Owner Scope = All Owners | 필드 전체에서 이 무리를 집계 |

예: **Both / Required Kinds = 3 / Required Instances = 5**이면 붉은 3마리 + 푸른 1마리 + 녹색 1마리는 충족한다. 붉은 5마리는 개체 조건만 충족하므로 발동하지 않는다. 두 조건 중 하나로 판정하려면 규칙의 Requirement Mode를 해당 조건으로 선택한다. 종류·개체 조건에 서로 다른 아이템을 발동하려면 규칙을 두 개 만든다.

활성화되고 초기화를 완료한 소환물만 센다. 비활성화·만료·사망·취소된 소환물은 제외한다. 정의의 무리가 비어 있으면 집계에 참여하지 않는다. 런타임에 종류를 바꿀 때는 `ConfigureDefinition()`을 사용한다.

## 3. 조건이 맞으면 아이템 사용

규칙의 **Item**에 사용할 ItemData를 연결한다. 추가 몬스터를 원하면 그 아이템의 Effect Datas에 `SummonSpawnEffect`를 넣고 추가 소환 프리팹을 지정한다. 같은 기능으로 공격·버프·회복 아이템도 사용할 수 있다.

**Item Use → Mode**에서 전달 방식을 선택한다.

- **Immediate**: 조건을 완성한 소환물 위치에서 아이템을 바로 사용한다.
- **Throw**: 아이템을 던지고 도착하면 사용한다. 원래 사용 방향을 유지하며 시작점·목표점·오프셋·배치·프리팹·비행 시간을 설정할 수 있다.

위치 기준의 **Target Position / Use Position**은 이 반응에서는 조건을 완성한 소환물의 현재 위치다. **Owner Position**은 그 소환물 소유자의 현재 위치다. 시작과 목표의 기준·오프셋은 각각 설정한다. **World / Forward And Side** 오프셋, 원형·산탄 배치, **Throw Options → Projectile Prefab / Override Motion → Motion**은 [상태 반응 투척](StatusItemUseReactions.md)과 같은 공용 전달 코드를 사용한다.

무리마다 다른 모션을 원하면 각 무리의 규칙에서 별도 투척 프리팹 또는 Motion 값을 설정한다. 소환물의 투척 실행기를 사용하며, 컴포넌트가 없으면 해당 소환물에 생성한다. 재고·가방 슬롯·쿨다운은 소비하지 않는다. **Consume Use Buffs**는 추가 아이템 사용이 횟수제 버프를 소비할지 정한다.

보상은 조건을 완성한 소환물의 소유자·가방·버프 매니저·실행 배율을 전달받는다. All Owners로 수를 합산해도 보상 소유자는 마지막 소환을 만든 소유자다. 아이템과 하위 소환·투척은 원본 효과 수명에 연결되므로 종료 후 아이템과 종료 연출도 함께 기다린다.

## 4. 반복 발동 규칙

기본 **On Condition Reached**는 조건을 충족한 소환에서 한 번 발동한다. 조건을 계속 만족하는 동안 다른 소환물이 추가되어도 다시 발동하지 않는다. 필요한 종류/개체가 줄어 조건이 깨진 뒤 다시 소환해서 채우면 재발동한다.

**Every Matching Summon**은 조건이 맞는 매 새 소환마다 발동한다. 소환물이 이탈할 때는 아이템을 발동하지 않고 조건의 재준비만 갱신한다. Comparison = Exactly도 새 소환 직후의 필드 수를 기준으로 평가한다. 여러 개를 한 번에 만드는 소환 효과는 개체가 초기화될 때마다 평가한다.

보상 아이템으로 나온 소환물도 무리에 소속되어 있으면 수에는 포함된다. 보상과 하위 실행에 `allowSummonGroupReactions = false`를 전달하여 보상이 다시 보상을 만드는 반복은 차단한다. 원본 상태 반응에서 나온 소환물은 무리 조건에 참여할 수 있다. 보상 아이템의 하위 상태 반응은 실행하지 않는다.

같은 종류의 여러 개체, 소유자마다의 발동 여부, 초기화/재활성화, 전투 실행 세대는 런타임에서 관리한다. 공유 정의 에셋에 실행 중 카운터를 저장하지 않는다. 보상 아이템 설정이 없어 실패했다면 이후의 새 소환에서 다시 시도할 수 있다.

## 5. 준비된 슬라임 예제

`Assets/Data/AdditionalEffectsExamples/SummonGroups`에 다음을 연결해 두었다. 이미지는 기존 소환 프리팹을 색으로 구분한 예제다.

- `ExampleSlimeGroup`: 같은 소유자의 종류 3개·개체 3개가 모이면 `SlimeKing_Item` 사용.
- `RedSlime_Item / BlueSlime_Item / GreenSlime_Item`: 해당 종류를 한 마리씩 30초 동안 소환. Definition의 무리는 모두 ExampleSlimeGroup.
- `SlimeKing_Item`: 왕 프리팹 한 개를 30초 동안 소환. 왕은 자체 접촉 피해 15·반경 0.8·주기 1초, 체력 100 모듈을 가진다.

세 기본 아이템을 같은 소유자로 사용하면 세 번째 종류가 소환되는 즉시 왕이 생긴다. 붉은 아이템만 세 번 쓰면 왕은 생기지 않는다. 예제 규칙의 **Item Use → Mode = Throw**로 바꾸면 같은 왕 소환 아이템이 투척 후 도착할 때 왕을 만든다. 기존 프리팹과 기존 아이템 설정은 예제 생성 과정에서 수정하지 않았다.

런타임 수치를 코드에서 확인하려면 다음을 사용한다.

```csharp
SummonGroupPopulation population = SummonGroupRuntime.GetPopulation(slimeGroup, owner);
Debug.Log($"종류 {population.kindCount}, 개체 {population.instanceCount}");
```

## 6. 검증

Unity Test Runner → EditMode의 `SummonGroupTests`는 실제 소환 경로의 종류/개체 수, 소유자 분리, 왕 추가 소환, 투척 후 소환, 재활성화와 수명 완료를 검사한다. 공용 전달 코드는 기존 `StatusReactionThrowTests`와 `StatusItemUseModuleTests`도 함께 검사한다.

클라우드의 `python3 Tools/ValidateEffects/check.py`는 코드·Unity 테스트를 컴파일하고 실제 무리 집계/발동 코드의 로직 검사를 실행한다. 클라우드 로직 검사의 Scene·소환물/투척 참조는 대체 구현이며, 실제 Unity 렌더링·물리·비행 검증은 Editor에서 실행한다.

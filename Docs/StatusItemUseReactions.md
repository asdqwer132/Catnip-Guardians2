# 상태가 있을 때 아이템 사용에 반응하는 모듈

`SpecialItemManager`는 플레이어의 활성 특수 상태를 확인하고 **SpecialStatusDefinition → On Item Used Modules**를 실행한다. 상태마다 여러 모듈을 배열 순서대로 붙일 수 있다. 특정 상태나 아이템의 이름을 매니저 코드에 추가하지 않는다.

## BlacksmithDelicate

`Assets/Data/AdditionalEffects/BlacksmithDelicate.asset`에 `BlacksmithDelicate_MineralRepeat.asset`를 연결했다.

| 설정 | 값 |
| --- | --- |
| Allowed Series | Mineral |
| Probability | 0.1 = 10% |
| Item Source | Used Item |
| Delivery | Execute At Impact |
| Consume Use Buffs | 꺼짐 |

활성 상태가 있을 때 미네랄 아이템을 사용하면 10% 확률로 **같은 아이템의 효과를 같은 도착 위치에서 한 번 더 실행**한다. 원본 사용 후, 횟수제 상태의 사용 횟수를 차감하기 전에 판정한다. 투척 아이템은 실제 착지·효과 실행 시 판정한다. 추가 사용은 재고·가방 슬롯·쿨다운을 소비하지 않는다.

`MineralMaterial1 / 4 / 7`의 비어 있던 Effect Datas에는 각각 `BlacksmithDelicateFor10Seconds / 20Seconds / 30Seconds`를 연결했다. 같은 키 재부여 시 남은 지속시간에 더한다. 상태 중첩 수만큼 확률을 여러 번 판정하지 않는다.

## 같은 아이템 대신 지정 아이템 사용하기

1. **Create → 04.Items → Effects → Direct Effects → Special Status → Item Use Modules → Use Item**으로 `UseItemStatusReactionModule` 에셋을 만든다.
2. **Item Source = Configured Item**을 선택하고 **Configured Item**에 사용할 ItemData를 연결한다. **Used Item**이면 방금 사용한 ItemData를 사용한다.
3. **Delivery**를 선택한다.
   - **Execute At Impact**: 원본 사용의 도착 위치·방향에서 효과를 즉시 실행한다.
   - **Throw At Impact**: 도착 위치에 **Throw Start Offset**을 더한 곳에서 같은 도착 위치로 던진다. 매니저의 **Item Throw Executor** 참조가 필요하다.
4. **Allowed Series / Allowed Items**는 비우면 전체다. 둘을 채우면 둘 다 만족해야 한다. **Excluded Items / Excluded Item Ids**는 제외 목록이다.
5. **Probability**에 확률을 입력한다. 0은 실행하지 않고, 1은 항상 실행한다. 조건을 만족하지 않으면 난수를 뽑지 않는다.
6. 해당 상태 키의 **On Item Used Modules**에 연결한다. 그 키를 PlayerStatus 대상으로 부여하면 동작한다.

**Consume Use Buffs**를 켜면 추가 아이템 실행도 횟수제 버프를 차감한다. 부모 실행이 소비를 끈 경우에는 추가 실행도 소비하지 않는다. 소유자·원본 가방·효과 배율은 추가 사용에 전달하고, 새 아이템은 자신의 ItemData와 스탯 스냅샷을 사용한다.

## BrokeStar 이전

기존 `Star_SpecialStatusKey`의 GUID를 유지하고 `BrokeStar_ExtraItem.asset` 모듈을 연결했다. 설정은 **Configured Item = WeaponExtra1**, **Probability = 1**, **Delivery = Throw At Impact**, **Throw Start Offset = (0, 3, 0)**다. WeaponExtra1 자체는 제외한다. 기존 추가 투척의 횟수제 버프 소비는 켠 상태로 유지했다.

매니저의 이전 **Special Items / Required Special Status Key** 필드는 제거했다. **Item Throw Executor**와 선택적 **Status Manager**만 참조한다. Status Manager가 비어 있으면 소유자의 부모에서 찾고, 이후 전역 PlayerStatus를 사용한다. 매니저를 비활성화하면 상태 반응도 멈춘다.

기존과 같은 위치에서 추가 아이템을 던지되, 이제 실제 원본 소유자·가방·수명을 전달한다. 원본 종료 연출과 종료 후 아이템은 모듈의 추가 투척·하위 효과까지 기다린다.

## 아이템 없이 추가 효과만 실행하기

**Item Use Modules → Execute Effects**로 `ExecuteEffectsStatusReactionModule`을 만든다. **Effects**에 기존 공격·회복·버프·소환 이펙트를 연결하면 사용한 아이템의 출처·도착 위치를 유지해서 배열 순서로 실행한다. 이 모듈에도 시리즈/아이템 필터와 확률을 설정할 수 있다.

예: 한 상태 키에 “미네랄 사용 시 10%로 같은 아이템 재사용” 모듈과 “무기 사용 시 20%로 회복 효과” 모듈을 함께 넣는다. 시리즈 조건이 맞는 모듈만 판정한다.

## 반복·수명 규칙과 확장

- 같은 활성 상태 키는 사용 한 번에 한 번 처리하고, 같은 모듈을 배열에 중복 넣어도 한 번만 실행한다. 서로 다른 모듈·상태는 각각 판정한다.
- 추가 사용/효과에서 발생하는 투척·소환·종료 후 아이템에는 `allowItemUseReactions = false`가 이어진다. 추가 사용이 다시 상태 반응을 불러 무한 반복하지 않는다.
- 모듈 실행 중 다른 상태가 제거되면 그 상태의 남은 모듈을 실행하지 않는다. 같은 상태의 시간을 연장하는 경우에는 나머지 모듈도 실행한다.
- 모듈 에셋은 설정만 보관한다. 재생 시간이나 처리 중 플래그를 공유 에셋에 저장하지 않아 여러 사용이 서로 섞이지 않는다.
- 현재 사건은 **아이템 효과 실행 후**다. 장판 진입/피격/주기 같은 다른 사건은 그 사건용 호출 지점을 별도로 추가한다. 새 아이템 사용 반응은 `SpecialStatusItemUseModule`을 상속하고 `CanPerform`·`Perform`을 구현해 상태 키에 연결하면 된다.

## 검사

Unity Test Runner의 EditMode에서 `StatusItemUseModuleTests`, `AdditionalSpecialStatusTests`를 실행한다. 상태 활성/만료/정화, 확률 경계, 시리즈, 마지막 사용 횟수, 추가 투척과 완료 수명, 하위 아이템의 재반응 방지를 검사한다.

클라우드에서는 `python3 Tools/ValidateEffects/check.py`로 코드/Unity 테스트 컴파일과 실제 모듈·디스패처의 순수 로직 검사를 실행한다. 순수 검사의 Scene/투척 참조는 대체 구현이므로 실제 Unity 물리·비행 검증은 Editor에서 실행해야 한다.

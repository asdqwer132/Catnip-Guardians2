# 아이템 스텝 · 반응형 장판 · 소환수 모듈

## 스텝 하나에서 아이템과 개수 지정

Create → GameData → Items → Effects → Repeat Item으로 생성하고 아이템의 Effect Datas에 연결합니다.

- **Steps → Entries**에서 Item과 Count를 지정합니다. 예: A 아이템 Count 6, B 아이템 Count 2이면 한 스텝에서 8발을 동시에 사용합니다.
- **Item Repeat Count**는 스텝을 실행하는 총 횟수입니다. 목록보다 많으면 InOrder는 처음부터 순환하고 Random은 매번 스텝을 다시 선택합니다.
- **Item Repeat Interval**은 첫 스텝까지의 대기, **Interval After**는 해당 스텝 뒤 다음 스텝까지의 대기입니다. 마지막 스텝 뒤에는 대기하지 않습니다.
- **Item Repeat Projectile Count Multiplier**는 Count에 곱하는 버프용 배율입니다. 반올림하며 0이면 발사하지 않습니다. 한 스텝은 최대 128발입니다.
- 기존 Items 목록은 Entries의 Count 1로 변환됩니다. 중복 아이템과 스텝 간격은 유지됩니다. 변환 전 에셋도 런타임에서 그대로 읽습니다.

**Placement**는 공통 설정입니다. 각 스텝의 Override Placement를 켜면 그 스텝만 다르게 설정합니다. 다중 발사 시 위치는 발사체마다 계산됩니다.

| 방식 | 동작 |
| --- | --- |
| FixedPoint | 모두 원래 효과 위치에서 실행 |
| Forward | 투척 방향으로 Forward Offset/Side Offset만큼 차례로 진행 |
| CircleEven | Radius와 Spread Angle 범위에 균등하게 배치 |
| CircleRandom | 부채꼴 영역 안에서 각도와 거리를 모두 무작위 선택 |
| Shotgun | 각도만 매 발 무작위 선택, 거리는 Radius로 고정 |

산탄 예: Entries에 원하는 아이템 Count 8, Placement = Shotgun, Radius = 3, Spread Angle = 35, Throw Items = 켜기. 방향은 원래 아이템 투척 방향이며 Direction Mode를 FixedWorldDirection으로 바꿀 수도 있습니다. 발사 시작점은 Effect Position 또는 현재 Owner Position을 선택합니다. 목표 위치는 원래 효과 위치를 기준으로 계산됩니다.

## Sequence Attack 통합

SequenceAttack의 별도 이펙트·러너·스탯·폭탄 구현을 제거했습니다. 현재 추적 에셋에 해당 스크립트 GUID 참조는 없습니다. 로컬에서 별도로 만든 Sequence 에셋이 있다면 Unity에서 열기 전에 백업하고 아래 설정으로 Repeat Item 에셋을 새로 만들어 참조를 교체하세요.

- **On Start Effects**: 실행 시작 시 한 번.
- **Steps → On Impact Effects**: 각 발사체의 착지/폭발 시 실행. Entries 없이 Effect Only Count만 지정해서 효과를 반복할 수도 있습니다. Entries가 있으면 각 아이템 사용 뒤 해당 효과도 실행합니다.
- **After Last Impact Effects**: 마지막 스텝의 모든 발사체가 해결된 뒤 한 번. 하위 효과의 실제 수명은 기존 완료 추적 시스템이 계속 기다립니다.
- **Travel Mode**: Inherit는 공통 Throw Items 사용, Instant는 즉시, Throw는 투척, Bounce는 이전 착지점에서 다음 지점으로 이동하고 착지 이후 Interval After만큼 기다립니다. Bounce의 다중 발사는 한 묶음으로 움직이며 다음 스텝은 묶음 전체의 해결을 기다립니다.
- **First Step At Origin**: 바운스 첫 공격을 원래 위치에서 즉시 실행합니다. 바운스 포함 총 공격 횟수는 Item Repeat Count입니다.
- **Override Projectile Motion**: Flight Time/Arc Height 사용. Bounce는 항상 이 설정을 사용합니다. 끄면 일반 투척은 기존 Projectile Prefab의 비행 설정을 유지합니다.
- **Impact Trigger**: OnArrival, AfterDelay, EnemyNearbyOrTimeout. 지연은 도착 이후부터 셉니다. 근접 감지는 Bomb Delay 이후 시작하며 Bomb Lifetime이 되면 반드시 폭발합니다. Enemy Layer Mask와 Trigger Radius를 지정합니다.
- **Projectile Sprite**: 공통 이미지 재정의. 비우면 아이템 아이콘을 사용합니다.

| 이전 Sequence 기능 | Repeat Item 설정 |
| --- | --- |
| ForwardBounce | Placement Forward, Travel Mode Bounce, First Step At Origin 켜기, 첫 대기 0 |
| ScatterBombs | CircleEven/CircleRandom/Shotgun, Travel Mode Throw, 원하는 Count와 Impact Trigger |
| RepeatAtPoint | FixedPoint, Travel Mode Instant |
| attackCount / attackInterval | Item Repeat Count / 스텝 Interval After |
| forwardOffset / sideOffset / scatterRadius / spreadAngle | Repeat Stat의 대응하는 Offset / Radius / Spread Angle |

## 반응형 장판

Create → GameData → Items → Effects → Attack → Reactive Ground로 생성합니다.

- **Default Effects**는 장판 생성 시 한 번, 이후 Ground Tick Interval마다 중심에서 실행합니다. 기존 DamageAreaAttackEffect, BuffEffect, 이동 제어 효과 등을 연결합니다. 연결한 효과의 범위와 수명도 별도로 설정합니다.
- **Special Effects**는 투척 아이템의 효과가 발동할 때 장판 내부이면 한 번 실행합니다. 일반 투척은 착지 시점, 지연 폭탄은 폭발 시점에 반응합니다. 아이템이 공중에서 스쳐 지나가는 것에는 반응하지 않습니다.
- **Accepted Items**를 비우면 모든 아이템, 채우면 해당 ItemData만 받습니다. **Only Same Owner**는 같은 소유자의 아이템만 받습니다.
- **Ground Special Duration = 0**은 반응 순간만 특수 효과를 실행하고 이후 기본 Tick을 계속합니다. 0보다 크면 반응 즉시 특수 효과를 실행한 뒤 해당 시간 동안 Tick도 특수 효과로 대체합니다. 다시 받으면 유지 시간이 갱신됩니다.
- **Ground Reaction Cooldown**은 같은 장판의 연속 반응을 제한합니다. 산탄 각 발마다 반응시키려면 0으로 설정합니다.
- **Replace Incoming Item Effects**를 켜면 반응한 아이템의 기본 Effect Datas를 실행하지 않습니다. 버프 사용 횟수 및 인벤토리 소비 규칙은 유지합니다.
- 피해/버프의 출처는 장판 생성 아이템입니다. 투척 아이템은 반응 필터에 사용됩니다. 겹친 장판들은 각각 한 번 반응하며, 하나라도 Replace가 켜져 있으면 투척 아이템의 기본 효과가 대체됩니다.
- **Area Visual Prefab**은 장판이 살아 있는 동안 표시할 프리팹입니다. 단위 지름으로 제작하고 Scale Visual By Radius를 켜면 실제 반경에 맞춰 확대됩니다. 기존 Visual Data는 생성 시 연출에 사용할 수 있습니다.

예: 기본으로 약한 지속 피해, 지정 아이템 착지 시 강한 폭발. Default Effects에 작은 DamageArea, Special Effects에 큰 DamageArea를 연결하고 Accepted Items에 반응할 아이템을 넣습니다. Ground Special Duration을 2로 주면 2초 동안 강한 효과가 Tick마다 발동합니다.

스텝과 소환수 투척에서는 자체 효과가 없는 재료 아이템도 장판 반응에 사용할 수 있습니다.

이번 착지로 생성된 장판은 같은 착지에 즉시 반응하지 않습니다. 전투 초기화·비활성화 시 반응 등록과 수명 핸들도 정리됩니다.

## 소환수 행동 조합

기존 SummonedObject 프리팹의 SummonItemThrower 컴포넌트를 공통 소환수 호스트로 사용합니다. 클래스/GUID, Summon 버프 타깃 그룹, 공격 범위·수명 스탯은 유지했습니다.

Create → GameData → Items → Summon Modules에서 행동 에셋을 만들고 프리팹의 **Modules**에 원하는 순서로 넣습니다. 이동 모듈을 공격 모듈보다 앞에 두면 이동한 위치에서 같은 프레임에 공격합니다. **SummonAttackEffect → Override Modules**를 켜면 같은 프리팹을 쓰면서 이펙트별 행동 목록을 재정의할 수 있습니다. 이 상태의 빈 목록은 행동 없는 소환수입니다.

| 모듈 | 설정 및 역할 |
| --- | --- |
| Item Throw | Item, Count, 타깃 방식, 분산 반경·각도. 기존처럼 다른 아이템을 던짐 |
| Turret | 아이템 사용 없이 자체 직선/유도 탄환 발사. 탄환 프리팹/스프라이트, 속도·수명·충돌 반경, 명중 효과 지정 |
| Pull | Attack Range 내부의 적을 소환수 중심으로 흡인. 당김 속도·정지 거리·중심 붙잡기·패턴/공격 중단 설정. 기존 이동 면역과 저항 적용 |
| Contact Damage | Contact Radius에 닿은 적마다 Hit Interval 간격으로 피해. 여러 콜라이더가 있는 적도 한 번만 처리. 명중 효과 추가 가능 |
| Orbit | 소환 지점 또는 소유자를 중심으로 공전. 반경·각속도·시작 각도 설정 |

조합 예:

- 고정 포탑: Turret만 연결.
- 블랙홀: Pull + Contact Damage. Contact Radius를 작게 해서 중심에 모인 적에게만 피해.
- 떠다니는 접촉 공격: Orbit + Contact Damage. Owner 중심 공전으로 설정.
- 움직이는 아이템 포탑: Orbit + Item Throw.

Turret와 Contact Damage의 최종 피해는 `Base Damage + Summon Attack Power × Attack Power Multiplier`입니다. Turret/Item Throw의 간격은 `Summon Throw Interval × Interval Multiplier`입니다. Enemy Layer Mask를 실제 적 레이어에 맞추세요. Turret의 탄환은 적을 대상으로만 충돌하며 지형에는 막히지 않습니다. 스프라이트 또는 표시 가능한 Projectile Prefab을 지정해야 탄환이 보입니다.

각 모듈 에셋은 설정만 공유하고 타이머, 재명중 간격, 흡인 대상, 공전 각도는 소환수마다 독립적입니다. 흡인 종료 시 자신이 적용한 이동만 해제하고, 풀에서 재생성된 적의 새 생명에는 이전 제어/명중 상태를 전달하지 않습니다.

기존 프리팹에 모듈이 없으면 Use Legacy Throw When No Modules 기본값으로 기존 투척 동작이 유지됩니다.

## 검증

Assets/Editor/Tests/CombatEffectTests.cs에 EditMode 테스트를 추가했습니다. Unity Test Runner의 EditMode에서 CombatEffectTests를 실행하세요. 분산, 개수, 구형 데이터 변환, 장판 반응/대체/소유자 필터, 소환수별 공전 상태 분리를 검사합니다.

현재 클라우드에는 Unity Editor가 없어 실제 Unity 임포트·컴파일·물리·플레이 검증은 수행하지 못했습니다. 별도 .NET API 테스트 환경에서 변경한 C#을 컴파일하고 EditMode 테스트 17개 및 수명·폭발·접촉·흡인 등 시뮬레이션 검사 12개(총 29개)를 실행해 모두 통과했으며, 이 검사는 Unity 엔진 검증을 대체하지 않습니다. Unity에서 투척 비행, 지연/근접 폭발, 장판 수명, 다중 콜라이더 접촉 피해, 포탑 탄환의 고속 충돌, 흡인 종료 및 전투 초기화를 확인하세요.

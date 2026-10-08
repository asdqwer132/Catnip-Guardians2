# 추가 이펙트 제작 및 연결

Unity Inspector에서 에셋을 만들고 아이템에 연결하는 순서는 [추가 이펙트 사용 설명서](AdditionalEffectsUsage.md)를 참고한다. 수프·로켓화살·무지개 분기 예제와 각 필드의 입력값, 작동하지 않을 때 확인할 항목을 정리했다.

첨부된 `캣잎_가디언_추가_이펙트_설계.md`, 「캣잎 가디언 레벨 디자인(아이템 상세) (3) (1)」 원본 CSV, 원격 `work`의 기존 코드(`8497ead`)를 비교해 추가했다. CSV의 일반 아이템 240개와 재료 78개를 검토했고, 원본은 `Docs/Design/ItemDetails.csv`, 행별 연결 계획은 `Docs/ItemEffectCoverage.csv`에 보관했다. 기존 제작분을 유지하며 필요한 공통 기능과 수치가 명시된 설정 에셋을 추가했다.

## 기존 제작분 재사용

- 원형/직사각형 공격, 기절/DoT/수치 버프, 밀기/당기기와 기존 연출 풀을 재사용한다.
- 현재 프로젝트의 `RepeatItemEffect`에 순차 공격·동시 발사·바운스·지연 폭발·마지막 착지 후 효과가 통합되어 있다. 제거된 SequenceAttack 스크립트를 다시 만들지 않는다.
- 장판의 종류·조건·소비는 기존 `ReactiveGroundArea`에 연결했다.
- 소환수는 기존 Item Throw / Turret / Pull / Contact Damage / Orbit 모듈을 유지하며 개조·오라·합체 기능을 추가했다.

## 추가된 공통 기능

| 기능 | 설정 에셋/코드 | 연결 방법 |
| --- | --- | --- |
| 장판 종류·진입/이탈/종료/체류 | `AreaDefinition`, `AreaEffectData` | 장판 정의의 효과 목록에 기존 공격·버프 등을 연결 |
| 장판 조건·소비 | `HasAreaConditionData`, `ConsumeAreaEffect` | 조건에서 확인한 실제 장판을 소비한 뒤 후속 효과 실행 |
| 상태 키·조건·소비 | `StatusDefinition`, 기존 `BuffEffect`, `HasStatusConditionData`, `ConsumeStatusEffect` | 버프의 Status Definition에 키 지정; 수치 Modifier가 없어도 등록 가능 |
| 정화 | `CleanseEffect` | 해로운 상태이면서 Dispellable인 상태만 제거 |
| 회복·피해·체력 비용 | `HealthChangeEffect` | 식물/소유자/명중 적/소환물/범위 대상, 고정/현재 HP%/최대 HP% |
| 지속 회복 | `RegenerationEffect` | 회복량·간격·지속·첫 회복 시점 설정 |
| 방어·보호막 | `Health`, `ShieldEffect` | Health의 방어 공식 선택; 보호막 흡수 후 남은 HP 차감 |
| 쿨다운 | `CooldownControlEffect`, `ItemTagDefinition` | 지정 가방·아이템·시리즈·하위 태그의 남은 시간 감소/비율 감소/준비 완료 |
| 지속 쿨다운 버프 | `ItemCooldownStat`, `BagCooldownStat`, 전체 아이템/가방 대상 설정 | 기본 시간의 고정 초 감소, 독립적인 회복속도·지속 시간·중첩 |
| 명중 위치 후속 실행 | `ExecuteEffectOnHitData` | 공격의 On Hit Effects에 연결; 처치된 타격에서도 위치 기반 효과 실행 |
| 공통 대상 선택 | `TargetSelection` | 최근접/최원거리/현재 HP 최대/무작위/강적 우선 |
| 선택 대상/전체 적에 명중 효과 | `ApplyHitEffectsEffect` | 전체 기절이나 처형 등 기존 HitEffectData를 선택 결과에 직접 적용 |
| 연쇄 공격 | `ChainAttackEffect` | 최초 피해·후속 피해·전이별 증감·총 명중 수·전이 거리/간격 |
| 이동 공격 | `ProjectileAttackEffect` | 직선/왕복/회전/추적, 경로 충돌·관통·재명중 정책 |
| 부채꼴 공격·이동제어 | `SectorDamageAreaAttackEffect`, Sector/Rectangle 이동제어 모양 | 기존 모양·이동제어 연결; Sideways From Path는 진행 방향의 좌우로 밀기 |
| 소환물 조회·개조·합체 | `SummonDefinition`, `SummonModifyEffect`, `SummonTransformEffect`, `HasSummonConditionData` | 활성 소환물 조회, 공격 프로필·배율·수명 변경, 요구 종류/개수 지정 |
| 소환수 오라 | `SummonAuraModule` | 범위 진입 소환수에 지정 버프 등록, 이탈 시 해당 오라 버프만 제거 |
| 가중치 선택·동시 무작위 묶음 | `WeightedRandomEffect` | 70/30이면 하나만 선택; Selection Count=5이면 먼저 5개 추첨 후 같은 프레임 실행 |
| 실행별 배율 | `ScaledEffectData` | 피해·회복·범위·지속을 각각 선택해 조정; 공유 아이템/이펙트 에셋은 수정하지 않음 |
| 종료 후 후속 실행 | `ThenEffectData` | 연결된 하위 효과가 모두 자연 종료하면 후속 실행; 강제 취소에는 실행하지 않음 |
| 이동만 속박·타겟 변경 | `RootHitEffectData`, `TargetOverrideHitEffectData` | 속박은 공격을 허용; 타겟 변경은 소유자/근처 다른 적 등 모드와 우선순위 설정 |
| 처형 | `ExecuteHitEffectData` | HP 비율 임계값·면역·제한 대상 정책 지정 |
| 표식 반응 | `MarkHitEffectData` | 상태 키에 피격/처치/자연 만료 후 효과를 연결; 임의 추가 피해나 처치 보상 없음 |
| 시간 정지 | `TimeStopEffect` | 적 행동·적 투사체·스폰 정지, 선택적으로 적 상태 시계 정지; 종료 시 자신의 정지만 해제 |

Project 창의 **Create → GameData → Items → Effects / Hit Effects / Conditions**에서 에셋을 만든다. 소환 모듈, 상태/장판 정의, 태그는 해당 하위 메뉴에 있다. 실제 화면 연출은 각 효과의 `Visual Data`와 기존 프리팹을 연결한다.

## 바로 열어볼 예제 에셋

`Assets/Data/AdditionalEffectsExamples`에 기존 예제 21개와 `CooldownBuffs`의 설정 10개를 넣었다. 기존 아이템의 `Effect Datas` 또는 공격의 `On Hit Effects`에 연결해 확인할 수 있다. 쿨다운 폴더는 전체 아이템/가방 대상 2개, Modifier 4개, 5초 BuffEffect 4개로 구성된다.

`Assets/Data/AdditionalEffects`에는 CSV 수치로 구성한 에셋 28개가 있다. 따뜻한 수프·꿀단지·케이크·우유, 청록고등어·새우·전기뱀방어·잉어, 속성 스태프3·마법사의 상자·타이무 스토쁘, 무지개 광맥/축복·심연 포션·두 모자의 분기 등 기존 Effect Datas가 비어 있던 16개 ItemData에 연결했다. 정확한 목록은 `Docs/AdditionalEffectsConfiguration.json`에 있다.

로켓화살은 `Weapon28_FirstHitExplosion`을 기존 직사각형 공격의 **On Hit Effects**에 추가하면 된다. 후속 폭발은 CSV의 반경 1/피해 20이며 기존 공격 에셋의 수치·연출·참조를 유지했다. `Magic9_BeamAlliesHeal3`은 기존 광선 공격과 같은 아이템의 **Effect Datas**에 연결해 가로 0.4/길이 5의 아군 HP 3 회복을 추가한다. 크리스탈 파이의 100 회복도 별도 구성 요소로 제공하며 나머지 버프와 조합한다.

| 예제 | 동작 |
| --- | --- |
| `WarmSoup_Effects` | 식물 즉시 15 회복 + 1초 뒤부터 1초마다 1 회복, 5초 |
| `PoisonApple_CurrentHpCost20Percent` | 식물 현재 HP의 20% 비용. 예제 정책은 비치명, 방어/보호막 무시, 피격 반응 없음 |
| `Shield15For10Seconds_PlantExample` | 식물 대상 15 보호막, 10초. 견습 모자의 실제 대상 확정으로 취급하지 않음 |
| `Mackerel_Chain8Then6_Max4Hits` | 최초 8, 이후 6, 총 4회 |
| `ElectricEel_20_17_14_11_8` | 서로 다른 적 5명, 20/17/14/11/8 피해와 0.5초 기절 |
| `FirstHitExplosion_Example` | 공격 전체 최초 명중 한 번 폭발. 피해 10/반경 1은 테스트용 예제값 |
| `ExecuteAtTenPercentHp` | 현재/최대 HP 10% 이하 처형; 제한 대상과 면역 존중 |
| `TimeStopEnemiesFor5Seconds` | 5초 동안 적 행동·적 투사체·적 스폰 정지 |
| `ExampleStatusForFiveSeconds`, `HasExampleStatus` | 5초 상태 키 등록과 존재 조건 |
| `CreateExampleDefinedArea`, `HasExampleAreaAtImpact`, `ConsumeExampleArea` | 5초/반경 2의 식별 가능한 장판 생성·충돌 위치 조회·확인한 장판 소비 |

미정인 전이 거리·주기·장판 범위와 비용 정책에는 테스트용 설정을 사용했다. 실제 기획값으로 조정해야 한다. 연출이 없는 예제는 효과가 작동해도 별도 그림이 표시되지 않는다. 명중 폭발은 기존 DamageArea 프리팹을 연결했으며 로켓화살의 확정된 피해값으로 취급하지 않는다.

## 상태와 장판 연결 예시

광물의 `상태 & 장판` 분기는 다음과 같이 연결한다.

1. `StatusDefinition`과 `AreaDefinition`을 각각 만든다. 상태 표시 이름과 장판 표시 이름으로 비교하지 않고 에셋 참조로 구분한다.
2. `BuffEffect.buffInfo.statusDefinition`을 지정한다. 상태만 필요하면 Modifiers를 비워 둔다. Context Status 타깃으로 Player Status를 고르면 기존 상태 매니저에 등록한다.
3. `HasStatusConditionData`와 `HasAreaConditionData`를 만든다. 장판 조회 위치는 Impact/Owner/Hit/Anywhere 중 선택한다.
4. 기존 `ConditionalItemEffectData`의 조건 모드를 All로 두고 두 조건을 연결한다.
5. 장판을 소비하려면 `ConsumeAreaEffect.checkedCondition`에 동일한 장판 조건을 연결한다. 확인된 장판이 사라지면 다른 장판을 대신 소비하지 않는다.
6. 소비 후 기절·버프 등의 효과를 After Consume Effects에 연결한다. 장판 소비는 자연 종료 효과를 발동하지 않는다.

LED처럼 사용 전 상태를 기준으로 분기하려면 `Freeze Branch At Prepare`를 켠다. 기존 조건의 선택은 사용 시점에 고정하고, `ConsumeStatusEffect`로 확인한 이전 상태를 소비한 뒤 새 상태 버프를 연결한다. 같은 사용 중 새로 등록된 상태는 이전 상태 대신 소비하지 않는다.

## 회복·방어·쿨다운

- HP가 없는 플레이어에게 HP를 새로 추가하지 않았다. 기본 회복 대상은 식물이고, 소환물은 Health가 있는 경우만 대상이 된다.
- 퍼센트 회복/비용은 20이면 20%이다. 처형 Threshold는 0.1이면 10%이다.
- `Health.defenseFormula` 기본값은 None이다. Flat Reduction 또는 Percent Reduction은 기획의 방어 공식을 선택해 사용한다. HealthStat의 defense 수치가 실제 피해 계산에 연결된다.
- 최대 HP 버프는 기본 최대 HP와 현재 HP를 분리해 반복 갱신 때 중첩 계산하지 않는다. 최대 HP 증가 시 유지/증가량만큼 회복/비율 유지 정책을 선택할 수 있다.
- 쿨다운 Ready는 슬롯의 준비 완료 상태를 유지한다. 기존 Reset처럼 다음 사용에 초기 준비 대기를 다시 시작하지 않는다.
- 아이템 쿨다운은 `ItemCooldownStat.cooldown`, 회복속도는 `ItemCooldownStat.cooldownRecoveryRate`를 조정한다. 기존 `PlayerStat.cooldownRecoveryRate`도 아이템 시계에만 적용하며, 아이템별 속도와 곱해진다.
- 가방 공통 쿨다운은 `BagCooldownStat.cooldown`, 회복속도는 `BagCooldownStat.cooldownRecoveryRate`를 조정한다. 전체 가방 조회를 전체 아이템 조회와 분리해 다른 종류의 버프가 섞이지 않는다.
- 기본 시간은 준비 시작 시 확정하고 버프 만료 후 새 준비부터 복원한다. 진행 중인 회복속도는 현재 버프를 조회한다. 원본 데이터는 수정하지 않으며 UI는 시작할 때의 총 시간을 사용한다.
- 아군 관통 회복은 HealthChangeEffect의 Targets를 All Allies, Use Radius를 켜고 Shape를 Directional Rectangle로 지정한다. 방향과 시작 위치를 기존 광선 공격과 공유한다.
- 작은뼈/곤충날개는 Item Tag 에셋을 만들어 해당 ItemData에 연결한다. Monster 전체 시리즈로 대체하지 않는다.
- 지연 쿨다운 조작은 사용 당시 설정을 복사하고 전투 초기화 시 취소한다.

## 명중과 이동 공격

- 기존 First Hit Only의 직렬화 값은 유지하며 의미는 같은 적 생명당 한 번이다.
- `EveryHit`, `OncePerTarget`, `FirstHitPerAttack`, `FirstHitPerUse`를 구분한다. 같은 아이템 사용에서 생성된 자식 공격은 FirstHitPerUse 토큰을 공유한다.
- 피해 전에 적 위치·진행 방향·생명 번호를 보관한다. 적이 처치되어도 후속 폭발은 그 위치에서 실행하며, 기절/버프는 살아 있는 동일 생명에만 적용한다.
- 왕복은 나가는 경로와 돌아오는 경로, 회전은 회전 주기별로 재명중 기록을 나눈다. Interval/Once Per Life 정책도 선택할 수 있다.
- 새 이동 공격은 구간 충돌을 조회해 빠른 직선 이동의 중간 적을 검사한다. 회전은 짧은 구간으로 나누며 극단적인 설정에는 프레임당 처리 상한이 있다.

## 소환물과 무작위 연결

- `SummonAttackEffect.definition`을 지정하면 종류/태그로 조회할 수 있다. 개조는 해당 소환물의 런타임 상태에만 적용한다.
- 개조의 Duration/Attack Count가 끝나면 이전의 유효 공격 프로필을 복구한다. Attack Count는 실제 소환 공격이 성공했을 때만 소비한다.
- 소환물 HP는 Definition에 설정한다. 미끼 대상은 실제 피해를 받을 수 있는 소환물을 사용한다.
- 합체는 요구 Definition/개수를 먼저 확보한 후 결과 소환물을 만든다. `Consume Participants`는 기본 꺼짐이며 기획에서 재료 소비를 확정한 경우 켠다. 같은 재료로 반복 생성할지도 별도 선택한다.
- 소환수의 아이템 투척 공격력 덮어쓰기는 명시적으로 켠 경우에만 적용한다. 기존 Turret/Contact Damage의 공격력 계산은 유지한다.
- `WeightedRandomEffect`에 아이템 또는 효과 목록과 가중치를 지정한다. 자동 사용의 재고/가방 슬롯은 소비하지 않으며 사용 횟수 버프와 추가 공격은 각각 옵션이다.
- 마법사의 상자는 CSV의 Common/Magic 후보 14개 에셋을 연결했다. 후보 중 아직 기존 효과가 비어 있는 아이템은 행별 검토표대로 효과를 연결해야 실제 공격/상태가 발동한다. 상자 자체의 추첨·동시 실행은 구현되어 있다.
- `ScaledEffectData`는 명시된 의미의 스탯만 조정한다. 확률·개수·공격 간격 전체에 배율을 일괄 적용하지 않는다.
- 버프 그룹 입력은 자동 선택과 직접 입력을 함께 제공한다. DamageArea는 그룹/직접/범위 대상으로 등록되며 `DamageAreaAttackStat` Modifier를 실제 공격 수치에 반영한다. 그룹 이름 `DamageArea`와 스탯 타입 이름 `DamageAreaAttackStat`을 구분한다.
- 피해·범위 등의 실행 배율은 Snapshot와 Dynamic 버프를 계산한 뒤 적용한다. 기본 피해 10에 동적 피해 +10, 0.5배이면 최종 피해 10이다.
- 꽃 소환물 자체의 만개 조건은 `HasStatusConditionData.target = SourceSummon`으로 조회한다. 생성한 플레이어의 상태와 구분하며 소환물 생명 번호를 확인한다.

## 미정 기획과 연결 범위

상태 이름만 있는 꽃가루/발광/연결/차원 회피 등에 임의의 피해 증가나 회복을 넣지 않았다. 표식은 후속 효과 목록이 비어 있으면 추가 피해나 보상이 없다. 저주인형의 피해 공유 비율/대상 같은 실제 규칙은 원본 기획 확인 후 연결해야 한다. 소환물 합체의 소비·시점, 만개의 배율 대상, 장판 범위 등도 Inspector 설정으로 남겼다.

남은 아이템은 `Docs/ItemEffectCoverage.csv`의 Series+ID와 실제 ItemData 경로를 기준으로 연결한다. 시리즈마다 반복되는 Item1 같은 ID만으로 일괄 연결하지 않는다. 검토표는 연결 계획이며 모든 행의 게임 플레이 검증 완료를 뜻하지 않는다.

## 확인 방법

프로젝트는 Unity `6000.4.4f1`이다. Unity에서 열고 **Window → General → Test Runner → EditMode**에서 `CombatEffectTests`와 `Additional*Tests`를 실행한다. 이후 실제 씬에서 다음을 확인한다.

- 수프의 즉시 회복과 정확히 5회의 지연 주기 회복.
- 장판 조건·소비, 체류 판정, 소비 시 자연 종료 효과가 발동하지 않는지.
- 처치 타격의 최초 명중 폭발과 산탄 전체 FirstHitPerUse 범위.
- 왕복/회전/추적의 재명중과 다중 콜라이더 적의 중복 피해.
- 보호막 소진·갱신·만료와 방어 계산, 최대 HP 버프 만료.
- 소환물 프로필 만료·합체·오라 이탈·풀 재사용.
- 정화로 표식이 해제될 때 자연 만료 반응이 실행되지 않는지.
- 중첩 시간 정지와 기존 기절이 서로의 정지를 해제하지 않는지.
- 전투 초기화·비활성화 시 지연 효과/후속 실행이 취소되는지.

클라우드에는 Unity Editor가 없다. 별도 C# 검사는 실제 UnityEngine 2022.3.62 참조 DLL로 관련 코드를 컴파일하고, 엔진 없이 실행 가능한 관리 코드 검사를 수행한다. 관련 없는 매니저와 RNG에는 검사 전용 대체 구현을 사용한다. 이는 Unity 6000.4의 임포트·물리·씬 플레이 검증을 대체하지 않는다. 재현 방법은 `Tools/ValidateEffects/README.md`에 있다.

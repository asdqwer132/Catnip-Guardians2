# Food1 → Weapon1 → 기본 적 테스트 시작하기

테스트 코드는 [Food1Weapon1BuffTests.cs](../Assets/Editor/Tests/Food1Weapon1BuffTests.cs)에 있습니다. 프로젝트의 실제 Food1·Weapon1 에셋과 DefaultEnemy 프리팹을 읽어 아이템 실행기, 버프 매니저, 공격 장판, 적, Health까지 확인합니다.

## 실행 방법

1. Unity `6000.4.4f1`에서 프로젝트를 열고 컴파일이 끝날 때까지 기다립니다.
2. 재생을 중지하고 **Window → General → Test Runner**를 엽니다.
3. **EditMode** 탭에서 `Food1Weapon1BuffTests`를 찾습니다. 검색창에 이름을 입력해도 됩니다.
4. 테스트 클래스를 선택하고 **Run Selected**를 누릅니다. 처음에는 다른 테스트를 함께 실행하지 않아도 됩니다.
5. 네 개가 모두 초록색이면 성공입니다. 빨간 테스트를 클릭하면 실패한 비교의 `Expected`(기대값), `But was`/`Actual`(실제값), 코드 위치를 확인할 수 있습니다.

이 테스트는 `[Test]` 메서드와 NUnit의 `Assert.That`으로 작성했습니다. 이미 프로젝트에 있는 `com.unity.test-framework`를 사용하므로 별도 패키지 설치나 전투 씬 실행은 필요 없습니다.

## 무엇을 확인하나요?

현재 기획값은 Weapon1 공격력 **5**, Food1 공격력 증가 **10%**, 버프 시간 **8초**, 기본 적 HP **10**입니다. Modifier의 `Multiply Value = 0.1`은 0.1배가 아니라 **1 + 0.1 = 1.1배**를 뜻합니다.

| 테스트 이름 | 확인할 동작 | 기대값 |
| --- | --- | --- |
| `Weapon1_WithoutFood1_Deals5DamageToDefaultEnemy` | 버프 없이 공격 | Damage 5, 적 HP 10 → 5 |
| `Food1_BeforeWeapon1_RegistersBuffAndNewDamageAreaHas5Point5Damage` | Food1 사용 후 새 장판 생성 | DamageArea 그룹에 8초 버프 등록, Damage 5.5 |
| `Food1_AfterWeapon1_UpdatesExistingDamageAreaFrom5To5Point5` | 이미 존재하는 장판에 Food1 적용 | Damage 5 → 5.5 |
| `Food1_ThenWeapon1_HitDefaultEnemy_ReducesHpFrom10To4Point5OnlyOnce` | 버프 받은 장판으로 기본 적 공격 | 실제 피해 5.5, 적 HP 10 → 4.5, 중복 충돌 시 HP 유지 |

기대값을 직접 적었기 때문에 Food1 대상 연결이 끊기거나 증가율이 달라지면 실패합니다. 기획 의도로 수치를 변경했다면 파일 위쪽 상수와 관련 기대값을 새 기획에 맞게 수정하세요. 에셋에서 읽은 실제 계산 결과를 그대로 기대값으로 사용하면 계산 오류를 놓칠 수 있습니다.

## 코드를 읽는 순서

먼저 `Food1_ThenWeapon1_HitDefaultEnemy_ReducesHpFrom10To4Point5OnlyOnce`를 읽어 보세요.

1. **준비(Arrange)**: Food1 사용, Weapon1 사용, 기본 적 생성, 공격 전 HP 저장.
2. **실행(Act)**: `SendHit`으로 장판에 적의 충돌 알림 전달.
3. **확인(Assert)**: 최종 Damage, Health가 받은 피해, 실제 HP 감소량, 남은 HP를 비교.

예를 들어 다음 줄은 “적 HP가 4.5이면 통과하고, 다르면 이 테스트를 실패시킨다”는 뜻입니다.

```csharp
Assert.That(enemy.health.Hp, Is.EqualTo(4.5f).Within(0.0001f));
```

`Within`은 `float`의 아주 작은 소수점 오차를 허용합니다. `[SetUp]`은 각 테스트 전에 새 매니저/임시 씬을 준비하고, `[TearDown]`은 성공·실패에 관계없이 복제본을 정리합니다. 열려 있던 작업 씬은 보존하고 원래 활성 씬과 매니저를 복원합니다.

파일 아래의 도우미는 실제 에셋 읽기, 아이템 사용, 생성된 장판 찾기, 적 초기화, 명중, Damage 읽기를 맡습니다. 주요 테스트부터 읽고 필요할 때 해당 도우미로 내려가면 됩니다.

## 값은 어디서 볼 수 있나요?

- 테스트 실행 후 **Console**에서 `[Food1 버프]`, `[기존 장판 갱신]`, `[버프 후 명중]` 로그를 확인합니다. Test Runner의 테스트 출력에서도 볼 수 있습니다.
- `ReadDamage(area)`가 읽는 값은 **생성된 DamageArea 컴포넌트 → Runtime Stat → Damage**입니다. Weapon1 에셋의 기본 Attack Stat은 버프 후에도 5로 유지됩니다.
- `enemy.health.Hp`가 실제 남은 HP이고, `OnDamageResolved`의 `requestedDamage`는 전달받은 피해, `hpDamage`는 방어/보호막 계산 이후 실제 HP 감소량입니다. 현재 기본 적은 둘 다 5.5입니다.
- 중단점을 `SendHit` 바로 앞에 놓고 디버깅하면 공격 전 장판/버프/적 값을 살펴볼 수 있습니다. 테스트가 끝나면 임시 객체는 삭제됩니다.

## 이 테스트의 범위

`UseItem`은 실제 `ItemEffectExecutor.ExecuteItem`을 호출합니다. UI 클릭, 투척 이동, 가방 슬롯 소비는 생략합니다. 명중은 `OnTriggerEnter2D`에 실제 적의 Collider2D를 전달하며, 피해는 **DamageArea → HitEffectDispatcher → Enemy → Health**의 실제 코드가 계산합니다. 적의 `TakeDamage(5.5)`를 테스트에서 직접 호출하지 않습니다. 특수 아이템 추가 공격과 피해 팝업은 이 시나리오에서 끕니다.

EditMode에서 시간은 기다리지 않으므로 장판 수명 0.1초나 버프 8초의 실제 만료 시간, 자동 물리 충돌 감지, 이동·애니메이션은 검사하지 않습니다. 이 부분은 Play Mode에서 별도로 확인해야 합니다. 기획 수치와 버프 연결, 기존/새 장판의 공격력, 명중 후 HP 감소를 빠르게 확인하는 테스트입니다.

클라우드의 `Tools/ValidateEffects/check.py`는 이 파일을 Editor API 대체 선언으로 **컴파일만** 확인합니다. 네 개의 Unity 테스트 실행 결과는 위 Test Runner에서 확인해야 합니다.

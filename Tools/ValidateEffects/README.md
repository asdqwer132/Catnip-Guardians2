# 이펙트 보조 검증

Unity가 없는 환경에서 C# API와 관리 코드의 규칙을 확인하는 도구입니다. 프로젝트의 Unity 버전은 **6000.4.4f1**이며, 이 도구의 API 참조 버전은 **2022.3.62**입니다. 실제 Unity 임포트·컴파일·물리·애니메이션·Play Mode 검증을 대체하지 않습니다.

```bash
python3 Tools/ValidateEffects/check.py
```

Python 3.12 이상, curl, Linux x64가 있으면 .NET SDK를 임시 캐시에 설치합니다. 다른 플랫폼에서는 .NET SDK 8 이상을 먼저 설치하세요. 다운로드·생성한 csproj·빌드 결과는 시스템 임시 폴더에 저장하며 프로젝트 안에 저장하지 않습니다. 의존성 캐시는 `--cache /원하는/경로`로 지정할 수 있습니다.

다운로드 없이 Unity 메타데이터만 확인하려면 다음을 실행합니다.

```bash
python3 Tools/ValidateEffects/check.py --metadata-only
```

검증 범위:

- [씬 연결](../../Docs/SceneFlow.md)의 로더·카메라/Canvas 연결·설정·재화·입력 코드를 함께 API 컴파일합니다. 일곱 씬의 빌드 등록, 공용 설정 버튼과 EventSystem, 씬 내부 참조도 검사하며 실제 화면 비율 계산과 이전 씬 이름 변환은 관리 코드로 실행합니다. Input System 및 범위 밖 서비스의 대체 선언은 `SceneFlowApi.cs`에 있습니다. Additive 비동기 로딩·렌더링·버튼 클릭·설정 일시정지는 Unity에서 확인해야 하며 `SceneFlowTests`의 엔진 검사는 여기서 컴파일만 합니다.

- 아이템 이펙트, 버프 UI, Entity 런타임, 관련 아이템 데이터, `Assets/Editor/Tests`의 테스트 코드를 실제 Unity/NUnit 참조 DLL로 컴파일합니다. 테스트 코드의 컴파일 성공과 Unity 테스트 실행은 구분합니다. `AdditionalEffectBuffUITests`는 재생·보호막·시간 정지의 표시 옵션과 실제 시계·소진·취소·필터를 Unity에서 검사합니다.
- `ManagedChecks.cs`의 103개 검사를 실행합니다. 상태 재부여의 남은 시간·횟수 누적, 기존 갱신 동작, 수명 모드 변경, 오버플로 방지와 출처가 다른 동일 상태의 합산·대상 격리, 마지막 횟수의 특수 기능 실행 순서도 검사합니다. 버프 분류·기존 상태 호환·플래그 Any/All과 OR/AND·정화 금지·분류 복사·갱신·정화 취소, 샷건 균등/랜덤 각도·시작 방향·회전 순서·거리 오프셋·반지름 범위·거리 하한·범위 배율, 투척 도착 시간 범위·함수별 분포·무게·오프셋·상한, 고정 초 Modifier·전체 아이템/가방 및 사용 가방 대상 판정·회복속도 분리·시작 시간과 진행률 보존, 아이템 전체 종료 후 후속 사용의 대기·취소·순환·중첩 수명·연출 순서·자동 사용 옵션, 쿨다운 준비 상태·감소, 배율 복사, 재귀·실행량 제한, 명중 범위·풀 생명 번호, 가중치 선택, 중첩 시간 정지를 검사합니다. ActiveBuff·BuffStorage·BuffCleanseFilter, 거리와 도착 시간의 계산·분포 함수, Modifier·대상 판정·ItemEffectExecutor의 실행과 수명 연결은 실제 코드를 사용합니다. 관리 검사에서는 씬 객체·연출 재생·개별 효과·버프 매니저 연결을 대체합니다. 실제 효과의 등록·만료·씬 객체 연결은 Unity EditMode 테스트에서 확인해야 합니다. 정화 등록과 수치 복구는 `AdditionalBuffClassificationTests`, 투척 이동과 무게 전달은 `AdditionalThrowArrivalTests`, 실제 샷건 배치와 각도는 `CombatEffectTests`에서 검사합니다.
- 변경한 C#·에셋·프리팹의 `.meta`, 기존 GUID 유지, Assets 전체의 GUID 중복을 확인합니다.

제한과 대체 API:

- API 컴파일의 `GameApi.cs`는 범위 밖의 게임 매니저와 일부 UI를 대체합니다. 버프 UI·이펙트·Enemy·Health·소환물·이동·패턴·스폰 코드와 Unity API는 실제 소스와 참조 DLL을 사용합니다. 아이템 사용 입력/UI 컨트롤러는 이 컴파일 범위에서 제외합니다.
- 실행 검사의 `ManagedApi.cs`는 게임 객체·에셋·버프 매니저를 단순한 객체로 대체하고, Unity의 네이티브 Random을 고정 시드의 .NET Random으로 대체합니다. AnimationCurve는 빈 커브·선형 커브만 처리하는 관리 코드로 대체합니다. Mathf와 Vector의 관리 코드는 실제 Unity 참조 DLL을 사용합니다. 실제 커브의 보간·접선, 장면 객체 생성, 물리 충돌, 엔진의 Random 분포는 실행 검사 대상이 아닙니다.
- Unity 6000에서 `Window → General → Test Runner → EditMode`로 프로젝트 테스트를 실행하고 Play Mode에서 투사체·장판·회복·소환·정지를 확인하세요.

의존성 출처와 고정 버전:

| 의존성 | 버전 | 출처 |
| --- | --- | --- |
| .NET SDK | 8.0.414 | Microsoft `builds.dotnet.microsoft.com/dotnet/Sdk` (시스템 SDK가 없을 때만 설치) |
| Unity 관리 참조 DLL | RocketModFix.UnityEngine.Redist 2022.3.62.3 | NuGet `api.nuget.org/v3-flatcontainer/rocketmodfix.unityengine.redist` |
| NUnit | 3.14.0 | NuGet `api.nuget.org/v3-flatcontainer/nunit` |

참조 DLL은 NuGet의 제3자 배포 패키지이며 Unity 6000 공식 Editor 설치에서 얻은 DLL이 아닙니다. 바이너리는 저장소에 포함하지 않습니다. 설정된 HTTPS 프록시와 인증서 검증을 유지해 다운로드합니다.

특수 상태 기능은 `SpecialItemManager`의 실제 소스도 API 컴파일에 포함합니다. `AdditionalSpecialStatusTests`는 상태 키 보유에 따른 별 공격·마지막 횟수·만료·정화·대상 격리·자기 재호출 방지를 Unity에서 확인합니다. 이 테스트는 여기서 컴파일만 수행하며 Unity Editor에서 실행해야 합니다.

특수 상태와 버프 분류를 분리한 회귀 검사도 포함합니다. 관리형 실행은 상태 키 누적, 분류만 있는 버프의 상태 미부여, 공유/추가 태그 결합, 재부여 시 분류 갱신을 검사합니다. `AdditionalDirectStatusTests`·`AdditionalBuffClassificationTests`의 대상별 조회와 정화 검사는 Unity에서 실행합니다.

버프 그룹 검증은 `IBuffTarget`의 문자열 getter·필드 기본값·상속·명시적 구현·추가 선언을 실제 메타데이터 수집 코드로 실행합니다. 생성자·정적 생성자·동적 getter를 실행하지 않는 것도 검사합니다. `BuffTargetInspectorTests`의 프리팹·비활성 씬·Undo 검사는 Unity EditMode에서 실행해야 합니다.

그룹 Inspector·PropertyDrawer·EditMode 테스트는 `EditorApi.cs`의 대체 API 선언으로 추가 컴파일합니다. 이는 C# 연결 검증이며 UnityEditor DLL이나 실제 GUI 실행 검증을 대신하지 않습니다.

Float Field Modifier Inspector도 같은 방식으로 컴파일합니다. `EditorMetadataChecks.cs`는 실제 스탯/필드 목록을 조회하는 `BuffDurationEditorTests` 한 개만 관리 코드로 실행해 `BuffInfo.duration`의 선택 가능 여부를 확인합니다. GUI와 Unity 씬 API는 실행하지 않습니다. `AdditionalBuffDurationTests`의 실제 버프 부여·재부여·시간 유지 검사는 Unity EditMode에서 실행해야 합니다.

`Food1Weapon1BuffTests`도 Editor API 대체 선언으로 컴파일합니다. 실제 Food1 사용 → Weapon1 장판 공격력 5.5 → 기본 적 HP 4.5 검사는 Unity EditMode에서 실행해야 합니다. 처음 실행하는 방법과 코드 설명은 [Food1/Weapon1 테스트 안내](../../Docs/Food1Weapon1Tests.md)를 참고하세요.

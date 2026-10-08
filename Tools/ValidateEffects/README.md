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

- 아이템 이펙트, Entity 런타임, 관련 아이템 데이터, `Assets/Editor/Tests`의 테스트 코드를 실제 Unity/NUnit 참조 DLL로 컴파일합니다. 테스트 코드의 컴파일 성공과 Unity 테스트 실행은 구분합니다.
- `ManagedChecks.cs`의 47개 검사를 실행합니다. 고정 초 Modifier·전체 아이템/가방 및 사용 가방 대상 판정·회복속도 분리·시작 시간과 진행률 보존, 쿨다운 준비 상태·감소, 배율 복사, 수명·취소, 재귀·실행량 제한, 명중 범위·풀 생명 번호, 가중치 선택, 중첩 시간 정지를 검사합니다. Modifier와 대상 판정은 실제 코드를 사용하고 관리 검사에서는 ScriptableObject와 버프 매니저 연결을 대체합니다. 실제 버프 등록·만료·씬 대상 연결은 Unity EditMode 테스트에서 확인해야 합니다.
- 변경한 C#·에셋·프리팹의 `.meta`, 기존 GUID 유지, Assets 전체의 GUID 중복을 확인합니다.

제한과 대체 API:

- API 컴파일의 `GameApi.cs`는 범위 밖의 게임 매니저와 UI를 대체합니다. 이펙트·Enemy·Health·소환물·이동·패턴·스폰 코드와 Unity API는 실제 소스와 참조 DLL을 사용합니다. 아이템 사용 입력/UI 컨트롤러는 이 컴파일 범위에서 제외합니다.
- 실행 검사의 `ManagedApi.cs`는 게임 객체·에셋·버프 매니저를 단순한 객체로 대체하고, Unity의 네이티브 Random을 고정 시드의 .NET Random으로 대체합니다. Mathf와 Vector의 관리 코드는 실제 Unity 참조 DLL을 사용합니다. 장면 객체 생성, 물리 충돌, 엔진의 Random 분포는 실행 검사 대상이 아닙니다.
- Unity 6000에서 `Window → General → Test Runner → EditMode`로 프로젝트 테스트를 실행하고 Play Mode에서 투사체·장판·회복·소환·정지를 확인하세요.

의존성 출처와 고정 버전:

| 의존성 | 버전 | 출처 |
| --- | --- | --- |
| .NET SDK | 8.0.414 | Microsoft `builds.dotnet.microsoft.com/dotnet/Sdk` (시스템 SDK가 없을 때만 설치) |
| Unity 관리 참조 DLL | RocketModFix.UnityEngine.Redist 2022.3.62.3 | NuGet `api.nuget.org/v3-flatcontainer/rocketmodfix.unityengine.redist` |
| NUnit | 3.14.0 | NuGet `api.nuget.org/v3-flatcontainer/nunit` |

참조 DLL은 NuGet의 제3자 배포 패키지이며 Unity 6000 공식 Editor 설치에서 얻은 DLL이 아닙니다. 바이너리는 저장소에 포함하지 않습니다. 설정된 HTTPS 프록시와 인증서 검증을 유지해 다운로드합니다.

# Catnip-Guardians2 개발 환경

## 설치 승인 후 현재 상태 (2026-10-22)

사용자가 설치를 승인한 뒤 공식 Unity 플러그인 `unity@unity-agent-plugin` `0.1.8-beta`와 Unity CLI `1.0.0-beta.14`를 설치했다. CLI는 공식 manifest의 SHA-256과 일치하며 `/workspace/.local/unity-cli/bin/unity`, PATH의 `/home/agent/.local/bin/unity`에서 실행된다.

Codex app-server의 실제 `skills/list` 결과에서 공식 Unity 스킬 32개와 저장소 범위 `unity-project-workflow`의 활성/검색을 확인했다. 프로젝트 스킬은 공식 문서에서 확인한 `.agents/skills/unity-project-workflow/SKILL.md`로 설치했다. 현재 채팅의 사전 제공 도구 목록과 CLI의 새 검색 결과는 별개이므로 현재 채팅에 Editor MCP가 연결되었다고 해석하지 않는다.

공식 CLI로 `Packages/manifest.json`에 `com.unity.pipeline: 0.9.0-exp.1`을 등록했다. 공식 registry의 최소 Unity 버전은 6000.0이고, 기존 Unity 6000.4.4f1과 직접 지정된 Test Framework 1.6.0을 변경하지 않았다. 실제 UPM 임포트와 lockfile 생성/Editor 서버 연결은 아직 수행하지 않았다.

Unity Editor 6000.4.4f1도 `/workspace/.local/unity-editors/6000.4.4f1`에 설치했다. 원본 프로젝트를 열지 않는 `Unity -version` 출력과 CLI `editors verify` 구조 검사가 통과했다. 라이선스 조회 결과는 0개이므로 실제 프로젝트 실행/임포트/연결은 수행하지 않았다. CLI 등록에는 폴더 대신 실제 `Editor/Unity` 실행 파일 경로를 사용한다.

네트워크 정책은 현재 unrestricted이며 이전에 차단되었던 공식 Unity/OpenAI 문서에 직접 접속했다. 아래의 차단·미설치·스킬 초안 기록은 설치 승인 전 조사 결과다. 최신 설치 결과와 남은 제약은 [setup-report.md](setup-report.md)를 기준으로 한다.

## 설치 승인 전 조사 기록

확인일: 2026-10-22 (Asia/Seoul). 대상: `/workspace/Catnip-Guardians2`, 브랜치 `work`, 기준 커밋 `d20fe39b410240228affefdd6816a189c4f0b0f8`.

## 확인한 프로젝트와 도구

| 항목 | 확인 결과 | 근거/한계 |
| --- | --- | --- |
| 프로젝트 | Catnip-Guardians2, productName `Catnip2` | `Assets`, `Packages`, `ProjectSettings` 존재 |
| 프로젝트 Unity | `6000.4.4f1`, revision `360f97ecca93` | `ProjectSettings/ProjectVersion.txt` |
| 설치된 Unity Editor / Hub | PATH와 조사한 표준 경로에서 미발견 | 프로젝트 버전과 별개이며 실행 검증 불가 |
| 호스트 | Linux x86_64, glibc 2.41 | 실행 환경 조사 |
| Codex CLI | `0.159.2` | `codex --version`; `plugin` 명령 지원 |
| Python | 3.12 | 기존 검증 스크립트 실행 환경 |
| .NET SDK | 캐시의 `8.0.414` | `/workspace/.cache/catnip-validation/dotnet/dotnet --version`; PATH에는 없음 |
| 렌더링 | URP `17.4.0`, 2D Renderer 설정 | manifest/lock 및 `Assets/Settings/Renderer2D.asset` |
| 입력 | Input System `1.19.0` | `activeInputHandler: 1`, 실제 `UnityEngine.InputSystem` 사용 |
| UI | uGUI `2.0.0`, TextMeshPro 사용 | 패키지 및 기존 씬/UI 스크립트 |
| Unity Test Framework | `1.6.0`, `com.unity.ext.nunit` `2.0.3` | `Packages/packages-lock.json` |
| 테스트/어셈블리 | `Assets/Editor/Tests`에 C# 34개, Assets 내 asmdef/asmref 없음 | Editor 폴더의 기존 테스트 구성; 별도 PlayMode 테스트 어셈블리 미발견 |
| 타깃 플랫폼 | 미확인 | Standalone 설정은 있으나 활성 빌드 타깃/배포 플랫폼을 확정할 근거 없음 |
| Unity 라이선스 | 사용 가능 여부 미검증 | Editor 미발견, 조사한 라이선스 관련 변수 미설정; 인증 정보 요구/변경 없음 |

주요 경로는 `Assets/Script/Items/Effect`(이펙트/버프/소환), `Assets/Script/Items/Merge`(머지 게임), `Assets/Script/Entity`, `Assets/Script/Manager/Utility/Scene`(씬/카메라/Context), `Assets/Editor`, `Assets/Scenes`, `Assets/Prefab`, `Docs`, `Tools/ValidateEffects`이다. Runtime은 기존 `Assembly-CSharp`, Editor는 `Assembly-CSharp-Editor`의 기본 폴더 구성을 따른다.

## 공식 Unity 플러그인 조사

Unity Technologies의 공식 저장소를 직접 열어 다음 자료를 읽었다. 설치 파일을 실행하거나 플러그인으로 등록하지 않았다.

- [공식 저장소](https://github.com/Unity-Technologies/unity-agent-plugin)
- [고정 커밋의 공식 안내](https://github.com/Unity-Technologies/unity-agent-plugin/blob/d95425b6c0055e1473566476d90e116645999878/.github/README.md)
- [Codex 플러그인 manifest](https://github.com/Unity-Technologies/unity-agent-plugin/blob/d95425b6c0055e1473566476d90e116645999878/.codex-plugin/plugin.json)
- [Codex marketplace manifest](https://github.com/Unity-Technologies/unity-agent-plugin/blob/d95425b6c0055e1473566476d90e116645999878/.agents/plugins/marketplace.json)
- [Unity CLI 스킬](https://github.com/Unity-Technologies/unity-agent-plugin/blob/d95425b6c0055e1473566476d90e116645999878/skills/unity-cli/SKILL.md)
- [공식 CLI의 테스트 설명](https://github.com/Unity-Technologies/unity-agent-plugin/blob/d95425b6c0055e1473566476d90e116645999878/skills/unity-cli/references/build-run-test.md)

| 구분 | 확인 결과 |
| --- | --- |
| 제공 주체/버전 | Unity Technologies, `0.1.8-beta` |
| 설치 식별자 | `unity@unity-agent-plugin` |
| 조사한 정확한 커밋 | `d95425b6c0055e1473566476d90e116645999878` |
| 공개 지원 범위 | 공식 저장소의 안내는 Unity 6+; 프로젝트 6000.4는 이 범위에 포함 |
| 실제 호환성 | 정확한 6000.4.4f1 프로젝트 연결/실행은 미검증. 공식 문서 사이트 접근도 차단되어 최종 호환성은 미확인 |
| Codex 요건 | manifest에 `requires_local_executor: true`; 현재 CLI에는 marketplace/add/list 명령이 존재 |
| 현재 설치 | 로컬 CLI의 `plugin list --json`은 installed 빈 목록. 이 채팅에서 사용 가능한 스킬 목록에도 Unity 없음. 원격 카탈로그 조회는 실패하여 전체 카탈로그 상태는 미확인 |
| 제공 스킬 | manifest의 `skills: ./skills/`; 개발 지식/절차를 제공하는 에이전트 플러그인 |
| Unity CLI | `unity` 실행 파일 미발견. 공식 스킬에 macOS/Linux 및 Windows 설치 안내가 있으나 설치/업데이트 미실행 |
| Editor 직접 제어 | 별도의 Unity CLI, 실행 중인 Editor와 프로젝트의 `com.unity.pipeline`(공식 안내상 Unity 6.0+) 필요. 현재 manifest/lock에 해당 패키지 없음 |
| MCP | CLI에서 별도로 구성하는 선택 사항. 현재 Unity MCP 연결 없음/연결 검증 미실행 |
| Unity 패키지와 구분 | Codex 플러그인 설치는 `Packages/manifest.json`의 Pipeline 패키지 설치를 대신하지 않음 |

공식 플러그인 설치 제안과 변경/복구 범위는 [workflow.md](workflow.md)에 있다. 플러그인의 자체 자동 설치 지침은 이번 설정의 설치·패키지 변경 승인 경계를 확장하지 않는다. Unity 버전을 올리거나 최신 Editor로 대체하지 않는다.

## 공식 문서 접근과 스킬 경로

다음 URL은 2026-10-22 직접 요청했으나 프록시가 CONNECT 403으로 거절했다.

- https://docs.unity.com/en-us/ai/unity-plugin/about-unity-plugin
- https://learn.chatgpt.com/docs/agent-configuration/agents-md
- https://developers.openai.com/plugins/build/skills
- https://developers.openai.com/codex/skills
- https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/reference-command-line.html
- https://docs.unity3d.com/6000.4/Documentation/Manual/EditorCommandLineArguments.html

OpenAI 공식 저장소의 [skills 문서](https://github.com/openai/codex/blob/main/docs/skills.md)도 확인했지만 현재 내용은 차단된 공식 사이트로 안내하는 링크다. 따라서 현재 공식 문서에서 프로젝트 범위 스킬 검색 경로를 검증하지 못했다. 첨부 지시문의 대체 경로 규칙에 따라 [프로젝트 스킬 초안](skill-drafts/unity-project-workflow/SKILL.md)을 기존 문서 루트 `Docs/Codex/skill-drafts/`에 저장했다. 자동 검색 경로에 설치한 것으로 간주하지 않는다.

사용 가능한 cloud/executor 스킬 목록과 로컬 스킬 경로에서 활성 `skill-creator`는 찾지 못했다. OpenAI 공식 저장소의 [작성 지침](https://github.com/openai/codex/blob/main/codex-rs/skills/src/assets/samples/skill-creator/SKILL.md)을 참고해 짧은 단일 스킬로 작성했다. 스킬 메타데이터/로컬 링크 검증과 실제 Codex 자동 검색·호출은 서로 다른 검증이다.

## Git과 파일 보존

작업 시작 시 `work`는 깨끗했고 `git fetch origin work` 후 HEAD와 FETCH_HEAD의 차이는 없었다. Git 읽기는 기존 연결로 성공했으며 인증 값은 문서에 저장하지 않았다.

- 추적 파일 8,322개, 그중 Assets 아래 8,236개(에셋과 meta 포함).
- 추적 에셋의 누락 meta, 미추적 meta sidecar, GUID 중복 없음. 파일명은 Git NUL 구분 출력으로 확인했다.
- `.gitignore`는 Library/Temp/Obj/Build/Builds/Logs/UserSettings와 IDE 생성 파일을 제외한다. 해당 생성 폴더의 추적 파일은 0개다.
- `.gitattributes`가 없고 `git lfs ls-files`는 빈 목록이다. LFS 도입·변환·이력 변경은 수행하지 않았다.
- TestResults.xml, MemoryCaptures, Recordings의 ignore는 없어서 별도 [제안 파일](gitignore.proposed)을 준비했다. 현재 검증 출력은 프로젝트 밖에 쓴다.
- 원래 `AGENTS.md`는 유지했다. [통합 수정안](AGENTS.proposed.md)과 [차이](AGENTS.proposed.diff)는 검토용이며 루트 규칙을 덮어쓰지 않는다.

## 검증 상태

기존 SDK/참조 패키지 캐시를 재사용해 다음 명령을 실행했고 종료 코드 0이었다. Unity Editor를 실행하지 않았다.

```bash
cd /workspace/Catnip-Guardians2
python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation
```

이번 실행은 production/test 소스 348개의 보조 API 컴파일, Editor API 대체 선언을 통한 추가 컴파일, 관리 로직 312개와 입력/오디오/카메라/Context 수명주기 모형 검사를 통과했다. 로그: `/tmp/catnip-codex-setup-validation.log`.

보조 DLL은 제3자 NuGet 패키지 `RocketModFix.UnityEngine.Redist 2022.3.62.3`과 NUnit 3.14.0이다. 프로젝트의 Unity 6000 공식 Editor 및 UTF 1.6 실행을 대신하지 않는다. 보조 csproj에는 대체 선언용 경고 제외 설정도 있으므로 보조 빌드의 경고 0을 Unity Editor의 경고 없음으로 해석하지 않는다. 기존 검증 코드는 이번 설정에서 수정하지 않았다.

Unity 임포트/실제 EditMode 실행/PlayMode/그래픽/물리/게임 빌드는 환경 제약으로 미실행이다. 이 문서는 전체 Unity 개발 환경 준비 완료나 플러그인 호환성 확인을 선언하지 않는다.

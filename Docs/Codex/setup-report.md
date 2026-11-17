# 개발 환경 설정 결과

## 설치 승인 후 결과 (2026-10-22)

사용자가 “그럼 설치해”로 승인한 뒤 실제 설치를 수행했다. 현재 상태는 다음과 같다.

| 항목 | 현재 결과 |
| --- | --- |
| 공식 Unity 플러그인 | `unity@unity-agent-plugin` `0.1.8-beta` 설치·활성 확인. 공식 커밋 `d95425b6c0055e1473566476d90e116645999878`로 고정 |
| Unity CLI | `1.0.0-beta.14` 설치. 공식 CDN manifest의 SHA-256 검증 및 버전/최신 여부 확인 |
| CLI 실행 경로 | `/workspace/.local/unity-cli/bin/unity`, PATH의 `/home/agent/.local/bin/unity` |
| 프로젝트 스킬 | `.agents/skills/unity-project-workflow/SKILL.md` 설치. 실제 Codex `skills/list`에서 enabled/repo 확인 |
| 공식 스킬 검색 | 실제 Codex 검색에서 공식 Unity 스킬 32개가 enabled 상태이며 오류 없음 |
| Pipeline | 공식 CLI로 `Packages/manifest.json`에 `0.9.0-exp.1` 등록. 최소 Unity 6000.0 확인; UPM 임포트/lock 갱신은 미실행 |
| Unity Editor | 정확한 `6000.4.4f1` 공식 배포본 설치 완료. 비실행 `-version` 출력과 `editors verify` 구조 검사 통과; 설치 경로 `/workspace/.local/unity-editors/6000.4.4f1` |
| 라이선스 | 공식 CLI의 조회는 성공했지만 활성 라이선스 수 0. 계정/라이선스 변경 없음 |
| Editor 연결 | `unity status`의 실제 연결 인스턴스 0. 원본 프로젝트 실행/실제 Unity 테스트 미실행 |
| 네트워크 | 현재 unrestricted이며 공식 Unity/OpenAI 문서 직접 접속 성공 |

공식 문서에서 저장소 스킬 경로를 확인했으므로 문서 폴더의 초안을 정식 `.agents/skills` 경로에 배치했다. 이 채팅에 처음 제공된 도구/스킬 목록과 새로운 CLI 검색 결과는 별개이다. Editor 연결이나 현재 채팅의 MCP 활성화를 완료했다고 선언하지 않는다.

프로젝트의 Unity 버전과 직접 지정된 Test Framework 1.6.0은 유지했다. 설치 후 기존 metadata/씬 연결 검사는 통과했다. Git 변경은 연결용 manifest와 새 프로젝트 스킬/설정 문서이며, C#·씬·프리팹·루트 AGENTS·ignore·lockfile은 수정하지 않았다. 최신 원격 `work`의 사용자 UI/에셋 변경도 조사했으며 덮어쓰거나 브랜치를 전환하지 않았다. 커밋/푸시는 하지 않았다.

설치와 검색 검증 기록은 `/workspace/.cache/catnip-onboarding/`에 보관했다. 약 3.84 GiB의 Editor 배포본을 공식 CLI의 무결성 검사를 유지해 설치했고, 다운로드 아카이브는 설치기가 정리했다. 재사용 설치 절차 전체를 재실행해 통과했으며 보조 컴파일과 관리 검사 312개도 다시 통과했다. `editor-install-receipt.json`과 `installed-workflow-validation.log`에 파일/검증 근거를 남겼다. 활성 라이선스가 준비되기 전에는 실제 Editor 실행/연결이 완료되지 않는다.

실제로 통과한 전체 설치 스크립트와 설치 후 시작 지침을 환경 설정 초안에 저장했다. `UNITY_CLI_HOME=/workspace/.local/unity-cli`, `UNITY_EDITOR_PATH=/workspace/.local/unity-editors/6000.4.4f1/Editor/Unity`의 비밀이 아닌 경로 제안도 함께 저장했다. 초안 저장은 게시나 다른 인스턴스에서의 재현 검증을 의미하지 않는다. 현재 인스턴스의 설치와 검증은 완료되었지만 라이선스/실행 중인 Editor/MCP 연결을 제공하지 않는다.

## 설치 승인 전 준비 기록

확인일: 2026-10-22 (Asia/Seoul). 대상은 `/workspace/Catnip-Guardians2`, `work`의 `d20fe39b`이다.

문서/프로젝트 스킬 초안과 기존 클라우드 보조 검증 절차를 준비했다. 공식 Unity 플러그인 설치, Unity Editor 직접 제어, 실제 Unity 테스트와 스킬 자동 로딩은 완료하지 않았다.

## 작성한 파일

| 파일 | 역할과 적용 상태 |
| --- | --- |
| [environment.md](environment.md) | 정확한 버전·패키지·설치 상태·공식 출처·호환성 한계 |
| [workflow.md](workflow.md) | 작업/Git 절차, 기존 검사, 미실행 Unity batch 계획, 플러그인 설치/복구안 |
| [AGENTS.proposed.md](AGENTS.proposed.md) | 원래 루트 규칙을 보존한 통합 제안; 원본에는 미적용 |
| [AGENTS.proposed.diff](AGENTS.proposed.diff) | 검토용 차이; `git apply --check`만 통과, apply 미실행 |
| [gitignore.proposed](gitignore.proposed) | 기존 ignore에 캡처/일반 테스트 출력 제외를 추가하는 제안; 미적용 |
| [프로젝트 스킬 초안](skill-drafts/unity-project-workflow/SKILL.md) | name/description·참조 검증 완료, 이번 세션에서 직접 읽음. 자동 검색·호출 미확인 |
| [setup-report.md](setup-report.md) | 이 결과와 다음 단계 |

기존 문서 루트가 `Docs`이므로 첨부 지시문의 예시 `docs/codex` 대신 `Docs/Codex`를 사용했다. 기존 동일 파일은 없었다. 공식 문서에서 프로젝트 스킬 검색 경로를 검증하지 못했으므로 지시문이 허용한 `skill-drafts` 대체 경로를 사용했다. 전역 스킬이나 다른 프로젝트는 수정하지 않았다.

## 실제 수행한 검증

| 항목 | 상태 | 근거 |
| --- | --- | --- |
| 프로젝트/Editor 버전 분리 | 확인 | 프로젝트 6000.4.4f1, Editor 실행 파일 미발견 |
| 최신 work 조사 | 성공 | fetch 후 HEAD/FETCH_HEAD 차이 없음, 브랜치 전환 없음 |
| 보조 API 컴파일 | 성공 | 실제 production/test 소스 348개 및 기존 Editor 대체 API 검사 |
| 관리 로직 | 성공 | 312개 관리 검사, 입력/오디오/카메라/Battle Context 수명주기 모형 통과 |
| 재사용 install_script | 성공 | `bash -n` 및 저장할 스크립트 그대로 실행; 종료 0, 같은 검사 통과 |
| meta/GUID | 성공 | 기존 4,714개 GUID 중복 없음; 추적 에셋의 누락/미추적 meta sidecar 없음 |
| 문서/스킬 형식 | 성공 | name/description 형식, 파일 인코딩/줄바꿈, 모든 로컬 Markdown 링크 |
| 원본 보존 | 성공 | 추적 파일 8,322개의 SHA-256이 조사 기준과 동일 |
| Unity 임포트/EditMode/PlayMode/물리/렌더링/빌드 | 환경 제약으로 미실행 | Editor/검증된 라이선스/승인된 복사본 없음 |
| 공식 웹 문서 열기 | 실패 | 환경 프록시 CONNECT 403; 원격 문서 서버의 거절로 확인된 것이 아님 |
| 플러그인 설치·스킬 자동 검색 | 미실행/미확인 | 로컬 설치 목록 빈 값, executor 스킬 목록 빈 값. 원격 카탈로그 조회 실패 |

이번 실행 로그는 `/tmp/catnip-codex-setup-validation.log`와 `/tmp/catnip-codex-setup-install-validation.log`이다. 로그는 현재 실행 환경의 임시 파일이며 다른 인스턴스에서 보존된다고 가정하지 않는다. 보조 검증의 제3자 Unity 2022 참조 DLL과 경고 제외 설정의 한계는 환경 문서에 기록했다. 이번 설정에서 기존 코드/테스트를 고치거나 검사를 비활성화하지 않았다.

## 저장한 환경 설정 초안

환경 설정 도구가 `status: saved`, `requires_publish: true`를 반환했고, 다시 읽어 저장 내용을 확인했다.

- `install_script`: 현재 승인된 SDK/두 참조 패키지 캐시가 모두 있을 때만 기존 보조 검증을 실행한다. 캐시가 없으면 설치 제안을 요구하며 종료한다. Unity/플러그인을 설치하거나 Editor를 실행하지 않는다.
- `start_skill`: 기존 checkout 사용, 원본 보존, 프로젝트 문서/스킬 초안 수동 읽기, 실제 검사 수 확인, Editor/플러그인 한계를 저장했다. 과거 고정 검사 수 90개라는 안내를 현재 검사 결과에 의존하도록 수정했다. 상주 서비스는 없다.
- 네트워크: 기존 명시 허용 `github.com`과 패키지 관리자 preset을 보존하고 `docs.unity.com`, `docs.unity3d.com`, `developers.openai.com`, `learn.chatgpt.com`을 추가하는 초안을 저장했다. 비밀값이나 신규 인증 바인딩은 추가하지 않았다.

저장은 실행/게시/현재 정책 변경과 다르다. 재조회 시 실제 실행 정책의 명시 허용 도메인은 여전히 `github.com` 하나였다. 공식 문서 접근을 진행하려면 환경 설정에서 저장된 네트워크 변경을 검토·반영하고 실제 정책을 다시 확인해야 한다.

## 남은 변경과 승인 경계

첨부한 `Unity_Codex_개발환경_설정_지시문.md`는 “새 설치·업데이트가 필요하면 출처, 정확한 설치 방법, 변경 범위와 필요한 권한을 먼저 제시하고 승인받으세요”라고 명시한다. 기존 AGENTS도 “원문을 덮어쓰지 말고 제안 파일과 검토 가능한 차이를 준비”하도록 요청했다. 이에 따라 다음 항목은 제안/계획 상태로 남겼다.

1. 기존 AGENTS/ignore 적용: 준비한 제안과 diff를 검토하고 적용을 승인해야 한다. 현재 원본은 유지됐다.
2. 공식 Unity 플러그인: 공식 출처/버전은 확인했지만 정확한 Editor 연결 호환성은 미확인이다. workflow의 고정 커밋 marketplace/add 명령, 쓰기 범위와 복구안을 검토한 후 설치 승인이 필요하다. `.codex` 전역 쓰기 제한과 로컬 executor 요건도 해결해야 한다.
3. 실제 Editor 검증: 정확한 6000.4.4f1 설치·라이선스·필요 모듈을 별도로 확인하고, 명시적으로 승인된 복사본에서만 설정 검증을 실행한다. Unity 버전/패키지 업그레이드는 제안하지 않았다.
4. 프로젝트 스킬 자동 등록: 공식 문서 접근이 가능해진 뒤 검색 경로를 확인하고 초안의 배치/실제 검색·호출을 검증해야 한다. 현재는 문서를 직접 읽어 사용할 수 있다.
5. Git 반영: 새 문서만 미추적 상태로 남겼다. 자동 커밋/푸시, ignore/LFS 적용, 원격 변경은 하지 않았다.

기존 보조 검증과 코드 검토는 현재 checkout에서 계속할 수 있다. 다음 개발 작업을 요청할 때 이 프로젝트 문서와 스킬 초안을 명시적으로 읽게 하면 확인된 규칙을 사용할 수 있다. 다음 세션에서의 자동 인식이나 새 환경의 게시 완료는 검증하지 않았다.

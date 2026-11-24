# 프로젝트 작업 절차와 설정 변경안

## 설치 승인 후 적용 사항 (2026-10-22)

사용자의 설치 요청에 따라 공식 Unity 플러그인 0.1.8-beta, CLI 1.0.0-beta.14와 프로젝트 스킬을 설치했고 실제 Codex 검색을 확인했다. `.agents/skills/unity-project-workflow/SKILL.md`가 현재 프로젝트 스킬이며 문서 폴더의 초안은 초기 검토 기록이다. CLI 명령은 PATH에서 `unity`로 실행한다.

Pipeline 0.9.0-exp.1은 공식 CLI로 manifest에 등록했다. `pipeline install --help`를 확인한 현재 바이너리의 인수는 `--version`이므로 실제 실행은 `unity pipeline install --project-path /workspace/Catnip-Guardians2 --version 0.9.0-exp.1 --non-interactive --format json`이었다. Unity 버전 업그레이드나 기존 Test Framework 다운그레이드는 하지 않았다.

아래의 설치 보류/승인 전 상태는 최초 계획의 기록이다. 최신 Editor 설치와 연결 상태는 [설정 결과](setup-report.md)에 기록한다. 이번 설치 승인을 원본 프로젝트의 Editor 실행, 라이선스/인증 변경, 무관한 규칙 적용이나 Git 커밋/푸시 승인으로 확장하지 않는다.

Editor 파일 설치와 구조 검증까지 완료했다. 실제 경로는 `/workspace/.local/unity-editors/6000.4.4f1/Editor/Unity`이며 CLI 설치 목록 등록도 확인했다. `unity editors add`에는 이 실행 파일 경로를 전달하고, 이미 같은 버전이 등록되었다면 다시 등록하지 않는다. 재사용 설치 스크립트 `/workspace/.cache/catnip-onboarding/install-approved-tools.sh`의 전체 재실행도 통과했다. 이 스크립트는 원본 프로젝트를 열거나 라이선스를 활성화하지 않는다.

## 현재 사용할 수 있는 흐름

1. `/workspace/Catnip-Guardians2`의 기존 checkout을 사용하고 적용되는 `AGENTS.md`를 먼저 읽는다. 사용자가 요청하지 않은 worktree를 만들지 않는다.
2. `git status --short`, `git fetch origin work`, `git log --oneline --left-right HEAD...FETCH_HEAD`로 사용자 변경과 최신 원격을 확인한다. 충돌/차이가 있으면 자동 reset/stash/브랜치 전환으로 처리하지 않는다.
3. `ProjectSettings/ProjectVersion.txt`와 해당 작업의 코드·직렬화 참조를 읽는다. 현재 버전은 `6000.4.4f1`이며 다른 Editor로 대체하지 않는다.
4. 요청 범위에서 최소 변경을 하고 기존 UI/프리팹 작업을 보존한다. Runtime과 Editor 코드, 공개 API/직렬화 이름, 기존 meta/GUID를 보존한다. 일부 기존 파일의 CP949 인코딩을 불필요하게 변환하지 않는다.
5. 아래 보조 검증과 변경에 필요한 Unity 검증을 구분하고, 실행 결과/미실행 이유를 보고한다.

관련 상세 설명: [씬 연결](../SceneFlow.md), [머지 게임](../MergeGame.md), [이펙트 검증](../../Tools/ValidateEffects/README.md), [Food1/Weapon1 검사](../Food1Weapon1Tests.md).

## 프로젝트에 맞는 검증

| 검증 | 방법 | 이번 환경에서의 상태 |
| --- | --- | --- |
| 에셋 메타데이터/씬 연결 | `python3 Tools/ValidateEffects/check.py --metadata-only` | 실행 가능, 기존 검사 통과 |
| 보조 API 컴파일/관리 로직 | `python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation` | 실행 완료; 312개 관리 검사 및 수명주기 모형 통과 |
| 실제 EditMode | 정확한 Editor의 Test Runner 또는 승인된 격리 복사본에서 batch 실행 | Editor가 없어 미실행 |
| 실제 PlayMode/그래픽 | 승인된 복사본에서 Core/General 시작과 Battle/Item 독립 실행 | 미실행; 기존 별도 PlayMode 테스트 어셈블리 미발견 |
| 게임 빌드 | 타깃 플랫폼 확인 및 필요한 모듈/라이선스 승인 후 | 타깃 미확인, 미실행 |

보조 검증은 기존 캐시를 사용한다. 캐시가 없으면 SDK/참조 패키지를 자동 설치하는 기존 스크립트 경로가 있으므로 설정 작업에서는 먼저 설치 범위를 제시한다. 새 환경용 설정 초안의 install_script에는 필수 캐시 존재 확인을 추가해 승인 없는 새 의존성 설치를 막았다. 이 스크립트는 저장된다고 자동 실행되지 않는다.

Unity 테스트 코드는 `Assets/Editor/Tests`의 기존 NUnit 구성을 사용한다. `NonParallelizable` 같은 추가 속성이나 새 asmdef/패키지를 설정 작업만을 위해 추가하지 않는다. 성공 판정은 종료 코드뿐 아니라 이번 실행의 새 로그/XML, 실행 테스트 수가 0보다 큰지, 실패·무시 수를 함께 확인한다.

### Unity batch 실행 계획 — 아직 실행 승인·경로 검증 전

UTF 1.6 및 Unity 6000.4 공식 웹 문서는 차단되었다. 아래 인수 형태는 조사한 공식 Unity 플러그인의 `build-run-test.md`에서 확인했지만, 정확한 프로젝트 버전의 실행 호환성은 미검증이다. 프로젝트 원본을 열지 않고 명시적으로 승인된 복사본에만 적용할 계획이다.

예정 복사본: `/workspace/unity-validation/Catnip-Guardians2-6000.4.4f1`. 현재 생성하지 않았다. Editor 경로는 설치 후 확인한 실제 `6000.4.4f1` 실행 파일을 `UNITY_EDITOR_PATH`로 지정해야 한다. 현재 변수/실행 파일이 없어 아래 사전 확인은 통과할 수 없다.

```bash
set -euo pipefail
catnip_editor="${UNITY_EDITOR_PATH:?확인한 Unity 6000.4.4f1 실행 파일 경로가 필요합니다}"
catnip_copy="/workspace/unity-validation/Catnip-Guardians2-6000.4.4f1"
catnip_results="/workspace/.cache/catnip-unity-results"
test -x "$catnip_editor"
test -d "$catnip_copy/Assets"
test -f "$catnip_copy/Packages/manifest.json"
test -f "$catnip_copy/ProjectSettings/ProjectVersion.txt"
rg -q '^m_EditorVersion: 6000\.4\.4f1$' "$catnip_copy/ProjectSettings/ProjectVersion.txt"
mkdir -p "$catnip_results"
"$catnip_editor" -batchmode -projectPath "$catnip_copy" \
  -runTests -testPlatform EditMode \
  -testResults "$catnip_results/editmode.xml" \
  -logFile "$catnip_results/editmode.log"
```

`-quit`은 테스트 완료 전에 종료할 수 있어 위 계획에 넣지 않았다. Linux 그래픽 세션과 UI/렌더링 검사 요건도 실행 전에 확인해야 한다. Editor 실행은 복사본에도 자동 임포트/스크립트 실행/파일 변화를 유발하며 라이선스가 필요하다. 이 계획은 실행한 결과가 아니다.

수동 확인은 Core → 설정한 General 진입, General의 배틀/아이템 동시 표시와 창 크기 변경, Battle/Item 각각 단독 실행, 설정/로딩/패배 결과 화면, EventSystem/리스너 중복, 로컬 Light2D와 마스크/패딩, 머지 물리/드래그 입력을 작업에 맞게 선택한다.

## 공식 Unity 플러그인 설치안 — 미실행

공식 `.github/README.md`의 Codex 명령과 로컬 CLI `--help`를 대조했다. 승인 후에는 조사한 커밋으로 marketplace를 고정한다.

```bash
codex plugin marketplace add Unity-Technologies/unity-agent-plugin \
  --ref d95425b6c0055e1473566476d90e116645999878
codex plugin add unity@unity-agent-plugin
codex plugin list --json
```

대상은 Codex의 marketplace 설정과 Unity 플러그인 캐시다. 프로젝트 Packages/Assets 변경은 이 설치에 포함하지 않는다. 사용자별 Codex 설정은 수정 전에 변경 전 상태/이미 설치된 항목을 다시 확인하고, 해당 경로 쓰기 권한은 별도로 확보한다. 현재 관리형 Codex 경로는 쓰기 제약이 있으므로 권한 우회나 CODEX_HOME 변경으로 설치하지 않는다. 원격 카탈로그 조회도 현재 실패했다.

설치 후 `unity@unity-agent-plugin`의 활성/버전/스킬 검색을 실제로 확인해야 하며 현재 채팅이나 다음 세션에서 자동 인식된다고 미리 선언하지 않는다. 이 플러그인은 `requires_local_executor: true`이며 Editor 제어에는 추가 도구와 Pipeline 패키지가 필요하다. 이 클라우드에서의 연결 호환성은 미확인이다.

이번 작업으로 새로 설치한 항목만 되돌리는 명령 형식은 다음과 같다. 기존 marketplace/플러그인을 제거하거나 사용자 설정을 덮어쓰는 복구는 하지 않는다.

```bash
codex plugin remove unity@unity-agent-plugin
codex plugin marketplace remove unity-agent-plugin
```

Unity CLI/Editor/Hub/빌드 모듈/Pipeline 패키지/MCP/라이선스 설정은 각각 별도 범위다. 원격 스크립트를 바로 파이프 실행하지 않는다. 설치가 승인되면 먼저 스크립트와 다운로드 출처/검증 방법, 정확한 설치 경로·버전·프로젝트 변경을 검토한다.

## Git과 승인 경계

이번 설정에서는 새 문서와 스킬 초안만 작성한다. 기존 게임 코드, 테스트, 씬/프리팹/에셋, manifest/lock, ProjectSettings, 루트 AGENTS, ignore/LFS 설정을 변경하지 않는다. 자동 커밋·푸시·브랜치 전환은 하지 않는다.

- [AGENTS 통합안](AGENTS.proposed.md)은 기존 규칙 전체 뒤에 확인된 환경/관례를 추가한 제안이다. [diff](AGENTS.proposed.diff)로 검토한다. 적용 전 원본 해시/사용자 변경을 다시 확인한다.
- [ignore 제안](gitignore.proposed)은 기존 항목을 보존하고 캡처/일반 테스트 결과의 제외만 제안한다. 추적 파일 제거는 포함하지 않는다.
- `.gitattributes`/LFS는 현재 도입하지 않는다. 큰 에셋 협업 필요가 구체화되면 대상 확장자·용량·저장소 정책을 조사하고 변경안을 제시한다. 이력 마이그레이션은 별도 승인 대상이다.
- Unity 설치/버전/패키지/설정/라이선스/외부 연결과 Editor 실행은 첨부 지시문의 명시 승인 경계를 따른다. 에셋 이동·이름변경·삭제는 참조 영향과 복구안을 검토한다.
- 이후 사용자가 구체적인 기능 구현이나 수정 작업을 요청하면 그 요청 범위의 승인을 존중한다. 이 설정 절차를 근거로 이미 승인된 작업에 반복 확인을 요구하지 않는다.

실패하면 로그와 변경 전후 상태로 환경 제약·기존 결함·새 변경의 문제를 구분하고, 사용자 변경을 보존한다. 새 문서/초안은 개별 파일 단위로 검토해 제거할 수 있으며 reset/clean/캐시 삭제/일괄 되돌리기를 사용하지 않는다.

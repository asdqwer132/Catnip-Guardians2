---
name: unity-project-workflow
description: Follow Catnip-Guardians2 conventions when editing its Unity gameplay, merge logic, scene connections, or tests; select the relevant validation and preserve authored UI and serialized references.
---

# Catnip-Guardians2 작업

공식 Codex 문서에서 확인한 저장소 범위 `.agents/skills`에 설치한 프로젝트 작업 스킬이다. 기존 사용자 UI와 직렬화 참조를 보존하면서 요청한 변경과 필요한 검증을 수행한다.

## 시작과 변경

- [루트 규칙](../../../AGENTS.md)을 먼저 읽고, 하위 AGENTS가 있으면 함께 적용한다. 사용자 UI/프리팹/데이터 변경을 보존한다.
- 기존 checkout에서 상태와 최신 `origin/work`를 확인한다. 사용자가 요청하지 않은 worktree/브랜치 전환, 자동 stash/reset을 하지 않는다.
- `ProjectSettings/ProjectVersion.txt`의 정확한 Unity 버전을 확인한다. 기준은 6000.4.4f1이며 보조 Unity 2022 DLL을 프로젝트 버전으로 오해하지 않는다.
- 이펙트/버프/소환은 `Assets/Script/Items/Effect`, 머지는 `Assets/Script/Items/Merge`, 씬/카메라/Context는 `Assets/Script/Manager/Utility/Scene`, 검사는 `Assets/Editor/Tests`와 `Tools/ValidateEffects`를 먼저 조사한다.
- UI는 사용자가 작성한다. 기존 UI 참조/이벤트를 연결하고 코드로 UI나 레이아웃을 생성하지 않는다. 씬 간 호출은 같은 씬의 Inspector 참조와 수명주기 등록 Context를 사용한다.
- FindObjects 계열 전역 검색과 초기화 시 EventSystem.current 직접 대입을 피한다. 기존 EventSystem/AudioListener 소유권, 로컬 조명, 공용 UI Scale Profile과 작성된 Layout Group을 유지한다.
- 공개 API, 직렬화 이름, meta/GUID, 기존 파일 인코딩을 보존한다. Runtime에서 UnityEditor API를 사용하지 않고 기존 Editor 폴더 경계를 따른다.

## 검증과 보고

- [작업 절차](../../../Docs/Codex/workflow.md)와 관련 `Docs`를 참고해 필요한 검사를 고른다. 설치 없는 클라우드 기본 검사는 `python3 Tools/ValidateEffects/check.py --metadata-only`다.
- 캐시된 SDK/참조 패키지가 있으면 `python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation`로 보조 API 컴파일과 관리 검사를 실행한다. 캐시가 없으면 설치 범위를 먼저 확인한다.
- 보조 API/모형 검사와 실제 Unity 임포트·EditMode·PlayMode·물리를 구분한다. 수행한 검사 수와 실패/미실행 이유를 정확히 보고한다. 보조 컴파일만으로 Unity 경고가 없다고 선언하지 않는다.
- 승인된 Unity 검증은 정확한 Editor와 허용된 프로젝트/격리 복사본을 사용한다. 설정 작업 중 원본 Editor 실행은 하지 않는다.
- 실패 시 사용자 변경을 보존하며 원인과 관련 로그를 보고한다. 검사를 끄거나 결과를 성공으로 바꾸지 않는다.

## 도구와 작업 범위

공식 Unity 플러그인이 실제 사용 가능한 스킬 목록에 있다면 관련 기능의 스킬만 찾아 읽는다. 현재 설치 상태와 출처/요건은 [환경 조사](../../../Docs/Codex/environment.md)에 기록되어 있다. 미설치 플러그인이나 Editor 연결을 있다고 가정하지 않는다.

사용자의 현재 요청과 이미 승인된 범위 안에서 작업한다. 2026-10-22 공식 플러그인·CLI·정확한 Editor 버전과 연결 패키지 설치가 승인되었다. 이를 무관한 버전 업그레이드, 라이선스/인증 변경, 원본 프로젝트 실행이나 Git 커밋/푸시 승인으로 확장하지 않는다. 이후 명확한 기능 구현 요청의 승인 범위는 해당 요청에 따라 판단하고 불필요한 반복 승인을 요구하지 않는다.

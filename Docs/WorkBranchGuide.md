# work 브랜치에서 추가 이펙트 가져오기

Unity를 닫고 `Assets`, `Packages`, `ProjectSettings` 폴더가 있는 프로젝트 루트에서 터미널을 연다. GitHub Desktop을 사용하면 Repository → Open in Terminal로 열 수 있다.

## 현재 파일 확인

```bash
git status
git remote -v
```

원격 주소는 `https://github.com/asdqwer132/Catnip-Guardians2.git`이다. 로컬 변경이 있으면 본인의 변경을 먼저 커밋하거나 다음 명령으로 임시 보관한 뒤 진행한다.

```bash
git stash push -u -m "before pulling work effects"
```

## 이미 로컬 work 브랜치가 있는 경우

```bash
git fetch origin
git switch work
git pull --ff-only origin work
```

## 로컬 work 브랜치가 없는 경우

```bash
git fetch origin work:refs/remotes/origin/work
git switch --track -c work origin/work
```

이후 `git log -1 --oneline`으로 최신 커밋을 확인한다. 이 가이드가 들어 있는 커밋의 메시지는 `Add missing configurable item effects`이다.

임시 보관했던 작업을 work에서 이어갈 경우 `git stash pop`으로 복원한다. 다른 브랜치의 작업이었다면 그 브랜치로 돌아가 복원한다. 충돌 메시지가 나오거나 `--ff-only`가 실패하면 현재 상태를 보존한 채 분기 차이와 충돌 파일을 확인한다.

## Unity에서 확인

1. Unity Hub에서 기존 프로젝트를 Unity `6000.4.4f1`로 연다.
2. 임포트와 컴파일이 끝나면 Console의 오류를 확인한다.
3. `Assets/Data/AdditionalEffects`의 CSV 설정과 `Assets/Data/AdditionalEffectsExamples`의 예제를 확인한다. 16개 ItemData는 새 기능이 연결되어 있고 나머지는 Effect Datas 또는 On Hit Effects에 연결한다.
4. [추가 이펙트 사용 설명서](AdditionalEffectsUsage.md)에 따라 대상·레이어·범위·연출을 설정한다. [구현 범위와 미정 기획](AdditionalEffects.md)도 확인한다.
5. Test Runner의 EditMode에서 `CombatEffectTests`, `Additional*Tests`, `BuffTargetInspectorTests`를 실행하고 실제 전투 씬을 확인한다.

`.meta`는 에셋 참조에 필요하므로 스크립트/에셋과 함께 가져온다.

## 이후 내 수정 올리기

```bash
git switch work
git status
git add Assets/Script Assets/Editor Assets/Data/AdditionalEffects Assets/Data/AdditionalEffectsExamples Assets/Data/Scriptable/Item Docs Tools/ValidateEffects
git diff --cached --stat
git commit -m "Configure additional item effects"
git push origin work
```

예제 외 다른 기존 에셋을 수정했다면 해당 경로도 `git add`에 포함한다. 작업을 올린 뒤 다른 PC에서는 같은 `git pull --ff-only origin work`로 가져온다.

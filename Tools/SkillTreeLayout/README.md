# 스킬트리 배치기

Python 3.9 이상에서 실행한다. SVG를 브라우저에서 열면 번호, 효과 요약, 화살표를 확인할 수 있다. 노드 위에 마우스를 올리면 상세 효과가 표시된다. CSV에는 중앙 기준 좌표를 출력한다. X는 오른쪽, Y는 위로 증가한다.

```bash
python Tools/SkillTreeLayout/layout.py Tools/SkillTreeLayout/example.json --mix constellation:3+spiral:1 --group weapon=diamond+fan --group potion=cluster:2+radial --group growth=spiral+fan --seed 67 --jitter 0.32 --glow --palette antique -o ./preview/skill-tree
```

- `--mix`에는 `radial`, `constellation`, `fan`, `cluster`, `spiral`, `diamond`를 원하는 비율로 더한다. `--group`을 반복해서 가지마다 다른 조합을 지정할 수 있다.
- `--seed`와 `--jitter`로 위치 변화를 조절한다. 같은 입력과 시드는 같은 결과를 만든다. `--spacing`, `--clearance`, `--spread`, `--twist`, `--rotate`, `--sweep`로 간격, 퍼짐, 회전과 각도 범위를 조절한다.
- `--line curve|straight|angular`와 `--curve`로 연결선을 바꾼다. `--shape circle|diamond|hex`는 노드 외곽을, `--palette jewel|antique|night`는 색을 바꾼다. `--rings`, `--glow`, `--hide-labels`는 각각 켤 수 있다.
- JSON의 `groups`에서 가지별 `mix`, `angle`, `spacing` 배율, `line`, `curve`, `shape`를 지정할 수 있다. `--group`의 조합은 JSON의 해당 조합보다 우선한다.
- 노드는 `id`, `parents`, `group`, `label`, `detail`을 사용한다. 개별 `shape`를 덮어쓰거나 `position: [x, y]`로 고정할 수 있다. JSON 위치의 Y는 SVG와 동일하게 아래로 증가한다. `root`는 중앙에 고정한다.

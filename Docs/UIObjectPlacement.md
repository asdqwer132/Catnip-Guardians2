# UI 기준 월드 오브젝트 배치 도구

Unity 메뉴 **Catnip → Tools → UI Object Placement**에서 연다. 직접 만든 `RectTransform`을 기준으로 **씬에 있는 오브젝트의 위치**를 확정하는 에디터 도구다. 프리팹은 먼저 Hierarchy에 직접 넣은 후 대상으로 선택한다.

## 기본 사용

1. `GameItemScene`을 열고 도구의 **기준 UI**에 Canvas 아래의 `BoxArea`를 넣는다.
2. **좌표 변환 방식 = ScreenToWorld**, **월드 카메라 = 게임 아이템 씬의 Main Camera**로 지정한다. UI 카메라를 비우면 기준 UI의 Canvas에 연결된 카메라를 사용한다. Overlay Canvas는 UI 카메라가 필요 없다.
3. Hierarchy에서 `CragtingBox` 아래의 `Box1` 등 배치할 오브젝트를 선택하고 **선택 오브젝트 추가**를 누른다. 목록에 직접 드래그해 넣어도 된다. 기준 UI를 포함한 부모와 자식을 함께 이동시키지 않도록 대상에는 이동할 루트 하나만 넣는다.
4. 처음에는 **패턴 = Point**, **UI 기준점 = Center**, **대상 피벗 계산 = RendererBounds**, **대상 정렬점 = Center**, **배치할 월드 Z = 0**을 사용한다. 활성 Renderer들의 경계 중심이 UI 중심과 맞는다.
5. Scene 뷰에서 청록색 기준 영역, 노란색 목적지와 이동선을 확인한 후 **배치 적용**을 누른다. **Ctrl/Cmd+Z** 한 번으로 해당 적용을 되돌릴 수 있다. 씬 저장은 평소처럼 직접 한다.

기준점이 이미 맞는 Transform 원점을 쓰려면 대상 피벗을 `TransformOrigin`으로 고르면 된다. 예를 들어 투입구를 `TopCenter`에 놓고 UI 오프셋의 Y를 조금 줄이면 UI 윗부분 안쪽으로 배치할 수 있다.

## 배치 옵션

| 옵션 | 동작 |
| --- | --- |
| Point | 모든 대상의 정렬점을 하나의 UI 기준점에 맞춘다. 벽이나 상자 루트를 하나씩 정렬할 때 사용한다. |
| Horizontal / Vertical | 목록 순서대로 가로 / 위에서 아래로 나열한다. |
| Grid | 열 개수를 정해 왼쪽 위부터 가로로 채우고 다음 행으로 내려간다. 마지막 행은 빈 칸이 있어도 된다. |
| Ellipse | 가로·세로 반지름이 다른 원/타원에 배치한다. 시작 각도와 시계 방향을 설정할 수 있다. 0도는 오른쪽, 90도는 위다. |
| FitReference | 여백을 뺀 UI 범위에 정렬점들을 균등 분배한다. 타원은 해당 범위의 가로·세로 반지름을 사용한다. |
| Fixed | 직접 지정한 UI 로컬 간격 또는 타원 반지름을 사용한다. 가로·세로·격자의 전체 배치 그룹 피벗도 고를 수 있다. |
| 배치 순서 반대로 | 목록의 대상을 반대 순서로 슬롯에 배치한다. |
| 여백 | 왼쪽/아래, 오른쪽/위 여백을 UI 로컬 단위로 지정한다. 정렬점이 경계에 놓이므로 오브젝트 전체를 안에 두려면 대상 크기를 고려해 여백을 준다. |

**UI 기준점**과 **대상 정렬점**은 9방향 또는 Custom을 지원한다. Custom의 `(0, 0)`은 왼쪽 아래, `(0.5, 0.5)`는 중심, `(1, 1)`은 오른쪽 위다. 범위 밖의 값도 입력할 수 있다.

**기준 UI의 실제 피벗 사용**을 켜면 RectTransform에 설정한 실제 피벗을 기준점으로 쓴다. 여백으로 이 피벗 자체가 움직이지는 않는다. FitReference의 여러 열/행은 전체 UI 범위에 분배되며 기준점은 한 칸만 있는 축에 적용된다. Fixed의 **배치 그룹 피벗**은 대상 하나의 피벗과 별개로 전체 나열 묶음을 어느 부분에서 맞출지 정한다.

## 피벗·오프셋·좌표

| 옵션 | 동작 |
| --- | --- |
| TransformOrigin | 대상 Transform의 원점을 맞춘다. |
| RendererBounds | 활성화된 자식 Renderer 전체를 감싸는 월드 XY 경계의 정렬점을 맞춘다. 꺼진 Drag 표시 같은 Renderer는 제외한다. |
| Collider2DBounds | 활성화된 자식 Collider2D 전체를 감싸는 월드 XY 경계의 정렬점을 맞춘다. 스프라이트 여백 대신 실제 충돌 경계를 기준으로 쓸 수 있다. |
| RectTransformBounds | 기존 UI 오브젝트를 대상으로 넣었을 때 그 RectTransform의 월드 XY 경계를 사용한다. |
| UI 오프셋 | UI 로컬 좌표에 먼저 더한다. UI의 회전·스케일과 CanvasScaler를 따른다. |
| 월드 오프셋 | 변환된 위치에 월드 X/Y 거리로 더한다. |
| 기존 Z 유지 / 월드 Z | 각 대상의 기존 깊이를 유지하거나 지정한 XY 평면으로 투영한다. |
| 가로 위치 적용 / 세로 위치 적용 | 한 축만 정렬하고 다른 축은 기존 좌표를 유지할 수 있다. |
| 월드 격자 스냅 | 대상 정렬점을 월드 가로·세로 스냅 간격에 반올림한다. UI 오프셋·월드 오프셋 적용 후 스냅하고, 적용을 끈 축은 원래 좌표를 유지한다. |

예를 들어 바닥 벽을 UI 아랫변 **바깥쪽**에 놓으려면 `Point / UI 기준점 = BottomCenter / 대상 피벗 = Collider2DBounds / 대상 정렬점 = TopCenter`로 정렬한다. 오른쪽 벽은 `MiddleRight / MiddleLeft`, 왼쪽 벽은 `MiddleLeft / MiddleRight`로 같은 방식으로 정렬할 수 있다.

`ScreenToWorld`는 기준 UI의 화면 좌표를 지정한 월드 카메라의 XY 평면으로 투영한다. UI/월드 카메라가 다르거나 화면을 분할해도 각 카메라의 현재 viewport와 투영을 사용한다. `RectWorldCoordinates`는 카메라를 거치지 않고 RectTransform의 월드 X/Y 좌표를 직접 쓴다. World Space Canvas를 직접 기준으로 삼을 때 사용할 수 있다.

배치 적용은 위치만 바꾼다. 크기·회전·부모·콜라이더 설정은 그대로다. 미리보기는 오브젝트를 움직이지 않으며 프리팹 인스턴스 적용은 위치 override로 기록된다. UI나 카메라·Game 뷰 해상도·화면 분할 비율을 변경하면 새 상태에서 도구를 다시 적용한다. 런타임에 UI를 따라 계속 이동하는 기능은 없다. LayoutGroup·Animator 등 다른 컴포넌트가 대상 위치를 제어한다면 적용 후 그 컴포넌트가 위치를 다시 설정할 수 있다.

## 검사

`python3 Tools/ValidateEffects/check.py --cache /workspace/.cache/catnip-validation`은 배치 계산의 9방향/사용자 피벗, 여백, 고정/균등 간격, 불완전한 격자 행, 역순, 타원, 스냅과 잘못된 설정 거부를 실제 관리 코드로 실행한다. Unity 에디터 도구와 **UIObjectPlacementTests**도 보조 API로 컴파일한다.

Unity Test Runner의 EditMode에서 `UIObjectPlacementTests`를 실행하면 카메라별 viewport 투영, 콜라이더 피벗, 미리보기의 비변경 동작, 축 잠금, Undo, 잘못된 계층과 오래된 계획 거부를 엔진에서 검사할 수 있다. 클라우드에는 Unity Editor가 없어 이 엔진 검사와 도구 창의 GUI 실행은 수행하지 않는다.

# 트리 전투 데이터·게이지 수정

## 설정 위치

`Assets/Data/Scriptable/MainPlant`의 `PlantData`에서 설정한다.

| 필드 | 역할 |
|---|---|
| `Max HP` | 현재 라운드의 최대 적 수. 이 수에 도달하면 패배한다. 소수는 올림하며 최소 1이다. |
| `Nutrients Required` | 성장 완료에 필요한 총 양분. 기존 `Grow Time` 값을 이전했다. |
| `Nutrients Per Second` | 초당 자동으로 더해지는 양분. 기존 에셋의 초기값은 1이다. |
| `Visual Sprite` | 성장 단계와 관계없이 사용하는 하나의 이미지. 기존 `Grown Up` 이미지를 이전했다. |
| `Reward` | 기존 성장 완료 보상. 변경하지 않았다. |

적 처치 시 `growEx`로 얻는 양분도 기존처럼 더해진다. 시간 성장은 게임 시간 기준이며 일시정지하면 멈춘다. 양분이 목표에 도달하면 기존 승리·보상·다음 트리 진행을 사용한다.

기존 에셋의 `Max HP` 값은 유지했다. 예를 들어 1,000,000으로 설정된 트리는 적 수 한도도 1,000,000이므로 HP 게이지가 매우 천천히 찬다.

## 수정한 코드

| 파일 | 변경 |
|---|---|
| `Assets/Script/Entity/Plant/PlantData.cs` | 필요 양분·초당 양분·단일 이미지로 정리. `Buy Costs`, 단계별 이미지 제거. |
| `Assets/Script/Entity/Plant/GrowManager.cs` | 트리 데이터에서 성장 목표·속도를 받고 매 프레임 양분 누적. 시작·정지·완료 및 기존 처치 보너스 유지. |
| `Assets/Script/Entity/Plant/PlantUI.cs` | 초기화 시 단일 이미지를 적용. 성장 단계별 이미지 교체 제거. |
| `Assets/Script/Entity/Enemy/Spawner/EnemyManager.cs` | 라운드 초기화 시 트리 `Max HP`를 적 수 한도로 적용하고 게이지에 통지. |

`MainPlant`의 기존 데이터 15개도 새 필드로 이전했다. 이미지·성장 목표·보상·적 스폰 설정과 에셋 GUID는 보존했다.

## 게이지 연결

- **배틀 HP**: `HealthBarUI` → 같은 씬의 `EnemyManager` → 현재 적 수 / 트리에서 받은 최대 적 수.
- **배틀 성장**: `GrowManager.growthFill` → 현재 양분 / 필요 양분.
- **제너럴 HP·성장**: 배틀 `GeneralUIDataSender.uIData` → 제너럴 `GeneralBattleGaugeUI` → 각 HP·성장 게이지.
- 씬 참조는 연결되어 있었다. `ImageFillUI`가 제어하는 `FillMask` 이미지가 `Sliced` 모드였고, 배틀 테스트 마스크 이미지는 비활성화되어 있었다. 두 씬의 마스크 4개를 **Filled / Horizontal**로 맞추고 배틀 마스크를 활성화했다. 실제 막대 그림인 자식 `Fill`의 **Sliced** 설정은 유지한다. 위치·크기·스프라이트·캔버스 비율은 변경하지 않았다.
- 씬 파일: `Assets/Scenes/GameBattleScene.unity`, `Assets/Scenes/GameGeneralScene.unity`.

## 데이터 헤더

- `DefaultData` 공통 필드는 **Basic Data**로 표시한다: `icon`, `data`(언어·이름·설명·상세 설명), `dataNumbering`, `dataId`, `dataType`, `requireUnlock`.
- 각 타입에만 있는 필드는 **Extra Data**로 표시한다: 식물, 적, 아이템, 상자, 가방, 스킬 노드·맵, 캐릭터·맵·스테이지 코덱스.
- 필드가 없는 `CodexEffectDescription`은 상속받은 Basic Data만 사용한다.

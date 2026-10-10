#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 이 파일은 Unity Test Runner의 EditMode에서 실행하는 테스트입니다.
// 테스트는 보통 준비(Arrange) → 실행(Act) → 확인(Assert)의 세 단계로 읽으면 됩니다.
// 실제 Food1/Weapon1 에셋과 DefaultEnemy 프리팹을 사용합니다.
// 충돌 알림만 직접 보내므로 이동, 물리 엔진의 충돌 감지, 애니메이션 시간은 검사하지 않습니다.
[NonParallelizable] // 활성 씬과 BuffManager.instance를 사용하므로 동시에 실행하지 않습니다.
public sealed class Food1Weapon1BuffTests
{
    private const string FoodPath = "Assets/Data/Scriptable/Item/Items/02.Food/Food1.asset";
    private const string WeaponPath = "Assets/Data/Scriptable/Item/Items/01.Weapon/Weapon1.asset";
    private const string EnemyPrefabPath = "Assets/Enemy/Default/DefaultEnemy.prefab";
    private const string EnemyDataPath = "Assets/Enemy/Default/DefaultEnemy SetData.asset";

    // '기대값'은 기획상 맞아야 하는 숫자를 직접 적습니다.
    // 실제 버프 계산 함수로 기대값까지 만들면 같은 계산 오류를 놓칠 수 있습니다.
    // 현재 설정: 기본 공격력 5, Food1 +10% → 5 × 1.1 = 5.5, 적 HP 10 → 4.5.
    // 기획 의도로 에셋 값을 바꿀 때는 아래 기대값과 테스트 설명도 함께 바꾸세요.
    private const float BaseDamage = 5f;
    private const float BuffedDamage = 5.5f;
    private const float EnemyStartHp = 10f;
    private const float HpAfterBuffedHit = 4.5f;
    private const float Tolerance = 0.0001f; // float의 작은 소수점 오차를 허용합니다.

    private Scene previousScene;
    private Scene testScene;
    private BuffManager previousManager;
    private AudioManager previousAudioManager;
    private Random.State previousRandomState;
    private BuffManager manager;
    private ItemData food;
    private ItemData weapon;
    private GameObject enemyPrefab;
    private EnemyDataSet enemyData;

    [SetUp]
    public void SetUp()
    {
        // [SetUp]은 아래의 [Test]마다 먼저 실행됩니다.
        // 매번 빈 씬과 새 매니저를 사용해 이전 테스트의 버프/적 HP가 섞이지 않게 합니다.
        previousScene = SceneManager.GetActiveScene();
        previousManager = BuffManager.instance;
        previousAudioManager = AudioManager.instance;
        previousRandomState = Random.state;
        Assert.That(Application.isPlaying, Is.False, "Test Runner의 EditMode 탭에서 실행하세요.");
        AudioManager.instance = null; // 피해 처리에서 작업 씬의 오디오 매니저를 호출하지 않습니다.

        // Additive는 열려 있던 작업 씬을 보존하면서 테스트용 씬을 추가한다는 뜻입니다.
        testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);

        manager = new GameObject("Food1 Weapon1 Test BuffManager").AddComponent<BuffManager>();
        // EditMode에서는 일반 MonoBehaviour의 Awake가 자동 호출되지 않을 수 있습니다.
        // Storage가 비어 있을 때만 Awake를 보내 실제 매니저 초기화 코드를 실행합니다.
        if (manager.Storage == null)
            manager.SendMessage("Awake", SendMessageOptions.RequireReceiver);
        BuffManager.instance = manager;
        Assert.That(manager.Storage, Is.Not.Null, "BuffManager 초기화에 실패했습니다.");

        // CreateInstance로 가짜 설정을 만드는 대신, 프로젝트에 저장된 실제 에셋을 읽습니다.
        // 그래서 인스펙터에서 Food1의 대상 연결을 잘못 바꾸어도 이 테스트가 알아챕니다.
        food = LoadAsset<ItemData>(FoodPath);
        weapon = LoadAsset<ItemData>(WeaponPath);
        enemyPrefab = LoadAsset<GameObject>(EnemyPrefabPath);
        enemyData = LoadAsset<EnemyDataSet>(EnemyDataPath);
    }

    [TearDown]
    public void TearDown()
    {
        // [TearDown]은 테스트가 실패해도 실행됩니다. 원본 에셋은 삭제하지 않습니다.
        // 임시 씬에 만든 복제본만 지우고 작업 씬/전역 매니저를 원래대로 돌려놓습니다.
        try
        {
            if (manager != null && manager.Storage != null)
                manager.ClearAllBuffs();
        }
        finally
        {
            // 버프 정리 코드가 예외를 내더라도 씬/전역 상태 복구는 반드시 실행합니다.
            try
            {
                if (testScene.IsValid() && testScene.isLoaded)
                    EditorSceneManager.CloseScene(testScene, true); // 임시 씬의 복제본들도 함께 삭제됩니다.
            }
            finally
            {
                BuffManager.instance = previousManager;
                AudioManager.instance = previousAudioManager;
                Random.state = previousRandomState; // 적 초기화에서 사용한 난수 상태도 되돌립니다.
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
            }
        }
    }

    [Test]
    public void Weapon1_WithoutFood1_Deals5DamageToDefaultEnemy()
    {
        // 1. 준비: 버프 없는 Weapon1 장판과 HP가 가득 찬 기본 적을 만듭니다.
        DamageArea area = UseWeaponAndGetDamageArea();
        Enemy enemy = CreateDefaultEnemy();
        float hpBefore = enemy.health.Hp;
        Assert.That(manager.Storage.activeBuffs, Is.Empty, "시작할 때 버프가 없어야 합니다.");
        Assert.That(ReadDamage(area), Is.EqualTo(BaseDamage).Within(Tolerance));

        // 2. 실행: 적의 콜라이더가 장판에 들어왔다고 알립니다.
        SendHit(area, enemy);

        // 3. 확인: HP 10 - 기본 피해 5 = HP 5.
        Assert.That(hpBefore, Is.EqualTo(EnemyStartHp).Within(Tolerance));
        Assert.That(enemy.health.Hp, Is.EqualTo(5f).Within(Tolerance),
            "버프 없는 Weapon1은 기본 적의 HP를 10에서 5로 줄여야 합니다.");
        Debug.Log($"[기본 공격] Damage={ReadDamage(area)}, 적 HP={hpBefore} → {enemy.health.Hp}");
    }

    [Test]
    public void Food1_BeforeWeapon1_RegistersBuffAndNewDamageAreaHas5Point5Damage()
    {
        // 1. 준비는 SetUp에서 끝났습니다. 아직 공격 장판은 없는 상태입니다.

        // 2. 실행: Food1을 사용하고, 그 다음 Weapon1을 사용합니다.
        UseItem(food);
        DamageArea area = UseWeaponAndGetDamageArea();

        // 3-1. 확인: Food1 사용이 실제로 버프를 등록했는지 먼저 확인합니다.
        // 개수를 먼저 검사해야 다음 줄의 [0] 접근도 안전합니다.
        Assert.That(manager.Storage.activeBuffs, Has.Count.EqualTo(1),
            "Food1 사용 후 활성 버프 한 개가 있어야 합니다.");
        ActiveBuff buff = manager.Storage.activeBuffs[0];
        Assert.That(buff.sourceItemData, Is.SameAs(food));
        Assert.That(buff.target.kind, Is.EqualTo(BuffTargetKind.Group));
        Assert.That(buff.target.targetGroup, Is.EqualTo("DamageArea"));
        Assert.That(buff.useLimitType, Is.EqualTo(BuffUseLimitType.Time));
        Assert.That(buff.remainTime, Is.EqualTo(8f).Within(Tolerance));

        // 3-2. 확인: 계산 미리보기 대신 실제 생성된 장판의 최종 Damage를 읽습니다.
        // 그룹 버프는 Food1을 쓸 때 장판이 없어도, 나중에 생긴 장판에 적용되어야 합니다.
        float actualDamage = ReadDamage(area);
        Assert.That(actualDamage, Is.EqualTo(BuffedDamage).Within(Tolerance),
            "Weapon1 공격력 5에 Food1의 10% 증가가 적용되어 5.5가 되어야 합니다.");

        // 원본 ScriptableObject의 기본 공격력은 유지되어야 합니다.
        // 그렇지 않으면 테스트/다음 공격마다 기본값에 버프가 누적될 수 있습니다.
        DamageAreaAttackEffect effect = GetWeaponAttackEffect();
        Assert.That(effect.attackStat.damageAreaPower, Is.EqualTo(BaseDamage).Within(Tolerance));
        Debug.Log($"[Food1 버프] 대상={buff.target.targetGroup}, 남은 시간={buff.remainTime}, Damage={actualDamage}");
    }

    [Test]
    public void Food1_AfterWeapon1_UpdatesExistingDamageAreaFrom5To5Point5()
    {
        // 1. 준비: 이번에는 순서를 바꾸어 장판을 먼저 만듭니다.
        DamageArea area = UseWeaponAndGetDamageArea();
        Assert.That(ReadDamage(area), Is.EqualTo(BaseDamage).Within(Tolerance));

        // 2. 실행: 장판이 이미 존재하는 상태에서 Food1을 사용합니다.
        UseItem(food);

        // 3. 확인: RefreshBuffedStat를 테스트에서 직접 호출하지 않습니다.
        // 실제 버프 등록 알림만으로 기존 장판의 Damage가 바뀌어야 합니다.
        Assert.That(ReadDamage(area), Is.EqualTo(BuffedDamage).Within(Tolerance),
            "이미 생성된 DamageArea에도 Food1 버프가 즉시 적용되어야 합니다.");
        Debug.Log($"[기존 장판 갱신] Damage={BaseDamage} → {ReadDamage(area)}");
    }

    [Test]
    public void Food1_ThenWeapon1_HitDefaultEnemy_ReducesHpFrom10To4Point5OnlyOnce()
    {
        // 1. 준비: Food1 → Weapon1 순서로 사용하고, 실제 기본 적을 초기화합니다.
        UseItem(food);
        DamageArea area = UseWeaponAndGetDamageArea();
        Enemy enemy = CreateDefaultEnemy();
        float hpBefore = enemy.health.Hp;
        float actualDamage = ReadDamage(area);
        Assert.That(actualDamage, Is.EqualTo(BuffedDamage).Within(Tolerance));
        Assert.That(hpBefore, Is.EqualTo(EnemyStartHp).Within(Tolerance));

        // OnDamageResolved는 Health가 피해 계산을 끝낸 뒤 보내는 이벤트입니다.
        // 전달받은 공격력과 실제 HP 감소량을 따로 확인할 수 있습니다.
        HealthDamageResult result = default(HealthDamageResult);
        int damageEventCount = 0;
        enemy.health.OnDamageResolved += value =>
        {
            result = value;
            damageEventCount++;
        };

        // 2. 실행: DamageArea → HitEffectDispatcher → Enemy → Health 경로로 명중시킵니다.
        // enemy.health.TakeDamage(5.5f)를 직접 부르면 장판이 잘못된 피해를 줘도 놓칩니다.
        SendHit(area, enemy);

        // 3. 확인: 들어온 피해 5.5, 실제 감소량 5.5, 최종 HP 4.5를 각각 검사합니다.
        Assert.That(damageEventCount, Is.EqualTo(1), "피해 처리 이벤트가 한 번 발생해야 합니다.");
        Assert.That(result.requestedDamage, Is.EqualTo(BuffedDamage).Within(Tolerance));
        Assert.That(result.hpDamage, Is.EqualTo(BuffedDamage).Within(Tolerance));
        Assert.That(hpBefore - enemy.health.Hp, Is.EqualTo(BuffedDamage).Within(Tolerance));
        Assert.That(enemy.health.Hp, Is.EqualTo(HpAfterBuffedHit).Within(Tolerance));
        Assert.That(enemy.IsDead, Is.False);
        Assert.That(result.sourceContext, Is.Not.Null);
        Assert.That(result.sourceContext.sourceItemData, Is.SameAs(weapon),
            "Food1은 버프의 출처이고, 실제 공격의 출처는 Weapon1이어야 합니다.");

        // Weapon1은 HitOnce입니다. 같은 적의 충돌 알림이 반복되어도 추가 피해가 없어야 합니다.
        SendHit(area, enemy);
        area.SendMessage("OnTriggerStay2D", GetEnemyCollider(enemy), SendMessageOptions.RequireReceiver);
        Assert.That(damageEventCount, Is.EqualTo(1));
        Assert.That(enemy.health.Hp, Is.EqualTo(HpAfterBuffedHit).Within(Tolerance),
            "HitOnce 장판은 같은 적에게 중복 피해를 주면 안 됩니다.");
        Debug.Log($"[버프 후 명중] Damage={actualDamage}, 실제 피해={result.hpDamage}, 적 HP={hpBefore} → {enemy.health.Hp}");
    }

    // 여기부터는 위의 테스트를 읽기 쉽게 만들기 위한 공통 준비/실행 도우미입니다.

    private void UseItem(ItemData item)
    {
        // 실제 아이템 효과 실행기를 호출합니다. 인벤토리/가방의 아이템은 소비하지 않습니다.
        // 특수 아이템의 추가 공격은 이 시나리오와 무관하므로 비활성화합니다.
        ItemEffectExecutor.ExecuteItem(item,
            usePosition: Vector3.zero, targetPosition: Vector3.zero, direction: Vector3.right,
            owner: null, sourceBag: null, manager: manager, triggerSpecialItems: false);
    }

    private DamageArea UseWeaponAndGetDamageArea()
    {
        GetWeaponAttackEffect(); // 에셋에 공격 이펙트와 프리팹이 연결되어 있는지도 확인합니다.
        UseItem(weapon);

        // 실행기가 만든 장판을 찾습니다. 직접 DamageArea를 만들거나 Damage 값을 설정하지 않습니다.
        var areas = new List<DamageArea>();
        foreach (GameObject root in testScene.GetRootGameObjects())
            areas.AddRange(root.GetComponentsInChildren<DamageArea>(true));
        Assert.That(areas, Has.Count.EqualTo(1), "Weapon1 사용으로 장판 한 개가 생성되어야 합니다.");
        Assert.That(manager.GetRegisteredBuffTargetsUnsafe(), Does.Contain(areas[0]),
            "생성된 장판은 BuffManager의 버프 대상으로 등록되어야 합니다.");
        return areas[0];
    }

    private DamageAreaAttackEffect GetWeaponAttackEffect()
    {
        Assert.That(weapon.effectDatas, Has.Length.EqualTo(1), "현재 Weapon1은 공격 이펙트 한 개를 사용합니다.");
        Assert.That(weapon.effectDatas[0], Is.InstanceOf<DamageAreaAttackEffect>());
        var effect = (DamageAreaAttackEffect)weapon.effectDatas[0];
        Assert.That(effect.attackStat, Is.Not.Null);
        Assert.That(effect.attackPrefab, Is.Not.Null, "Weapon1의 Attack Prefab 연결을 확인하세요.");
        Assert.That(effect.damageApplyMode, Is.EqualTo(DamageApplyMode.HitOnce));
        return effect;
    }

    private Enemy CreateDefaultEnemy()
    {
        GameObject clone = Object.Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
        Enemy enemy = clone.GetComponent<Enemy>();
        Assert.That(enemy, Is.Not.Null, "DefaultEnemy 프리팹에 Enemy 컴포넌트가 필요합니다.");
        Assert.That(enemy.health, Is.Not.Null);

        // 적 루트의 실제 Awake들로 컴포넌트 참조/초기 스케일을 준비합니다.
        // 원본 프리팹은 건드리지 않고 복제된 적만 초기화합니다.
        clone.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
        enemy.useDamagePopup = false; // HP 계산 테스트에는 팝업/팝업용 오브젝트 풀이 필요 없습니다.
        enemy.Init(target: null, injectedBuffManager: manager, dataSet: enemyData);
        Assert.That(enemy.CanReceiveHitEffects, Is.True, "Init을 마친 활성 적은 공격을 받을 수 있어야 합니다.");
        return enemy;
    }

    private static Collider2D GetEnemyCollider(Enemy enemy)
    {
        Collider2D collider = enemy.GetComponentInChildren<Collider2D>();
        Assert.That(collider, Is.Not.Null, "기본 적의 피격용 Collider2D가 필요합니다.");
        Assert.That(collider.enabled && collider.gameObject.activeInHierarchy, Is.True);
        return collider;
    }

    private static void SendHit(DamageArea area, Enemy enemy)
    {
        // 게임 중에는 Unity 물리 엔진이 OnTriggerEnter2D를 호출합니다.
        // EditMode에서는 같은 콜라이더를 직접 전달해 명중 이후의 실제 게임 코드를 검사합니다.
        area.SendMessage("OnTriggerEnter2D", GetEnemyCollider(enemy), SendMessageOptions.RequireReceiver);
    }

    private static float ReadDamage(DamageArea area)
    {
        // DamageArea.damage는 private이지만 [SerializeField]이므로 SerializedObject로 읽을 수 있습니다.
        // 인스펙터의 Runtime Stat → Damage와 같은 값입니다. 읽기만 하고 수정하지 않습니다.
        var serialized = new SerializedObject(area);
        serialized.Update();
        SerializedProperty damageProperty = serialized.FindProperty("damage");
        Assert.That(damageProperty, Is.Not.Null, "DamageArea의 직렬화된 damage 필드를 찾을 수 없습니다.");
        return damageProperty.floatValue;
    }

    private static T LoadAsset<T>(string path) where T : Object
    {
        // T는 읽을 에셋의 종류입니다. 예: LoadAsset<ItemData>는 ItemData로 읽습니다.
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.That(asset, Is.Not.Null, $"에셋을 찾지 못했습니다. 경로/타입을 확인하세요: {path}");
        return asset;
    }
}
#endif

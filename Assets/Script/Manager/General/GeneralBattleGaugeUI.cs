using UnityEngine;

// 같은 씬의 UI만 Inspector에 연결합니다. 배틀 데이터는 송신자의 수명주기를 따라 연결합니다.
public sealed class GeneralBattleGaugeUI : MonoBehaviour
{
    public HealthBarUI hp;
    public ImageFillUI grow;
    private GrowManager growthSource;

    private void OnEnable()
    {
        GeneralUIDataSender.InstanceChanged += Bind;
        Bind();
    }

    private void OnDisable()
    {
        GeneralUIDataSender.InstanceChanged -= Bind;
        UnbindGrowth();
        if (hp != null) hp.SetEnemyManager(null);
    }

    private void Bind()
    {
        UnbindGrowth();
        var data = GeneralUIDataSender.instance != null ? GeneralUIDataSender.instance.GetData() : null;
        if (hp != null) hp.SetEnemyManager(data?.enemyManager);
        growthSource = data?.growManager;
        if (growthSource != null) growthSource.GrowthChanged += RefreshGrowth;
        RefreshGrowth(growthSource != null ? growthSource.CurrentGrowValue : 0f,
            growthSource != null ? growthSource.MaxGrowValue : 0f);
    }

    private void UnbindGrowth()
    {
        if (growthSource != null) growthSource.GrowthChanged -= RefreshGrowth;
        growthSource = null;
    }

    private void RefreshGrowth(float current, float maximum)
    {
        if (grow != null) grow.SetFill(current, maximum);
    }
}

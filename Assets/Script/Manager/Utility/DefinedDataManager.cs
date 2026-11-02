using System;
using UnityEngine;

[DefaultExecutionOrder(-600)]
public class DefinedDataManager : MonoBehaviour
{
    public ItemSeriesData seriesData;
    public ItemGradeData gradeData;

    private static DefinedDataManager current;
    public static event Action DefinitionsChanged;
    public static DefinedDataManager instance
    {
        get => current;
        set
        {
            if (ReferenceEquals(current, value)) return;
            current = value;
            DefinitionsChanged?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { current = null; DefinitionsChanged = null; }

    private void OnEnable() => instance = this;
    private void OnDisable() { if (instance == this) instance = null; }

    public DefinedData GetSeries(ItemSeries type) => seriesData != null ? seriesData.GetData(type) : null;
    public DefinedData GetGrade(ItemGrade type) => gradeData != null ? gradeData.GetData(type) : null;
    public void RefreshDefinitions() => DefinitionsChanged?.Invoke();
}

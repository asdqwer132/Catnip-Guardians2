using UnityEngine;

public class DefinedDataManager : MonoBehaviour
{

    public ItemSeriesData seriesData;
    public ItemGradeData gradeData;

    public static DefinedDataManager instance;

    private void Awake()
    {
        instance = this;
    }
}

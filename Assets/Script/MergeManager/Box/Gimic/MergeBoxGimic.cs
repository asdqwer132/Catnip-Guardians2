using UnityEngine;

public abstract class MergeBoxGimic : MonoBehaviour
{
    GimicData data;
    public void Init(GimicData data)
    {
        this.data = data;
    }
    public virtual void GimicStart() { }

    public virtual void UseBoxSkill(int index) { }
}

public class GimicData { }
public class WeaponGimicData  : GimicData { }
public class FoodGimicData : GimicData { } 
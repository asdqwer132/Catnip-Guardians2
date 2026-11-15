using UnityEngine;

public interface IMergeBoxGimic
{
    public void Init();
    public void GimicStart();

    public void UseBoxSkill(int index);
}

public class GimicData { }
public class WeaponGimicData  : GimicData { }
public class FoodGimicData : GimicData { } 
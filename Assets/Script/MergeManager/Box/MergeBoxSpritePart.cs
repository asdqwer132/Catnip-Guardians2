using System;
using UnityEngine;

public enum MergeBoxSpriteRole { SideWall, BottomWall, BackFrame, Fixed, LeftWall, RightWall, LeftDeco, RightDeco }

[Serializable]
public sealed class MergeBoxSpritePart
{
    public MergeBoxSpriteRole role;
    public GameObject target;
    public SpriteRenderer renderer => target != null ? target.GetComponent<SpriteRenderer>() : null;
    public BoxCollider2D wallCollider => target != null ? target.GetComponent<BoxCollider2D>() : null;
}

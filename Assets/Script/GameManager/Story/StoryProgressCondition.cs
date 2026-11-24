using UnityEngine;

public abstract class StoryProgressCondition : ScriptableObject
{
    public abstract bool Matches(StoryProgressEvent fact);
}

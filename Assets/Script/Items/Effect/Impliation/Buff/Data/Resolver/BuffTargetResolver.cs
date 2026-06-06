using System.Collections.Generic;
using UnityEngine;

public abstract class BuffTargetResolver : ScriptableObject
{
    public abstract void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results);
}

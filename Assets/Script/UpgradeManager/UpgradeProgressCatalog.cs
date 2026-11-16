using System;
using System.Collections.Generic;
using UnityEngine;

// Stable asset references for save files; skill unlock identity remains ProgressKey.
public sealed class UpgradeProgressCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry { public string key; public UnityEngine.Object asset; }
    public List<Entry> entries = new List<Entry>();
}

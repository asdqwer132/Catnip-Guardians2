using UnityEngine;

// Captures a prefab clone during tests without searching the scene.
[ExecuteAlways]
public sealed class ThrowMoverTestProbe : MonoBehaviour
{
    public static ItemThrowMover LastCreated;
    private void Awake() { LastCreated = GetComponent<ItemThrowMover>(); }
}

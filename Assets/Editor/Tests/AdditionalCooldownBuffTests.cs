using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AdditionalCooldownBuffTests
{
    [UnitySetUp] public IEnumerator EnterRuntime()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private ItemUseController Controller()
    { var host = new GameObject("Item cooldowns"); owned.Add(host); return host.AddComponent<ItemUseController>(); }
    [UnityTearDown] public IEnumerator Cleanup()
    { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear();
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    [TestCase(CooldownOperation.ReduceSeconds, 3f, 7f)]
    [TestCase(CooldownOperation.ReduceFraction, .3f, 7f)]
    [TestCase(CooldownOperation.Ready, 0f, 0f)]
    [TestCase(CooldownOperation.ReduceSeconds, 20f, 0f)]
    public void CooldownEffectsOperateOnFilteredItemsWithoutBagSelection(CooldownOperation operation, float amount, float expected)
    {
        var a = Asset<ItemData>(); var b = Asset<ItemData>(); var controller = Controller();
        controller.StartCooldown(a, 10); controller.StartCooldown(b, 10);
        var effect = Asset<CooldownControlEffect>(); effect.operation = operation; effect.amount = amount; effect.items = new[] { a };
        effect.Execute(new ItemEffectContext(null, null, Vector3.zero, Vector3.zero));
        Assert.That(controller.GetRemaining(a), Is.EqualTo(expected).Within(.001f));
        Assert.That(controller.GetRemaining(b), Is.EqualTo(10));
    }
    [Test] public void CooldownTicksAndReadyDoesNotRestartACompletedTimer()
    {
        var item = Asset<ItemData>(); var controller = Controller();
        controller.StartCooldown(item, 5); controller.Tick(2);
        Assert.That(controller.GetRemaining(item), Is.EqualTo(3));
        var effect = Asset<CooldownControlEffect>(); effect.operation = CooldownOperation.Ready;
        effect.Execute(new ItemEffectContext(null, null, Vector3.zero, Vector3.zero)); controller.Tick(1);
        Assert.That(controller.GetRemaining(item), Is.Zero);
    }
}

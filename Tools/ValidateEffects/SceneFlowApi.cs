// Compile-only substitutes for unchanged scene services and the Input System package.
// SceneManager/Camera/Canvas APIs use Unity DLLs; these declarations never simulate a transition.
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }
    public sealed class InputAction : System.IDisposable
    {
        public struct CallbackContext { }
        public event System.Action<CallbackContext> performed;
        public event System.Action<CallbackContext> canceled;
        public InputAction(string name, InputActionType type, string binding = null) { }
        public T ReadValue<T>() where T : struct => default;
        public void Enable() { } public void Disable() { } public void Dispose() { }
    }
    public enum Key { None, R, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9 }
    public class ButtonControl { public bool wasPressedThisFrame, wasReleasedThisFrame; }
    public class PositionControl { public Vector2 ReadValue() => Vector2.zero; }
    public class Mouse { public static Mouse current; public ButtonControl leftButton, rightButton; public PositionControl position; }
    public class Keyboard
    {
        public static Keyboard current;
        public ButtonControl this[Key key] => null;
        public ButtonControl bKey, cKey, enterKey, escapeKey, iKey, kKey, qKey;
    }
}
public class RegisteredBuffSkillItem { public ItemData itemData; public string bagId; }
public class BuffSkillManager : MonoBehaviour
{
    public IReadOnlyList<RegisteredBuffSkillItem> RegisteredBuffItems;
    public void ClearRegisteredBuffItems() { }
    public void RegisterBuffItem(ItemData data, string bag) { }
}
public class ItemInitManager : MonoBehaviour { public void ApplyDefaultInventoryItems() { } }
public class UnlockManager : MonoBehaviour { public void Init() { } }
public class ShopManager : MonoBehaviour { public void InitShop() { } }
public class SkillTreeUI : MonoBehaviour { public void Init() { } }
public class InventoryUI : MonoBehaviour { public void Init() { } }
public class EquipmentBagManager : MonoBehaviour { public List<EquipmentBag> bags; public void Init() { } }
public class TutorialProgressManager : MonoBehaviour
{
    public static TutorialProgressManager instance;
    public void ResetProgress() { }
    public void IncreaseProgress() { }
    public void DecreaseProgress() { }
    public void SetProgress(TutorialProgress progress) { }
}
public class TutorialDialogueController : MonoBehaviour
{
    public void Show() { } public void Hide() { } public void SetSpeakerName(string value) { }
    public void SetText(string value) { } public void Clear() { }
}
public class TutorialHighlightController : MonoBehaviour { public void ShowHighlight(int index) { } public void HideHighlight() { } }

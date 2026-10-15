// Compile-only references for unchanged scene services and the Input System package.
// These are not used to claim that Unity rendering, physics, input, or scene tests executed.
using UnityEngine;
public class ItemUseManager : MonoBehaviour { public void Init(){} }
public class SubPlantManager : MonoBehaviour { public System.Collections.Generic.List<InventoryItem> currentMaterials=new System.Collections.Generic.List<InventoryItem>(); public ItemData resultItem; public System.Action onMaterialChanged; public void Combine(){} }
public class BagUIInitializer : MonoBehaviour { public void InitAll(){} }
public class SelectedBagPreviewUI : MonoBehaviour { public void Init(){} }
public class ItemTester : MonoBehaviour { public void Init(){} }
public class ItemInitManager : MonoBehaviour { public void ApplyDefaultInventoryItems(){} }
public class EquipmentBagManager : MonoBehaviour { public EquipmentBag[] bags; public void Init(){} }
public class ShopManager : MonoBehaviour { public void InitShop(){} }
public class SkillTreeUI : MonoBehaviour { public void Init(){} }
public class InventoryUI : MonoBehaviour { public void Init(){} }
public class SceneMoveManager : MonoBehaviour { public void GoGameScene(){} }
namespace UnityEngine.InputSystem
{
    public enum Key { R, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9 }
    public class CompileButton { public bool wasPressedThisFrame, wasReleasedThisFrame; }
    public class CompilePosition { public Vector2 ReadValue() => Vector2.zero; }
    public class Mouse { public static Mouse current; public CompilePosition position; public CompileButton leftButton, rightButton; }
    public class Keyboard
    {
        public static Keyboard current;
        public CompileButton this[Key key] => null;
        public CompileButton qKey, iKey, kKey, cKey, bKey, escapeKey, enterKey;
    }
}

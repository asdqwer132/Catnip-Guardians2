using UnityEngine.UI;

public sealed class CodexPortraitSlotUI : DataInfoUI
{
    public Toggle toggle;
    public Button button;
    private CodexDetailsUI owner;
    private int index = -1;
    public CharacterCodexPortrait Portrait { get; private set; }

    private void Awake()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (button == null) button = GetComponent<Button>();
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggleChanged);
        else if (button != null) button.onClick.AddListener(Select);
    }

    public void Bind(CharacterCodexPortrait portrait, int portraitIndex, CodexDetailsUI details)
    {
        owner = details; index = portraitIndex; Portrait = portrait;
        SetDefinedData(portrait, portrait?.stateId ?? "");
        if (toggle != null) toggle.interactable = portrait != null;
        if (button != null) button.interactable = portrait != null;
    }

    public void ApplySelection(bool selected)
    {
        if (toggle == null) return;
        var group = toggle.group;
        bool allowSwitchOff = group != null && group.allowSwitchOff;
        try
        {
            if (!selected && group != null) group.allowSwitchOff = true;
            toggle.SetIsOnWithoutNotify(selected);
        }
        finally { if (group != null) group.allowSwitchOff = allowSwitchOff; }
    }

    private void OnToggleChanged(bool selected)
    {
        if (selected) Select();
        else if (owner != null && owner.SelectedPortrait == index &&
                 (toggle.group == null || !toggle.group.AnyTogglesOn())) ApplySelection(true);
    }
    private void Select() { if (owner != null && Portrait != null) owner.SelectPortrait(index); }
    private void OnDestroy()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
        else if (button != null) button.onClick.RemoveListener(Select);
    }
}

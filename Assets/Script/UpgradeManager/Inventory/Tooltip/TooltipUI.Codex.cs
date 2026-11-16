using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All objects below are authored UI references or instances of authored slot prefabs.
public partial class TooltipUI
{
    private List<CodexPortraitSlotUI> portraitSlots => CodexInformation?.Slots ?? EmptyPortraitSlots;
    private static readonly List<CodexPortraitSlotUI> EmptyPortraitSlots = new List<CodexPortraitSlotUI>();
    public DefaultData SelectedData { get; private set; }
    public int SelectedPortrait { get; private set; } = -1;

    private void BindPortraitButtons()
    {
        if (previousPortrait != null) previousPortrait.onClick.AddListener(PreviousPortrait);
        if (nextPortrait != null) nextPortrait.onClick.AddListener(NextPortrait);
    }

    private void UnbindPortraitButtons()
    {
        if (previousPortrait != null) previousPortrait.onClick.RemoveListener(PreviousPortrait);
        if (nextPortrait != null) nextPortrait.onClick.RemoveListener(NextPortrait);
    }

    private bool bindingCodexData;

    private void BindCodexData(DefaultData entry)
    {
        if (bindingCodexData) return;
        bindingCodexData = true;
        try
        {
            SelectedData = entry;
            SelectedPortrait = -1;
            var character = entry as CharacterCodexData;
            int count = character?.portraits?.Count ?? 0;
            if (portraitContent != null && portraitSlotPrefab != null)
            {
                while (portraitSlots.Count < count)
                {
                    var slot = Instantiate(portraitSlotPrefab, portraitContent);
                    if (slot.toggle != null) slot.toggle.group = portraitSelectionGroup;
                    portraitSlots.Add(slot);
                }
                for (int i = 0; i < portraitSlots.Count; i++)
                {
                    bool active = i < count;
                    portraitSlots[i].gameObject.SetActive(active);
                    if (active) portraitSlots[i].Bind(character.GetPortrait(i), i, this);
                    portraitSlots[i].ApplySelection(false);
                }
            }
            if (count > 0) SelectPortrait(0);
            else
            {
                SetImage(portraitImage, null);
                if (portraitInformation != null) portraitInformation.SetDefinedData(null);
            }
            if (childInventory != null)
            {
                var map = entry as MapCodexData;
                childInventory.SetEntries(map != null ? map.stages : null);
                childInventory.Select(null);
            }
            ApplyPreview();
            RefreshPortraitInformation(DisplayLanguage);
        }
        finally { bindingCodexData = false; }
    }

    public void SelectPortrait(int index)
    {
        var character = SelectedData as CharacterCodexData;
        var portrait = character != null ? character.GetPortrait(index) : null;
        SelectedPortrait = portrait != null ? index : -1;
        SetImage(portraitImage, portrait?.sprite);
        if (portraitInformation != null) portraitInformation.SetDefinedData(portrait, portrait?.stateId ?? "");
        for (int i = 0; i < portraitSlots.Count; i++) portraitSlots[i].ApplySelection(i == SelectedPortrait);
        RefreshPortraitInformation(DisplayLanguage);
    }

    public void PreviousPortrait() { if (SelectedPortrait > 0) SelectPortrait(SelectedPortrait - 1); }
    public void NextPortrait()
    {
        var character = SelectedData as CharacterCodexData;
        if (character?.portraits != null && SelectedPortrait + 1 < character.portraits.Count) SelectPortrait(SelectedPortrait + 1);
    }

    private void RefreshPortraitInformation(language selected)
    {
        var character = SelectedData as CharacterCodexData;
        int count = character?.portraits?.Count ?? 0;
        var portrait = character != null ? character.GetPortrait(SelectedPortrait) : null;
        if (portraitDescription != null)
        {
            string text = DefinedDataUtility.GetDetailDescription(portrait, selected);
            if (string.IsNullOrEmpty(text)) text = DefinedDataUtility.GetDescription(portrait, selected);
            portraitDescription.text = text ?? "";
            portraitDescription.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
        if (portraitPage != null)
        {
            portraitPage.text = (SelectedPortrait >= 0 ? SelectedPortrait + 1 : 0) + " / " + count;
            portraitPage.gameObject.SetActive(count > 0);
        }
        if (previousPortrait != null)
        {
            previousPortrait.gameObject.SetActive(count > 0);
            previousPortrait.interactable = SelectedPortrait > 0;
        }
        if (nextPortrait != null)
        {
            nextPortrait.gameObject.SetActive(count > 0);
            nextPortrait.interactable = SelectedPortrait >= 0 && SelectedPortrait + 1 < count;
        }
    }

    private void RefreshCodexLanguage(language selected)
    {
        if (SelectedPortrait >= 0 && SelectedData is CharacterCodexData character)
            SetImage(portraitImage, character.GetPortrait(SelectedPortrait)?.sprite);
        ApplyPreview();
        RefreshPortraitInformation(selected);
    }

    private void ApplyPreview()
    {
        Sprite preview = null;
        if (SelectedData is MapCodexData map) preview = map.preview != null ? map.preview : map.icon;
        else if (SelectedData is StageCodexData stage)
            preview = stage.preview != null ? stage.preview : stage.map != null && stage.map.preview != null ? stage.map.preview : stage.icon;
        SetImage(previewImage, preview);
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}

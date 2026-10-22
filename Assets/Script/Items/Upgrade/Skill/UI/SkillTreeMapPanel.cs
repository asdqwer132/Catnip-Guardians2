using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Connects scene-authored map selectors, graphs, and the fixed information panel.
public sealed class SkillTreeMapPanel : MonoBehaviour
{
    [Serializable]
    public sealed class MapBinding
    {
        public Toggle toggle;
        public RectTransform graph;
        public SkillMapTooltipProvider information;
        public ScrollRect scrollRect;
        public UIZoomController zoom;
    }

    public MapBinding[] maps = Array.Empty<MapBinding>();
    public ItemTooltipUI mapInformation;
    public ItemTooltipUI nodeInformation;
    public Image progressFill;

    private UnityAction<bool>[] listeners;
    private SkillTreeManager subscribedManager;
    private int selectedIndex;

    private void OnEnable()
    {
        listeners = new UnityAction<bool>[maps.Length];
        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            listeners[i] = selected => { if (selected) SelectMap(index); };
            if (maps[i].toggle != null)
                maps[i].toggle.onValueChanged.AddListener(listeners[i]);
        }
        Subscribe();
        SelectMap(selectedIndex);
    }

    private void Start()
    {
        Subscribe();
        RefreshInformation();
    }

    private void Subscribe()
    {
        if (subscribedManager == SkillTreeManager.Instance)
            return;
        if (subscribedManager != null)
            subscribedManager.OnSkillTreeChanged -= RefreshInformation;
        subscribedManager = SkillTreeManager.Instance;
        if (subscribedManager != null)
            subscribedManager.OnSkillTreeChanged += RefreshInformation;
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
            subscribedManager.OnSkillTreeChanged -= RefreshInformation;
        subscribedManager = null;
        for (int i = 0; listeners != null && i < maps.Length; i++)
            if (maps[i].toggle != null)
                maps[i].toggle.onValueChanged.RemoveListener(listeners[i]);
        if (nodeInformation != null)
            nodeInformation.Hide();
    }

    public void SelectMap(int index)
    {
        if (index < 0 || index >= maps.Length || maps[index].graph == null)
            return;
        selectedIndex = index;
        if (nodeInformation != null)
            nodeInformation.Hide();
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].graph != null)
                maps[i].graph.gameObject.SetActive(i == index);
            if (maps[i].toggle != null)
                maps[i].toggle.SetIsOnWithoutNotify(i == index);
        }
        ScrollRect scrollRect = maps[index].scrollRect;
        if (scrollRect != null && scrollRect.content != null)
        {
            scrollRect.StopMovement();
            scrollRect.content.anchoredPosition = Vector2.zero;
        }
        if (maps[index].zoom != null)
            maps[index].zoom.ResetZoom();
        RefreshInformation();
    }

    private void RefreshInformation()
    {
        if (selectedIndex >= maps.Length)
            return;
        SkillMapTooltipProvider provider = maps[selectedIndex].information;
        if (provider == null)
            return;
        if (mapInformation != null)
            mapInformation.Show(provider);
        SkillMapData map = provider.MapData;
        if (progressFill != null && map != null)
        {
            int unlocked = subscribedManager != null ? subscribedManager.GetUnlockedSkillCount(map) : 0;
            progressFill.fillAmount = map.totalNodeCount > 0
                ? Mathf.Clamp01((float)unlocked / map.totalNodeCount) : 0f;
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Splits one UI bar into segments using the selected manager's gauge ratios.</summary>
public sealed class CraftGaugeBarUI : MonoBehaviour
{
    public enum GaugeFillOrientation
    {
        Horizontal,
        Vertical
    }

    [Serializable]
    public sealed class GaugeBarBinding
    {
        public CraftGaugeType gaugeType;
        [FormerlySerializedAs("fillImage")] public Image segmentImage;
        public TMP_Text percentText;
        public TMP_Text nameText;
    }

    [SerializeField] private GeneralCraftManager generalCraftManager;
    [Header("One shared bar")]
    [SerializeField] private RectTransform barArea;
    [SerializeField] private GaugeFillOrientation orientation = GaugeFillOrientation.Horizontal;
    [SerializeField] private bool reverseOrder;
    [SerializeField, FormerlySerializedAs("bars")]
    private GaugeBarBinding[] segments = new GaugeBarBinding[0];

    private GeneralCraftManager subscribedGeneral;
    private CraftGaugeManager subscribedGauge;

    private void OnEnable()
    {
        RefreshUI();
    }

    // Handles a GeneralCraftManager whose Awake ran after our OnEnable.
    private void Start()
    {
        RefreshUI();
    }

    private void OnDisable()
    {
        if (subscribedGeneral != null)
        {
            subscribedGeneral.CurrentManagerChanged -= OnManagerChanged;
            subscribedGeneral = null;
        }

        BindGauge(null);
    }

    private void BindGeneral()
    {
        GeneralCraftManager general = generalCraftManager != null
            ? generalCraftManager
            : GeneralCraftManager.instance;

        if (subscribedGeneral != general)
        {
            if (subscribedGeneral != null)
                subscribedGeneral.CurrentManagerChanged -= OnManagerChanged;

            subscribedGeneral = general;
            if (subscribedGeneral != null)
                subscribedGeneral.CurrentManagerChanged += OnManagerChanged;
        }

        ItemCraftManager selected = general != null ? general.GetCurrentManager() : null;
        BindGauge(selected != null ? selected.GaugeManager : null);
    }

    private void BindGauge(CraftGaugeManager gauge)
    {
        if (subscribedGauge == gauge)
            return;

        if (subscribedGauge != null)
            subscribedGauge.GaugeChanged -= OnGaugeChanged;

        subscribedGauge = gauge;
        if (subscribedGauge != null)
            subscribedGauge.GaugeChanged += OnGaugeChanged;
    }

    private void OnManagerChanged(ItemCraftManager selected)
    {
        BindGauge(selected != null ? selected.GaugeManager : null);
        RefreshUI();
    }

    private void OnGaugeChanged(CraftGaugeSnapshot snapshot)
    {
        Apply(snapshot);
    }

    public void RefreshUI()
    {
        BindGeneral();

        ItemCraftManager selected = subscribedGeneral != null
            ? subscribedGeneral.GetCurrentManager()
            : null;
        CraftGaugeSnapshot snapshot = subscribedGauge != null && selected != null
            ? subscribedGauge.CalculateSnapshot(selected.CurrentMaterials)
            : CraftGaugeSnapshot.Empty;

        Apply(snapshot);
    }

    private void Apply(CraftGaugeSnapshot snapshot)
    {
        if (segments == null)
            return;

        CraftGaugeSnapshot values = snapshot ?? CraftGaugeSnapshot.Empty;
        RectTransform area = barArea != null ? barArea : transform as RectTransform;
        float cursor = 0f;

        // Array order is the left-to-right or bottom-to-top segment order.
        foreach (GaugeBarBinding segment in segments)
        {
            if (segment == null)
                continue;

            float percent = segment.gaugeType != null ? values.GetPercentage(segment.gaugeType) : 0f;
            float next = Mathf.Min(1f, cursor + Mathf.Clamp01(percent / 100f));

            if (segment.segmentImage != null)
            {
                RectTransform rect = segment.segmentImage.rectTransform;
                if (area != null)
                {
                    float start = reverseOrder ? 1f - next : cursor;
                    float end = reverseOrder ? 1f - cursor : next;

                    rect.anchorMin = orientation == GaugeFillOrientation.Horizontal
                        ? new Vector2(start, 0f)
                        : new Vector2(0f, start);
                    rect.anchorMax = orientation == GaugeFillOrientation.Horizontal
                        ? new Vector2(end, 1f)
                        : new Vector2(1f, end);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }

                // The segment's rectangle defines its size; the Image must be fully visible.
                segment.segmentImage.fillAmount = 1f;
                segment.segmentImage.enabled = area != null && next > cursor;
            }

            if (segment.percentText != null)
                segment.percentText.text = percent > 0f ? percent.ToString("0.#") + "%" : string.Empty;

            if (segment.nameText != null)
            {
                segment.nameText.text = segment.gaugeType == null
                    ? string.Empty
                    : (string.IsNullOrEmpty(segment.gaugeType.displayName)
                        ? segment.gaugeType.name
                        : segment.gaugeType.displayName);
            }

            cursor = next;
        }
    }
}

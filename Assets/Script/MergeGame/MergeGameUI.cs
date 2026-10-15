using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬 연결은 직렬화하고, 반복되는 통/사전 목록만 데이터로 만듭니다.
public static class MergeGameUI
{
    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    public static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    { var rect = Rect(name, parent, min, max); var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
    public static TextMeshProUGUI Label(Transform parent, string value, TMP_FontAsset font, int size = 20)
    {
        var rect = Rect("Label", parent, Vector2.zero, Vector2.one);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value; text.fontSize = size; text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.margin = new Vector4(10, 5, 10, 5);
        text.raycastTarget = false; return text;
    }
    public static Button Button(Transform parent, string title, TMP_FontAsset font, Action clicked)
    {
        var rect = Rect(title, parent, Vector2.zero, Vector2.one);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.16f, 0.23f, 0.3f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var text = Label(rect, title, font, 18); text.alignment = TextAlignmentOptions.Center;
        button.onClick.AddListener(() => clicked()); return button;
    }
    public static RectTransform Scroll(Transform parent)
    {
        var root = Rect("Scroll", parent, Vector2.zero, Vector2.one);
        root.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.97f);
        var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 35f;
        var viewport = Rect("Viewport", root, Vector2.zero, Vector2.one); viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one); content.pivot = new Vector2(0.5f, 1f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f; layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlHeight = true; layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content; return content;
    }
    public static RectTransform Row(Transform content, float height)
    {
        var row = Rect("Row", content, Vector2.zero, Vector2.one);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return row;
    }
    public static string Name(DefaultData data)
    {
        if (data == null) return "";
        var value = data.GetDataName(LanguageManager.instance != null ? LanguageManager.instance.selectedLan : default(language));
        return string.IsNullOrEmpty(value) ? data.name : value;
    }
}

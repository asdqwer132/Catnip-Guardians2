using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum LoadingRunnerMovement { LoadingProgress, ConstantSpeed }
public enum LoadingCharacterSelection { RandomSingle, AcquiredCharacters }

// LoadingScene owns the authored UI; the persistent loader only supplies progress and a roster.
[DisallowMultipleComponent]
public sealed class LoadingScenePresentation : MonoBehaviour
{
    [Header("Loading Text")]
    public TMP_Text loadingText;
    public string loadingLabel = "Loading";
    [Min(0.01f)] public float dotIntervalSeconds = 0.4f;
    [Header("Road / Authored Runner Template")]
    public RectTransform road;
    [Tooltip("Full-screen Canvas bounds, used to wrap only after leaving the screen completely.")]
    public RectTransform screenBounds;
    public Image runnerTemplate;
    public Vector2 characterSize = new Vector2(128f, 128f);
    [Tooltip("Feet height relative to the top edge of the road, in Canvas units.")]
    public float roadHeightOffset = -20f;
    [Header("Movement")]
    public LoadingRunnerMovement movement;
    [Tooltip("Optional UI point for the first character's starting X. If empty, use the road's left edge. Height is controlled by Road Height Offset.")]
    public RectTransform startPoint;
    [Tooltip("Horizontal offset from Start Point or the road's left edge, in road Canvas units. Negative values start farther left, including outside the screen.")]
    public float startOffsetX;
    [Min(1f)] public float speed = 180f;
    [Tooltip("Gap between characters, in Canvas units.")]
    [Min(0f)] public float characterSpacing = 24f;
    [Header("Characters")]
    public LoadingCharacterSelection characterSelection;
    public LoadingCharacterAnimation[] randomCharacters = Array.Empty<LoadingCharacterAnimation>();
    [Tooltip("Can also be supplied by SceneMoveManager.SetAcquiredCharacters before changing scenes.")]
    public LoadingCharacterAnimation[] acquiredCharacters = Array.Empty<LoadingCharacterAnimation>();

    public float Progress { get; private set; }
    private static readonly Dictionary<Scene, LoadingScenePresentation> active = new Dictionary<Scene, LoadingScenePresentation>();
    private readonly List<Image> runners = new List<Image>();
    private readonly List<LoadingCharacterAnimation> roster = new List<LoadingCharacterAnimation>();
    private readonly List<LoadingCharacterAnimation> candidates = new List<LoadingCharacterAnimation>();
    private readonly Vector3[] corners = new Vector3[4];
    private static readonly string[] dots = { ".", "..", "..." };
    private float elapsed;
    private int lastDot = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();
    public static LoadingScenePresentation In(Scene scene)
        => active.TryGetValue(scene, out var value) ? value : null;

    private void OnEnable()
    {
        active[gameObject.scene] = this;
        Begin();
    }
    private void OnDisable()
    {
        if (In(gameObject.scene) == this) active.Remove(gameObject.scene);
    }

    public void Begin(LoadingCharacterAnimation[] ownedCharacters = null)
    {
        if (ownedCharacters != null) acquiredCharacters = (LoadingCharacterAnimation[])ownedCharacters.Clone();
        elapsed = 0f; Progress = 0f; lastDot = -1;
        roster.Clear(); candidates.Clear();
        var source = characterSelection == LoadingCharacterSelection.RandomSingle ? randomCharacters : acquiredCharacters;
        if (source != null)
            foreach (var character in source)
                if (character != null && character.runFrames != null && character.runFrames.Length > 0 && !candidates.Contains(character))
                    candidates.Add(character);
        if (characterSelection == LoadingCharacterSelection.RandomSingle && candidates.Count > 0)
            roster.Add(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        else roster.AddRange(candidates);

        // Only copy the authored visual. No Canvas, layout or UI components are generated here.
        if (runnerTemplate != null && road != null)
        {
            runnerTemplate.gameObject.SetActive(false);
            while (runners.Count < roster.Count) runners.Add(Instantiate(runnerTemplate, road, false));
        }
        for (int i = 0; i < runners.Count; i++) runners[i].gameObject.SetActive(i < roster.Count);
        // The Canvas can still have zero-sized bounds in OnEnable. Resolve its layout
        // before positioning the first visible frame, rather than briefly showing the center.
        Canvas.ForceUpdateCanvases();
        RefreshVisuals();
    }

    public void SetProgress(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        Progress = Mathf.Max(Progress, Mathf.Clamp01(value));
        RefreshVisuals();
    }

    private void Update()
    {
        // Loading also animates while gameplay is paused.
        elapsed += Time.unscaledDeltaTime;
        RefreshVisuals();
    }
    private void RefreshVisuals()
    {
        int dot = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, dotIntervalSeconds)) % dots.Length;
        if (loadingText != null && dot != lastDot)
        { loadingText.text = loadingLabel + dots[dot]; lastDot = dot; }
        if (road == null) return;
        float width = Mathf.Max(1f, characterSize.x), height = Mathf.Max(1f, characterSize.y);
        float spacing = width + Mathf.Max(0f, characterSpacing);
        float left = road.rect.xMin, right = road.rect.xMax;
        float startX = (startPoint != null ? road.InverseTransformPoint(startPoint.position).x : left) + startOffsetX;
        float loopLeft = left - width * 0.5f, loopRight = right + width * 0.5f;
        if (screenBounds != null)
        {
            screenBounds.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                float x = road.InverseTransformPoint(corners[i]).x;
                loopLeft = Mathf.Min(loopLeft, x - width * 0.5f);
                loopRight = Mathf.Max(loopRight, x + width * 0.5f);
            }
        }
        // Keep enough room for every acquired character, even with large spacing.
        float loopLength = Mathf.Max(loopRight - loopLeft, roster.Count * spacing);
        for (int i = 0; i < runners.Count && i < roster.Count; i++)
        {
            var image = runners[i];
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = road.pivot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = new Vector3(roster[i].flipHorizontal ? -1f : 1f, 1f, 1f);
            float initialX = startX - i * spacing;
            float x = Mathf.Lerp(startX, right, Progress) - i * spacing;
            if (movement == LoadingRunnerMovement.ConstantSpeed)
            {
                float distance = elapsed * Mathf.Max(1f, speed);
                float distanceToExit = Mathf.Max(0f, loopRight - initialX);
                // Do not wrap an off-screen starting point (or trailing characters)
                // to the right on the very first frame. Wrap only after passing the exit.
                x = distance <= distanceToExit ? initialX + distance
                    : loopRight - loopLength + Mathf.Repeat(distance - distanceToExit, loopLength);
            }
            rect.anchoredPosition = new Vector2(x, road.rect.yMax + roadHeightOffset + height * 0.5f);
            image.sprite = roster[i].FrameAt(elapsed);
        }
    }
}

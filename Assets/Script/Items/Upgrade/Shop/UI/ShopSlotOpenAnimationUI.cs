using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>Plays the box's opening clip on this slot's icon, once, and holds its last frame.</summary>
[RequireComponent(typeof(Image), typeof(Animator))]
public sealed class ShopSlotOpenAnimationUI : MonoBehaviour
{
    public Image icon;
    public Animator targetAnimator;
    public bool useUnscaledTime = true;
    public bool IsPlaying { get; private set; }
    public AnimationClip PlayedClip { get; private set; }

    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private float elapsed;

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
        targetAnimator.enabled = false;
    }

    public void ResetToBox(ItemBoxData box)
    {
        StopPlayback();
        PlayedClip = null;
        if (icon == null) return;
        icon.sprite = box != null ? box.icon : null;
        icon.enabled = icon.sprite != null;
    }

    public void Play(ItemBoxData box)
    {
        StopPlayback();
        PlayedClip = box != null ? box.openClip : null;
        if (PlayedClip == null || targetAnimator == null || !isActiveAndEnabled) return;
        targetAnimator.enabled = true;
        targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Shop slot opening");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, PlayedClip);
        playable.SetApplyFootIK(false);
        AnimationPlayableOutput.Create(graph, "Icon", targetAnimator).SetSourcePlayable(playable);
        elapsed = 0f;
        graph.Play();
        SampleFrame(0f);
        IsPlaying = PlayedClip.length > 0f;
    }

    private void Update()
    {
        if (!IsPlaying || !graph.IsValid()) return;
        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        SampleFrame(Mathf.Min(elapsed, PlayedClip.length));
        if (elapsed >= PlayedClip.length) IsPlaying = false;
    }

    private void SampleFrame(float time)
    {
        float end = PlayedClip.isLooping ? Mathf.Max(0f, PlayedClip.length - 0.0001f) : PlayedClip.length;
        playable.SetTime(Mathf.Min(time, end));
        graph.Evaluate(0f);
    }

    private void StopPlayback()
    {
        IsPlaying = false;
        if (graph.IsValid()) graph.Destroy();
        if (targetAnimator != null) targetAnimator.enabled = false;
    }

    private void OnDisable() => StopPlayback();
    private void OnDestroy() => StopPlayback();
}

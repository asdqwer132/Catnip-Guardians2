using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Plays data-driven clips on a visual-only Animator.
internal sealed class BattleAnimationPlayer
{
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private AnimationClip current;
    private float elapsed;

    public bool IsPlaying => graph.IsValid();
    public bool IsFinished => IsPlaying && elapsed >= current.length;

    public void Play(Animator animator, AnimationClip clip, bool restart = false)
    {
        if (!restart && IsPlaying && current == clip) return;
        Stop();
        if (animator == null || clip == null) return;

        current = clip;
        graph = PlayableGraph.Create("Battle visual animation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, clip);
        AnimationPlayableOutput.Create(graph, "Battle visual", animator).SetSourcePlayable(playable);
        graph.Play();
        graph.Evaluate(0f);
    }

    public void Tick(float deltaTime, bool loop)
    {
        if (!IsPlaying || deltaTime <= 0f) return;

        elapsed += deltaTime;
        if (loop && current.length > 0f) elapsed %= current.length;
        playable.SetTime(Mathf.Min(elapsed, current.length));
        graph.Evaluate(0f);
    }

    public void Stop()
    {
        if (graph.IsValid()) graph.Destroy();
        current = null;
        elapsed = 0f;
    }
}

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Animator Controller 없이 공용 SpriteRenderer 오브젝트에서 클립을 재생합니다.
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class EffectClipPlayer : MonoBehaviour, IPoolable
{
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private float clipLength;
    private bool looping;
    public void Play(AnimationClip clip, bool loop)
    {
        Stop();
        if (clip == null) return;
        var animator = GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Effect clip");
        playable = AnimationClipPlayable.Create(graph, clip);
        clipLength = Mathf.Max(.01f, clip.length); looping = loop;
        AnimationPlayableOutput.Create(graph, "Sprite animation", animator).SetSourcePlayable(playable);
        graph.Play(); graph.Evaluate(0f);
    }
    private void Update()
    {
        if (looping && graph.IsValid() && playable.IsValid() && playable.GetTime() >= clipLength)
            playable.SetTime(playable.GetTime() % clipLength);
    }
    private void Stop()
    {
        if (graph.IsValid()) graph.Destroy();
        var sprite = GetComponent<SpriteRenderer>();
        sprite.sprite = null; sprite.color = Color.white; sprite.flipX = sprite.flipY = false;
    }
    public void OnSpawnedFromPool() => Stop();
    public void OnReturnedToPool() => Stop();
    private void OnDisable() => Stop();
    private void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
}

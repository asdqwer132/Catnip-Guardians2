using UnityEngine;

public class PlayOnActive : MonoBehaviour
{
    public Animator animator;
    public string stateName = "Open";

    void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);
        animator.Play(stateName, 0, 0f);
        animator.Update(0f);
    }
}
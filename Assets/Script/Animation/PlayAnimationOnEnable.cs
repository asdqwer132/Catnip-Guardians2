using UnityEngine;

public class PlayOnActive : MonoBehaviour
{
    public Animator animator;
    public string stateName = "Open";

    void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Animator 상태 완전 초기화
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);

        // 애니메이션 처음부터 재생
        animator.Play(stateName, 0, 0f);
        animator.Update(0f);
    }
}
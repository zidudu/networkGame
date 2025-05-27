using System.Collections;
using UnityEngine;

public class BreakableBlock : MonoBehaviour
{
    public float disappearTime = 1.5f; // 블록이 사라지기까지 걸리는 시간
    public float reappearTime = 3.0f; // 블록이 다시 생성되기까지 걸리는 시간

    private Renderer blockRenderer;
    private Collider blockCollider;
    private Animator animator;

    void Start()
    {
        blockRenderer = GetComponent<Renderer>();
        blockCollider = GetComponent<Collider>();
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) // 플레이어가 밟으면 타이머 시작
        {
            StartCoroutine(BreakBlock());
        }
    }

    private IEnumerator BreakBlock()
    {
        animator.SetTrigger("Shake"); // 애니메이션 실행 (Idle → ShakeBlock)
        yield return new WaitForSeconds(disappearTime);

        blockRenderer.enabled = false;
        blockCollider.enabled = false;

        animator.Play("Idle"); // 애니메이션을 Idle 상태로 강제 전환

        yield return new WaitForSeconds(reappearTime);

        blockRenderer.enabled = true;
        blockCollider.enabled = true;
    }

}

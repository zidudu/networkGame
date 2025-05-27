using UnityEngine;
using System.Collections;

public class FirePillar : MonoBehaviour
{
    public float activeDuration = 3f;
    public float damageCooldown = 1f;
    private bool canDamage = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canDamage)
        {
            Debug.Log("불에 닿음!");
            StartCoroutine(DamageCooldown());
        }
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageCooldown);
        canDamage = true;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        StartCoroutine(DeactivateAfterTime());
    }

    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(activeDuration);
        Destroy(gameObject); // 불기둥 삭제
    }


    //  기즈모로 불기둥 테두리 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // 기즈모 색상 설정
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider box) // BoxCollider의 경우
        {
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        }
        else if (col is CapsuleCollider capsule) // CapsuleCollider의 경우
        {
            Gizmos.DrawWireSphere(transform.position, capsule.radius);
        }
    }
}

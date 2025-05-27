using UnityEngine;
using System.Collections;

public class LavaRiseController : MonoBehaviour
{
    [Header("상승 설정")]
    [Tooltip("증가할 스케일 Y(최대치 = 현재 + riseScale)")]
    public float riseScale = 4f;
    [Tooltip("상승에 걸리는 시간(초)")]
    public float riseDuration = 6f;

    private Vector3 startScale;
    private Vector3 targetScale;
    private float bottomY;
    private bool isRising = false;

    void Awake()
    {
        // 초기 스케일과 목표 스케일 계산
        startScale = transform.localScale;
        targetScale = startScale + new Vector3(0, riseScale, 0);

        // 현재 월드 좌표에서 오브젝트 바닥 Y 위치 저장
        float halfHeight = startScale.y * 0.5f;
        bottomY = transform.position.y - halfHeight;
    }

    /// <summary>외부에서 한 번 호출</summary>
    public void StartRise()
    {
        if (isRising) return;
        isRising = true;
        StartCoroutine(RiseRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / riseDuration;
            float newYScale = Mathf.Lerp(startScale.y, targetScale.y, t);

            // 로컬 스케일 업데이트
            Vector3 ls = transform.localScale;
            ls.y = newYScale;
            transform.localScale = ls;

            // 월드 위치 조정: 바닥이 고정되도록 Y 중점 계산
            float newCenterY = bottomY + newYScale * 0.5f;
            Vector3 pos = transform.position;
            pos.y = newCenterY;
            transform.position = pos;

            yield return null;
        }
    }
}

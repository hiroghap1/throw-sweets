using System.Collections;
using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    public static MergeSystem Instance { get; private set; }

    [Tooltip("ティア昇順（1〜8）")]
    public SweetData[] tiers;

    private void Awake() => Instance = this;

    public void Merge(SweetController a, SweetController b)
    {
        // 最大ティア同士は合成しない
        if (a.data.tier >= tiers.Length) return;

        a.isMerging = b.isMerging = true;
        SweetData next = tiers[a.data.tier]; // tier は 1 始まりのため [tier] が次ティア
        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;
        Destroy(a.gameObject);
        Destroy(b.gameObject);
        GameManager.Instance.AddScore(next.scoreOnMerge);
        StartCoroutine(SpawnMerged(next, mid));
    }

    private IEnumerator SpawnMerged(SweetData next, Vector3 pos)
    {
        GameObject go = Instantiate(next.prefab, pos, Quaternion.identity);
        go.GetComponent<SweetController>().OnLaunched();

        var rb = go.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        // ポップ演出: 0 → 等倍へスケールイン
        Vector3 fullScale = go.transform.localScale;
        const float duration = 0.15f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            go.transform.localScale = fullScale * Mathf.SmoothStep(0f, 1f, t / duration);
            yield return null;
        }
        go.transform.localScale = fullScale;
        rb.isKinematic = false;
    }
}

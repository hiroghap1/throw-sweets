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

        // 物理を有効にしたまま小→等倍へ成長させ、周囲をなだらかに押しのける
        // （キネマティックで等倍出現させると、めり込み解消で周囲が吹き飛ぶ）
        Vector3 fullScale = go.transform.localScale;
        const float duration = 0.25f;
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            go.transform.localScale = fullScale * Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        if (go != null) go.transform.localScale = fullScale;
    }
}

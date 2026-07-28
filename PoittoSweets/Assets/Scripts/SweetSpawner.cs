using System.Collections;
using UnityEngine;

public class SweetSpawner : MonoBehaviour
{
    [Tooltip("ティア昇順（1〜8）")]
    public SweetData[] tiers;
    public float respawnDelay = 0.9f;

    public SweetController Current { get; private set; }
    public SweetData NextData { get; private set; }

    private void Start()
    {
        NextData = PickWeighted();
        SpawnHeld();
    }

    private void Update()
    {
        // 保持中のスイーツは発射位置に追従（カウンター面に接するよう半径分持ち上げる）
        if (Current != null) Current.transform.position = transform.position + Vector3.up * Current.data.radius;
    }

    private SweetData PickWeighted()
    {
        float total = 0f;
        foreach (var t in tiers) total += t.dropWeight;
        float r = Random.value * total;
        foreach (var t in tiers)
        {
            r -= t.dropWeight;
            if (r <= 0f) return t;
        }
        return tiers[0];
    }

    private void SpawnHeld()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        SweetData data = NextData;
        NextData = PickWeighted();

        GameObject go = Instantiate(data.prefab, transform.position + Vector3.up * data.radius, Quaternion.identity);
        go.GetComponent<Rigidbody>().isKinematic = true;
        Current = go.GetComponent<SweetController>();

        GameManager.Instance.ShowNext(NextData);
    }

    public void Throw(Vector3 velocity)
    {
        if (Current == null) return;

        var rb = Current.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        Current.OnLaunched();
        Current = null;

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnHeld();
    }
}

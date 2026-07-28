using System.Collections.Generic;
using UnityEngine;

public class SweetController : MonoBehaviour
{
    public static readonly List<SweetController> All = new();

    public SweetData data;

    [HideInInspector] public bool isMerging;

    public bool Launched { get; private set; }
    public float TimeSinceLaunch => Launched ? Time.time - launchTime : 0f;

    private float launchTime;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public void OnLaunched()
    {
        Launched = true;
        launchTime = Time.time;
    }

    private void Update()
    {
        // 万一カウンター外へ落ちたときの保険
        if (transform.position.y < -5f) Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out SweetController other)) return;
        if (other.data.tier != data.tier) return;
        if (!Launched || !other.Launched) return;
        if (isMerging || other.isMerging) return;
        // 衝突は両側で発火するため、片側だけが合成を処理する
        if (GetInstanceID() > other.GetInstanceID()) return;
        MergeSystem.Instance.Merge(this, other);
    }
}

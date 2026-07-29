using UnityEngine;

/// <summary>
/// スイーツの表情。顔クアッド（自身）を常にカメラ側の表面に貼り付け、
/// 状態に応じて顔アトラス（2x2）の表示セルを切り替える。
/// </summary>
public class SweetFace : MonoBehaviour
{
    public enum Expression { Normal, Blink, Surprised, Squint }

    // アトラスのセル位置（UV オフセット）。左上=通常 / 右上=瞬き / 左下=びっくり / 右下=ギュッ
    private static readonly Vector2[] CellOffset =
    {
        new(0f, 0.5f),
        new(0.5f, 0.5f),
        new(0f, 0f),
        new(0.5f, 0f),
    };

    [Header("瞬き")]
    public float blinkIntervalMin = 2f;
    public float blinkIntervalMax = 5f;
    public float blinkDuration = 0.12f;

    public float squintDuration = 0.3f;

    private Material mat;
    private Camera cam;
    private Expression current = Expression.Normal;
    private Expression baseExpression = Expression.Normal;
    private float blinkTimer;
    private float squintUntil;

    private void Start()
    {
        // 個体ごとに表情を切り替えるためインスタンス化
        mat = GetComponent<Renderer>().material;
        mat.mainTextureScale = new Vector2(0.5f, 0.5f);
        cam = Camera.main;
        ResetBlinkTimer();
        Apply(Expression.Normal);
    }

    public void SetSurprised(bool on) => baseExpression = on ? Expression.Surprised : Expression.Normal;

    public void Impact() => squintUntil = Time.time + squintDuration;

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // 転がりに追従せず、常にカメラ側の表面に位置させる
        Transform body = transform.parent;
        float r = body.lossyScale.x * 0.5f;
        Vector3 dir = (cam.transform.position - body.position).normalized;
        transform.position = body.position + dir * (r * 1.02f);
        transform.rotation = Quaternion.LookRotation(-dir);

        Expression target;
        if (Time.time < squintUntil)
        {
            target = Expression.Squint;
        }
        else if (baseExpression == Expression.Surprised)
        {
            target = Expression.Surprised;
        }
        else
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= -blinkDuration) ResetBlinkTimer();
            target = blinkTimer <= 0f ? Expression.Blink : Expression.Normal;
        }

        if (target != current) Apply(target);
    }

    private void ResetBlinkTimer() => blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);

    private void Apply(Expression e)
    {
        current = e;
        mat.mainTextureOffset = CellOffset[(int)e];
    }
}

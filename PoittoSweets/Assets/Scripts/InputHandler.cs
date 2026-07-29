using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public SweetSpawner spawner;
    public LineRenderer directionLine;
    public LineRenderer landingRing;

    [Header("移動・角度（画面幅・高さ全体のドラッグを基準にした倍率。解像度非依存）")]
    public float moveRange = 1.25f;
    public float moveGain = 1.2f;
    public float angleMin = 20f;
    public float angleMax = 70f;
    public float angleGain = 1.5f;

    [Header("投げ")]
    public float throwSpeed = 6.5f;

    private float angle = 45f;
    private bool dragging;

    private void Update()
    {
        if (GameManager.Instance == null) return; // Play 中の再コンパイル等で参照が消えた場合の保険
        if (GameManager.Instance.IsGameOver)
        {
            directionLine.enabled = false;
            landingRing.enabled = false;
            return;
        }

        bool hasSweet = spawner.Current != null;
        directionLine.enabled = hasSweet;
        landingRing.enabled = hasSweet;
        if (hasSweet) UpdateLine();

        var pointer = Pointer.current;
        if (pointer == null || !hasSweet) return;

        if (pointer.press.wasPressedThisFrame) dragging = true;

        if (dragging && pointer.press.isPressed)
        {
            Vector2 delta = pointer.delta.ReadValue();
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + delta.x / Screen.width * moveRange * 2f * moveGain, -moveRange, moveRange);
            transform.position = p;
            angle = Mathf.Clamp(angle + delta.y / Screen.height * (angleMax - angleMin) * angleGain, angleMin, angleMax);
        }

        if (dragging && pointer.press.wasReleasedThisFrame)
        {
            dragging = false;
            spawner.Throw(ThrowVelocity());
        }
    }

    private Vector3 ThrowVelocity()
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Sin(rad), Mathf.Cos(rad)) * throwSpeed;
    }

    // カウンター天面の高さ（傾斜 2°、z=-2.0 で y=0.11）
    private static float SurfaceY(float z) => 0.11f - (z + 2.0f) * Mathf.Tan(2f * Mathf.Deg2Rad);

    private void UpdateLine()
    {
        // 放物線は着地点の計算のみに使い、表示は「面に沿ったガイド線 + 着地リング」
        // （正面投げの放物線は真後ろ視点では縦棒に潰れて見えないため）
        SweetController sweet = spawner.Current;
        float r = sweet.data.radius;
        Vector3 p = sweet.transform.position;
        Vector3 vel = ThrowVelocity();

        const float dt = 0.02f;
        for (int i = 0; i < 300; i++)
        {
            vel += Physics.gravity * dt;
            p += vel * dt;
            if (vel.y < 0f && p.y <= SurfaceY(p.z) + r) break;
            if (p.z > 5f) break;
        }

        const float hover = 0.04f;
        Vector3 start = sweet.transform.position;
        start.z += r;
        start.y = SurfaceY(start.z) + hover;
        var end = new Vector3(p.x, SurfaceY(p.z) + hover, p.z);

        directionLine.positionCount = 2;
        directionLine.SetPosition(0, start);
        directionLine.SetPosition(1, end);

        int seg = landingRing.positionCount;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            landingRing.SetPosition(i, end + new Vector3(Mathf.Cos(a) * r, 0.01f, Mathf.Sin(a) * r));
        }
    }
}

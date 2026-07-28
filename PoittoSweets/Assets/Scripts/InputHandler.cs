using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public SweetSpawner spawner;
    public LineRenderer directionLine;

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
            return;
        }

        bool hasSweet = spawner.Current != null;
        directionLine.enabled = hasSweet;
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

    private void UpdateLine()
    {
        Vector3 origin = spawner.Current.transform.position;
        directionLine.SetPosition(0, origin);
        directionLine.SetPosition(1, origin + ThrowVelocity().normalized * 1.2f);
    }
}

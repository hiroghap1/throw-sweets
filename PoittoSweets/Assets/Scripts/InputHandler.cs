using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public SweetSpawner spawner;
    public LineRenderer directionLine;

    [Header("移動・角度")]
    public float moveRange = 1.8f;
    public float moveSensitivity = 0.01f;
    public float angleMin = 20f;
    public float angleMax = 70f;
    public float angleSensitivity = 0.15f;

    [Header("投げ")]
    public float throwSpeed = 5.2f;

    private float angle = 45f;
    private bool dragging;

    private void Update()
    {
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
            p.x = Mathf.Clamp(p.x + delta.x * moveSensitivity, -moveRange, moveRange);
            transform.position = p;
            angle = Mathf.Clamp(angle + delta.y * angleSensitivity, angleMin, angleMax);
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
        directionLine.SetPosition(0, transform.position);
        directionLine.SetPosition(1, transform.position + ThrowVelocity().normalized * 1.2f);
    }
}

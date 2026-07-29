using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("ゲームオーバー判定")]
    [Tooltip("発射ラインの Z 座標。これより手前に着地スイーツが滞留したらアウト")]
    public float lineZ = -1.8f;
    [Tooltip("投げた直後は判定から除外する秒数")]
    public float graceAfterLaunch = 3f;
    [Tooltip("ライン越え滞留がこの秒数続いたらゲームオーバー")]
    public float overDuration = 2.5f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverText;
    public Image nextImage;

    public bool IsGameOver { get; private set; }

    private int score;
    private float overTimer;

    private void Awake() => Instance = this;

    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = score.ToString("N0");
    }

    public void ShowNext(SweetData next)
    {
        var rend = next.prefab.GetComponentInChildren<Renderer>();
        if (rend != null) nextImage.color = rend.sharedMaterial.color;
    }

    private void Update()
    {
        if (IsGameOver)
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        bool anyOverLine = false;
        foreach (var sweet in SweetController.All)
        {
            if (!sweet.Launched || sweet.TimeSinceLaunch < graceAfterLaunch) continue;
            // 中心ではなく「ボールの手前端」で判定（大きいボールは壁があるため中心がラインまで届かない）
            if (sweet.transform.position.z - sweet.data.radius < lineZ)
            {
                anyOverLine = true;
                break;
            }
        }

        overTimer = anyOverLine ? overTimer + Time.deltaTime : 0f;
        if (overTimer >= overDuration)
        {
            IsGameOver = true;
            gameOverText.gameObject.SetActive(true);
        }
    }
}

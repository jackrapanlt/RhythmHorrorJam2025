using UnityEngine;
using TMPro;

public class Score : MonoBehaviour
{
    public static Score Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;     // ลาก TextMeshProUGUI/TMP_Text มาวางที่นี่

    [Header("Label")]
    [SerializeField] private string label = "SCORE : "; // คำขึ้นต้นก่อนตัวเลข

    [Header("Config")]
    public int addScore = 10;                        // คะแนนต่อ 1 ครั้งที่โดน Hit
    [SerializeField] private bool padWithZeros = false;
    [SerializeField] private int padDigits = 6;      // ใช้เมื่อ padWithZeros = true เช่น 000010

    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ถ้าต้องการให้คะแนนไม่หายตอนเปลี่ยนฉาก ให้เปิดบรรทัดนี้
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshText();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        CurrentScore += amount;
        RefreshText();
    }

    public void SetScore(int value)
    {
        CurrentScore = Mathf.Max(0, value);
        RefreshText();
    }

    public void ResetScore()
    {
        SetScore(0);
    }

    private void RefreshText()
    {
        if (!scoreText) return;

        string number = padWithZeros
            ? CurrentScore.ToString(new string('0', Mathf.Max(1, padDigits)))
            : CurrentScore.ToString();

        scoreText.text = string.Concat(label, number);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        RefreshText();
    }
#endif
}

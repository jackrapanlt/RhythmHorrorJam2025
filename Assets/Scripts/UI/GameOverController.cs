using TMPro;
using UnityEngine;

public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    [Header("GameOver UI")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Hide on Game Over")]
    [SerializeField] private GameObject hide; // ซ่อนวัตถุนี้เมื่อ Game Over

    [Header("Highs (per run)")]
    [SerializeField] private TMP_Text hightRank;   // แสดงแรงค์สูงสุด เช่น "S"
    [SerializeField] private TMP_Text hightCombo;  // แสดงคอมโบสูงสุดของเกมนี้
    [SerializeField] private TMP_Text hightScore;  // แสดงคะแนนรวมเมื่อจบเกม (รอบนี้)

    private bool isGameOver;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TriggerGameOver() => SetGameOver(true);

    private void SetGameOver(bool on)
    {
        isGameOver = on;

        if (gameOverPanel) gameOverPanel.SetActive(on);

        // ซ่อน/คืนค่าออบเจ็กต์ที่กำหนด
        if (hide)
        {
            if (on) hide.SetActive(false);
            else hide.SetActive(true); // กรณีอยากให้กลับมาเมื่อยกเลิก Game Over
        }

        if (on)
        {
            // หยุดเกม
            var gm = (GameManager.Instance != null)
                ? GameManager.Instance
                : Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            gm?.PauseGame();

            // อัปเดตสรุปค่าสูงสุด
            UpdateHighTexts();
        }
    }

    private void UpdateHighTexts()
    {
        // Rank สูงสุด (จาก Ranking)
        if (hightRank)
        {
            string rankName = Ranking.Instance ? Ranking.Instance.PeakRankName : "-";
            hightRank.text = rankName;
        }

        // คอมโบสูงสุด (จาก Ranking)
        if (hightCombo)
        {
            int maxCombo = Ranking.Instance ? Ranking.Instance.PeakCombo : 0;
            hightCombo.text = maxCombo.ToString();
        }

        // คะแนนสุดท้ายของรอบนี้ (เท่ากับค่าสูงสุดในรอบอยู่แล้ว)
        if (hightScore)
        {
            int finalScore = Score.Instance ? Score.Instance.CurrentScore : 0;
            hightScore.text = finalScore.ToString();
        }
    }
}

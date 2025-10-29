using System;
using System.Collections;
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

    [Header("UI Prefix")]
    [SerializeField] private string comboPrefix = "High combo : ";
    [SerializeField] private string scorePrefix = "Score : ";

    private bool isGameOver;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        isGameOver = false;
    }

    public void TriggerGameOver() => SetGameOver(true);

    private void SetGameOver(bool on)
    {
        isGameOver = on;

        if (gameOverPanel) gameOverPanel.SetActive(on);

        // ซ่อน/คืนออบเจ็กต์ที่กำหนด
        if (hide) hide.SetActive(!on);

        if (on)
        {
            // 1) แช่เกม แต่ "ยังไม่" พักเสียง (เพื่อให้ SFX เล่นต่อได้)
            var gm = (GameManager.Instance != null)
                ? GameManager.Instance
                : FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            gm?.PauseGame(false); // timeScale=0, Audio ยังไม่ pause

            // 2) หยุดเพลงทันที (Playmusic -> Stop)
            if (AudioManager.instance && AudioManager.instance.musicSource)
                AudioManager.instance.musicSource.Stop();

            // 3) เล่น SFX "Game Over" ทันที
            AudioManager.instance?.PlaySFX("Game Over");

            // 4) อัปเดตสรุป
            UpdateHighTexts();

            // 5) รอให้ SFX เล่นจนจบ -> แล้วค่อย pause เสียงทั้งเกม
            StartCoroutine(CoWaitSfxThenPauseAudio());
        }
    }

    private IEnumerator CoWaitSfxThenPauseAudio()
    {
        // รอแบบเวลาจริง (ไม่สน timeScale=0)
        float wait = 0f;

        // เผื่อก่อนหน้ามี SFX "Hurt01" ดังพร้อม ๆ กัน ให้รอความยาวมากสุดของ Hurt01/ Game Over
        if (AudioManager.instance != null && AudioManager.instance.sfxSound != null)
        {
            wait = Mathf.Max(GetSfxLength("Hurt01"), GetSfxLength("Game Over"));
        }

        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        // เมื่อ SFX จบแล้ว ค่อยพักเสียงทั้งเกม
        AudioListener.pause = true;
    }

    private float GetSfxLength(string name)
    {
        try
        {
            var arr = AudioManager.instance.sfxSound;
            var s = Array.Find(arr, x => x != null && x.name == name);
            return (s != null && s.clip) ? s.clip.length : 0f;
        }
        catch { return 0f; }
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
            hightCombo.text = comboPrefix + maxCombo;
        }

        // คะแนนสุดท้าย (จาก Score) พร้อม prefix
        if (hightScore)
        {
            int finalScore = Score.Instance ? Score.Instance.CurrentScore : 0;
            hightScore.text = scorePrefix + finalScore;
        }
    }
}

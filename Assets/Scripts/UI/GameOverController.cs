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
    [SerializeField] private TMP_Text comboRankText;  // rankCombo จาก Ranking
    [SerializeField] private TMP_Text hightCombo;     // คอมโบสูงสุดจาก Ranking
    [SerializeField] private TMP_Text hightScore;     // คะแนนรวมสุดท้าย
    [SerializeField] private TMP_Text scoreRankText;  // scoreRank จาก Score

    [Header("UI Prefix")]
    [SerializeField] private string comboRankPrefix = "rankCombo : ";
    [SerializeField] private string comboPrefix = "High combo : ";
    [SerializeField] private string scorePrefix = "Score : ";
    [SerializeField] private string scoreRankPrefix = "";

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
        if (hide) hide.SetActive(!on);

        if (on)
        {
            // แช่เกม แต่ยังไม่ pause เสียงทั้งหมด
            var gm = (GameManager.Instance != null)
                ? GameManager.Instance
                : FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            gm?.PauseGame(false);

            // หยุดเพลง + เล่น SFX Game Over
            if (AudioManager.instance && AudioManager.instance.musicSource)
                AudioManager.instance.musicSource.Stop();
            AudioManager.instance?.PlaySFX("Game Over");

            // อัปเดตข้อความทั้งหมด (รวม rankCombo & scoreRank)
            UpdateHighTexts();

            // รอ SFX จบค่อย pause เสียงทั้งเกม
            StartCoroutine(CoWaitSfxThenPauseAudio());
        }
    }

    private IEnumerator CoWaitSfxThenPauseAudio()
    {
        float wait = 0f;
        if (AudioManager.instance != null && AudioManager.instance.sfxSound != null)
        {
            wait = Mathf.Max(GetSfxLength("Hurt01"), GetSfxLength("Game Over"));
        }
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
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
        // rankCombo (จาก Ranking)
        if (comboRankText)
        {
            string rankCombo = Ranking.Instance ? Ranking.Instance.PeakRankName : "-";
            comboRankText.text = comboRankPrefix + rankCombo;
        }

        // คอมโบสูงสุด (จาก Ranking)
        if (hightCombo)
        {
            int maxCombo = Ranking.Instance ? Ranking.Instance.PeakCombo : 0;
            hightCombo.text = comboPrefix + maxCombo;
        }

        // คะแนนรวมสุดท้าย
        if (hightScore)
        {
            int finalScore = Score.Instance ? Score.Instance.CurrentScore : 0;
            hightScore.text = scorePrefix + finalScore;
        }

        // scoreRank (จาก Score) — ถ้า prefix ว่างจะแสดงเฉพาะเกรด เช่น "B"
        if (scoreRankText)
        {
            string grade = (Score.Instance != null) ? Score.Instance.GetScoreRankName() : "-";
            scoreRankText.text = string.IsNullOrEmpty(scoreRankPrefix)
                ? grade : grade;
        }
    }
}

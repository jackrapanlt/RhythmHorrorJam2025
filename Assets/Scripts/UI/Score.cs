using System;
using UnityEngine;
using TMPro;

public class Score : MonoBehaviour
{
    public static Score Instance { get; private set; }

    [Header("Base Score Settings")]
    [SerializeField] private int basePerfect = 10;
    [SerializeField] private int baseGreat = 5;
    [SerializeField] private int basePass = 1;

    // Back-compat aliases (ให้ Ranking.cs เดิมใช้ต่อได้)
    public int scorePerfect => basePerfect;
    public int scoreGreat => baseGreat;
    public int scorePass => basePass;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Label")]
    [SerializeField] private string label = "SCORE : ";

    [Header("Formatting")]
    [SerializeField] private bool padWithZeros = false;  // เติม 0 ด้านซ้ายไหม
    [SerializeField] private int padDigits = 6;      // จำนวนหลักเมื่อเติม 0

    public int CurrentScore { get; private set; }

    // ===== Hook สำหรับบอส: ดาเมจจาก "Base Score" เท่านั้น =====
    public static event Action<int> OnBasePointsAwarded;
    public static void RaiseBasePoints(int basePoints)
    {
        if (basePoints > 0) OnBasePointsAwarded?.Invoke(basePoints);
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RefreshScoreText();   // แสดงค่าตั้งต้น
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        RefreshScoreText();
    }

    /// <summary>เพิ่มคะแนนรวม (หลังคูณ/โบนัสแล้ว)</summary>
    public void AddScore(int points)
    {
        if (points <= 0) return;
        CurrentScore += points;
        RefreshScoreText();
    }

    /// <summary>คืนค่า "คะแนนฐาน" ตาม judgement</summary>
    public int GetBaseScore(JudgementType j)
    {
        switch (j)
        {
            case JudgementType.Perfect: return basePerfect;
            case JudgementType.Great: return baseGreat;
            case JudgementType.Pass: return basePass;
            default: return 0;
        }
    }

    // ---------- UI Helpers ----------
    private void RefreshScoreText()
    {
        if (!scoreText) return;
        scoreText.text = label + FormatScore(CurrentScore);
    }

    private string FormatScore(int value)
    {
        if (padWithZeros && padDigits > 0)
        {
            // เติม 0 ด้านซ้ายให้ครบ padDigits หลัก (เช่น 000123)
            return value.ToString(new string('0', Mathf.Max(1, padDigits)));
        }
        // ไม่เติม 0 — แสดงเลขปกติ
        return value.ToString();
    }

    // อัปเดต preview ใน Inspector ตอนแก้ label/format
    private void OnValidate()
    {
        if (!scoreText) return;
        scoreText.text = label + FormatScore(CurrentScore);
    }
}

// ถ้าในโปรเจกต์มี enum นี้อยู่แล้ว ให้ลบบรรทัดล่างทิ้ง
public enum JudgementType { Perfect, Great, Pass }

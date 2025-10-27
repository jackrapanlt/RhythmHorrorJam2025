using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Ranking : MonoBehaviour
{
    public static Ranking Instance { get; private set; }

    [Serializable]
    public class RankEntry
    {
        public string name = "B";                 // ชื่อแรงค์
        [Min(1)] public int hitsToAdvance = 5;    // โดนกี่ครั้งเพื่อเลื่อนแรงค์
        public float multiplier = 1f;             // ตัวคูณคะแนนของแรงค์นี้
    }

    [Header("Config")]
    public List<RankEntry> ranks = new List<RankEntry>();

    [Header("UI")]
    [SerializeField] private TMP_Text rankText;       // ป้ายแรงค์ (B/A/S...)
    [SerializeField] private TMP_Text progressText;   // ป้ายคอมโบ: "Combo : {hitsAtThisRank}"
    [SerializeField] private TMP_Text xNumber;        // ป้ายตัวคูณ: "x1", "x2.5" เป็นต้น

    [Header("State")]
    [SerializeField, Min(0)] private int currentIndex = 0;
    [SerializeField, Min(0)] private int hitsAtThisRank = 0;

    [Header("Visibility")]
    [SerializeField] private bool hideUntilFirstHit = true;   // ซ่อนจนกว่าจะโดนครั้งแรก
    [SerializeField] private bool hideProgressWithRank = true;// ซ่อน progress ตาม rank
    [SerializeField] private bool hideComboWhenZero = true;   // ซ่อนเมื่อคอมโบ=0

    [Header("UI Label")]
    [SerializeField] private string comboLabel = "Combo : ";

    // ธง: โดนอย่างน้อย 1 ครั้งในเชนปัจจุบันแล้วหรือยัง
    private bool revealedThisChain = false;

    [Header("Peaks (per run)")]
    [SerializeField] private int peakRankIndex = 0; // แรงค์ที่เคยไปถึงสูงสุดในเกมนี้
    [SerializeField] private int comboChain = 0;    // คอมโบต่อเนื่อง (ไม่รีเซ็ตตอนเลื่อนแรงค์)
    [SerializeField] private int peakCombo = 0;     // คอมโบสูงสุดในเกมนี้

    public int PeakCombo => peakCombo;


    public string PeakRankName
    {
        get
        {
            if (ranks == null || ranks.Count == 0) return "-";
            int idx = Mathf.Clamp(peakRankIndex, 0, ranks.Count - 1);
            return ranks[idx].name;
        }
    }

    public string CurrentRankName => GetCurrentEntry()?.name ?? "-";
    public float CurrentMultiplier => GetCurrentEntry()?.multiplier ?? 1f;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        ClampState();
        RefreshUI(); // เริ่มต้นจะซ่อน rankText/progressText/xNumber หากตั้ง hideUntilFirstHit=true
    }

    private void Start()
    {
        ClampState();
        RefreshUI();
    }


    public int OnHitAndGetScoredPoints(int baseScore, JudgementType judgement)
    {
        var entry = GetCurrentEntry();
        float rankMul = (entry != null) ? Mathf.Max(0f, entry.multiplier) : 1f;
        float judgeMul = GetJudgementMultiplier(judgement);

        // โดนครั้งแรกของเชน -> เผย UI
        if (!revealedThisChain)
            revealedThisChain = true;

        // อัปเดตคอมโบต่อเนื่องและคอมโบสูงสุด
        comboChain++;
        if (comboChain > peakCombo) peakCombo = comboChain;

        int finalScore = Mathf.RoundToInt(baseScore * rankMul * judgeMul);

        // เพิ่มคอมโบตามแรงค์ปัจจุบัน + เลื่อนแรงค์ถ้าถึงเกณฑ์
        StepProgress();

        // บันทึกแรงค์สูงสุดที่เคยไปถึง
        if (currentIndex > peakRankIndex) peakRankIndex = currentIndex;

        return finalScore;
    }

 
    public int OnHitAndGetScoredPoints(int baseScore)
    {
        var entry = GetCurrentEntry();
        float rankMul = (entry != null) ? Mathf.Max(0f, entry.multiplier) : 1f;

        if (!revealedThisChain)
            revealedThisChain = true;

        comboChain++;
        if (comboChain > peakCombo) peakCombo = comboChain;

        int finalScore = Mathf.RoundToInt(baseScore * rankMul);

        StepProgress();

        if (currentIndex > peakRankIndex) peakRankIndex = currentIndex;

        return finalScore;
    }


    /////////// เพิ่ม Judgement เข้ามาดึง basescore แล้วคูณ AddScore///////////////////////

    public void ApplyHitToScore(JudgementType judgement)
    {
        int baseScore = Score.Instance ? Score.Instance.GetBaseScore(judgement) : 0;
        int gained = OnHitAndGetScoredPoints(baseScore);
        Score.Instance?.AddScore(gained);
    }


    public void ApplyHitToScore(int baseScore)
    {
        int gained = OnHitAndGetScoredPoints(baseScore);
        Score.Instance?.AddScore(gained);
    }



    private float GetJudgementMultiplier(JudgementType j)
    {
        if (Score.Instance == null)
            return 1f;

        switch (j)
        {
            case JudgementType.Perfect:
                return Mathf.Max(0f, Score.Instance.scorePerfect);
            case JudgementType.Great:
                return Mathf.Max(0f, Score.Instance.scoreGreat);
            case JudgementType.Pass:
                return Mathf.Max(0f, Score.Instance.scorePass);
            default:
                return 1f;
        }
    }


    private RankEntry GetCurrentEntry()
    {
        if (ranks == null || ranks.Count == 0) return null;
        currentIndex = Mathf.Clamp(currentIndex, 0, ranks.Count - 1);
        return ranks[currentIndex];
    }

    private void StepProgress()
    {
        var entry = GetCurrentEntry();
        if (entry == null) { RefreshUI(); return; }

        hitsAtThisRank++;

        // ถึงเกณฑ์ → เลื่อนแรงค์ (ถ้ายังไม่ใช่แรงค์สุดท้าย)
        if (currentIndex < ranks.Count - 1 && hitsAtThisRank >= Mathf.Max(1, entry.hitsToAdvance))
        {
            currentIndex++;
            hitsAtThisRank = 0; // รีเซ็ตคอมโบสำหรับแรงค์ใหม่
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var entry = GetCurrentEntry();
        bool showRank = !(hideUntilFirstHit && !revealedThisChain);

        if (rankText)
        {
            rankText.enabled = showRank;
            if (showRank) rankText.text = entry != null ? entry.name : "-";
        }

        if (progressText)
        {
            bool showCombo = showRank && (!hideProgressWithRank || rankText.enabled);
            if (hideComboWhenZero && hitsAtThisRank <= 0) showCombo = false;

            progressText.enabled = showCombo;
            if (showCombo)
                progressText.text = $"Combo : {hitsAtThisRank}";
        }

        if (xNumber)
        {
            xNumber.enabled = showRank;
            if (showRank)
            {
                float mul = entry != null ? entry.multiplier : 1f;
                xNumber.text = "x" + FormatMultiplier(mul);
            }
        }
    }

    private static string FormatMultiplier(float m)
    {
  
        float rounded = Mathf.Round(m);
        if (Mathf.Abs(m - rounded) < 0.0001f) return ((int)rounded).ToString();
        return m.ToString("0.##");
    }

    private void ClampState()
    {
        if (ranks == null || ranks.Count == 0) { currentIndex = 0; hitsAtThisRank = 0; return; }
        currentIndex = Mathf.Clamp(currentIndex, 0, ranks.Count - 1);
        hitsAtThisRank = Mathf.Max(0, hitsAtThisRank);
    }

    public void ResetToFirstRank()
    {
        currentIndex = 0;
        hitsAtThisRank = 0;

        
        comboChain = 0;

       
        revealedThisChain = false;
        RefreshUI();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ranks != null)
        {
            for (int i = 0; i < ranks.Count; i++)
            {
                if (ranks[i].hitsToAdvance < 1) ranks[i].hitsToAdvance = 1;
                if (ranks[i].multiplier <= 0f) ranks[i].multiplier = 1f;
            }
        }
        ClampState();
        if (!Application.isPlaying) RefreshUI();
    }
#endif
}

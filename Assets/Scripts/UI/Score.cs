using UnityEngine;
using TMPro;

public enum JudgementType { Perfect, Great, Pass }

public class Score : MonoBehaviour
{
    public static Score Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Label")]
    [SerializeField] private string label = "SCORE : ";

    [Header("Base Score Settings")]
     public int scorePerfect = 3;
     public int scoreGreat = 2;
     public int scorePass = 1;

    [Header("Formatting")]
    [SerializeField] private bool padWithZeros = false;
    [SerializeField] private int padDigits = 6;

    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        RefreshText();
    }

    public int GetBaseScore(JudgementType type)
    {
        return type switch
        {
            JudgementType.Perfect => scorePerfect,
            JudgementType.Great => scoreGreat,
            JudgementType.Pass => scorePass,
            _ => 0
        };
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

        string num = padWithZeros
            ? CurrentScore.ToString(new string('0', Mathf.Max(1, padDigits)))
            : CurrentScore.ToString();

        scoreText.text = label + num;
    }
}

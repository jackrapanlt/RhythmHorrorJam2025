using UnityEngine;
using System.Collections.Generic;

public class JudgementPopupUI : MonoBehaviour
{
    [Header("Spawn Root & Prefab")]
    [SerializeField] private RectTransform spawnRoot;        // จุดเกิดข้อความ (เช่น กลางจอ หรือเหนือฮิตโซน)
    [SerializeField] private JudgementTextItem itemPrefab;

    [Header("Style")]
    [SerializeField] private Color perfectColor = new Color(1f, 0.95f, 0.4f);
    [SerializeField] private Color greatColor = new Color(0.5f, 0.9f, 1f);
    [SerializeField] private Color passColor = new Color(0.7f, 1f, 0.7f);
    [SerializeField] private Color missColor = new Color(1f, 0.4f, 0.4f);

    [SerializeField] private float lifetime = 0.6f;       // เวลาที่โชว์
    [SerializeField] private float rise = 40f;        // ระยะลอยขึ้น
    [SerializeField] private Vector2 jitter = new Vector2(10f, 4f); // สุ่มตำแหน่งนิด ๆ กันทับ

    [Header("Pooling")]
    [SerializeField] private int poolSize = 8;

    private readonly Queue<JudgementTextItem> pool = new();

    private void Awake()
    {
        if (!spawnRoot) spawnRoot = transform as RectTransform;
        
        for (int i = 0; i < poolSize; i++)
        {
            var it = Instantiate(itemPrefab, spawnRoot);
            it.gameObject.SetActive(false);
            pool.Enqueue(it);
        }
    }

    private void OnEnable() => HitZone.OnJudgement += HandleJudgement;
    private void OnDisable() => HitZone.OnJudgement -= HandleJudgement;

    private void HandleJudgement(JudgementType j)
    {
        var it = Get();
        var (text, color) = GetStyle(j);

        
        var rt = it.transform as RectTransform;
        rt.anchoredPosition = new Vector2(Random.Range(-jitter.x, jitter.x),
                                          Random.Range(-jitter.y, jitter.y));

        it.Prime(text, color);
        it.Play(lifetime, rise, () =>
        {
            it.gameObject.SetActive(false);
            pool.Enqueue(it);
        });
    }

    private JudgementTextItem Get()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return Instantiate(itemPrefab, spawnRoot); 
    }

    private (string, Color) GetStyle(JudgementType j)
    {
        switch (j)
        {
            case JudgementType.Perfect: return ("Perfect", perfectColor);
            case JudgementType.Great: return ("Great", greatColor);
            case JudgementType.Pass: return ("Pass", passColor);
            case JudgementType.Miss: return ("Miss", missColor);
            default: return (j.ToString(), Color.white);
        }
    }
}

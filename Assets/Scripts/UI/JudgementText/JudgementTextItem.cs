using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class JudgementTextItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private CanvasGroup cg;
    private RectTransform rt;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        rt = transform as RectTransform;
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
    }

    public void Prime(string text, Color color)
    {
        if (label) { label.text = text; label.color = color; }
        if (cg) cg.alpha = 1f;
        if (rt) rt.anchoredPosition = Vector2.zero;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// แอนิเมตให้ลอยขึ้น + จางหาย แล้วเรียก onDone()
    /// </summary>
    public void Play(float lifetime = 0.6f, float riseDistance = 40f, System.Action onDone = null)
    {
        StartCoroutine(Co());

        System.Collections.IEnumerator Co()
        {
            float t = 0f;
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(0f, riseDistance);

            while (t < lifetime)
            {
                t += Time.unscaledDeltaTime; // ใช้เวลาไม่ผูกกับ Time.timeScale
                float k = Mathf.Clamp01(t / lifetime);
                if (rt) rt.anchoredPosition = Vector2.LerpUnclamped(start, end, k);
                if (cg) cg.alpha = 1f - k;
                yield return null;
            }

            onDone?.Invoke();
        }
    }
}

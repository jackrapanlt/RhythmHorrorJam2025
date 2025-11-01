using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class Ultimate : MonoBehaviour
{
    private Ultimate instance;

    [Header("Refs")]
    [SerializeField] private HP_Stamina hp;
    [SerializeField] private Slider stamina;
    [SerializeField] private Player player;
    [SerializeField] private TimePostFX timePostFX;  // ✅ อ้างอิง TimePostFX เพื่อปรับ Vignette

    [Header("Cost & Flow")]
    [SerializeField] private int cost = 100;
    [SerializeField] private int maxKills = 10;
    [SerializeField] private float intervalSeconds = 0.5f;

    [Header("Ultimate Visual FX")]
    [Tooltip("เวลาที่ใช้ค่อยๆ เฟดความมืดของ Vignette ให้หาย (วินาที)")]
    [SerializeField, Range(0.1f, 5f)] private float vignetteFadeDuration = 1.5f;

    [Header("Hit Simulation")]
    [SerializeField] private bool gainStaminaOnUltimateKills = false;
    [SerializeField] private int staminaGainPerUltimateHit = 5;

    [Header("Ultimate Scoring")]
    [SerializeField] private bool useCustomUltimateBaseScore = false;
    [SerializeField] private int ultimateBaseScore = 5;
    [SerializeField] private JudgementType ultimateJudgement = JudgementType.Great;

    private bool isRunning;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (!hp) hp = FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
        if (!player) player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (!timePostFX) timePostFX = FindAnyObjectByType<TimePostFX>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnUltimate += TryUltimate;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnUltimate -= TryUltimate;
    }

    public void TryUltimate()
    {
        if (isRunning) return;
        if (!CanUse()) return;

        // ✅ เริ่มคอร์รูทีนค่อยๆ เฟด Vignette ให้หาย
        if (timePostFX != null)
            StartCoroutine(FadeOutVignette());

        SpendCost();
        StartCoroutine(CoUltimate());
    }

    private bool CanUse()
    {
        if (hp != null)
        {
            var prop = hp.GetType().GetProperty("Stamina", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                int curr = (int)prop.GetValue(hp);
                if (curr >= cost) return true;
            }
        }

        if (stamina != null && stamina.value >= 1f) return true;
        return false;
    }

    private void SpendCost()
    {
        if (hp != null)
        {
            var prop = hp.GetType().GetProperty("Stamina", BindingFlags.Instance | BindingFlags.Public);
            var setM = hp.GetType().GetMethod("SetStamina", BindingFlags.Instance | BindingFlags.Public);

            if (prop != null && setM != null && prop.PropertyType == typeof(int))
            {
                int curr = (int)prop.GetValue(hp);
                setM.Invoke(hp, new object[] { Mathf.Max(0, curr - cost) });
                return;
            }
        }

        if (stamina != null)
        {
            stamina.value = Mathf.Clamp01(stamina.value - 1f);
        }
    }

    private IEnumerator CoUltimate()
    {
        isRunning = true;

        for (int i = 1; i <= maxKills; i++)
        {
            var target = FindNextTarget();
            if (target != null)
            {
                try { target.MarkHit(); } catch { }

                if (gainStaminaOnUltimateKills && staminaGainPerUltimateHit > 0)
                    hp?.GainStamina(staminaGainPerUltimateHit);

                int baseScore = useCustomUltimateBaseScore
                    ? ultimateBaseScore
                    : (Score.Instance != null ? Score.Instance.GetBaseScore(ultimateJudgement) : 0);

                if (Ranking.Instance != null)
                    Ranking.Instance.ApplyHitToScore(baseScore);
                else
                    Score.Instance?.AddScore(baseScore);

                var hittable = target as IHittable;
                if (hittable != null) hittable.Die();
                else Destroy(target.gameObject);
            }

            if (i < maxKills)
                yield return new WaitForSecondsRealtime(intervalSeconds);
        }

        isRunning = false;
    }

    private MonsterRhythm FindNextTarget()
    {
        var arr = FindObjectsByType<MonsterRhythm>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (arr == null || arr.Length == 0) return null;

        if (!player) return arr[0];

        MonsterRhythm best = null;
        float bestDist = float.PositiveInfinity;
        var p = player.transform.position;

        foreach (var m in arr)
        {
            if (!m || !m.gameObject.activeInHierarchy) continue;
            float d = (m.transform.position - p).sqrMagnitude;
            if (d < bestDist) { best = m; bestDist = d; }
        }
        return best;
    }

    // ✅ ค่อยๆ เฟดให้ Vignette หายไป
    private IEnumerator FadeOutVignette()
    {
        var field = typeof(TimePostFX).GetField("_vignette", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) yield break;

        var vignette = field.GetValue(timePostFX) as UnityEngine.Rendering.Universal.Vignette;
        if (vignette == null) yield break;

        float startValue = vignette.intensity.value;
        float t = 0f;

        while (t < vignetteFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / vignetteFadeDuration);
            float newValue = Mathf.Lerp(startValue, 0f, k);

            timePostFX.SetTargetIntensity(newValue);
            vignette.intensity.value = newValue; // อัปเดตค่าแบบเรียลไทม์
            yield return null;
        }

        vignette.intensity.value = 0f;
        timePostFX.SetTargetIntensity(0f);
        Debug.Log("[Ultimate] Vignette เฟดหายจนสว่างเต็มจอ");
    }
}

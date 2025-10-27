using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class Ultimate : MonoBehaviour
{
    [Header("Refs (ผูกอย่างน้อยหนึ่งอย่าง)")]
    [SerializeField] private HP_Stamina hp;    // ใช้เช็ค/หักสแตมินา และ +สแตมินาเมื่อ “นับเป็น Hit”
    [SerializeField] private Slider stamina;   // fallback: ใช้สไลเดอร์โดยตรง (Value 1 = 100)
    [SerializeField] private Player player;    // ใช้เลือกเป้าหมายใกล้ตัว (ทางเลือก)

    [Header("Cost & Flow")]
    [SerializeField] private int cost = 100;           // ใช้ได้เมื่อ >= 100
    [SerializeField] private int maxKills = 10;        // พยายามทำลายสูงสุด 10 ตัว
    [SerializeField] private float intervalSeconds = 0.5f; // ห่างกันตัวละ 0.5 วิ (รวม ~5 วิ)

    [Header("Hit Simulation (ให้เหมือน HitZone.TryHit)")]
    [SerializeField] private int staminaGainPerUltimateHit = 5; // +สแตมินาต่อการกำจัด 1 ตัว เหมือน HitZone

    private bool isRunning;

    private void Awake()
    {
        if (!hp) hp = FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
        if (!player) player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
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

        // หักสแตมินาทันทีเพื่อกันสแปม
        SpendCost();

        StartCoroutine(CoUltimate());
    }

    private bool CanUse()
    {
        // เช็ค HP_Stamina (มี int Stamina ไหม)
        if (hp != null)
        {
            var prop = hp.GetType().GetProperty("Stamina", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                int curr = (int)prop.GetValue(hp);
                if (curr >= cost) return true;
            }
        }

        // หรือเช็คสไลเดอร์โดยตรง (1.0 = เต็ม 100)
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
            // ลด 100 แต้ม = 1.0 บนสไลเดอร์
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
                // === ทำให้ “นับเหมือน HitZone.TryHit()” ===

                // 1) กัน MISS ภายหลัง
                target.MarkHit();

                // 2) บวกสแตมินาเหมือนโดนใน HitZone
                hp?.GainStamina(staminaGainPerUltimateHit);

                // 3) คูณคะแนนด้วย Ranking แล้วส่งให้ Score
                int baseScore = (Score.Instance != null) ? Score.Instance.addScore : 10;
                if (Ranking.Instance != null)
                    Ranking.Instance.ApplyHitToScore(baseScore);
                else
                    Score.Instance?.AddScore(baseScore);

                // 4) ทำลายเป้าหมาย (เหมือนเรียก IHittable.Die() ใน TryHit)
                var hittable = target as IHittable;
                if (hittable != null) hittable.Die();
                else Destroy(target.gameObject);

                Debug.Log($"ULTIMATE {i}/{maxKills} (counted as Hit)");
            }
            else
            {
                Debug.Log($"ULTIMATE {i}/{maxKills}: no target");
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

        // เลือกใกล้ผู้เล่นสุด (ถ้ามี player) ไม่งั้นตัวแรก
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
}

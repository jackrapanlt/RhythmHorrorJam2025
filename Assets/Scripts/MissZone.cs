using UnityEngine;
using System.Collections.Generic;

public class MissZone : MonoBehaviour
{
    [SerializeField] private HP_Stamina hp;
    [SerializeField] private int damageOnMiss = 5;

    // กันเล่นซ้ำจากคอลลายเดอร์หลายชิ้น/เด้งฟิสิกส์
    private readonly HashSet<int> _missedOnce = new();

    private void Awake()
    {
        if (!hp) hp = Object.FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
    }

    private void OnDisable() => _missedOnce.Clear();

    private void OnTriggerEnter(Collider other)
    {
        var m = other.GetComponentInParent<MonsterRhythm>();
        if (!m) return;
        if (m.WasHit) return; // โดนตีไปแล้วไม่ถือว่า MISS

        // กันซ้ำ: 1 มอน = 1 MISS
        int id = m.GetInstanceID();
        if (_missedOnce.Contains(id)) return;
        _missedOnce.Add(id);

        // ---- ตัดสินเสียงจากผลลัพธ์ "หลังโดนมิสครั้งนี้" ----
        string sfxToPlay = null;
        if (hp != null)
        {
            int hpBefore = hp.HP;
            int hpAfter = Mathf.Max(0, hpBefore - damageOnMiss);

            if (hpAfter == 0)
            {
                // มิสนี้ทำให้ตายจริง (HP กลายเป็น 0) -> เล่น Dying
                sfxToPlay = "Dying";
            }
            else
            {
                // ยังไม่ตาย -> สุ่ม Hurt01 / Hurt02
                sfxToPlay = (Random.value < 0.5f) ? "Hurt01" : "Hurt02";
            }
        }
        else
        {
            // ไม่มีอ้างอิง HP -> สุ่ม Hurt ตามปกติ
            sfxToPlay = (Random.value < 0.5f) ? "Hurt01" : "Hurt02";
        }

        if (!string.IsNullOrEmpty(sfxToPlay))
            AudioManager.instance?.PlaySFX(sfxToPlay);

        // ---- ทำดาเมจ & รีเซ็ตแรงค์ตามเดิม ----
        hp?.Damage(damageOnMiss);
        Ranking.Instance?.ResetToFirstRank();
    }
}

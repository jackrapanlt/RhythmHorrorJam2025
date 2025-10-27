using UnityEngine;

public class MissZone : MonoBehaviour
{
    [SerializeField] private HP_Stamina hp;
    [SerializeField] private int damageOnMiss = 5;

    private void Awake() {if (!hp) hp = Object.FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);}
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจว่าเป็นมอนสเตอร์ของเราไหม
        var m = other.GetComponentInParent<MonsterRhythm>();
        if (m == null) return;

        // ถ้าโดนตีไปแล้ว (WasHit) ให้มองข้าม ไม่ถือว่า MISS
        if (m.WasHit) return;

        // หักพลังชีวิตตามเดิม
        hp?.Damage(damageOnMiss);

        // รีเซ็ตแรงค์กลับตัวแรกสุด
        Ranking.Instance.ResetToFirstRank();

        // ดีบักไว้ดูเวลาและเลน
        Debug.LogWarning($"MISS | lane {m.lane}s");


    }
}

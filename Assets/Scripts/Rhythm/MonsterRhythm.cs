using UnityEngine;

/// <summary>
/// ตัวโน้ต/มอนสเตอร์ที่วิ่งตามเวลาใน Chart:
/// - ถึง Hitzone ตรงเวลา
/// - เก็บ absHitTime/useUnscaledForTiming ให้ HitZone ใช้วัด Perfect/Great/Pass
/// - หลังถึงจะไหลต่อด้วย "ความเร็วเดิม" เพื่อให้ไปชน MissZone
/// </summary>
public class MonsterRhythm : MonoBehaviour, IHittable
{
    [Header("Info (for debug/UI)")]
    public int lane = 1;                    // เลนของโน้ต (ให้ HitZone เช็คกับ Player)
    public float scheduledHitTime = -1f;    // เวลา (นาฬิกา spawner) ที่ควรถึง Hitzone – เอาไว้ debug

    [Header("Arrival Settings")]
    [Tooltip("กันกรณีเวลาบินสั้นเกินจนต้องวาร์ป")]
    [SerializeField] private float minTravelDuration = 0.05f;

    [Header("Post-Travel Settings")]
    [Tooltip("หลังผ่าน Hitzone ให้ไหลต่ออีกกี่วินาที (เพื่อไปชน MissZone)")]
    [SerializeField] private float postTravelDuration = 3f;

    // ===== สำหรับ HitZone ใช้วัดเวลา =====
    /// <summary> เวลา absolute ที่จะถึง Hitzone (เทียบกับ Time.time หรือ Time.unscaledTime ตามธง) </summary>
    public float absHitTime { get; private set; }
    /// <summary> ถ้า true ให้เทียบเวลาโดยใช้ Time.unscaledTime (ต้องตรงกับฝั่ง Spawner) </summary>
    public bool useUnscaledForTiming { get; private set; }

    // ===== สถานะภายในสำหรับการเคลื่อนที่ =====
    private Vector3 startPos;
    private Vector3 targetPos;

    private float spawnAbsTime;     // เวลา absolute ตอนเกิด
    private float travelDuration;   // ระยะเวลาเดินทางจนถึง Hitzone

    private bool configured;        // ถูกตั้งค่าแล้วหรือยัง
    private bool reached;           // ถึง Hitzone แล้วหรือยัง
    private float postTravelStart;  // เวลา absolute ตอนเริ่มไหลต่อ

    // ความเร็ว/ทิศทางตอนเข้า Hitzone (ใช้ต่อช่วง post-travel)
    private Vector3 arrivalDirection;
    private float arrivalSpeed;

    // ===== IHittable state (เผื่อระบบอื่นอยากเช็ค) =====
    public bool WasHit { get; private set; } = false;

    /// <summary>
    /// เรียกจาก Spawner เพื่อเซ็ตเส้นทางและเวลาให้ถึง Hitzone ตาม Chart
    /// </summary>
    public void ConfigureFlight(
        Vector3 startPos,
        Vector3 targetPos,
        float scheduledHitTime,
        float currentClock,
        bool useUnscaledTime
    )
    {
        this.startPos = startPos;
        this.targetPos = targetPos;

        useUnscaledForTiming = useUnscaledTime;

        // เวลา absolute ตอนเกิด (ต้องใช้ clock เดียวกันตลอดเส้นทาง)
        spawnAbsTime = useUnscaledForTiming ? Time.unscaledTime : Time.time;

        // เวลาเดินทาง = เวลาเป้าหมาย - เวลาปัจจุบันของ clock เดียวกัน
        travelDuration = Mathf.Max(minTravelDuration, scheduledHitTime - currentClock);

        this.scheduledHitTime = scheduledHitTime;

        // เวลาถึงแบบ absolute (ให้ HitZone ใช้เทียบ)
        absHitTime = spawnAbsTime + travelDuration;

        // คำนวณความเร็ว/ทิศ (เพื่อให้ post-travel ต่อเนื่องความเร็วเดิม)
        Vector3 disp = (targetPos - startPos);
        float dist = disp.magnitude;
        arrivalDirection = dist > 0f ? disp / dist : (transform.forward != Vector3.zero ? transform.forward : Vector3.forward);
        arrivalSpeed = dist / travelDuration;

        configured = true;
        reached = false;
        WasHit = false;

        // ตั้งตำแหน่งเริ่ม (กันเฟรมแรกที่ t>0 กระโดด)
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(arrivalDirection, Vector3.up);
    }

    private void Update()
    {
        if (!configured) return;

        float nowAbs = useUnscaledForTiming ? Time.unscaledTime : Time.time;

        if (!reached)
        {
            // เฟส 1: เดินทางด้วย Lerp ให้ถึงตรงเวลา
            float t = Mathf.Clamp01((nowAbs - spawnAbsTime) / travelDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (t >= 1f)
            {
                reached = true;
                postTravelStart = nowAbs; // เริ่มไหลต่อ
            }
        }
        else
        {
            // เฟส 2: ไหลต่อด้วย "ความเร็วเดิม"
            float dt = useUnscaledForTiming ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += arrivalDirection * arrivalSpeed * dt;

            // หมดเวลาช่วงโชว์แล้วทำลายตัวเอง (เพื่อไม่รกฉาก)
            if (nowAbs - postTravelStart >= postTravelDuration)
            {
                Destroy(gameObject);
            }
        }
    }
    public void MarkHit()
    {
        WasHit = true;
    }

    // ===== IHittable =====
    public void Die()
    {
        WasHit = true;      // ถูกตีแล้ว → จะไม่ไปชน MissZone
        Destroy(gameObject);
    }
}

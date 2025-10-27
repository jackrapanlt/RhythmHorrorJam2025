using UnityEngine;

public class MonsterRhythm : MonoBehaviour, IHittable
{
    [Header("Info (for debug/UI)")]
    public int lane = 1;                  
    public float scheduledHitTime = -1f;   

    [Header("Arrival Settings")]
    [SerializeField] private float minTravelDuration = 0.05f;
    [SerializeField] private float postTravelDuration = 3f;

    // HitZone
    public float absHitTime { get; private set; }
    public bool useUnscaledForTiming { get; private set; }

    //Move
    private Vector3 startPos;
    private Vector3 targetPos;
    private float spawnAbsTime;     
    private float travelDuration;   
    private bool configured;      
    private bool reached;           
    private float postTravelStart;  
    private Vector3 arrivalDirection;
    private float arrivalSpeed;
    public bool WasHit { get; private set; } = false;

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
        spawnAbsTime = useUnscaledForTiming ? Time.unscaledTime : Time.time;
        travelDuration = Mathf.Max(minTravelDuration, scheduledHitTime - currentClock);
        this.scheduledHitTime = scheduledHitTime;
        absHitTime = spawnAbsTime + travelDuration;

        // คำนวณความเร็ว/ทิศ
        Vector3 disp = (targetPos - startPos);
        float dist = disp.magnitude;
        arrivalDirection = dist > 0f ? disp / dist : (transform.forward != Vector3.zero ? transform.forward : Vector3.forward);
        arrivalSpeed = dist / travelDuration;

        configured = true;
        reached = false;
        WasHit = false;

        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(arrivalDirection, Vector3.up);
    }

    private void Update()
    {
        if (!configured) return;
        float nowAbs = useUnscaledForTiming ? Time.unscaledTime : Time.time;
        if (!reached)
        {     
            float t = Mathf.Clamp01((nowAbs - spawnAbsTime) / travelDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (t >= 1f)
            {
                reached = true;
                postTravelStart = nowAbs;
            }
        }
        else
        {
          
            float dt = useUnscaledForTiming ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += arrivalDirection * arrivalSpeed * dt;

            
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

    public void Die()
    {
        WasHit = true;      
        Destroy(gameObject);
    }
}

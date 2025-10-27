using UnityEngine;

public class MonsterRhythm : MonoBehaviour, IHittable
{
    [Header("Arrival Scheduling")]
    private float minDuration = 0.02f;

   
    [SerializeField] private float postTravelDuration = 3f;

    
    public int lane = 1;
    public bool WasHit { get; private set; } = false; 
    public float scheduledHitTime = -1f;

    //scheduling
    private Vector3 startPos, targetPos;
    private float spawnAbsTime;
    private float duration;
    private bool useUnscaled;
    private bool configured;

    // Speed ​​while running into the Hitzone is calculated from distance/time.
    private Vector3 arrivalVelocity;   
    private float arrivalSpeed;        
    private Vector3 arrivalDirection;  

    //arrive flags
    private bool arrived;
    private float arrivedAbsTime;

    //post-travel
    private bool postTravel = false;
    private float postTravelStartTime;

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
        this.useUnscaled = useUnscaledTime;

        spawnAbsTime = useUnscaled ? Time.unscaledTime : Time.time;

        duration = Mathf.Max(minDuration, scheduledHitTime - currentClock);
        this.scheduledHitTime = Mathf.Max(0f, scheduledHitTime);

        // Calculate the speed used to enter the Hitzone.
        Vector3 disp = (targetPos - startPos);
        arrivalVelocity = (duration > 0f) ? disp / duration : Vector3.zero;
        arrivalSpeed = arrivalVelocity.magnitude;
        arrivalDirection = (arrivalSpeed > 0f) ? (arrivalVelocity / arrivalSpeed) : transform.forward;

        configured = true;
        arrived = false;
        postTravel = false;
    }

    private void Update()
    {
        if (!configured) return;

        float nowAbs = useUnscaled ? Time.unscaledTime : Time.time;

        if (!postTravel)
        {
            
            float elapsed = nowAbs - spawnAbsTime;
            float t = (duration <= 0.0001f) ? 1f : Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (!arrived && t >= 1f)
            {
                arrived = true;
                arrivedAbsTime = nowAbs;
                OnReachHitzone();
            }
        }
        else
        {
            //Go ahead before it's destroyed
            float dt = useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += arrivalDirection * arrivalSpeed * dt;

            if (nowAbs - postTravelStartTime >= postTravelDuration)
                Destroy(gameObject);
        }
    }

    private void OnReachHitzone()
    {
        postTravel = true;
        postTravelStartTime = useUnscaled ? Time.unscaledTime : Time.time;

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

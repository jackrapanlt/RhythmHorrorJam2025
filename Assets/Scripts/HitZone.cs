using UnityEngine;
using System.Collections.Generic;

public interface IHittable
{
    void Die();
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HitZone : MonoBehaviour
{
    [Header("Refs")]
    public Player player;

    [SerializeField] private HP_Stamina hp;
    [SerializeField] private int staminaGainOnHit = 5;

    [Header("Judgement Windows)")]
    [SerializeField] private float perfectWindow = 0.05f;  // ms
    [SerializeField] private float greatWindow = 0.12f;  
    [SerializeField] private float passWindow = 0.22f;  

    private readonly List<IHittable> inside = new();   

    private void Reset()
    {
       
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;

       
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnEnable()
    {
        
        if (InputManager.Instance != null)
            InputManager.Instance.OnHit += TryHit;
        else
            Debug.LogWarning("[HitZone] InputManager.Instance is null on OnEnable.");
    }

    private void Start()
    {
        
        if (InputManager.Instance != null)
            InputManager.Instance.OnHit -= TryHit;
        if (InputManager.Instance != null)
            InputManager.Instance.OnHit += TryHit;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnHit -= TryHit;
    }

    private void OnTriggerEnter(Collider other)
    {
        var h = other.GetComponentInParent<IHittable>();
        if (h != null && !inside.Contains(h))
            inside.Add(h);
    }

    private void OnTriggerExit(Collider other)
    {
        var h = other.GetComponentInParent<IHittable>();
        if (h == null) return;

        
        var comp = h as Component;
        var m = comp ? comp.GetComponent<MonsterRhythm>() : null;  
        inside.Remove(h);
    }

    void TryHit()
    {
        if (player == null) return;

        
        MonsterRhythm best = null;
        float bestOffset = float.MaxValue;

     
        List<MonsterRhythm> candidates = new List<MonsterRhythm>();
        if (inside.Count > 0)
        {
            foreach (var h in inside)
            {
                var comp = h as Component;
                var m = comp ? comp.GetComponent<MonsterRhythm>() : null;
                if (m != null && m.lane == player.currentLane) candidates.Add(m);
            }
        }
        if (candidates.Count == 0)
        {
            
            foreach (var m in FindObjectsByType<MonsterRhythm>(FindObjectsSortMode.None))

            {
                if (m.lane == player.currentLane) candidates.Add(m);
            }
        }
        if (candidates.Count == 0) return;

        
        foreach (var m in candidates)
        {
            float nowAbs = m.useUnscaledForTiming ? Time.unscaledTime : Time.time;
            float offset = Mathf.Abs(nowAbs - m.absHitTime);
            if (offset < bestOffset)
            {
                bestOffset = offset;
                best = m;
            }
        }
        if (best == null) return;

        
        float nowForBest = best.useUnscaledForTiming ? Time.unscaledTime : Time.time;
        float offsetMs = Mathf.Abs(nowForBest - best.absHitTime) * 1000f;

        ///////////////Score//////////////
        void AwardCall(JudgementType j)
        {
            // Ranking
            if (Ranking.Instance != null)
            {
               
                Ranking.Instance.ApplyHitToScore(j);
                return;
            }

            // None Ranking
            var sc = Score.Instance;
            if (sc != null)
            {
                sc.AddScore(sc.GetBaseScore(j));
            }
        }


        if (bestOffset <= perfectWindow) // perfect
        {
            if (hp != null) hp.GainStamina(staminaGainOnHit);
            AwardCall(JudgementType.Perfect);
            Debug.Log("Perfect");
            (best as IHittable)?.Die();
        }
        else if (bestOffset <= greatWindow) // great
        {
            if (hp != null) hp.GainStamina(staminaGainOnHit);
            AwardCall(JudgementType.Great);
            Debug.Log("Great");
            (best as IHittable)?.Die();
        }
        else if (bestOffset <= passWindow) // pass
        {
            if (hp != null) hp.GainStamina(staminaGainOnHit);
            AwardCall(JudgementType.Pass);
            Debug.Log("Pass");
            (best as IHittable)?.Die();
        }
        


        if (best == null) return;
        for (int i = inside.Count - 1; i >= 0; i--)
        {
            var comp = inside[i] as Component;
            if (!comp) { inside.RemoveAt(i); continue; }
            if (comp.GetComponent<MonsterRhythm>() == best)
            {
                inside.RemoveAt(i);
                break;
            }
        }
    }

}



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
            Debug.LogWarning("[HitZone] InputManager.Instance is null on OnEnable. Will try auto-rebind in Start().");
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

        if (m != null && !m.WasHit)
        {
            
            Debug.LogWarning($"MISS | lane {m.lane} | t={Time.time:0.000}s");
        }

        
        inside.Remove(h);
    }

   
    void TryHit()
    {
        if (inside.Count == 0 || player == null) return;

        
        for (int i = inside.Count - 1; i >= 0; i--)
        {
            var h = inside[i];
            var comp = h as Component;
            if (!comp) { inside.RemoveAt(i); continue; }

            var m = comp.GetComponent<MonsterRhythm>();
            if (!m) continue;

         
            if (m.lane == player.currentLane)
            {
                h.Die();
                if (hp != null) {hp.GainStamina(staminaGainOnHit);}

                int baseScore = (Score.Instance != null) ? Score.Instance.addScore : 10;
                if (Ranking.Instance != null)
                    Ranking.Instance.ApplyHitToScore(baseScore);
                else
                    Score.Instance?.AddScore(baseScore);

                inside.RemoveAt(i);
                break;                  
            }
        }
    }


}

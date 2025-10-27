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

    [Header("Judgement Windows (seconds)")]
    [SerializeField] private float perfectWindow = 0.05f;  // 50 ms
    [SerializeField] private float greatWindow = 0.12f;  // 120 ms
    [SerializeField] private float passWindow = 0.22f;  // 220 ms

    [Header("Score per Judgement")]
    [SerializeField] private int scorePerfect = 3;
    [SerializeField] private int scoreGreat = 2;
    [SerializeField] private int scorePass = 1;


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

        
        MonsterRhythm best = null;
        IHittable bestH = null;
        float bestOffset = float.MaxValue;

        
        float now = Time.time;

        foreach (var h in inside)
        {
            var comp = h as Component;
            if (!comp) continue;

            var m = comp.GetComponent<MonsterRhythm>();
            if (!m || m.lane != player.currentLane) continue;

            float offset = Mathf.Abs(now - m.scheduledHitTime);
            if (offset < bestOffset)
            {
                bestOffset = offset;
                best = m;
                bestH = h;
            }
        }

        if (best == null) return;

        
        if (bestOffset <= perfectWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scorePerfect);
            Debug.Log($"Perfect (+{scorePerfect})");
            bestH.Die();
            inside.Remove(bestH);
        }
        else if (bestOffset <= greatWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scoreGreat);
            Debug.Log($"Great (+{scoreGreat})");
            bestH.Die();
            inside.Remove(bestH);
        }
        else if (bestOffset <= passWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scorePass);
            Debug.Log($"Pass (+{scorePass})");
            bestH.Die();
            inside.Remove(bestH);
        }
        
    }


}

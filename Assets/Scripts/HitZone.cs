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
        inside.Remove(h);
    }

    void TryHit()
    {
        if (player == null) return;

        // 1) หาโน้ตใน "เลนเดียวกับผู้เล่น" ทั้งฉาก แล้วเลือกตัวที่ |now - absHitTime| น้อยที่สุด
        MonsterRhythm best = null;
        float bestOffset = float.MaxValue;

        // ใช้ inside ก่อนถ้ามี (แม่น+เร็ว) ไม่มีก็ค่อย fallback ทั้งฉาก
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
            // fallback: มองทั้งฉาก
            foreach (var m in FindObjectsByType<MonsterRhythm>(FindObjectsSortMode.None))

            {
                if (m.lane == player.currentLane) candidates.Add(m);
            }
        }
        if (candidates.Count == 0) return;

        // เลือกตัวที่เวลาใกล้ที่สุด
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

        // 2) ตัดสินเฉพาะ 3 ช่วง; ถ้าเกิน passWindow → "ไม่ทำอะไร" ปล่อยให้ไปชน MissZone
        float nowForBest = best.useUnscaledForTiming ? Time.unscaledTime : Time.time;
        float offsetMs = Mathf.Abs(nowForBest - best.absHitTime) * 1000f;

        if (bestOffset <= perfectWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scorePerfect);
            Debug.Log($"Perfect (+{scorePerfect}) | {offsetMs:0} ms");
            (best as IHittable)?.Die();
        }
        else if (bestOffset <= greatWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scoreGreat);
            Debug.Log($"Great (+{scoreGreat}) | {offsetMs:0} ms");
            (best as IHittable)?.Die();
        }
        else if (bestOffset <= passWindow)
        {
            Ranking.Instance?.ApplyHitToScore(scorePass);
            Debug.Log($"Pass (+{scorePass}) | {offsetMs:0} ms");
            (best as IHittable)?.Die();
        }
        // else: นอกทุกกรอบ → ไม่แตะอะไร ให้ MissZone จัดการเอง

        // เอาออกจาก inside ถ้าตายไปแล้ว
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

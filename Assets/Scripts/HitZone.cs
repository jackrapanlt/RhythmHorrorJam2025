using UnityEngine;
using System.Collections.Generic;
using System; // ★ เพิ่มสำหรับ System.Action

public interface IHittable { void Die(); }

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HitZone : MonoBehaviour
{
    // ★ เพิ่ม: อีเวนต์แจ้งผลตัดสินให้ระบบอื่น (เช่น TimePostFX) รับฟังได้
    public static event Action<JudgementType> OnJudgement;

    [Header("Refs")]
    public Player player;
    [SerializeField] private HP_Stamina hp;
    [SerializeField] private int staminaGainOnHit = 5;

    [Header("Judgement Windows")]
    [SerializeField] private float perfectWindow = 0.05f;  // วินาที
    [SerializeField] private float greatWindow = 0.12f;
    [SerializeField] private float passWindow = 0.22f;

    private readonly List<IHittable> inside = new();

    private void Reset()
    {
        var c = GetComponent<Collider>(); if (c) c.isTrigger = true;
        var rb = GetComponent<Rigidbody>(); if (rb) { rb.isKinematic = true; rb.useGravity = false; }
    }

    private void OnEnable()
    {
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
        if (h != null && !inside.Contains(h)) inside.Add(h);
    }

    private void OnTriggerExit(Collider other)
    {
        var h = other.GetComponentInParent<IHittable>();
        if (h == null) return;
        inside.Remove(h);
    }

    private void TryHit()
    {
        if (!player) return;

        // เล่นเสียงตี "ทันที" ที่ได้รับอีเวนต์กดตี
        AudioManager.instance?.PlaySFX("Attack");

        // หาเป้าหมายในเลนปัจจุบันที่ใกล้เวลา hit ที่สุด
        MonsterRhythm best = null;
        float bestOffset = float.MaxValue;

        var candidates = new List<MonsterRhythm>();
        foreach (var h in inside)
        {
            var comp = h as Component;
            var m = comp ? comp.GetComponent<MonsterRhythm>() : null;
            if (m != null && m.lane == player.currentLane) candidates.Add(m);
        }
        if (candidates.Count == 0)
        {
            foreach (var m in FindObjectsByType<MonsterRhythm>(FindObjectsSortMode.None))
                if (m.lane == player.currentLane) candidates.Add(m);
        }
        if (candidates.Count == 0) return;

        foreach (var m in candidates)
        {
            float nowAbs = m.useUnscaledForTiming ? Time.unscaledTime : Time.time;
            float offset = Mathf.Abs(nowAbs - m.absHitTime);
            if (offset < bestOffset) { bestOffset = offset; best = m; }
        }
        if (!best) return;

        float nowForBest = best.useUnscaledForTiming ? Time.unscaledTime : Time.time;
        float offsetAbs = Mathf.Abs(nowForBest - best.absHitTime);

        void Award(JudgementType j)
        {
            if (hp) hp.GainStamina(staminaGainOnHit);

            // แจ้ง "Base Score" ให้บอสรู้ เพื่อทำดาเมจจากฐานเท่านั้น
            int basePts = (Score.Instance != null) ? Score.Instance.GetBaseScore(j) : 0;
            Score.RaiseBasePoints(basePts);

            // คะแนนรวมของผู้เล่น
            Ranking.Instance?.ApplyHitToScore(j);

            // ★ เพิ่ม: แจ้งผลตัดสินออกไปให้ระบบอื่นทราบ (TimePostFX จะฟังอีเวนต์นี้)
            OnJudgement?.Invoke(j);

            (best as IHittable)?.Die();
        }

        if (offsetAbs <= perfectWindow) { Award(JudgementType.Perfect); }
        else if (offsetAbs <= greatWindow) { Award(JudgementType.Great); }
        else if (offsetAbs <= passWindow) { Award(JudgementType.Pass); }

        // ล้างผู้ตายออกจากลิสต์ภายในโซน
        for (int i = inside.Count - 1; i >= 0; i--)
        {
            var comp = inside[i] as Component;
            if (!comp) { inside.RemoveAt(i); continue; }
            var mr = comp.GetComponent<MonsterRhythm>();
            if (mr == best) { inside.RemoveAt(i); break; }
        }
    }
}

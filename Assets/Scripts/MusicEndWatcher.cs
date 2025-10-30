using System;
using UnityEngine;

/// <summary>
/// เฝ้าดู AudioSource แล้วแจ้งอีเวนต์เมื่อ "เพลงจบตามธรรมชาติ"
/// - นับเวลาคลิปเองด้วยเวลาจริง (unscaled) และหยุดนับเมื่อเกม Pause
/// - ระวังเคสเปลี่ยนเพลง/เปลี่ยนคลิปกลางคัน
/// - ไม่ผูกกับ AudioManager โดยตรง
/// </summary>
public class MusicEndWatcher : MonoBehaviour
{
    public event Action<string> OnNaturalEnd;

    [SerializeField] private float epsilon = 0.02f;   // กันปลายคลิปเหลือน้อย ๆ
    private AudioSource _src;
    private AudioClip _clip;
    private string _musicName;
    private bool _watching;
    private float _remaining;

    public void Watch(AudioSource source, string musicName)
    {
        _src = source;
        _clip = _src ? _src.clip : null;
        _musicName = musicName;
        if (_src == null || _clip == null) { _watching = false; return; }

        // เริ่มนับเวลาที่เหลือ (รองรับเริ่มกลางเพลง)
        _remaining = Mathf.Max(0f, _clip.length - Mathf.Max(0f, _src.time));
        _watching = true;
    }

    public void StopWatching()
    {
        _watching = false;
        _src = null; _clip = null; _musicName = null;
    }

    private bool IsPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.IsPaused;
    }

    private void Update()
    {
        if (!_watching) return;
        if (_src == null || _clip == null) { _watching = false; return; }

        // เพลงถูกเปลี่ยนคลิปกลางคัน → ยกเลิกการเฝ้า (ไม่ถือว่าจบ)
        if (_src.clip != _clip) { _watching = false; return; }

        // ขณะ Pause: ไม่ลดเวลา และไม่ถือว่าจบ
        if (IsPaused()) return;

        // ลดเวลาที่เหลือเมื่อกำลังเล่น (รองรับ pitch)
        if (_src.isPlaying)
        {
            float speed = Mathf.Abs(_src.pitch);
            if (speed <= 0f) speed = 1f;
            _remaining -= Time.unscaledDeltaTime * speed;
        }

        // เงื่อนไข "จบตามธรรมชาติ"
        bool timeRanOut = _remaining <= epsilon;
        bool stoppedNaturally = !_src.isPlaying && !_src.loop;

        if (timeRanOut || stoppedNaturally)
        {
            var nameToSend = _musicName; // snapshot
            StopWatching();
            OnNaturalEnd?.Invoke(nameToSend);
        }
    }
}

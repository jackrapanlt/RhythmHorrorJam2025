using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ควบคุมเอฟเฟกต์ Vignette แบบนับเวลา + ปรับลดเมื่อ Hit ดี:
/// - ค่อย ๆ เพิ่มค่า Vignette จาก 0 → 1 เป็น "ขั้น" ทีละ stepAmount ทุก ๆ stepIntervalSeconds วินาที
/// - เฟดมืดนุ่มด้วย MoveTowards ตาม fadeLerpSpeed
/// - เมื่อ HitZone ตัดสิน: Perfect/Great/Pass จะลดความมืดลงตามค่าเซ็ตไว้
/// - ไม่พึ่งพา HP_Stamina และไม่มี FilmGrain
/// </summary>
public class TimePostFX : MonoBehaviour
{
    [Header("Post FX (URP Volume)")]
    [Tooltip("Volume ที่มี Vignette override")]
    [SerializeField] private Volume postVolume;

    [Header("Run Settings")]
    [Tooltip("เริ่มทำงานอัตโนมัติเมื่อเริ่มซีน")]
    private bool playOnStart = true;

    [Tooltip("หน่วงเวลาเริ่มต้น (วินาที) ก่อนเริ่มไล่ค่า")]
    [SerializeField] private float startDelay = 1f;

    [Tooltip("ช่วงเวลา (วินาที) ต่อการเพิ่มหนึ่งขั้น")]
    [SerializeField] private float stepIntervalSeconds = 1f;

    [Tooltip("ขนาดการเพิ่มต่อขั้น (0..1) เช่น 0.1")]
    [SerializeField, Range(0f, 1f)] private float stepAmount = .01f;

    [Header("Smoothing")]
    [Tooltip("ความเร็วการเฟดมืดระหว่างค่าปัจจุบัน → เป้าหมาย")]
    [SerializeField, Range(0.1f, 30f)] private float fadeLerpSpeed = .1f;

    [Header("Initial")]
    [Tooltip("ค่าเริ่มต้นของ Vignette (ปกติ 0)")]
    private float initialVignette = 0f;

    [Header("Judgement reactions")]
    [SerializeField, Tooltip("ลดความมืดเมื่อ Perfect (ค่าติดลบเพื่อลด)")]
    private float deltaPerfect = -0.05f;
    [SerializeField, Tooltip("ลดความมืดเมื่อ Great (ค่าติดลบเพื่อลด)")]
    private float deltaGreat = -0.02f;
    [SerializeField, Tooltip("ลดความมืดเมื่อ Pass (ค่าติดลบเพื่อลด)")]
    private float deltaPass = -0.01f;

    // ---- internals ----
    private Vignette _vignette;
    private bool _ready;
    private bool _isRunning;
    private float _target;        // ค่าที่อยากไป (เพิ่มเป็นขั้น ๆ จนถึง 1)
    private float _stepTimer;     // จับเวลาระหว่างขั้น
    private float _delayTimer;    // หน่วงก่อนเริ่ม

    private void Awake()
    {
        InitPostFxHandles();
        ApplyImmediate(initialVignette);
    }

    private void OnEnable()
    {
        // ★ สมัครฟังอีเวนต์ผลตัดสินจาก HitZone
        HitZone.OnJudgement += OnJudged;
    }

    private void OnDisable()
    {
        HitZone.OnJudgement -= OnJudged;
    }

    private void Start()
    {
        _target = Mathf.Clamp01(initialVignette);
        _isRunning = false;
        _stepTimer = 0f;
        _delayTimer = 0f;

        if (playOnStart)
        {
            // รอ startDelay ก่อนค่อยเริ่มนับขั้น
            if (startDelay <= 0f) _isRunning = true;
        }
    }

    private void Update()
    {
        if (!_ready) return;

        // หน่วงก่อนเริ่ม (ถ้าตั้งค่าไว้)
        if (!_isRunning && playOnStart && startDelay > 0f)
        {
            _delayTimer += Time.unscaledDeltaTime;
            if (_delayTimer >= startDelay)
            {
                _isRunning = true;
                _delayTimer = 0f;
            }
        }

        // เดินเวลาและเพิ่ม "ขั้น" ตาม interval
        if (_isRunning)
        {
            float interval = Mathf.Max(0.01f, stepIntervalSeconds); // กัน 0
            _stepTimer += Time.unscaledDeltaTime;
            if (_stepTimer >= interval)
            {
                _stepTimer -= interval;
                _target = Mathf.Min(1f, _target + stepAmount);
            }
        }

        // เฟดนุ่มนวลเข้าหาค่าเป้าหมาย
        float dt = Time.unscaledDeltaTime * fadeLerpSpeed;
        float curr = _vignette.intensity.value;
        float next = Mathf.MoveTowards(curr, _target, dt);
        _vignette.intensity.value = next;
    }

    // ---------- Public API ----------
    /// <summary>เริ่มนับเวลาเพิ่มเป็นขั้น (ถ้าหยุดอยู่)</summary>
    public void StartStepping() => _isRunning = true;

    /// <summary>หยุดนับเวลาชั่วคราว (ยังคงเฟดเข้าหาค่าเป้าหมายล่าสุด)</summary>
    public void PauseStepping() => _isRunning = false;

    /// <summary>รีเซ็ตกลับไปที่ค่าเริ่มต้น (เลือกให้วางค่าทันที)</summary>
    public void ResetFX(bool applyImmediate = true)
    {
        _target = Mathf.Clamp01(initialVignette);
        _stepTimer = 0f;
        _delayTimer = 0f;
        if (applyImmediate) ApplyImmediate(_target);
    }

    /// <summary>รีสตาร์ตใหม่จาก 0 และเริ่มนับเวลาใหม่</summary>
    public void RestartFromZero(bool applyImmediate = true)
    {
        _target = 0f;
        _stepTimer = 0f;
        _delayTimer = 0f;
        _isRunning = true;
        if (applyImmediate) ApplyImmediate(0f);
    }

    /// <summary>ตั้งค่าพารามิเตอร์การนับขั้นแบบรวดเร็ว</summary>
    public void SetStepParameters(float intervalSeconds, float amount)
    {
        stepIntervalSeconds = Mathf.Max(0.01f, intervalSeconds);
        stepAmount = Mathf.Clamp01(amount);
    }

    /// <summary>ตั้งค่าเป้าหมายเฉพาะกิจ (0..1) แล้วระบบจะเฟดไปหา</summary>
    public void SetTargetIntensity(float target01)
    {
        _target = Mathf.Clamp01(target01);
    }

    // ---------- event handler ----------
    private void OnJudged(JudgementType j)
    {
        float delta =
            (j == JudgementType.Perfect) ? deltaPerfect :
            (j == JudgementType.Great) ? deltaGreat :
            (j == JudgementType.Pass) ? deltaPass : 0f;

        // ปรับเป้าหมายลง (ทำให้ภาพ "สว่างขึ้น") แล้วเฟดไปหาด้วยความเร็วเดิม
        _target = Mathf.Clamp01(_target + delta);
    }

    // ---------- helpers ----------
    private void InitPostFxHandles()
    {
        _ready = false;
        _vignette = null;

        if (!postVolume || !postVolume.profile) return;

        // พยายามดึง Vignette จากโปรไฟล์ ถ้าไม่มีจะสร้างใหม่
        if (!postVolume.profile.TryGet(out _vignette))
        {
            _vignette = postVolume.profile.Add<Vignette>(true);
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
        }

        if (_vignette != null)
        {
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _ready = true;
        }
    }

    private void ApplyImmediate(float value01)
    {
        if (!_ready) return;
        _vignette.intensity.value = Mathf.Clamp01(value01);
    }
}

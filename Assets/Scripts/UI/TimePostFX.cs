using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ควบคุมเอฟเฟกต์ Vignette:
/// - ค่อย ๆ เพิ่มเป้าหมายความมืดเป็น "ขั้น" ทุก ๆ stepIntervalSeconds วินาที (ถ้าเปิดทำงาน)
/// - เฟดค่าจริงเข้าหาเป้าหมายด้วย fadeLerpSpeed (ใช้ unscaled time)
/// - เมื่อได้ Perfect/Great/Pass จะ "ลด" ความมืดลงตามค่า delta ที่ตั้ง
/// - เมื่อ Miss จะ "เพิ่ม" ความมืดตามค่า deltaMiss
/// </summary>
public class TimePostFX : MonoBehaviour
{
    [Header("Post FX (URP Volume)")]
    [Tooltip("Volume ที่มี Vignette override")]
    [SerializeField] private Volume postVolume;

    [Header("Run Settings")]
    [Tooltip("เริ่มทำงานอัตโนมัติเมื่อเริ่มซีน (เริ่มนับ step)")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("หน่วงเวลาเริ่มต้น (วินาที) ก่อนเริ่มนับ step")]
    [SerializeField] private float startDelay = 1f;

    [Tooltip("ช่วงเวลา (วินาที) ต่อการเพิ่มหนึ่งขั้น")]
    [SerializeField] private float stepIntervalSeconds = 1f;

    [Tooltip("ขนาดการเพิ่มต่อขั้น (0..1) เช่น 0.01")]
    [SerializeField, Range(0f, 1f)] private float stepAmount = 0.01f;

    [Header("Smoothing")]
    [Tooltip("ความเร็วการเฟดค่าจริงเข้าหาเป้าหมาย")]
    [SerializeField, Range(0.1f, 30f)] private float fadeLerpSpeed = 0.1f;

    [Header("Initial")]
    [Tooltip("ค่าเริ่มต้นของ Vignette (0..1)")]
    [SerializeField, Range(0f, 1f)] private float initialVignette = 0f;

    [Header("Judgement reactions (ค่าติดลบ = ทำให้สว่างขึ้น)")]
    [SerializeField, Range(-1f, 1f)] private float deltaPerfect = -0.05f;
    [SerializeField, Range(-1f, 1f)] private float deltaGreat = -0.02f;
    [SerializeField, Range(-1f, 1f)] private float deltaPass = -0.01f;
    [SerializeField, Range(-1f, 1f)] private float deltaMiss = 0.08f; // ✅ เพิ่มความมืดเมื่อ Miss

    // ---- internals ----
    private Vignette _vignette;
    private bool _ready;
    private bool _isRunning;
    private float _target;        // ค่าเป้าหมาย (0..1)
    private float _stepTimer;     // จับเวลาระหว่างขั้น
    private float _delayTimer;    // หน่วงก่อนเริ่ม

    private void Awake()
    {
        InitPostFxHandles();
        ApplyImmediate(initialVignette);
    }

    private void OnEnable()
    {
        // ฟังผลตัดสินจาก HitZone
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
            if (startDelay <= 0f) _isRunning = true;
        }
    }

    private void Update()
    {
        if (!_ready) return;

        // หน่วงก่อนเริ่ม
        if (!_isRunning && playOnStart && startDelay > 0f)
        {
            _delayTimer += Time.unscaledDeltaTime;
            if (_delayTimer >= startDelay)
            {
                _isRunning = true;
                _delayTimer = 0f;
            }
        }

        // เพิ่มเป้าหมายเป็นขั้น ๆ ตามเวลา
        if (_isRunning)
        {
            float interval = Mathf.Max(0.01f, stepIntervalSeconds);
            _stepTimer += Time.unscaledDeltaTime;
            if (_stepTimer >= interval)
            {
                _stepTimer -= interval;
                _target = Mathf.Min(1f, _target + stepAmount);
            }
        }

        // เฟดค่าจริงเข้าหาเป้าหมาย
        float dt = Time.unscaledDeltaTime * fadeLerpSpeed;
        float curr = _vignette.intensity.value;
        float next = Mathf.MoveTowards(curr, _target, dt);
        _vignette.intensity.value = next;
    }

    // ---------- Public API ----------
    public void StartStepping() => _isRunning = true;
    public void PauseStepping() => _isRunning = false;

    public void ResetFX(bool applyImmediate = true)
    {
        _target = Mathf.Clamp01(initialVignette);
        _stepTimer = 0f;
        _delayTimer = 0f;
        if (applyImmediate) ApplyImmediate(_target);
    }

    public void RestartFromZero(bool applyImmediate = true)
    {
        _target = 0f;
        _stepTimer = 0f;
        _delayTimer = 0f;
        _isRunning = true;
        if (applyImmediate) ApplyImmediate(0f);
    }

    public void SetStepParameters(float intervalSeconds, float amount)
    {
        stepIntervalSeconds = Mathf.Max(0.01f, intervalSeconds);
        stepAmount = Mathf.Clamp01(amount);
    }

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
            (j == JudgementType.Pass) ? deltaPass :
            /* j == Miss */                 deltaMiss;

        _target = Mathf.Clamp01(_target + delta);
    }

    // ---------- helpers ----------
    private void InitPostFxHandles()
    {
        _ready = false;
        _vignette = null;

        if (!postVolume || !postVolume.profile) return;

        // พยายามดึง Vignette จากโปรไฟล์ ถ้าไม่มีจะสร้าง
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

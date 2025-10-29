using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HP_Stamina : MonoBehaviour
{
    public static HP_Stamina Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    public int MaxHP => maxHP;
    public int HP { get; private set; }

    [Header("HP UI")]
    [SerializeField] private Slider hpSlider;        // ถ้า sliderIs01=true: ตั้ง Min=0, Max=1
    [SerializeField] private bool sliderIs01 = true; // ใช้สเกล 0..1 สำหรับ Slider
    [SerializeField] private float lerpSpeed = 10f;  // ความเร็วการเฟดของสไลเดอร์ HP
    private float shownHP01 = 1f;

    // -------------------- Stamina --------------------
    [Header("Stamina")]
    [SerializeField] private Slider stamina;         // ตั้ง Min=0, Max=1 (Value 1 = 100)
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int startStamina = 0;
    [SerializeField] private float staminaLerpSpeed = 10f;
    public int Stamina { get; private set; }
    private float shownStamina01 = 0f;

    // -------------------- Post FX (URP Volume) --------------------
    [Header("Post FX on Low HP (URP Volume)")]
    [Tooltip("ลาก Volume ที่มี Vignette และ FilmGrain overrides")]
    [SerializeField] private Volume postVolume;

    [Tooltip("HP (0..1) ปลายช่วงการไล่ค่าของเอฟเฟกต์ — 0.20 = 20%")]
    [Range(0f, 1f)] public float hpStartFraction = 0.20f;

    [Header("Vignette Intensity (HP 100% → 20%)")]
    [Range(0f, 1f)] public float vignetteAtFullHP = 0.035f; // HP 100%
    [Range(0f, 1f)] public float vignetteAt20HP = 0.39f;   // HP 20%

    [Header("Film Grain Intensity (HP 50% → 20%)")]
    [Range(0f, 1f)] public float filmGrainAtFullHP = 0.0f;  // เมื่อ HP >= filmGrainStartFraction (เช่น 50%)
    [Range(0f, 1f)] public float filmGrainAt20HP = 0.5f;   // เมื่อ HP ~ 20%
    [Tooltip("HP (0..1) ที่ Film Grain เริ่มไล่ค่า (0.5 = เริ่มที่ 50% HP)")]
    [Range(0f, 1f)] public float filmGrainStartFraction = 0.5f;

    [Header("Smoothing")]
    [SerializeField, Range(0.1f, 20f)] private float postFxLerpSpeed = 6f;

    private Vignette _vignette;
    private FilmGrain _filmGrain;
    private bool _postFxReady;

    // -------------------- State --------------------
    private bool isGameOver;

    // -------------------- Lifecycle --------------------
    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // HP เริ่มต้น
        HP = Mathf.Max(0, maxHP);
        shownHP01 = GetHP01();
        UpdateHPImmediate();

        // Stamina เริ่มต้น
        Stamina = Mathf.Clamp(startStamina, 0, maxStamina);
        shownStamina01 = GetStamina01();
        UpdateStaminaImmediate();

        // เตรียม Post FX และตั้งค่าตาม HP ปัจจุบัน (ค่าตั้งต้น)
        InitPostFxHandles();
        ApplyPostFxImmediate();
    }

    private void Update()
    {
        UpdateHPSmoothed();
        UpdateStaminaSmoothed();
        UpdatePostFxSmoothed(); // เฟดเอฟเฟกต์ตาม HP
    }

    // -------------------- Public API: HP --------------------
    public void Damage(int amount)
    {
        if (isGameOver) return;
        if (amount < 0) amount = 0;
        SetHP(HP - amount);
    }

    public void Heal(int amount)
    {
        if (isGameOver) return;
        if (amount < 0) amount = 0;
        SetHP(HP + amount);
    }

    public void SetHP(int value)
    {
        HP = Mathf.Clamp(value, 0, maxHP);
        UpdateHPImmediate();

        // ไม่เรียก ApplyPostFxImmediate เพื่อให้เห็นการเฟดนุ่ม ๆ ใน Update()

        if (HP <= 0 && !isGameOver)
        {
            isGameOver = true;
            GameOverController.Instance?.TriggerGameOver();
        }
    }

    public void BindHPSlider(Slider s, bool sliderRange01 = true)
    {
        hpSlider = s;
        sliderIs01 = sliderRange01;
        UpdateHPImmediate();
        // ไม่เรียก ApplyPostFxImmediate เพื่อให้เฟดนุ่ม ๆ ใน Update()
    }

    // -------------------- Public API: Stamina --------------------
    /// <summary>เพิ่มสแตมินาเป็น “คะแนน” (1 คะแนน = 0.01 บนสไลเดอร์ Max=1)</summary>
    public void GainStamina(int amount)
    {
        if (amount < 0) amount = 0;
        SetStamina(Stamina + amount);
    }

    public void SetStamina(int value)
    {
        Stamina = Mathf.Clamp(value, 0, maxStamina);
        UpdateStaminaImmediate();
    }

    public void BindStaminaSlider(Slider s)
    {
        stamina = s;
        UpdateStaminaImmediate();
    }

    // -------------------- UI helpers: HP --------------------
    private float GetHP01() => (maxHP > 0) ? (float)HP / maxHP : 0f;

    private void UpdateHPImmediate()
    {
        if (!hpSlider) return;

        if (sliderIs01)
        {
            float target01 = GetHP01();
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = target01;
            shownHP01 = target01; // กันเด้งย้อน
        }
        else
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = maxHP;
            hpSlider.value = HP;
        }
    }

    private void UpdateHPSmoothed()
    {
        if (!hpSlider) return;

        if (sliderIs01)
        {
            float target = GetHP01();
            shownHP01 = Mathf.MoveTowards(shownHP01, target, Time.unscaledDeltaTime * lerpSpeed);
            hpSlider.value = shownHP01;
        }
        else
        {
            float target = HP;
            float curr = Mathf.MoveTowards(hpSlider.value, target, Time.unscaledDeltaTime * lerpSpeed * maxHP);
            hpSlider.maxValue = maxHP;
            hpSlider.value = curr;
        }
    }

    // -------------------- UI helpers: Stamina --------------------
    private float GetStamina01() => (maxStamina > 0) ? (float)Stamina / maxStamina : 0f;

    private void UpdateStaminaImmediate()
    {
        if (!stamina) return;
        stamina.minValue = 0f;
        stamina.maxValue = 1f;            // “Value 1 = 100”
        float v = GetStamina01();
        stamina.value = v;
        shownStamina01 = v;
    }

    private void UpdateStaminaSmoothed()
    {
        if (!stamina) return;
        float target = GetStamina01();
        shownStamina01 = Mathf.MoveTowards(shownStamina01, target, Time.unscaledDeltaTime * staminaLerpSpeed);
        stamina.value = shownStamina01;
    }

    // -------------------- Post FX Helpers --------------------
    private void InitPostFxHandles()
    {
        _postFxReady = false;
        _vignette = null;
        _filmGrain = null;

        if (!postVolume || !postVolume.profile) return;

        postVolume.profile.TryGet(out _vignette);
        postVolume.profile.TryGet(out _filmGrain);

        _postFxReady = (_vignette != null) || (_filmGrain != null);
    }

    private void ApplyPostFxImmediate()
    {
        if (!_postFxReady) return;

        float hp01 = GetHP01();
        float end = Mathf.Max(0.0001f, hpStartFraction);

        // Vignette: 100% → 20%
        float tV = Mathf.Clamp01(Mathf.InverseLerp(1f, end, hp01));
        if (_vignette != null && _vignette.intensity != null)
            _vignette.intensity.value = Mathf.Lerp(vignetteAtFullHP, vignetteAt20HP, tV);

        // Film Grain: 50% → 20%
        float startG = Mathf.Clamp01(filmGrainStartFraction);
        float tG = Mathf.Clamp01(Mathf.InverseLerp(startG, end, hp01));
        if (_filmGrain != null && _filmGrain.intensity != null)
            _filmGrain.intensity.value = Mathf.Lerp(filmGrainAtFullHP, filmGrainAt20HP, tG);
    }

    private void UpdatePostFxSmoothed()
    {
        if (!_postFxReady) return;

        float hp01 = GetHP01();
        float end = Mathf.Max(0.0001f, hpStartFraction);
        float dt = Time.unscaledDeltaTime * postFxLerpSpeed;

        // Vignette
        float tV = Mathf.Clamp01(Mathf.InverseLerp(1f, end, hp01));
        float vTarget = Mathf.Lerp(vignetteAtFullHP, vignetteAt20HP, tV);
        if (_vignette != null && _vignette.intensity != null)
            _vignette.intensity.value = Mathf.MoveTowards(_vignette.intensity.value, vTarget, dt);

        // Film Grain
        float startG = Mathf.Clamp01(filmGrainStartFraction);
        float tG = Mathf.Clamp01(Mathf.InverseLerp(startG, end, hp01));
        float gTarget = Mathf.Lerp(filmGrainAtFullHP, filmGrainAt20HP, tG);
        if (_filmGrain != null && _filmGrain.intensity != null)
            _filmGrain.intensity.value = Mathf.MoveTowards(_filmGrain.intensity.value, gTarget, dt);
    }
}

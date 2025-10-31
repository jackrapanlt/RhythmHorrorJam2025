using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HP_Stamina : MonoBehaviour
{
    public static HP_Stamina Instance { get; private set; }
    public event System.Action<int> OnDamaged;

    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    public int MaxHP => maxHP;
    public int HP { get; private set; }

    [Header("HP UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private bool sliderIs01 = true;
    [SerializeField] private float lerpSpeed = 10f;
    private float shownHP01 = 1f;

    // -------------------- Stamina --------------------
    [Header("Stamina")]
    [SerializeField] private Slider stamina; // Min=0, Max=1
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int startStamina = 0;
    [SerializeField] private float staminaLerpSpeed = 10f;
    public int Stamina { get; private set; }
    private float shownStamina01 = 0f;

    // >>>>>>>>>>>>>>>>>>> ADD: Fire toggle <<<<<<<<<<<<<<<<<<
    [Header("Fire FX Toggle")]
    [Tooltip("ใส่ GameObject ไฟ/เอฟเฟกต์ ที่อยากให้ติดเมื่อสแตมินาเต็ม")]
    [SerializeField] private GameObject fire;
    [Tooltip("ถ้าติ๊ก จะถือว่า 'เต็ม' เมื่อ Stamina == maxStamina เท่านั้น")]
    [SerializeField] private bool requireExactFull = true;
    [Tooltip("ถ้าไม่ติ๊กด้านบน จะถือว่า 'เต็ม' เมื่อเปอร์เซ็นต์สแตมินา >= เกณฑ์นี้ (เช่น 0.999)")]
    [Range(0f, 1f)]
    [SerializeField] private float fullThreshold01 = 0.999f;

    // -------------------------------------------------

    private bool isGameOver;

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

        // >>> ADD: อัปเดตสถานะ Fire ตอนเริ่ม <<<
        UpdateFireActive();
    }

    private void Update()
    {
        UpdateHPSmoothed();
        UpdateStaminaSmoothed();
    }

    // -------------------- Public API: HP --------------------
    public void Damage(int amount)
    {
        if (isGameOver) return;
        if (amount < 0) amount = 0;
        int before = HP;
        SetHP(HP - amount);
        if (HP < before)
            OnDamaged?.Invoke(amount);
    }

    public void Heal(int amount)
    {
        if (amount < 0) amount = 0;
        SetHP(HP + amount);
    }

    public void SetHP(int value)
    {
        HP = Mathf.Clamp(value, 0, maxHP);
        UpdateHPImmediate();

        if (HP <= 0 && !isGameOver)
        {
            isGameOver = true;
        }
    }

    public void BindHPSlider(Slider s, bool sliderRange01 = true)
    {
        hpSlider = s;
        sliderIs01 = sliderRange01;
        UpdateHPImmediate();
    }

    // -------------------- Public API: Stamina --------------------
    public void GainStamina(int amount)
    {
        if (amount < 0) amount = 0;
        SetStamina(Stamina + amount);
    }

    public void SetStamina(int value)
    {
        Stamina = Mathf.Clamp(value, 0, maxStamina);
        UpdateStaminaImmediate();

        // >>> ADD: ทุกครั้งที่สแตมินาเปลี่ยน ให้เช็ค Fire <<<
        UpdateFireActive();
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
            shownHP01 = target01;
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
        stamina.maxValue = 1f;
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
        // หมายเหตุ: ใช้ค่า "จริง" (Stamina) สำหรับตัดสินใจเปิดไฟอยู่แล้ว จึงไม่ต้องเรียก UpdateFireActive ที่นี่
    }

    // >>>>>>>>>>>>>>>>>>> ADD: เมธอดสลับ Fire <<<<<<<<<<<<<<<<<<
    private void UpdateFireActive()
    {
        if (!fire) return;

        bool isFull = requireExactFull
            ? (Stamina >= maxStamina)                               // เต็มแบบเป๊ะ
            : (GetStamina01() >= fullThreshold01);                  // หรือใช้เกณฑ์เปอร์เซ็นต์

        if (fire.activeSelf != isFull)
            fire.SetActive(isFull);
    }
}

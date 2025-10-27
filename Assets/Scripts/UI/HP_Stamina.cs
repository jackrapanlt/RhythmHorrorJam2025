using UnityEngine;
using UnityEngine.UI;

public class HP_Stamina : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    public int HP { get; private set; }

    [Header("HP UI")]
    [SerializeField] private Slider hpSlider;        // ตั้ง Min=0, Max=1
    [SerializeField] private bool sliderIs01 = true; // true = ใช้สเกล 0..1
    [SerializeField] private float lerpSpeed = 10f;  // ความลื่นตอนอัปเดตหลอด
    private float shownHP01 = 1f;

    [Header("Stamina")]
    [SerializeField] private Slider stamina;         // ตั้ง Min=0, Max=1 (Value 1 = 100)
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int startStamina = 0;   // ค่าเริ่มต้น (เช่น 0)
    [SerializeField] private float staminaLerpSpeed = 10f;
    public int Stamina { get; private set; }
    private float shownStamina01 = 0f;

    private bool isGameOver;

    private void Start()
    {
        // HP เดิม
        HP = Mathf.Max(0, maxHP);
        shownHP01 = GetHP01();
        UpdateHPImmediate();

        // Stamina ใหม่
        Stamina = Mathf.Clamp(startStamina, 0, maxStamina);
        shownStamina01 = GetStamina01();
        UpdateStaminaImmediate();
    }

    private void Update()
    {
        UpdateHPSmoothed();
        UpdateStaminaSmoothed();
    }

    // ---------- Public API: HP ----------
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

        if (HP <= 0 && !isGameOver)
        {
            isGameOver = true;
            GameOverController.Instance?.TriggerGameOver(); // เรียก Game Over เดิม
        }
    }

    public void BindHPSlider(Slider s, bool sliderRange01 = true)
    {
        hpSlider = s;
        sliderIs01 = sliderRange01;
        UpdateHPImmediate();
    }

    // ---------- Public API: Stamina ----------
    /// <summary>เพิ่มสแตมินาเป็น “คะแนน” (1 คะแนน = 0.01 บนสไลเดอร์ที่ Max=1)</summary>
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

    // ---------- UI helpers: HP ----------
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

    // ---------- UI helpers: Stamina ----------
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
}

using UnityEngine;
using UnityEngine.UI;

public class BossRhythm : MonoBehaviour
{
    [Header("Boss HP")]
    [SerializeField] private int maxHP = 5000;
    public int MaxHP => maxHP;
    public int HP { get; private set; }

    [Header("HP Slider (ของบอส)")]
    [SerializeField] private Slider hpSlider;        // ถ้าใช้สเกล 0..1 ตั้ง Min=0, Max=1
    [SerializeField] private bool sliderIs01 = true; // true = 0..1, false = 0..maxHP
    [SerializeField] private float sliderLerpSpeed = 10f;
    private float shownHP01 = 1f;

    [Header("ดาเมจต่อ 1 คะแนนฐาน (Base Score)")]
    [SerializeField] private float damagePerBasePoint = 1f;

    private bool defeated;

    private void OnEnable()
    {
        defeated = false;
        HP = Mathf.Max(1, maxHP);
        UpdateSliderImmediate();

        // ฟัง event คะแนนฐานจาก Score → ใช้ทำดาเมจบอส
        Score.OnBasePointsAwarded += OnBasePoints;
    }

    private void OnDisable()
    {
        Score.OnBasePointsAwarded -= OnBasePoints;
    }

    private void Update()
    {
        UpdateSliderSmoothed();
    }

    // ได้ “คะแนนฐาน” มาหนึ่งช็อต → แปลงเป็นดาเมจทันที
    private void OnBasePoints(int basePts)
    {
        if (defeated || basePts <= 0) return;

        int dmg = Mathf.RoundToInt(basePts * damagePerBasePoint);
        HP = Mathf.Clamp(HP - dmg, 0, maxHP);

        if (!sliderIs01) UpdateSliderImmediate(); // โหมดตัวเลขเต็มอัปเดตทันที

        if (HP <= 0) Defeat();
    }

    // ===== UI helpers =====
    private float GetHP01() => (maxHP > 0) ? (float)HP / maxHP : 0f;

    private void UpdateSliderImmediate()
    {
        if (!hpSlider) return;

        if (sliderIs01)
        {
            float v = GetHP01();
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = v;
            shownHP01 = v;
        }
        else
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = maxHP;
            hpSlider.value = HP;
        }
    }

    private void UpdateSliderSmoothed()
    {
        if (!hpSlider) return;

        if (sliderIs01)
        {
            float target = GetHP01();
            shownHP01 = Mathf.MoveTowards(shownHP01, target, Time.unscaledDeltaTime * sliderLerpSpeed);
            hpSlider.value = shownHP01;
        }
        else
        {
            float target = HP;
            float v = Mathf.MoveTowards(hpSlider.value, target, Time.unscaledDeltaTime * sliderLerpSpeed * maxHP);
            hpSlider.maxValue = maxHP;
            hpSlider.value = v;
        }
    }

    // ===== บอสตาย → เปิดหน้าสรุปผลทันที =====
    private void Defeat()
    {
        defeated = true;

        // ใช้ GameOverController เป็น "หน้าสรุปผล" (ทั้งแพ้/ชนะ)
        GameOverController.Instance?.TriggerGameOver();

        // ไม่หยุดเพลง/เกมที่นี่ ปล่อยให้ลอจิกใน GameOverController/GameManager จัดการ
    }
}

using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Lane Points")]
    public Transform lane1;
    public Transform lane2;

    [Header("Move")]
    public float moveSpeed = 5f;
    public int currentLane = 1; // 1 = lane1, 2 = lane2

    [Header("Animation")]
    [SerializeField] private Animator animator;          // ใส่ Animator ของตัวละคร
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private string hurtTrigger = "Hurt";   // ✅ เพิ่ม Trigger เจ็บตัว
    [SerializeField] private string dieStateName = "PlayerDie";
    [SerializeField, Range(0.8f, 1.2f)] private float dieEndNormalized = 0.98f;

    public GameObject[] objectsSetActiveOnDie;

    private bool isDead = false;
    private bool gameOverSent = false;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwitchLane += ToggleLane;
            InputManager.Instance.OnHit += OnHit;   // กดตี -> เล่น PlayerAttack (ตาม Animator)
        }

        // ✅ ฟัง event จาก HP_Stamina ว่าถูกลดเลือด
        if (HP_Stamina.Instance != null)
            HP_Stamina.Instance.OnDamaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwitchLane -= ToggleLane;
            InputManager.Instance.OnHit -= OnHit;
        }

        if (HP_Stamina.Instance != null)
            HP_Stamina.Instance.OnDamaged -= OnPlayerDamaged;
    }

    private void Update()
    {
        // เคลื่อนระหว่างเลน (Run/Idle)
        Transform target = (currentLane == 1) ? lane1 : lane2;
        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(pos.z, target.position.z, Time.deltaTime * moveSpeed);
        transform.position = pos;

        // ตรวจ HP = 0 -> เล่นอนิเมชันตายครั้งเดียว
        var hp = HP_Stamina.Instance;
        if (!isDead && hp != null && hp.HP <= 0)
        {
            isDead = true;
            if (animator && !string.IsNullOrEmpty(dieTrigger))
            {
                animator.ResetTrigger(attackTrigger);
                animator.SetTrigger(dieTrigger); // Any State -> PlayerDie

                foreach (GameObject obj in objectsSetActiveOnDie)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
                AudioManager.instance?.StopMusic();

            }
            else
            {
                // ไม่มีอนิเมเตอร์ -> เรียก GameOver ทันที (กันค้าง)
                GameOverController.Instance?.TriggerGameOver();
                gameOverSent = true;
            }
        }

        // เมื่อเข้าสเตต PlayerDie และเล่นจบจริง -> ค่อย Game Over
        if (isDead && !gameOverSent && animator)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            bool inDie = st.IsName(dieStateName);
            bool finished = st.normalizedTime >= dieEndNormalized;
            bool transiting = animator.IsInTransition(0);

            if (inDie && finished && !transiting)
            {
                gameOverSent = true;
                GameOverController.Instance?.TriggerGameOver();
            }
        }
    }

    private void OnHit()
    {
        if (isDead) return;
        if (!animator) return;

        // ยิงทริกเกอร์ Attack
        if (!string.IsNullOrEmpty(attackTrigger))
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
    }

    private void ToggleLane()
    {
        if (isDead) return;
        currentLane = (currentLane == 1) ? 2 : 1;
    }

    // ✅ ฟังก์ชันใหม่: เรียกเมื่อโดน Damage จาก HP_Stamina
    private void OnPlayerDamaged(int dmg)
    {
        if (isDead) return;
        if (!animator) return;

        if (!string.IsNullOrEmpty(hurtTrigger))
        {
            animator.ResetTrigger(hurtTrigger);
            animator.SetTrigger(hurtTrigger);
        }
    }
}

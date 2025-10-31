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
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private string hurtTrigger = "Hurt";
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
            // ✅ เปลี่ยนมาใช้ OnLane1 / OnLane2
            InputManager.Instance.OnLane1 += GoLane1;
            InputManager.Instance.OnLane2 += GoLane2;
            InputManager.Instance.OnHit += OnHit;
        }

        // ฟัง event จาก HP_Stamina ว่าถูกลดเลือด
        if (HP_Stamina.Instance != null)
            HP_Stamina.Instance.OnDamaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLane1 -= GoLane1;
            InputManager.Instance.OnLane2 -= GoLane2;
            InputManager.Instance.OnHit -= OnHit;
        }

        if (HP_Stamina.Instance != null)
            HP_Stamina.Instance.OnDamaged -= OnPlayerDamaged;
    }

    private void Update()
    {
        // เคลื่อนระหว่างเลน
        Transform target = (currentLane == 1) ? lane1 : lane2;
        if (target != null)
        {
            Vector3 pos = transform.position;
            pos.z = Mathf.Lerp(pos.z, target.position.z, Time.deltaTime * moveSpeed);
            transform.position = pos;
        }

        // ตรวจ HP = 0 -> เล่นอนิเมชันตายครั้งเดียว
        var hp = HP_Stamina.Instance;
        if (!isDead && hp != null && hp.HP <= 0)
        {
            isDead = true;
            if (animator && !string.IsNullOrEmpty(dieTrigger))
            {
                animator.ResetTrigger(attackTrigger);
                animator.SetTrigger(dieTrigger);

                foreach (GameObject obj in objectsSetActiveOnDie)
                {
                    if (obj != null) obj.SetActive(false);
                }
                AudioManager.instance?.StopMusic();
            }
            else
            {
                GameOverController.Instance?.TriggerGameOver();
                gameOverSent = true;
            }
        }

        // รออนิเมชันตายเล่นจบก่อนค่อย Game Over
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
        if (isDead || !animator) return;

        if (!string.IsNullOrEmpty(attackTrigger))
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
    }

    // ✅ เมธอดใหม่สำหรับรับปุ่ม W/D
    private void GoLane1()
    {
        if (isDead) return;
        currentLane = 1;
    }

    private void GoLane2()
    {
        if (isDead) return;
        currentLane = 2;
    }

    // เล่นอนิเมชันเจ็บเมื่อโดนดาเมจ
    private void OnPlayerDamaged(int dmg)
    {
        if (isDead || !animator) return;

        if (!string.IsNullOrEmpty(hurtTrigger))
        {
            animator.ResetTrigger(hurtTrigger);
            animator.SetTrigger(hurtTrigger);
        }
    }

    // (ออปชัน) เก็บไว้ใช้ที่อื่นได้ ถ้าอยากสลับเลนด้วยปุ่มเดียว
    private void ToggleLane()
    {
        if (isDead) return;
        currentLane = (currentLane == 1) ? 2 : 1;
    }
}

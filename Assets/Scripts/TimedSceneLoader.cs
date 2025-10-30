using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// นับเวลาแบบรู้ Pause (ค่าเริ่มต้น 5 วินาที) แล้วโหลดซีนที่กำหนด
/// - ใช้เวลาจริง (unscaled) จึงไม่โดน Time.timeScale
/// - ถ้า pauseAware = true จะหยุดนับเมื่อ GameManager.IsPaused
/// - (ออปชัน) cancelIfPlayerDead: ถ้า HP=0 ระหว่างรอ จะยกเลิกไม่ย้ายซีน
/// </summary>
public class TimedSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    [Header("Timer")]
    [SerializeField, Min(0f)] private float delaySeconds = 5f;

    [Tooltip("เริ่มนับอัตโนมัติเมื่อ OnEnable")]
    [SerializeField] private bool startOnEnable = true;

    [Tooltip("หยุดนับเมื่อ GameManager.IsPaused เป็น true")]
    [SerializeField] private bool pauseAware = true;

    [Tooltip("ถ้า HP=0 ระหว่างรอ ให้ยกเลิกการโหลดซีน")]
    [SerializeField] private bool cancelIfPlayerDead = false;

    private float remaining;
    private bool running;

    private void OnEnable()
    {
        if (startOnEnable) StartTimer();
    }

    private void Update()
    {
        if (!running) return;
        if (pauseAware && IsPaused()) return;
        if (cancelIfPlayerDead && IsPlayerDead()) { running = false; return; }

        remaining -= Time.unscaledDeltaTime;
        if (remaining <= 0f)
        {
            running = false;
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("[TimedSceneLoader] nextSceneName ว่าง: ข้ามการโหลดซีน");
                return;
            }
            SceneManager.LoadScene(nextSceneName, loadMode);
        }
    }

    /// <summary>เริ่มนับถอยหลัง (ไม่ระบุจะใช้ค่า delaySeconds ที่ตั้งไว้)</summary>
    public void StartTimer(float? seconds = null)
    {
        remaining = Mathf.Max(0f, seconds ?? delaySeconds);
        running = true;
    }

    /// <summary>ยกเลิกการนับ</summary>
    public void CancelTimer()
    {
        running = false;
    }

    /// <summary>เริ่มนับใหม่ด้วยค่า delaySeconds เดิม</summary>
    public void RestartTimer()
    {
        StartTimer(delaySeconds);
    }

    /// <summary>เปลี่ยนชื่อซีนปลายทางตอนรันไทม์ได้</summary>
    public void SetTargetScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    // ---------- Helpers ----------
    private static bool IsPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.IsPaused;
    }

    private static bool IsPlayerDead()
    {
        var hp = (HP_Stamina.Instance != null)
            ? HP_Stamina.Instance
            : Object.FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
        return hp != null && hp.HP <= 0;
    }
}

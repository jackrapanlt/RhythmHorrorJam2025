using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

    public event Action OnPaused;
    public event Action OnResumed;

    private float _prevTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// หยุดเกม (ถ้า Pause อยู่แล้วจะไม่ทำอะไร)
    /// </summary>
    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;            // หยุดการอัปเดตที่อิงเวลาของเกม
        AudioListener.pause = true;      // หยุดเสียงทั้งหมด (ถ้าไม่ต้องการให้ปิดออก)

        OnPaused?.Invoke();
    }

    /// <summary>
    /// ให้เกมเดินต่อ (ถ้าไม่ได้ Pause อยู่จะไม่ทำอะไร)
    /// </summary>
    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
        AudioListener.pause = false;

        OnResumed?.Invoke();
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame(); else PauseGame();
    }

    public void RestartGame()
    {
        // เคลียร์สถานะ pause ให้เรียบร้อยก่อน
        if (IsPaused) ResumeGame();

        // กันพลาด: ปิด pause ที่อาจค้างจากระบบอื่น
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // โหลดฉากปัจจุบันซ้ำเพื่อเริ่มใหม่
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }
}

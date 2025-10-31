using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

    public event Action OnPaused;
    public event Action OnResumed;

    private float _prevTimeScale = 1f;

    private int menuBuildIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== Pause / Resume =====

    // ★ โอเวอร์โหลด: เรียกแบบเดิมได้ (จะพักเสียงด้วยเป็นดีฟอลต์)
    public void PauseGame() => PauseGame(true);

    /// <summary>
    /// หยุดเกม (เลือกได้ว่าจะพักเสียงไหม)
    /// pauseAudio=true: พักเสียงด้วย | false: ให้เสียงยังเล่นต่อ (เช่น รอ SFX จบ)
    /// </summary>
    public void PauseGame(bool pauseAudio)
    {
        if (IsPaused) return;
        IsPaused = true;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;              // แช่เกม
        AudioListener.pause = pauseAudio; // เลือกพักเสียงหรือไม่

        OnPaused?.Invoke();
    }

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
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    // ===== Restart / Scene change =====

    public void RestartGame()
    {
        // คลาย pause ให้เรียบร้อยก่อน
        if (IsPaused) ResumeGame();

        // กันพลาด: ปิด pause ที่อาจค้างจากระบบอื่น
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Score.Instance?.ResetScore();
        Ranking.Instance?.ResetToFirstRank();

        SceneManager.sceneLoaded += OnRestartSceneLoadedOnce;

        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
    }

    // ★ เรียกครั้งเดียวหลังซีนรีโหลดเสร็จ เพื่อเล่นเพลงล่าสุดกลับมา
    private void OnRestartSceneLoadedOnce(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnRestartSceneLoadedOnce;

        // เล่นเพลงล่าสุดที่ AudioManager จำไว้ (ถ้าเคยเล่นมาก่อน)
        AudioManager.instance?.ReplayLastMusic();
    }

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        ResumeGame();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadSceneByIndex(int buildIndex)
    {
        ResumeGame();
        DestroyScoreAndRankingIfMenuTarget(buildIndex);
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    // Async (เผื่อทำหน้าจอ Loading)
    public void LoadSceneAsyncButton(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        ResumeGame();
        StartCoroutine(CoLoadSceneAsync(sceneName));
    }

    private IEnumerator CoLoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = true; // เปลี่ยนซีนทันทีเมื่อโหลดเสร็จ
        while (!op.isDone)
            yield return null;
    }

    private void DestroyScoreAndRankingIfMenuTarget(int buildIndex)
    {
        if (buildIndex != menuBuildIndex) return;

        // ล้างค่าก่อนทำลาย (กันค้าง)
        Score.Instance?.ResetScore();
        Ranking.Instance?.ResetToFirstRank();

        if (Score.Instance != null)
            Destroy(Score.Instance.gameObject);

        if (Ranking.Instance != null)
            Destroy(Ranking.Instance.gameObject);
    }

    // ===== Quit Game =====
    public void QuitGame()
    {
        // เผื่อมีระบบเสียง/เวลา pause ค้างอยู่
        ResumeGame();
        Application.Quit();
#if UNITY_EDITOR
        // หยุด Play Mode ตอนทดสอบใน Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

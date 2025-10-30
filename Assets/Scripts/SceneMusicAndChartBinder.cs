using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicAndChartBinder : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Header("Scene Match")]
        public string sceneName;               // ชื่อซีนที่จะใช้รายการนี้

        [Header("Music")]
        public string musicName;               // ชื่อเพลงใน AudioManager
        public bool loop = true;               // ให้เพลงวนหรือไม่

        [Tooltip("ถ้า loop = false และกำหนดชื่อนี้ไว้ จะโหลดซีนนี้หลังเพลงจบ (ดีเลย์คงที่ 5 วิดูแลโดย MusicEndWatcher)")]
        public string nextSceneName = "";      // ซีนที่จะไปต่อเมื่อเพลงจบ

        [Header("Chart for this scene")]
        public TextAsset chartCsv;             // Chart ที่จะใช้กับฉากนี้
        public ChartOnlySpawner spawner;       // ถ้าเว้นว่าง จะหาในฉากให้
    }

    [SerializeField] private List<Entry> entries = new();

    private MusicEndWatcher watcher;

    // สถานะเพลง/ซีนที่กำลังเฝ้า
    private string watchingMusicName;
    private string watchingNextSceneName;

    private void Awake()
    {
        watcher = GetComponent<MusicEndWatcher>();
        if (!watcher) watcher = gameObject.AddComponent<MusicEndWatcher>();
        watcher.OnNaturalEnd += HandleMusicNaturalEnd;
    }

    private void OnDestroy()
    {
        if (watcher) watcher.OnNaturalEnd -= HandleMusicNaturalEnd;
    }

    private void Start()
    {
        ApplyForActiveScene();
    }

    private void OnDisable()
    {
        if (watcher) watcher.StopWatching();
        watchingMusicName = null;
        watchingNextSceneName = null;
    }

    /// <summary>เรียกใช้กฎสำหรับซีนปัจจุบัน</summary>
    public void ApplyForActiveScene()
    {
        string active = SceneManager.GetActiveScene().name;
        var e = entries.Find(x => x.sceneName == active);
        if (e == null) return;

        // 1) ตั้งค่า ChartOnlySpawner
        var sp = e.spawner ? e.spawner : FindFirstObjectByType<ChartOnlySpawner>(FindObjectsInactive.Include);
        if (sp != null) sp.chartCsv = e.chartCsv;

        // 2) เล่นเพลง
        var am = AudioManager.instance;
        if (am != null && !string.IsNullOrEmpty(e.musicName))
        {
            am.Playmusic(e.musicName);
            if (am.musicSource) am.musicSource.loop = e.loop;

            // 3) ถ้าไม่ loop และมี nextSceneName → ให้ MusicEndWatcher เฝ้าเพลงนี้
            if (!e.loop && !string.IsNullOrEmpty(e.nextSceneName) && am.musicSource)
            {
                watchingMusicName = e.musicName;
                watchingNextSceneName = e.nextSceneName;
                watcher.Watch(am.musicSource, watchingMusicName);
            }
            else
            {
                if (watcher) watcher.StopWatching();
                watchingMusicName = null;
                watchingNextSceneName = null;
            }
        }
    }

    // ========== Event Handler ==========
    private void HandleMusicNaturalEnd(string endedName)
    {
        // รับเฉพาะเพลงที่เฝ้าอยู่
        if (string.IsNullOrEmpty(watchingNextSceneName)) return;
        if (endedName != watchingMusicName) return;

        // กันเคสตายระหว่างรอ 5 วิ (MusicEndWatcher ดีเลย์ให้แล้ว แต่เราเช็กซ้ำตรงนี้อีกชั้น)
        var hp = (HP_Stamina.Instance != null)
            ? HP_Stamina.Instance
            : FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
        if (hp != null && hp.HP <= 0) return;

        SceneManager.LoadScene(watchingNextSceneName);
    }
}

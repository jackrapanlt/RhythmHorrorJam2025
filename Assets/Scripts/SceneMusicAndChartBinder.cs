using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicAndChartBinder : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Header("Scene Match")]
        public string sceneName;

        [Header("Music")]
        public string musicName;
        public bool loop = true;
        public string nextSceneName = "";

        [Header("Chart for this scene")]
        public TextAsset chartCsv;
        public ChartOnlySpawner spawner;

        [Header("Sync")]
        [Tooltip("เริ่มเพลงและสปอนเนอร์ด้วย DSP เดียวกัน")]
        public bool syncWithDsp = true;
        [Range(0f, 1f)] public float startDelay = 0.15f;
        [Tooltip("ชดเชยเวลา chart ถ้ารู้สึกช้า/เร็วเมื่อเทียบเพลง")]
        public float chartOffset = 0f;
    }

    [SerializeField] private List<Entry> entries = new();

    private MusicEndWatcher watcher;
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

    public void ApplyForActiveScene()
    {
        string active = SceneManager.GetActiveScene().name;
        var e = entries.Find(x => x.sceneName == active);
        if (e == null) return;

        var sp = e.spawner ? e.spawner : FindFirstObjectByType<ChartOnlySpawner>(FindObjectsInactive.Include);
        if (sp) sp.chartCsv = e.chartCsv;

        var am = AudioManager.instance;
        if (am != null && !string.IsNullOrEmpty(e.musicName))
        {
            // โหลดเพลงเข้าที่ musicSource ก่อน (สมมุติ Playmusic เซ็ต clip ให้อยู่แล้ว)
            am.Playmusic(e.musicName);
            if (am.musicSource) am.musicSource.loop = e.loop;

            if (sp && am.musicSource && e.syncWithDsp)
            {
                // รีเซ็ตแล้วสั่งเล่นด้วย DSP
                var src = am.musicSource;
                src.Stop();
                src.time = 0f;

                double dspStart = AudioSettings.dspTime + e.startDelay;
                src.PlayScheduled(dspStart);

                // ให้ Spawner เริ่มด้วย DSP เดียวกัน
                sp.StartChartAtDsp(dspStart, src, e.chartOffset);

                // ตั้ง watcher ถ้าต้องไปซีนต่อ
                SetupWatcherIfNeeded(e, src);
            }
            else
            {
                // โหมดไม่ sync: ใช้เดิม
                if (sp) sp.StartChart();
                SetupWatcherIfNeeded(e, am.musicSource);
            }
        }
    }

    private void SetupWatcherIfNeeded(Entry e, AudioSource src)
    {
        if (!e.loop && !string.IsNullOrEmpty(e.nextSceneName) && src != null)
        {
            watchingMusicName = e.musicName;
            watchingNextSceneName = e.nextSceneName;
            watcher.Watch(src, watchingMusicName);
        }
        else
        {
            watcher?.StopWatching();
            watchingMusicName = null;
            watchingNextSceneName = null;
        }
    }

    private void HandleMusicNaturalEnd(string endedName)
    {
        if (string.IsNullOrEmpty(watchingNextSceneName)) return;
        if (endedName != watchingMusicName) return;

        var hp = (HP_Stamina.Instance != null)
            ? HP_Stamina.Instance
            : FindAnyObjectByType<HP_Stamina>(FindObjectsInactive.Include);
        if (hp != null && hp.HP <= 0) return;

        SceneManager.LoadScene(watchingNextSceneName);
    }
}

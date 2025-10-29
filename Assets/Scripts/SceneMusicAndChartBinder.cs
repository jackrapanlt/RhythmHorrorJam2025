using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicAndChartBinder : MonoBehaviour
{

    [Serializable]
    public class Entry
    {
        public string sceneName;      // ชื่อซีน เช่น "Main menu", "Battle", "Boss"
        public string musicName;      // ชื่อเพลงที่อยู่ใน AudioManager.musicSounds
        public TextAsset chartCsv;    // ไฟล์ชาร์ตสำหรับ ChartOnlySpawner
    }

    [Header("Profiles (Scene -> Music -> Chart)")]
    public List<Entry> entries = new List<Entry>();

    [Header("Apply Targets")]
    public bool applyToAllSpawners = true;             // true = ใส่ให้ทุก ChartOnlySpawner ในซีน
    public ChartOnlySpawner[] targetSpawnersOverride;  // ถ้าปิด applyToAllSpawners จะใช้ลิสต์นี้แทน

    [Header("Chart Options")]
    public bool resetChartAfterAssign = true;          // เซ็ต CSV แล้ว ResetChart()
    public bool startChartAfterAssign = true;          // และ StartChart() ทันที

    [Header("When to Apply")]
    public bool applyOnStart = true;                   // เริ่มเกม/อยู่ในซีนนี้อยู่แล้ว -> ใช้ทันที
    public bool applyOnSceneLoaded = true;             // เวลามีการโหลดซีน -> ใช้โดยอัตโนมัติ

    private SceneMusicAndChartBinder Instance;
    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (applyOnStart)
            ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || entries == null || entries.Count == 0)
            return;

        // หาโปรไฟล์ที่ตรงชื่อซีน
        var entry = entries.Find(e => !string.IsNullOrEmpty(e.sceneName) && e.sceneName == sceneName);
        if (entry == null) return;

        // 1) เล่นเพลงตามโปรไฟล์ (หยุดเพลงเก่าก่อนถ้าอยากชัวร์ว่าไม่ซ้อน)
        if (AudioManager.instance != null)
        {
            // หยุดเพลงเก่าเพื่อกันค้าง (กรณีเกมโอเวอร์เพิ่ง Stop ไปแล้วจะไม่มีผล)
            if (AudioManager.instance.musicSource && AudioManager.instance.musicSource.isPlaying)
                AudioManager.instance.musicSource.Stop();

            if (!string.IsNullOrEmpty(entry.musicName))
                AudioManager.instance.Playmusic(entry.musicName);
        }

        // 2) เซ็ตชาร์ตให้ ChartOnlySpawner
        if (entry.chartCsv != null)
        {
            var targets = ResolveTargets();
            foreach (var sp in targets)
            {
                if (!sp) continue;
                sp.chartCsv = entry.chartCsv;
                if (resetChartAfterAssign) sp.ResetChart();
                if (startChartAfterAssign) sp.StartChart();
            }
        }
    }

    private IEnumerable<ChartOnlySpawner> ResolveTargets()
    {
        if (!applyToAllSpawners && targetSpawnersOverride != null && targetSpawnersOverride.Length > 0)
            return targetSpawnersOverride;

#if UNITY_2022_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        return FindObjectsByType<ChartOnlySpawner>(FindObjectsSortMode.None);
#else
        return GameObject.FindObjectsOfType<ChartOnlySpawner>();
#endif
    }
}

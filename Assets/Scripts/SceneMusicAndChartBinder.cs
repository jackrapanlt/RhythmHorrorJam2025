using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicAndChartBinder : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Header("Scene -> Music -> Chart")]
        public string sceneName;      // ชื่อซีน เช่น "Main menu", "Battle", "Boss"
        public string musicName;      // ชื่อเพลงใน AudioManager.musicSounds
        public bool loop = true;      // ★ ให้เพลงนี้วนลูปไหม
        public TextAsset chartCsv;    // ไฟล์ชาร์ตสำหรับ ChartOnlySpawner
    }

    [Header("Profiles (Scene -> Music -> Chart)")]
    public List<Entry> entries = new List<Entry>();

    [Header("Apply Targets")]
    public bool applyToAllSpawners = true;             // true = ใส่ให้ทุก ChartOnlySpawner ในซีน
    public ChartOnlySpawner[] targetSpawnersOverride;  // ถ้าปิด applyToAllSpawners ให้กำหนดเป้าหมายที่นี่

    [Header("Chart Options")]
    public bool resetChartAfterAssign = true;          // เซ็ต CSV แล้ว ResetChart()
    public bool startChartAfterAssign = true;          // และ StartChart() ทันที

    [Header("When to Apply")]
    public bool applyOnStart = true;                   // เริ่มเกม/อยู่ซีนนี้อยู่แล้ว -> ใช้ทันที
    public bool applyOnSceneLoaded = true;             // โหลดซีนใหม่ -> ใช้โดยอัตโนมัติ

    private void Awake()
    {
        // ค้างไว้ทั้งเกมเพื่อฟัง event เปลี่ยนซีน
        DontDestroyOnLoad(gameObject);
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

        // หาโปรไฟล์ของซีนนี้
        var entry = entries.Find(e => !string.IsNullOrEmpty(e.sceneName) && e.sceneName == sceneName);
        if (entry == null) return;

        // ---------- 1) เล่นเพลงตามโปรไฟล์ + ตั้งค่า Loop ----------
        if (AudioManager.instance != null)
        {
            var am = AudioManager.instance;

            // ตั้งค่า Loop ตามโปรไฟล์ไว้ก่อน
            if (am.musicSource)
                am.musicSource.loop = entry.loop; // ★ กำหนด loop/no-loop

            // หยุดเพลงเก่าป้องกันซ้อน
            if (am.musicSource && am.musicSource.isPlaying)
                am.musicSource.Stop();

            // เล่นเพลงใหม่ถ้าระบุชื่อมา
            if (!string.IsNullOrEmpty(entry.musicName))
                am.Playmusic(entry.musicName);
        }

        // ---------- 2) ตั้งค่า ChartOnlySpawner ----------
        if (entry.chartCsv != null)
        {
            foreach (var sp in ResolveTargets())
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

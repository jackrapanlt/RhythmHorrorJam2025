using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class ChartOnlySpawner : MonoBehaviour
{
    [Header("Chart (CSV: time,lane[,monster])")]
    public TextAsset chartCsv;

    [Header("Prefabs & Points")]
    public MonsterRhythm[] monsterPrefabs;
    public Transform spawnPoint1;
    public Transform spawnPoint2;
    public Transform hitZone;

    [Header("Clock")]
    public bool autoStart = true;
    public bool useUnscaledTime = false;   // ใช้เมื่อไม่ sync เพลง

    // === Audio-sync fields ===
    [Tooltip("ถ้าเปิด จะอ้างอิงเวลาจาก DSP ของเพลงแทน Time.time")]
    public bool syncToAudioSource = false;
    [Tooltip("เพลงที่ใช้เป็น clock เมื่อ syncToAudioSource = true")]
    public AudioSource music;
    [Tooltip("ชดเชยเวลา chart (บวก=ช้าลง, ลบ=เร่งขึ้น)")]
    public float chartOffsetSeconds = 0f;

    [Range(-0.2f, 0.2f)] public float visualOffsetSeconds = 0f;

    [Header("Lead (auto compute)")]
    [Range(0.2f, 5f)] public float minFlightDuration = 1.2f; // เวลาบินขั้นต่ำ
    [Range(0.5f, 50f)] public float laneSpeed = 10f;         // ใช้คำนวณเวลาบินจากระยะจริง

    [Header("Debug")]
    public bool verboseLog = false;

    // ======= Internal =======
    private struct Note { public float hitTime; public int lane; public int prefabIndex; }
    private readonly List<Note> notes = new();
    private int nextIndex = 0;

    private bool running = false;
    private float startClock;          // สำหรับโหมดไม่ sync เพลง
    private float pausedAt;

    // DSP start (ถ้าซิงก์เพลง)
    private double musicDspStart = -1.0;

    private readonly Dictionary<string, int> nameToIndex = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0) Debug.LogError("monsterPrefabs empty");
        if (!spawnPoint1 || !spawnPoint2) Debug.LogError("spawn points missing");
        if (!hitZone) Debug.LogError("hitZone missing");

        BuildPrefabNameIndex();
        ParseChart(chartCsv);
    }

    private void OnEnable()
    {
        ResetChart();
        if (autoStart && !syncToAudioSource) StartChart(); // ถ้า sync ต้องเรียกจากภายนอกด้วย DSP
    }

    private void Update()
    {
        if (!running || notes.Count == 0 || nextIndex >= notes.Count) return;

        float now = GetClock() + visualOffsetSeconds;

        while (nextIndex < notes.Count)
        {
            var n = notes[nextIndex];

            // คำนวณเวลาบินจากระยะจริงด้วย laneSpeed (อย่างน้อย minFlightDuration)
            float travel = ComputeTravelSeconds(n);
            float spawnMoment = n.hitTime - travel;

            if (now + 0.0001f >= spawnMoment)
            {
                SpawnOne(n);
                nextIndex++;
            }
            else break;
        }
    }

    // ===== Public control =====
    public void StartChart()
    {
        if (running) return;
        running = true;

        if (!syncToAudioSource)
        {
            float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
            startClock = (pausedAt > 0f) ? baseTime - pausedAt : baseTime;
            if (verboseLog) Debug.Log("[Spawner] Start (Time.time clock)");
        }
    }

    /// <summary>
    /// เริ่มสปอว์นโดยอิง DSP time (เรียกจาก Scene binder หลัง PlayScheduled)
    /// </summary>
    public void StartChartAtDsp(double dspStart, AudioSource src, float chartOffset = 0f)
    {
        musicDspStart = dspStart;
        music = src;
        chartOffsetSeconds = chartOffset;
        syncToAudioSource = true;
        running = true;
        if (verboseLog) Debug.Log($"[Spawner] StartAtDsp {dspStart:F3}, offset={chartOffset:F3}");
    }

    public void PauseChart()
    {
        if (!running) return;
        pausedAt = GetClock();
        running = false;
    }

    public void ResetChart()
    {
        running = false;
        pausedAt = 0f;
        nextIndex = 0;
        if (notes.Count == 0) ParseChart(chartCsv);
    }

    // ===== Helpers =====
    private float GetClock()
    {
        if (syncToAudioSource && musicDspStart > 0.0)
        {
            double now = AudioSettings.dspTime;
            return Mathf.Max(0f, (float)(now - musicDspStart) + chartOffsetSeconds);
        }
        else
        {
            float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
            return Mathf.Max(0f, baseTime - startClock);
        }
    }

    private float ComputeTravelSeconds(Note n)
    {
        Transform sp = (n.lane == 1) ? spawnPoint1 : spawnPoint2;
        Vector3 hitPoint = ComputeHitPointOnPlane(sp, hitZone);
        float dist = Vector3.Distance(sp.position, hitPoint);
        float bySpeed = (laneSpeed <= 0.0001f) ? minFlightDuration : dist / laneSpeed;
        return Mathf.Max(minFlightDuration, bySpeed);
    }

    private void SpawnOne(Note n)
    {
        Transform p = (n.lane == 1) ? spawnPoint1 : spawnPoint2;
        int idx = Mathf.Clamp(n.prefabIndex, 0, monsterPrefabs.Length - 1);
        var prefab = monsterPrefabs[idx] ? monsterPrefabs[idx] : monsterPrefabs[0];

        var m = Instantiate(prefab, p.position, p.rotation);

        Vector3 targetPos = ComputeHitPointOnPlane(p, hitZone);
        float nowClock = GetClock();

        m.ConfigureFlight(
            startPos: p.position,
            targetPos: targetPos,
            scheduledHitTime: n.hitTime,
            currentClock: nowClock,
            useUnscaledTime: true // เมื่อ sync เพลง เราไม่ใช้ unscaled
        );

        m.lane = n.lane;
        m.scheduledHitTime = n.hitTime;

        if (verboseLog)
            Debug.Log($"[Spawner] spawn L{n.lane} now={nowClock:F3} hit={n.hitTime:F3}");
    }

    private Vector3 ComputeHitPointOnPlane(Transform spawnPoint, Transform planeCenter)
    {
        Plane plane = new Plane(spawnPoint.forward, planeCenter.position);
        Ray ray = new Ray(spawnPoint.position, spawnPoint.forward);
        if (plane.Raycast(ray, out float enter))
            return ray.origin + ray.direction * Mathf.Max(0f, enter);
        return planeCenter.position;
    }

    private void BuildPrefabNameIndex()
    {
        nameToIndex.Clear();
        if (monsterPrefabs == null) return;
        for (int i = 0; i < monsterPrefabs.Length; i++)
        {
            var pf = monsterPrefabs[i];
            if (!pf) continue;
            if (!nameToIndex.ContainsKey(pf.name))
                nameToIndex.Add(pf.name, i);
        }
    }

    private void ParseChart(TextAsset csv)
    {
        notes.Clear();
        if (!csv) return;

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool headerSkipped = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (!headerSkipped && (line.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                   (line.IndexOf("lane", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                headerSkipped = true; continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            if (!float.TryParse(parts[0], out float t)) continue;
            if (!int.TryParse(parts[1], out int lane)) continue;

            int prefabIdx = 0;
            if (parts.Length >= 3)
            {
                var token = parts[2].Trim();
                if (int.TryParse(token, out int idx)) prefabIdx = Mathf.Clamp(idx, 0, (monsterPrefabs?.Length ?? 1) - 1);
                else if (nameToIndex.TryGetValue(token, out int map)) prefabIdx = map;
            }

            notes.Add(new Note { hitTime = Mathf.Max(0f, t), lane = Mathf.Clamp(lane, 1, 2), prefabIndex = prefabIdx });
        }

        notes.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        if (verboseLog) Debug.Log($"[Spawner] Loaded {notes.Count} notes.");
    }
}

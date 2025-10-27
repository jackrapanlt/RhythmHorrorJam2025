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

    [Header("Clock / Timing (no music)")]
    public bool autoStart = true;
    public bool useUnscaledTime = false;

    [Range(-0.2f, 0.2f)] public float visualOffsetSeconds = 0f;
    [Range(0f, 5f)] public float lookaheadSeconds = 1.0f;

    [Header("Flight Control")]
    public bool useMinFlightDuration = true;
    [Range(0.2f, 5f)] public float minFlightDuration = 2.0f;

    [Header("Debug")]
    public bool verboseLog = false;

    // ======= Internal =======
    private struct Note
    {
        public float hitTime;
        public int lane;
        public int prefabIndex;
    }

    private readonly List<Note> notes = new();
    private int nextIndex = 0;

    private bool running = false;
    private float startClock;
    private float pausedAt;

   
    private Dictionary<string, int> nameToIndex = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
            Debug.LogError("[ChartOnlySpawner] monsterPrefabs is empty.");
        if (!spawnPoint1 || !spawnPoint2) Debug.LogError("[ChartOnlySpawner] spawn points missing.");
        if (!hitZone) Debug.LogError("[ChartOnlySpawner] hitZone missing.");

        BuildPrefabNameIndex();
        ParseChart(chartCsv);
    }

    private void OnEnable()
    {
        ResetChart();
        if (autoStart) StartChart();
    }

    private void Update()
    {
        if (!running || notes.Count == 0 || nextIndex >= notes.Count) return;

        float now = GetClock() + visualOffsetSeconds;

        while (nextIndex < notes.Count)
        {
            var n = notes[nextIndex];

            float timeUntilHit = n.hitTime - now;
            bool inLookahead = (now + lookaheadSeconds >= n.hitTime);
            bool meetsMinFlight = useMinFlightDuration && (timeUntilHit <= minFlightDuration);
            bool overdue = (timeUntilHit <= 0f);

            if (inLookahead || meetsMinFlight || overdue)
            {
                SpawnOne(n);
                nextIndex++;
            }
            else
            {
                break;
            }
        }
    }

    // ======= Controls =======
    public void StartChart()
    {
        if (running) return;
        running = true;

        float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        startClock = (pausedAt > 0f) ? baseTime - pausedAt : baseTime;

        if (verboseLog) Debug.Log("[ChartOnlySpawner] Start");
    }

    public void PauseChart()
    {
        if (!running) return;
        pausedAt = GetClock();
        running = false;
        if (verboseLog) Debug.Log($"[ChartOnlySpawner] Pause at {pausedAt:F3}s");
    }

    public void ResetChart()
    {
        running = false;
        pausedAt = 0f;
        nextIndex = 0;
        if (notes.Count == 0) ParseChart(chartCsv);
        if (verboseLog) Debug.Log("[ChartOnlySpawner] Reset");
    }

    // ======= Helpers =======
    private float GetClock()
    {
        float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        return Mathf.Max(0f, baseTime - startClock);
    }

    private void SpawnOne(Note n)
    {
        Transform p = (n.lane == 1) ? spawnPoint1 : spawnPoint2;
        if (!p || monsterPrefabs == null || monsterPrefabs.Length == 0) return;

        int idx = (n.prefabIndex >= 0 && n.prefabIndex < monsterPrefabs.Length) ? n.prefabIndex : 0;
        var prefab = monsterPrefabs[idx];
        if (!prefab)
        {
            Debug.LogWarning($"[ChartOnlySpawner] Prefab at index {idx} is null. Using element 0.");
            prefab = monsterPrefabs[0];
        }

        var m = Instantiate(prefab, p.position, p.rotation);

        Vector3 targetPos = ComputeHitPointOnPlane(p, hitZone);
        float nowClock = GetClock() + visualOffsetSeconds;

        m.ConfigureFlight(
            startPos: p.position,
            targetPos: targetPos,
            scheduledHitTime: n.hitTime,
            currentClock: nowClock,
            useUnscaledTime: useUnscaledTime
        );

        m.lane = n.lane;
        m.scheduledHitTime = n.hitTime;

        if (verboseLog)
            Debug.Log($"[ChartOnlySpawner] Spawn L{n.lane} P{idx} now={nowClock:F3} -> hit {n.hitTime:F3} (remain={n.hitTime - nowClock:F3}s)");
    }

    private Vector3 ComputeHitPointOnPlane(Transform spawnPoint, Transform planeCenter)
    {
        Plane plane = new Plane(spawnPoint.forward, planeCenter.position);
        Ray ray = new Ray(spawnPoint.position, spawnPoint.forward);
        if (plane.Raycast(ray, out float enter))
            return ray.origin + ray.direction * Mathf.Max(0f, enter);
        return planeCenter.position; // fallback
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
        if (!csv)
        {
            Debug.LogWarning("[ChartOnlySpawner] chartCsv is null.");
            return;
        }

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool headerSkipped = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

          
            if (!headerSkipped && (line.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                   (line.IndexOf("lane", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                headerSkipped = true;
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            // time
            if (!float.TryParse(parts[0], out float t)) continue;

            // lane
            if (!int.TryParse(parts[1], out int lane)) continue;

            // monster (optional: index or name)
            int prefabIdx = 0; // default
            if (parts.Length >= 3)
            {
                var token = parts[2].Trim();

                // ถ้าเป็นเลข -> ใช้เป็น index
                if (int.TryParse(token, out int idx))
                {
                    prefabIdx = Mathf.Clamp(idx, 0, (monsterPrefabs != null && monsterPrefabs.Length > 0) ? monsterPrefabs.Length - 1 : 0);
                }
                else if (!string.IsNullOrEmpty(token))
                {
                    // ถ้าเป็นชื่อ -> map เป็น index
                    if (nameToIndex.TryGetValue(token, out int mapIdx))
                        prefabIdx = mapIdx;
                    else
                    {
                        // หาแบบ case-insensitive อีกที (เผื่อไม่ build index)
                        prefabIdx = FindPrefabIndexByName(token);
                        if (prefabIdx < 0)
                        {
                            if (verboseLog) Debug.LogWarning($"[ChartOnlySpawner] Unknown monster '{token}' -> fallback 0");
                            prefabIdx = 0;
                        }
                    }
                }
            }

            notes.Add(new Note
            {
                hitTime = Mathf.Max(0f, t),
                lane = Mathf.Clamp(lane, 1, 2),
                prefabIndex = prefabIdx
            });
        }

        notes.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        if (verboseLog) Debug.Log($"[ChartOnlySpawner] Loaded {notes.Count} notes.");
    }

    private int FindPrefabIndexByName(string name)
    {
        if (monsterPrefabs == null) return -1;
        for (int i = 0; i < monsterPrefabs.Length; i++)
        {
            var pf = monsterPrefabs[i];
            if (!pf) continue;
            if (string.Equals(pf.name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}

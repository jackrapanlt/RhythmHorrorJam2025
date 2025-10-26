using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RealtimeChartRecorder : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode lane1Key = KeyCode.J;
    public KeyCode lane2Key = KeyCode.K;
    public KeyCode saveKey = KeyCode.Return; 

    [Header("Spawn / Preview")]
    public MonsterRhythm monsterPrefab;
    public Transform spawnPointLane1;
    public Transform spawnPointLane2;
    public Transform hitZone;

  
    public float previewSpeed = 7f;

 
    public float minPreviewDuration = 0.08f;

    [Header("Clock")]
   
    public bool useUnscaledTime = false;

    [Header("CSV")]
   
    public string saveFileName = "my_chart.csv";

    [Header("Debug")]
    public bool verboseLog = false;

    private List<string> lines = new List<string>();
    private float startClock;
    private bool started;

    // ---------------- Lifecycle ----------------
    private void OnEnable()
    {
        lines.Clear();
        lines.Add("time,lane"); 
        StartClock();
    }

    private void Update()
    {
        if (Input.GetKeyDown(lane1Key)) AddNote(1);
        if (Input.GetKeyDown(lane2Key)) AddNote(2);

        if (Input.GetKeyDown(saveKey)) Save();
    }

    // ---------------- Core ----------------
    private void AddNote(int lane)
    {
        if (!monsterPrefab || !spawnPointLane1 || !spawnPointLane2 || !hitZone)
        {
            Debug.LogError("[Recorder] Missing prefab/spawn/hitzone references.");
            return;
        }

        float nowClock = GetClock();

        
        Transform sp = (lane == 1) ? spawnPointLane1 : spawnPointLane2;

        
        Vector3 targetPos = ComputeHitPointOnPlane(sp, hitZone);

        
        float dist = DistanceAlongForward(sp, sp.position, targetPos);
        float duration = Mathf.Max(minPreviewDuration, dist / Mathf.Max(0.001f, previewSpeed));

       
        float scheduledHitTime = nowClock + duration;

        
        var m = Instantiate(monsterPrefab, sp.position, sp.rotation);
        m.lane = lane;
        m.scheduledHitTime = scheduledHitTime;
        m.ConfigureFlight(
            startPos: sp.position,
            targetPos: targetPos,
            scheduledHitTime: scheduledHitTime,
            currentClock: nowClock,
            useUnscaledTime: useUnscaledTime
        );

       
        lines.Add($"{scheduledHitTime:F3},{lane}");

        if (verboseLog)
            Debug.Log($"[Recorder] AddNote lane={lane} now={nowClock:F3} hit={scheduledHitTime:F3} dist={dist:F3} dur={duration:F3}");
    }

    private void Save()
    {
        string path = Application.dataPath + "/" + saveFileName;
        File.WriteAllLines(path, lines);
        Debug.Log($"[Recorder] Saved Chart to: {path}");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    // ---------------- Time / Math helpers ----------------
    private void StartClock()
    {
        float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        startClock = baseTime;
        started = true;
    }

    private float GetClock()
    {
        if (!started) StartClock();
        float baseTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        return Mathf.Max(0f, baseTime - startClock);
    }

 
    private Vector3 ComputeHitPointOnPlane(Transform spawnPoint, Transform planeCenter)
    {
        Plane plane = new Plane(spawnPoint.forward, planeCenter.position);
        Ray ray = new Ray(spawnPoint.position, spawnPoint.forward);

        if (plane.Raycast(ray, out float enter))
            return ray.origin + ray.direction * Mathf.Max(0f, enter);

        return planeCenter.position;
    }

    
    private float DistanceAlongForward(Transform laneRef, Vector3 from, Vector3 to)
    {
        Vector3 dir = laneRef.forward.normalized;
        return Mathf.Abs(Vector3.Dot((to - from), dir));
    }

    
    private void OnDrawGizmosSelected()
    {
        if (spawnPointLane1)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(spawnPointLane1.position, spawnPointLane1.forward * 5f);
        }
        if (spawnPointLane2)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(spawnPointLane2.position, spawnPointLane2.forward * 5f);
        }
        if (hitZone)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hitZone.position, 0.05f);
        }
    }
}

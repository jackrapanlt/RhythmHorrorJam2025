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

        [Tooltip("ถ้า loop = false และกำหนดชื่อนี้ไว้ จะโหลดซีนนี้หลังเพลงจบ + หน่วง")]
        public string nextSceneName = "";      // ซีนที่จะไปต่อเมื่อเพลงจบ
        [Tooltip("เวลาหน่วงหลังเพลงจบ (วินาที)")]
        public float delayAfterEnd = 5f;       // ค่าเริ่มต้น 5 วิ

        [Header("Chart for this scene")]
        public TextAsset chartCsv;             // Chart ที่จะใช้กับฉากนี้
        public ChartOnlySpawner spawner;       // ถ้าเว้นว่าง จะหาในฉากให้
    }

    [SerializeField] private List<Entry> entries = new();

    private Coroutine pendingLoad;             // ไว้ยกเลิกถ้ามีการสลับรายการระหว่างรัน

    private void Start()
    {
        ApplyForActiveScene();
    }

    private void OnDisable()
    {
        if (pendingLoad != null) { StopCoroutine(pendingLoad); pendingLoad = null; }
    }

    /// <summary>เรียกใช้กฎสำหรับซีนปัจจุบัน</summary>
    public void ApplyForActiveScene()
    {
        string active = SceneManager.GetActiveScene().name;
        var e = entries.Find(x => x.sceneName == active);
        if (e == null) return;

        // 1) ตั้งค่า ChartOnlySpawner (ถ้าไม่ล็อกไว้ในอินสแตนซ์)
        var sp = e.spawner ? e.spawner : FindFirstObjectByType<ChartOnlySpawner>(FindObjectsInactive.Include);
        if (sp != null)
        {
            // หมายเหตุ: ถ้า chartCsv ใน Spawner เป็น public field ก็พอ
            // ถ้าโปรเจกต์คุณมีเมธอดตั้งค่าเฉพาะ ให้เรียกตรงนี้แทน
            sp.chartCsv = e.chartCsv;
        }

        // 2) เล่นเพลงตามรายการ
        var am = AudioManager.instance;
        if (am != null && !string.IsNullOrEmpty(e.musicName))
        {
            am.Playmusic(e.musicName);
            if (am.musicSource) am.musicSource.loop = e.loop;

            // 3) ถ้าไม่ loop และตั้ง nextSceneName ไว้ → รอเพลงจบ + หน่วง แล้วค่อยโหลดซีน
            if (pendingLoad != null) { StopCoroutine(pendingLoad); pendingLoad = null; }
            if (!e.loop && !string.IsNullOrEmpty(e.nextSceneName))
            {
                pendingLoad = StartCoroutine(WaitMusicEndThenLoad(am, e.musicName, e.nextSceneName, Mathf.Max(0f, e.delayAfterEnd)));
            }
        }
    }

    /// <summary>
    /// รอจนเพลงที่สั่งเล่นหยุด (หรือถูกเปลี่ยนเพลง) แล้วรอเพิ่มอีก delay วินาที จากนั้นโหลดซีน
    /// ใช้ WaitForSecondsRealtime เพื่อไม่โดน Time.timeScale
    /// </summary>
    private IEnumerator WaitMusicEndThenLoad(AudioManager am, string expectMusicName, string sceneToLoad, float delay)
    {
        var src = am ? am.musicSource : null;
        if (src == null) yield break;

        // รอจนกว่าเพลงที่คาดหวังจะหยุดเล่น (หรือถูกเปลี่ยนเป็นเพลงอื่น)
        while (src.isPlaying && am.LastMusicName == expectMusicName)
            yield return null;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        SceneManager.LoadScene(sceneToLoad);
    }
}

using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Lane Points")]
    public Transform lane1;
    public Transform lane2;

    [Header("Move")]
    public float moveSpeed = 5f;

    public int currentLane = 1; // 1 = lane1, 2 = lane2

    private void OnEnable()
    {
        InputManager.Instance.OnSwitchLane += ToggleLane;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnSwitchLane -= ToggleLane;
    }

    private void Update()
    {
        Transform target = (currentLane == 1) ? lane1 : lane2;
        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(pos.z, target.position.z, Time.deltaTime * moveSpeed);
        transform.position = pos;
    }

    void ToggleLane()
    {
        currentLane = (currentLane == 1) ? 2 : 1;
    }
}

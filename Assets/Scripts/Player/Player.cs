using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Lane Points")]
    public Transform lane1;
    public Transform lane2;

    [Header("Move")]
    public float moveSpeed = 5f;


    public int currentLane = 1;

    private void OnEnable()
    {
        InputManager.Instance.OnSwitchUp += MoveUp;
        InputManager.Instance.OnSwitchDown += MoveDown;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnSwitchUp -= MoveUp;
        InputManager.Instance.OnSwitchDown -= MoveDown;
    }

    private void Update()
    {
        Transform target = (currentLane == 1) ? lane1 : lane2;

        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(pos.z, target.position.z, Time.deltaTime * moveSpeed);
        transform.position = pos;
    }

    void MoveUp()
    {
        currentLane = 1;
    }

    void MoveDown()
    {
        currentLane = 2;
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Events
    public event Action OnLane1;       // กด W
    public event Action OnLane2;       // กด D
    public event Action OnHit;         // ตี
    public event Action OnUltimate;    // สกิลพิเศษ
    public static event Action EscPressed;

    // Input Actions
    private InputAction actionLane1;
    private InputAction actionLane2;
    private InputAction actionHit;
    private InputAction actionUltimate;
    private InputAction actionEsc;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Lane 1 (W)
        actionLane1 = new InputAction("Lane1", binding: "<Keyboard>/w");

        // Lane 2 (D)
        actionLane2 = new InputAction("Lane2", binding: "<Keyboard>/d");

        // Hit (K หรือ O)
        actionHit = new InputAction("Hit");
        actionHit.AddBinding("<Keyboard>/k");
        actionHit.AddBinding("<Keyboard>/o");

        // Ultimate (Space)
        actionUltimate = new InputAction("Ultimate", binding: "<Keyboard>/space");

        // Esc
        actionEsc = new InputAction("Esc", InputActionType.Button, "<Keyboard>/escape");

        // Subscribe event
        actionLane1.performed += _ => OnLane1?.Invoke();
        actionLane2.performed += _ => OnLane2?.Invoke();
        actionHit.performed += _ => OnHit?.Invoke();
        actionUltimate.performed += _ => OnUltimate?.Invoke();
        actionEsc.performed += _ => EscPressed?.Invoke();
    }

    private void OnEnable()
    {
        actionLane1.Enable();
        actionLane2.Enable();
        actionHit.Enable();
        actionUltimate.Enable();
        actionEsc.Enable();
    }

    private void OnDisable()
    {
        actionLane1.Disable();
        actionLane2.Disable();
        actionHit.Disable();
        actionUltimate.Disable();
        actionEsc.Disable();
    }
}

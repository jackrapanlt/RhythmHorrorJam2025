using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Events
    public event Action OnSwitchLane;  
    public event Action OnHit;         
    public event Action OnUltimate;    
    public static event Action EscPressed;

    // Input Actions
    private InputAction actionSwitchLane;
    private InputAction actionHit;
    private InputAction actionUltimate;
    private InputAction actionEsc;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Toggle Lane
        actionSwitchLane = new InputAction("SwitchLane", binding: "<Keyboard>/s");

        // Hit
        actionHit = new InputAction("Hit");
        actionHit.AddBinding("<Keyboard>/k");
        actionHit.AddBinding("<Keyboard>/l");

     
        actionUltimate = new InputAction("Ultimate", binding: "<Keyboard>/e");
        actionEsc = new InputAction("Esc", InputActionType.Button, "<Keyboard>/escape");

        // Subscribe
        actionSwitchLane.performed += _ => OnSwitchLane?.Invoke();
        actionHit.performed += _ => OnHit?.Invoke();
        actionUltimate.performed += _ => OnUltimate?.Invoke();
        actionEsc.performed += _ => EscPressed?.Invoke();
    }

    private void OnEnable()
    {
        actionSwitchLane.Enable();
        actionHit.Enable();
        actionUltimate.Enable();
        actionEsc.Enable();
    }

    private void OnDisable()
    {
        actionSwitchLane.Disable();
        actionHit.Disable();
        actionUltimate.Disable();
        actionEsc.Disable();
    }
}

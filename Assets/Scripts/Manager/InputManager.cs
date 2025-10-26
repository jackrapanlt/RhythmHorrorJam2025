using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public event Action OnSwitchUp;
    public event Action OnSwitchDown;
    public event Action OnHit;

    private InputAction actionSwitchUp;
    private InputAction actionSwitchDown;
    private InputAction actionHit;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        actionSwitchUp = new InputAction("SwitchUp", binding: "<Keyboard>/w");
        actionSwitchDown = new InputAction("SwitchDown", binding: "<Keyboard>/s");
        actionHit = new InputAction("Hit", binding: "<Keyboard>/space");

        actionSwitchUp.performed += _ => { Debug.Log("[Input] W"); OnSwitchUp?.Invoke(); };
        actionSwitchDown.performed += _ => { Debug.Log("[Input] S"); OnSwitchDown?.Invoke(); };
        actionHit.performed += _ => { Debug.Log("[Input] Space"); OnHit?.Invoke(); };
    }

    private void OnEnable() { actionSwitchUp.Enable(); actionSwitchDown.Enable(); actionHit.Enable(); }
    private void OnDisable() { actionSwitchUp.Disable(); actionSwitchDown.Disable(); actionHit.Disable(); }
}

using System;
using UnityEngine;

public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject PausePanel; // ใช้เป็นเมนูหลักเมื่อกด Esc
    [SerializeField] private GameObject optionPanel; // สำหรับ OptionPanel ที่สร้างเพิ่ม

    private GameObject currentlyActiveMenu = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SwitchTo(null);
    }

    private void OnEnable()
    {
        InputManager.EscPressed += HandleEsc;
    }

    private void OnDisable()
    {
        InputManager.EscPressed -= HandleEsc;
    }

    private void HandleEsc()
    {
        if (currentlyActiveMenu != null) CloseAllMenus();
        else SwitchWithKeyTo(PausePanel);
    }

    public void SwitchWithKeyTo(GameObject _menu)
    {
        if (_menu != null && _menu.activeSelf) {CloseAllMenus();return;}
        SwitchTo(_menu);
    }

    public void SwitchTo(GameObject _menu)
    {
        // ปิดเมนูเก่าทั้งหมด (ปิด children ใต้ UI)
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        currentlyActiveMenu = _menu;

        if (_menu != null)
        {
            _menu.SetActive(true);
            GameManager.Instance?.PauseGame();
        }
        else
        {
            currentlyActiveMenu = null;
            GameManager.Instance?.ResumeGame();
        }
    }

    public void CloseAllMenus()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        currentlyActiveMenu = null;
        GameManager.Instance?.ResumeGame();
    }

    public void SetPausePanel(GameObject go) => PausePanel = go;
}

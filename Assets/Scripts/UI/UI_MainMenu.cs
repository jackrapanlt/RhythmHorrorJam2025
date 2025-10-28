using UnityEngine;

public class UI_MainMenu : MonoBehaviour
{
    [Header("ลากวัตถุที่ต้องการเปิด/ปิดลงช่องนี้")]
    [SerializeField] private GameObject soundSettingPanel;

    // ปุ่ม: เปิด
    public void ShowSoundSetting()
    {
        if (!soundSettingPanel)
        {
            Debug.LogWarning("[UI_MainMenu] ยังไม่ได้ลาก Sound Setting Panel ใส่ช่อง");
            return;
        }
        soundSettingPanel.SetActive(true);
    }

    // ปุ่ม: ปิด
    public void HideSoundSetting()
    {
        if (!soundSettingPanel)
        {
            Debug.LogWarning("[UI_MainMenu] ยังไม่ได้ลาก Sound Setting Panel ใส่ช่อง");
            return;
        }
        soundSettingPanel.SetActive(false);
    }
}

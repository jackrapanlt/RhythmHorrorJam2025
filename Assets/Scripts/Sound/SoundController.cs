using UnityEngine;
using UnityEngine.UI;

public class SoundController : MonoBehaviour
{
    public Slider _musicSlider, _sfxSlider;

    private void Start()
    {
        // ซิงก์สไลเดอร์ให้ตรงกับค่าจริงจาก AudioManager ตอนเปิดหน้า/ซีน
        if (AudioManager.instance != null)
        {
            if (_musicSlider && AudioManager.instance.musicSource)
                _musicSlider.value = AudioManager.instance.musicSource.volume;

            if (_sfxSlider && AudioManager.instance.sfxSource)
                _sfxSlider.value = AudioManager.instance.sfxSource.volume;
        }
    }

    public void ToggleMusic()
    {
        AudioManager.instance.ToggleMusic();
    }

    public void ToggleSFX()
    {
        AudioManager.instance.ToggleSFX();
    }

    public void MusicVolume()
    {
        AudioManager.instance.MusicVolume(_musicSlider.value);
    }

    public void SFXVolume()
    {
        AudioManager.instance.SFXVolume(_sfxSlider.value);
    }
}

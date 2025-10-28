using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public Sound[] musicSounds, sfxSound;
    public AudioSource musicSource, sfxSource;

    // --- PlayerPrefs keys ---
    private const string KEY_MUSIC_VOL = "vol_music";
    private const string KEY_SFX_VOL = "vol_sfx";
    private const string KEY_MUSIC_MUTE = "mute_music";
    private const string KEY_SFX_MUTE = "mute_sfx";

    private void Start()
    {
        Playmusic("Battle");
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // โหลดค่าที่เคยบันทึกไว้ (ถ้าไม่มีจะใช้ค่าปัจจุบันของ AudioSource)
        if (musicSource)
        {
            musicSource.volume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, musicSource.volume);
            musicSource.mute = PlayerPrefs.GetInt(KEY_MUSIC_MUTE, 0) == 1;
        }
        if (sfxSource)
        {
            sfxSource.volume = PlayerPrefs.GetFloat(KEY_SFX_VOL, sfxSource.volume);
            sfxSource.mute = PlayerPrefs.GetInt(KEY_SFX_MUTE, 0) == 1;
        }
    }

    public void Playmusic(string name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }
        else
        {
            musicSource.clip = s.clip;
            musicSource.Play();
        }
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSound, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Secon Not Found");
        }
        else
        {
            sfxSource.PlayOneShot(s.clip);
        }
    }

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
        PlayerPrefs.SetInt(KEY_MUSIC_MUTE, musicSource.mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
        PlayerPrefs.SetInt(KEY_SFX_MUTE, sfxSource.mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void MusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, volume);
        PlayerPrefs.Save();
    }

    public void SFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat(KEY_SFX_VOL, volume);
        PlayerPrefs.Save();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("🎵 Sound Clips")]
    public AudioClip shootClip;
    public AudioClip bodyExplodeClip;
    public AudioClip slideBoxClip;
    public AudioClip coinClip;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip btnClip;
    public AudioClip unlockClip;
    public AudioClip backgroundMusic;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    public AudioClip helpSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.clip = backgroundMusic;
        musicSource.volume = 0.5f;
        musicSource.Play();
        LoadSettings();
    }

    /// <summary>
    /// play sfx
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// load sound settings
    /// </summary>
    private void LoadSettings()
    {
        SetMusic(PlayerPrefs.GetInt("MusicOn", 1) == 1);
        SetSound(PlayerPrefs.GetInt("SoundOn", 1) == 1);
        SetVibration(PlayerPrefs.GetInt("VibrationOn", 1) == 1);
    }

    /// <summary>
    /// toggle sound
    /// </summary>
    public void ToggleSound()
    {
        bool isOn = !IsSoundOn();
        SetSound(isOn);
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// toggle music
    /// </summary>
    public void ToggleMusic()
    {
        bool isOn = !IsMusicOn();
        SetMusic(isOn);
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// toggle vibration
    /// </summary>
    public void ToggleVibration()
    {
        bool isOn = !IsVibrationOn();
        SetVibration(isOn);
        PlayerPrefs.SetInt("VibrationOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set music state directly
    /// </summary>
    public void SetMusicState(bool isOn)
    {
        SetMusic(isOn);
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set sound state directly
    /// </summary>
    public void SetSoundState(bool isOn)
    {
        SetSound(isOn);
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set vibration state directly
    /// </summary>
    public void SetVibrationState(bool isOn)
    {
        SetVibration(isOn);
        PlayerPrefs.SetInt("VibrationOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// check sound status
    /// </summary>
    public bool IsSoundOn() => PlayerPrefs.GetInt("SoundOn", 1) == 1;

    /// <summary>
    /// check music status
    /// </summary>
    public bool IsMusicOn() => PlayerPrefs.GetInt("MusicOn", 1) == 1;

    /// <summary>
    /// check vibration status
    /// </summary>
    public bool IsVibrationOn() => PlayerPrefs.GetInt("VibrationOn", 1) == 1;

    /// <summary>
    /// set music
    /// </summary>
    private void SetMusic(bool isOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !isOn;
        }
    }

    /// <summary>
    /// set sound
    /// </summary>
    private void SetSound(bool isOn)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !isOn;
        }
    }

    /// <summary>
    /// set vibration
    /// </summary>
    private void SetVibration(bool isOn)
    {
        // Store vibration state for later use
        // Actual vibration will be triggered when needed
        if (isOn)
        {
            // Vibration is enabled, can be used when needed
            Debug.Log("Vibration enabled");
        }
        else
        {
            // Vibration is disabled
            Debug.Log("Vibration disabled");
        }
    }

    /// <summary>
    /// Trigger vibration if enabled
    /// </summary>
    public void TriggerVibration()
    {
        if (!IsVibrationOn())
            return;

        // UnityEngine.Handheld 在 WebGL（抖音小游戏）平台下整个类都不存在，
        // 直接调用会让 player build 编译失败（error CS0103: 名称"Handheld"不存在），
        // 所以只在原生平台编译这段；小游戏端的振动请走 VibrationHelper / TTSDK
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    internal void PlaySFX(object helpSound, float v)
    {
        throw new NotImplementedException();
    }
}
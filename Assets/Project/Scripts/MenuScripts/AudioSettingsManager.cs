using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;
    
    [Header("Volume Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI ambientVolumeLabel;
    
    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string AMBIENT_VOLUME_KEY = "AmbientVolume";
    
    // AudioMixer parameter names (AudioMixer'da bu isimlerle exposed olmalı)
    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string AMBIENT_PARAM = "AmbientVolume";
    
    private void Start()
    {
        LoadSettings();
        
        // Slider listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (ambientVolumeSlider != null)
            ambientVolumeSlider.onValueChanged.AddListener(SetAmbientVolume);
    }
    
    public void SetMasterVolume(float value)
    {
        SetVolume(MASTER_PARAM, value);
        UpdateLabel(masterVolumeLabel, value);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
    }
    
    public void SetMusicVolume(float value)
    {
        SetVolume(MUSIC_PARAM, value);
        UpdateLabel(musicVolumeLabel, value);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
    }
    
    public void SetAmbientVolume(float value)
    {
        SetVolume(AMBIENT_PARAM, value);
        UpdateLabel(ambientVolumeLabel, value);
        PlayerPrefs.SetFloat(AMBIENT_VOLUME_KEY, value);
    }
    
    private void SetVolume(string parameter, float sliderValue)
{
    float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
    
    Debug.Log($"[AudioSettings] Setting {parameter} to {dB} dB (slider: {sliderValue})");
    
    if (audioMixer != null)
    {
        bool success = audioMixer.SetFloat(parameter, dB);
        Debug.Log($"[AudioSettings] SetFloat success: {success}");
    }
    else
    {
        Debug.LogError("[AudioSettings] AudioMixer is NULL!");
    }
}
    
    private void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            label.text = $"{percentage}%";
        }
    }
    
    public void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.8f);
        float music = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
        float ambient = PlayerPrefs.GetFloat(AMBIENT_VOLUME_KEY, 0.8f);
        
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = master;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = music;
        
        if (ambientVolumeSlider != null)
            ambientVolumeSlider.value = ambient;
        
        // Apply to mixer
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetAmbientVolume(ambient);
    }
    
    public void ResetToDefault()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = 0.8f;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 0.7f;
        
        if (ambientVolumeSlider != null)
            ambientVolumeSlider.value = 0.8f;
    }
    
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}
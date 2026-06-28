using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    private const string SoundEnabledKey = "SoundEnabled";
    private const string BgmEnabledKey = "BgmEnabled";

    public static AudioSettingsManager Instance { get; private set; }

    [Header("Optional Manual References")]
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Button bgmToggleButton;
    [SerializeField] private TMP_Text soundToggleText;
    [SerializeField] private TMP_Text bgmToggleText;

    [Header("Optional Audio Sources")]
    [SerializeField] private AudioSource[] soundEffectSources;
    [SerializeField] private AudioSource[] bgmSources;

    [Header("Gameplay Sound Effects")]
    [SerializeField] private AudioClip placeClip;
    [SerializeField] private AudioClip clearClip;
    [SerializeField] private AudioClip bombClip;

    [Header("Auto Discovery")]
    [SerializeField] private string soundEffectPlayerName = "SoundEffectPlayer";
    [SerializeField] private string backgroundMusicPlayerName = "BackgroundMusicPlayer";

    [Header("Button Visuals")]
    [SerializeField] private Color enabledButtonColor = new(0.98f, 0.77f, 0.26f, 1.0f);
    [SerializeField] private Color disabledButtonColor = new(0.45f, 0.29f, 0.20f, 1.0f);
    [SerializeField] private Color enabledTextColor = new(0.31f, 0.18f, 0.10f, 1.0f);
    [SerializeField] private Color disabledTextColor = Color.white;

    private readonly List<AudioSource> runtimeSoundSources = new();
    private readonly List<AudioSource> runtimeBgmSources = new();

    private bool soundEnabled = true;
    private bool bgmEnabled = true;

    public bool IsSoundEnabled => soundEnabled;
    public bool IsBgmEnabled => bgmEnabled;

    public static bool GetStoredSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
    }

    public static bool GetStoredBgmEnabled()
    {
        return PlayerPrefs.GetInt(BgmEnabledKey, 1) == 1;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CacheSceneReferences();
        CacheAudioSources();
        BindButtons();
        LoadSettings();
        ApplySettings();
        RefreshToggleUi();
    }

    private void OnEnable()
    {
        CacheSceneReferences();
        CacheAudioSources();
        BindButtons();
        ApplySettings();
        RefreshToggleUi();
    }

    public void RegisterSoundSource(AudioSource source)
    {
        RegisterSource(runtimeSoundSources, source);
        ApplySoundSourceState(source, soundEnabled);
    }

    public void RegisterBgmSource(AudioSource source)
    {
        RegisterSource(runtimeBgmSources, source);
        ApplyBgmSourceState(source, bgmEnabled);
    }

    public void UnregisterSoundSource(AudioSource source)
    {
        runtimeSoundSources.Remove(source);
    }

    public void UnregisterBgmSource(AudioSource source)
    {
        runtimeBgmSources.Remove(source);
    }

    public void ToggleSound()
    {
        SetSoundEnabled(soundEnabled == false);
    }

    public void ToggleBgm()
    {
        SetBgmEnabled(bgmEnabled == false);
    }

    public void SetSoundEnabled(bool value)
    {
        soundEnabled = value;
        SaveSettings();
        ApplySoundSources();
        RefreshToggleUi();
    }

    public void SetBgmEnabled(bool value)
    {
        bgmEnabled = value;
        SaveSettings();
        ApplyBgmSources();
        RefreshToggleUi();
    }

    public void PlaySound(AudioClip clip, float volumeScale = 1.0f)
    {
        if (soundEnabled == false || clip == null)
        {
            return;
        }

        var source = GetPrimarySoundSource();
        if (source == null)
        {
            return;
        }

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayPlaceSound()
    {
        PlaySound(placeClip);
    }

    public void PlayClearSound()
    {
        PlaySound(clearClip);
    }

    public void PlayBombSound()
    {
        PlaySound(bombClip);
    }

    private void LoadSettings()
    {
        soundEnabled = GetStoredSoundEnabled();
        bgmEnabled = GetStoredBgmEnabled();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(SoundEnabledKey, soundEnabled ? 1 : 0);
        PlayerPrefs.SetInt(BgmEnabledKey, bgmEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        ApplySoundSources();
        ApplyBgmSources();
    }

    private void ApplySoundSources()
    {
        CacheAudioSources();
        ApplySoundSourceGroup(soundEffectSources, runtimeSoundSources, soundEnabled);
    }

    private void ApplyBgmSources()
    {
        CacheAudioSources();
        ApplyBgmSourceGroup(bgmSources, runtimeBgmSources, bgmEnabled);
    }

    private void ApplySoundSourceGroup(IEnumerable<AudioSource> serializedSources, IEnumerable<AudioSource> runtimeSources, bool enabled)
    {
        if (serializedSources != null)
        {
            foreach (var source in serializedSources)
            {
                ApplySoundSourceState(source, enabled);
            }
        }

        foreach (var source in runtimeSources)
        {
            ApplySoundSourceState(source, enabled);
        }
    }

    private void ApplyBgmSourceGroup(IEnumerable<AudioSource> serializedSources, IEnumerable<AudioSource> runtimeSources, bool enabled)
    {
        if (serializedSources != null)
        {
            foreach (var source in serializedSources)
            {
                ApplyBgmSourceState(source, enabled);
            }
        }

        foreach (var source in runtimeSources)
        {
            ApplyBgmSourceState(source, enabled);
        }
    }

    private void ApplySoundSourceState(AudioSource source, bool enabled)
    {
        if (source == null)
        {
            return;
        }

        source.mute = enabled == false;
    }

    private void ApplyBgmSourceState(AudioSource source, bool enabled)
    {
        if (source == null)
        {
            return;
        }

        source.mute = enabled == false;

        if (enabled == false)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }

            return;
        }

        if (source.clip == null || source.isPlaying)
        {
            return;
        }

        if (source.timeSamples > 0)
        {
            source.UnPause();
            return;
        }

        source.Play();
    }

    private void RefreshToggleUi()
    {
        RefreshButton(soundToggleButton, soundToggleText, soundEnabled);
        RefreshButton(bgmToggleButton, bgmToggleText, bgmEnabled);
    }

    private void RefreshButton(Button button, TMP_Text label, bool enabled)
    {
        if (label != null)
        {
            label.text = enabled ? "ON" : "OFF";
            label.color = enabled ? enabledTextColor : disabledTextColor;
        }

        if (button != null && button.image != null)
        {
            button.image.color = enabled ? enabledButtonColor : disabledButtonColor;
        }
    }

    private void BindButtons()
    {
        BindButton(soundToggleButton, ToggleSound);
        BindButton(bgmToggleButton, ToggleBgm);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void CacheSceneReferences()
    {
        if (soundToggleButton == null)
        {
            soundToggleButton = FindChildComponent<Button>(transform, "SoundToggleButton");
        }

        if (bgmToggleButton == null)
        {
            bgmToggleButton = FindChildComponent<Button>(transform, "BgmToggleButton");
        }

        if (soundToggleText == null)
        {
            soundToggleText = FindChildComponent<TMP_Text>(transform, "SoundToggleText");
        }

        if (bgmToggleText == null)
        {
            bgmToggleText = FindChildComponent<TMP_Text>(transform, "BgmToggleText");
        }
    }

    private void CacheAudioSources()
    {
        soundEffectSources = FilterNullSources(soundEffectSources);
        bgmSources = FilterNullSources(bgmSources);

        runtimeSoundSources.RemoveAll(source => source == null);
        runtimeBgmSources.RemoveAll(source => source == null);

        if (soundEffectSources.Length == 0 && string.IsNullOrWhiteSpace(soundEffectPlayerName) == false)
        {
            var soundEffectPlayer = GameObject.Find(soundEffectPlayerName);
            if (soundEffectPlayer != null)
            {
                var soundSource = soundEffectPlayer.GetComponent<AudioSource>();
                if (soundSource != null)
                {
                    soundEffectSources = new[] { soundSource };
                }
            }
        }

        if (bgmSources.Length > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(backgroundMusicPlayerName))
        {
            return;
        }

        var backgroundMusicPlayer = GameObject.Find(backgroundMusicPlayerName);
        if (backgroundMusicPlayer == null)
        {
            return;
        }

        var source = backgroundMusicPlayer.GetComponent<AudioSource>();
        if (source != null)
        {
            bgmSources = new[] { source };
        }
    }

    private void RegisterSource(List<AudioSource> targetList, AudioSource source)
    {
        if (source == null || targetList.Contains(source))
        {
            return;
        }

        targetList.Add(source);
    }

    private AudioSource GetPrimarySoundSource()
    {
        CacheAudioSources();

        if (soundEffectSources != null)
        {
            foreach (var source in soundEffectSources)
            {
                if (source != null)
                {
                    return source;
                }
            }
        }

        foreach (var source in runtimeSoundSources)
        {
            if (source != null)
            {
                return source;
            }
        }

        return null;
    }

    private AudioSource[] FilterNullSources(AudioSource[] sources)
    {
        if (sources == null || sources.Length == 0)
        {
            return System.Array.Empty<AudioSource>();
        }

        var filteredSources = new List<AudioSource>(sources.Length);
        foreach (var source in sources)
        {
            if (source != null && filteredSources.Contains(source) == false)
            {
                filteredSources.Add(source);
            }
        }

        return filteredSources.ToArray();
    }

    private T FindChildComponent<T>(Transform parent, string name) where T : Component
    {
        var child = FindNamedTransformRecursive(parent, name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindNamedTransformRecursive(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child != parent && child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}

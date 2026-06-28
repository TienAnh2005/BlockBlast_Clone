using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourceRegistration : MonoBehaviour
{
    public enum AudioChannel
    {
        SoundEffect,
        BackgroundMusic
    }

    [SerializeField] private AudioChannel channel = AudioChannel.SoundEffect;
    [SerializeField] private bool registerOnEnable = true;

    private AudioSource cachedAudioSource;
    private bool hasStarted;

    private void Awake()
    {
        cachedAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        hasStarted = true;
        if (registerOnEnable)
        {
            Register();
        }
    }

    private void OnEnable()
    {
        if (hasStarted && registerOnEnable)
        {
            Register();
        }
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    public void Register()
    {
        if (cachedAudioSource == null)
        {
            cachedAudioSource = GetComponent<AudioSource>();
        }

        if (cachedAudioSource == null || AudioSettingsManager.Instance == null)
        {
            return;
        }

        switch (channel)
        {
            case AudioChannel.BackgroundMusic:
                AudioSettingsManager.Instance.RegisterBgmSource(cachedAudioSource);
                break;
            default:
                AudioSettingsManager.Instance.RegisterSoundSource(cachedAudioSource);
                break;
        }
    }

    public void Unregister()
    {
        if (cachedAudioSource == null || AudioSettingsManager.Instance == null)
        {
            return;
        }

        switch (channel)
        {
            case AudioChannel.BackgroundMusic:
                AudioSettingsManager.Instance.UnregisterBgmSource(cachedAudioSource);
                break;
            default:
                AudioSettingsManager.Instance.UnregisterSoundSource(cachedAudioSource);
                break;
        }
    }
}

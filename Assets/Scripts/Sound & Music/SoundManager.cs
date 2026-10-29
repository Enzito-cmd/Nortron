using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Volumes")]
    [Range(0f, 1f), SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f), SerializeField] private float sfxVolume = 0.8f;

    [Header("Music Clips")]
    [SerializeField] private AudioClip lobbyMusic;
    [SerializeField] private AudioClip raceMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip victorySound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicAudioSource != null) musicAudioSource.volume = musicVolume;
        if (sfxAudioSource != null) sfxAudioSource.volume = sfxVolume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == NetworkManager.LobbySceneIndex || scene.name == "MainMenu")
        {
            PlayMusic(lobbyMusic);
        }
        else if (scene.buildIndex == NetworkManager.NewRaceSceneIndex)
        {
            PlayMusic(raceMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicAudioSource == null || clip == null) return;

        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying) return;

        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    public void PlayCountdownSound()
    {
        if (sfxAudioSource != null && countdownSound != null)
        {
            sfxAudioSource.PlayOneShot(countdownSound, sfxVolume);
        }
    }

    public void PlayVictorySound()
    {
        if (sfxAudioSource != null && victorySound != null)
        {
            sfxAudioSource.PlayOneShot(victorySound, sfxVolume);
        }
    }
}
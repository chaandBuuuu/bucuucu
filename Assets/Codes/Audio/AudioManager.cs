using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ✅ Audio Manager cho background music+SFX
/// - Hỗ trợ music cho Lobby, Racing, Victory
/// - Fade in/out transitions
/// - 1 master volume control
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum GameState { Lobby, Racing, Victory, Menu }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip lobbyMusic;
    [SerializeField] private AudioClip racingMusic;
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip menuMusic;

    [Header("Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float fadeDuration = 1f;

    private GameState _currentState = GameState.Menu;
    private float _fadeTimer = 0f;
    private bool _isFading = false;
    private AudioClip _targetClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Tạo audio sources nếu chưa có
        if (musicSource == null)
        {
            var musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        if (sfxSource == null)
        {
            var sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
        }

        // Subscribe to game events
        var raceManager = RaceManager.Instance;
        if (raceManager != null)
        {
            raceManager.OnRaceStart += () => ChangeMusic(GameState.Racing);
            raceManager.OnRaceEnd += (winner) => ChangeMusic(GameState.Victory);
        }
    }

    private void Update()
    {
        if (_isFading)
        {
            _fadeTimer += Time.deltaTime;
            float progress = _fadeTimer / fadeDuration;

            if (progress >= 1f)
            {
                _isFading = false;
                _fadeTimer = 0f;
                musicSource.volume = musicVolume;
                if (_targetClip != null)
                {
                    musicSource.clip = _targetClip;
                    musicSource.Play();
                }
            }
            else
            {
                musicSource.volume = Mathf.Lerp(musicVolume, 0, progress);
            }
        }
    }

    public void ChangeMusic(GameState state)
    {
        if (_currentState == state || _isFading) return;

        _currentState = state;
        AudioClip clip = state switch
        {
            GameState.Menu => menuMusic,
            GameState.Lobby => lobbyMusic,
            GameState.Racing => racingMusic,
            GameState.Victory => victoryMusic,
            _ => null
        };

        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Không có music clip cho state {state}");
            return;
        }

        PlayMusicWithFade(clip);
        Debug.Log($"[AudioManager] 🎵 Changing to {state} music");
    }

    private void PlayMusicWithFade(AudioClip clip)
    {
        _targetClip = clip;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}

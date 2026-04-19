using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ✅ Audio Manager cho background music+SFX
/// - Tự động trigger nhạc khi vào Menu/Lobby/Racing/Victory
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

    private GameState _currentState = GameState.Victory;  // Initialize thành state khác để force play lần đầu
    private float _fadeTimer = 0f;
    private bool _isFading = false;
    private AudioClip _targetClip;

    /// <summary>
    /// ✅ NEW: Tự động tạo AudioManager nếu chưa tồn tại
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;

        var existingAM = FindAnyObjectByType<AudioManager>();
        if (existingAM != null)
        {
            Instance = existingAM;
            return;
        }

        // Tạo mới nếu chưa có
        var go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        Debug.Log("[AudioManager] ✅ Created new AudioManager instance");
    }

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
            musicSource.playOnAwake = false;
            Debug.Log($"[AudioManager] ✅ Created musicSource (volume: {musicSource.volume})");
        }

        if (sfxSource == null)
        {
            var sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
            sfxSource.playOnAwake = false;
            // ✅ Đảm bảo SFX source là 2D audio
            sfxSource.spatialBlend = 0f;
            Debug.Log($"[AudioManager] ✅ Created sfxSource (volume: {sfxSource.volume})");
        }

        Debug.Log($"[AudioManager] ✅ Initialized | Music: {(musicSource != null ? "✓" : "✗")} | SFX: {(sfxSource != null ? "✓" : "✗")}");

        // ✅ NEW: Auto load music clips từ Resources nếu chưa assign
        TryLoadMusicClips();

        // ✅ NEW: Listen to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Subscribe to game events
        var raceManager = RaceManager.Instance;
        if (raceManager != null)
        {
            raceManager.OnRaceStart += () => ChangeMusic(GameState.Racing);
            raceManager.OnRaceEnd += (winner) => ChangeMusic(GameState.Victory);
            Debug.Log("[AudioManager] ✅ Subscribed to RaceManager events");
        }
        else
        {
            Debug.LogWarning("[AudioManager] ⚠️ RaceManager not found");
        }

        // ✅ NEW: Trigger music cho scene hiện tại
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// ✅ NEW: Auto load music clips từ Resources/Audio folder
    /// </summary>
    private void TryLoadMusicClips()
    {
        // Nếu đã assign, không cần load
        if (menuMusic != null && lobbyMusic != null && racingMusic != null && victoryMusic != null)
            return;

        // Load từ Resources/Audio/
        if (menuMusic == null)
        {
            menuMusic = Resources.Load<AudioClip>("Audio/menu");
            if (menuMusic == null) Debug.LogWarning("[AudioManager] ⚠️ Không tìm thấy Audio/menu");
        }
        
        if (lobbyMusic == null)
        {
            lobbyMusic = Resources.Load<AudioClip>("Audio/menu");
            if (lobbyMusic == null) Debug.LogWarning("[AudioManager] ⚠️ Không tìm thấy Audio/menu");
        }
        
        if (racingMusic == null)
        {
            racingMusic = Resources.Load<AudioClip>("Audio/gameplay");
            if (racingMusic == null) Debug.LogWarning("[AudioManager] ⚠️ Không tìm thấy Audio/gameplay");
        }
        
        if (victoryMusic == null)
        {
            victoryMusic = Resources.Load<AudioClip>("Audio/gameplay");
            if (victoryMusic == null) Debug.LogWarning("[AudioManager] ⚠️ Không tìm thấy Audio/gameplay");
        }

        Debug.Log($"[AudioManager] 📁 Loaded music clips | Menu: {(menuMusic != null ? "✓" : "✗")} | Lobby: {(lobbyMusic != null ? "✓" : "✗")} | Racing: {(racingMusic != null ? "✓" : "✗")} | Victory: {(victoryMusic != null ? "✓" : "✗")}");
    }

    /// <summary>
    /// ✅ NEW: Auto trigger music khi vào scene
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] 🎬 OnSceneLoaded triggered | Scene: {scene.name}");

        // Map scene name → music state
        GameState targetState = scene.name.ToLower() switch
        {
            "menu" or "main menu" => GameState.Menu,
            "lobby" or "1_lobby" => GameState.Lobby,
            "gameplay" or "2_racing" or "racing" => GameState.Racing,  // Racing khi vào gameplay
            _ => GameState.Menu
        };

        Debug.Log($"[AudioManager] 📍 Mapped scene '{scene.name}' → {targetState}");

        // ✅ NEW: Re-subscribe tới RaceManager.OnRaceStart khi vào Gameplay scene
        if (targetState == GameState.Racing)
        {
            var raceManager = RaceManager.Instance;
            if (raceManager != null)
            {
                // Unsubscribe cái cũ trước (nếu có)
                raceManager.OnRaceStart -= () => ChangeMusic(GameState.Racing);
                // Subscribe lại
                raceManager.OnRaceStart += () => ChangeMusic(GameState.Racing);
                raceManager.OnRaceEnd -= (winner) => ChangeMusic(GameState.Victory);
                raceManager.OnRaceEnd += (winner) => ChangeMusic(GameState.Victory);
                Debug.Log("[AudioManager] ✅ Re-subscribed to RaceManager events");
            }
            else
            {
                Debug.LogWarning("[AudioManager] ⚠️ RaceManager.Instance is null in GamePlay scene");
            }
        }

        // Trigger music ngay khi vào scene
        ChangeMusic(targetState);
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
                if (musicSource != null)
                {
                    musicSource.volume = musicVolume;
                    if (_targetClip != null)
                    {
                        musicSource.clip = _targetClip;
                        musicSource.Play();
                        Debug.Log($"[AudioManager] ▶️ Now playing: {_targetClip.name} (volume: {musicSource.volume})");
                    }
                }
            }
            else
            {
                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(musicVolume, 0, progress);
            }
        }
    }

    public void ChangeMusic(GameState state)
    {
        Debug.Log($"[AudioManager] 🎬 ChangeMusic called | Current: {_currentState} | Target: {state} | IsFading: {_isFading} | musicVolume: {musicVolume}");
        
        if (_currentState == state || _isFading)
        {
            Debug.Log($"[AudioManager] ⚠️ ChangeMusic skipped (same state or fading)");
            return;
        }

        _currentState = state;
        AudioClip clip = state switch
        {
            GameState.Menu => menuMusic,
            GameState.Lobby => lobbyMusic,
            GameState.Racing => racingMusic,
            GameState.Victory => victoryMusic,
            _ => null
        };

        Debug.Log($"[AudioManager] 🔍 Resolved clip for {state}: {(clip != null ? clip.name : "NULL")}");

        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] ⚠️ Không có music clip cho state {state}");
            return;
        }

        PlayMusicWithFade(clip);
        Debug.Log($"[AudioManager] 🎵 Changing to {state} music | Clip: {clip.name}");
    }

    private void PlayMusicWithFade(AudioClip clip)
    {
        if (musicSource == null)
        {
            Debug.LogError("[AudioManager] ❌ PlayMusicWithFade: musicSource is null!");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("[AudioManager] ❌ PlayMusicWithFade: clip is null!");
            return;
        }

        Debug.Log($"[AudioManager] 🎵 Starting fade for clip: {clip.name}");
        _targetClip = clip;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] ⚠️ PlaySFX: clip is null");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogError("[AudioManager] ❌ PlaySFX: sfxSource is null!");
            return;
        }

        if (sfxSource.volume <= 0)
        {
            Debug.LogWarning($"[AudioManager] ⚠️ PlaySFX: sfxSource volume is {sfxSource.volume} (muted?)");
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
        Debug.Log($"[AudioManager] ✅ Playing SFX: {clip.name} (volume: {sfxVolume})");
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

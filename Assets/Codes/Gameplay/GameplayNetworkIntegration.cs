using UnityEngine;
using Fusion;
using System;

/// <summary>
/// Extended Network Manager - Integration point for gameplay systems
/// Kết nối Gameplay System với existing network infrastructure
/// </summary>
public class GameplayNetworkManager : MonoBehaviour
{
    public static GameplayNetworkManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NetworkRunner networkRunner;
    [SerializeField] private GameplayStateManager gameplayStateManager;
    [SerializeField] private GameStartController gameStartController;

    [Header("Lobby")]
    [SerializeField] private LobbyCharacterSelectManager lobbyUI;

    [Header("Gameplay")]
    [SerializeField] private GameplayUIManager gameplayUI;
    [SerializeField] private GameEndUIManager gameEndUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (networkRunner == null)
            networkRunner = FindAnyObjectByType<NetworkRunner>();

        Debug.Log("[GameplayNetworkManager] Initialized");
    }

    /// <summary>
    /// Gọi khi player vào lobby
    /// </summary>
    public void OnPlayerEnteredLobby(PlayerRef player)
    {
        Debug.Log($"[GameplayNetworkManager] Player {player} entered lobby");
        
        // Hiển thị character selection UI
        if (lobbyUI != null)
            lobbyUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Gọi khi toàn bộ player sẵn sàng với character selection
    /// </summary>
    public void OnGameStart()
    {
        Debug.Log("[GameplayNetworkManager] Game starting");
        
        // Ẩn lobby UI
        if (lobbyUI != null)
            lobbyUI.gameObject.SetActive(false);

        // Hiển thị gameplay UI
        if (gameplayUI != null)
            gameplayUI.gameObject.SetActive(true);

        // Kích hoạt game state
        if (gameplayStateManager != null)
            gameplayStateManager.RPC_StartGame();
    }

    /// <summary>
    /// Gọi khi game kết thúc
    /// </summary>
    public void OnGameEnd(GameWinner winner)
    {
        Debug.Log($"[GameplayNetworkManager] Game ended. Winner: {winner}");

        // Hiển thị game end UI
        if (gameEndUI != null)
            gameEndUI.gameObject.SetActive(true);

        // Dừng gameplay
        if (gameplayUI != null)
            gameplayUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// Helper: Tìm character hiện tại của local player
    /// </summary>
    public NetworkCharacterController GetLocalCharacter()
    {
        var allCharacters = FindObjectsByType<NetworkCharacterController>();
        foreach (var character in allCharacters)
        {
            if (character.HasInputAuthority)
                return character;
        }
        return null;
    }

    /// <summary>
    /// Helper: Tìm tất cả hunter
    /// </summary>
    public NetworkCharacterController[] GetAllHunters()
    {
        var allCharacters = FindObjectsByType<NetworkCharacterController>();
        var hunters = new System.Collections.Generic.List<NetworkCharacterController>();

        foreach (var character in allCharacters)
        {
            if (character.IsHunter)
                hunters.Add(character);
        }

        return hunters.ToArray();
    }

    /// <summary>
    /// Helper: Tìm tất cả survivor
    /// </summary>
    public NetworkCharacterController[] GetAllSurvivors()
    {
        var allCharacters = FindObjectsByType<NetworkCharacterController>();
        var survivors = new System.Collections.Generic.List<NetworkCharacterController>();

        foreach (var character in allCharacters)
        {
            if (character.IsSurvivor)
                survivors.Add(character);
        }

        return survivors.ToArray();
    }
}

/// <summary>
/// Integration helper - Add vào FusionNetworkManager
/// </summary>
public partial class FusionNetworkManager
{
    private GameplayNetworkManager _gameplayNetworkManager;

    // Thêm vào Awake hoặc Start:
    public void InitializeGameplaySystem()
    {
        _gameplayNetworkManager = FindAnyObjectByType<GameplayNetworkManager>();
        
        if (_gameplayNetworkManager == null)
        {
            Debug.LogError("[FusionNetworkManager] GameplayNetworkManager not found in scene!");
        }
    }

    // Thêm vào OnPlayerJoined callback:
    public void OnGameplayPlayerJoined(PlayerRef player)
    {
        Debug.Log($"[FusionNetworkManager.OnGameplayPlayerJoined] Player {player}");
        
        _gameplayNetworkManager?.OnPlayerEnteredLobby(player);
    }

    // Thêm vào OnPlayerLeft callback:
    public void OnGameplayPlayerLeft(PlayerRef player)
    {
        Debug.Log($"[FusionNetworkManager.OnGameplayPlayerLeft] Player {player}");
        
        // Xử lý player disconnect
        var character = FindCharacterForPlayer(player);
        if (character != null)
        {
            Destroy(character.gameObject);
        }
    }

    private NetworkCharacterController FindCharacterForPlayer(PlayerRef player)
    {
        var allCharacters = FindObjectsByType<NetworkCharacterController>();
        foreach (var character in allCharacters)
        {
            if (character.Object.InputAuthority == player)
                return character;
        }
        return null;
    }
}

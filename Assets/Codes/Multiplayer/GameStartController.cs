using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

/// <summary>
/// Quản lý start race - chờ tất cả player sẵn sàng
/// </summary>
public class GameStartController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int requiredPlayers = 4;
    [SerializeField] private float checkInterval = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startRaceButton;

    [Networked] private int ReadyCount { get; set; }
    [Networked] private bool RaceStarting { get; set; }
    private bool _isSpawned = false;
    private float _lastCheck = 0f;

    public override void Spawned()
    {
        _isSpawned = true;
        Debug.Log("[GameStartController] Chờ player sẵn sàng...");
        
        if (startRaceButton != null)
        {
            startRaceButton.onClick.AddListener(OnStartRaceClicked);
            bool isHost = HasStateAuthority;
            startRaceButton.gameObject.SetActive(isHost);
        }
        Debug.Log("[GameStartController] Spawned!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer || RaceStarting) return;
        if (Runner.SimulationTime - _lastCheck < checkInterval) return;
        _lastCheck = Runner.SimulationTime;
        CheckIfCanStart();
    }

    private void Update()
    {
        if (!_isSpawned) return;  // ✅ FIX: Guard against pre-Spawn access to [Networked] properties
        if (statusText != null)
            statusText.text = $"Sẵn sàng: {ReadyCount}/{requiredPlayers}";
    }

    private void CheckIfCanStart()
    {
        int playerCount = 0;
        foreach (var _ in Runner.ActivePlayers) playerCount++;

        int readyCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject obj))
            {
                readyCount++;
            }
        }

        ReadyCount = readyCount;
        Debug.Log($"[GameStartController] Players: {playerCount}/{requiredPlayers}, Ready: {readyCount}");
    }

    private void OnStartRaceClicked()
    {
        if (!HasStateAuthority) 
        {
            Debug.LogWarning("Chỉ Host mới được bắt đầu game!");
            return;
        }

        // Kiểm tra tất cả player đã ready chưa
        int readyCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            readyCount++;
        }

        if (readyCount < requiredPlayers)
        {
            Debug.LogWarning($"Cần ít nhất {requiredPlayers} người sẵn sàng. Hiện có {readyCount}");
            return;
        }

        Debug.Log("[GameStartController] Host bắt đầu game → Load Racing Scene");
        Runner.LoadScene(SceneRef.FromIndex(2));   // Racing Scene Index = 2
    }

    private void LoadRacingScene()
    {
        // Load the racing scene (adjust index if needed)
        Runner.LoadScene(SceneRef.FromIndex(2));
    }
}
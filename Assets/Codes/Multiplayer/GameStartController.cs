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

    private float _lastCheck = 0f;

    public override void Spawned()
    {
        Debug.Log("[GameStartController] Chờ player sẵn sàng...");
        
        if (startRaceButton != null)
        {
            startRaceButton.onClick.AddListener(OnStartRaceClicked);
            bool isHost = HasStateAuthority;
            startRaceButton.gameObject.SetActive(isHost);
        }
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
        if (!HasStateAuthority) return;
        
        int playerCount = 0;
        foreach (var _ in Runner.ActivePlayers) playerCount++;

        if (playerCount < requiredPlayers)
        {
            Debug.LogWarning($"[GameStartController] Cần {requiredPlayers} player, hiện có {playerCount}");
            return;
        }

        RaceStarting = true;
        Debug.Log("[GameStartController] Bắt đầu race!");
        LoadRacingScene();
    }

    private void LoadRacingScene()
    {
        // Load the racing scene (adjust index if needed)
        Runner.LoadScene(SceneRef.FromIndex(2));
    }
}

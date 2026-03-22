using UnityEngine;
using TMPro;
using Fusion;

public class GameStartController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int   requiredPlayers = 4;
    [SerializeField] private float checkInterval   = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [Networked] private int  ReadyCount   { get; set; }
    [Networked] private bool GameStarting { get; set; }

    private float _lastCheck = 0f;

    public override void Spawned()
        => Debug.Log("[GameStartController] Chờ tất cả player...");

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer || GameStarting) return;
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
            if (Runner.TryGetPlayerObject(player, out NetworkObject obj))
                if (obj.GetComponent<LobbyPlayerController>() != null)
                    readyCount++;

        ReadyCount = readyCount;
        Debug.Log($"[GameStartController] Players: {playerCount}, Ready: {readyCount}");

        if (playerCount >= requiredPlayers && readyCount >= requiredPlayers)
        {
            GameStarting = true;
            Debug.Log("[GameStartController] Bắt đầu game!");
            Runner.LoadScene(SceneRef.FromIndex(2));
        }
    }
}
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-start game khi tất cả 4 player sẵn sàng
/// Đơn giản: kiểm tra số player + tất cả đã chọn character
/// </summary>
public class GameStartController : MonoBehaviourPun
{
    [Header("Game Start Config")]
    [SerializeField] private int requiredPlayersToStart = 4;
    [SerializeField] private float checkInterval = 1f;

    [Header("UI")]
    [SerializeField] private Text statusText;

    private float lastCheckTime = 0f;
    private bool gameStarted = false;

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[GameStartController] Not in a room!");
            return;
        }

        Debug.Log("[GameStartController] Waiting for all players to be ready...");
    }

    private void Update()
    {
        if (gameStarted)
            return;

        // Check mỗi 1 giây
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            CheckIfCanStart();
        }
    }

    private void CheckIfCanStart()
    {
        // Return if not in room yet
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            Debug.Log("[GameStartController] Waiting to join room...");
            return;
        }

        // Kiểm tra số lượng player
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        // Kiểm tra tất cả đã chọn character
        bool allSelectedCharacter = CheckAllPlayersSelectedCharacter();

        string status = $"Ready: {playerCount}/{requiredPlayersToStart}";

        if (statusText != null)
            statusText.text = status;

        Debug.Log($"[GameStartController] Players: {playerCount}, All selected: {allSelectedCharacter}");

        // Điều kiện start:
        // - Có đủ 4 người
        // - Tất cả đã chọn character
        if (playerCount >= requiredPlayersToStart && allSelectedCharacter)
        {
            Debug.Log("[GameStartController] All ready! Starting game...");
            StartGameScene();
        }
    }

    private bool CheckAllPlayersSelectedCharacter()
    {
        if (!PhotonNetwork.InRoom)
            return false;

        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            // Check if player has CharacterIndex property
            if (!player.CustomProperties.ContainsKey("CharacterIndex"))
            {
                return false;
            }
        }

        return true;
    }

    private void StartGameScene()
    {
        if (gameStarted)
            return;

        gameStarted = true;
        Debug.Log("[GameStartController] Loading GameScene...");
        PhotonNetwork.LoadLevel("GameScene");
    }
}

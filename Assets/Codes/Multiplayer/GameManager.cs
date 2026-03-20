using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

/// <summary>
/// Quản lý trạng thái game chính
/// - Đồng bộ trạng thái game
/// - Quản lý pause/resume
/// - Quản lý UI trong game
/// - Xử lý game over và kết thúc
/// </summary>
public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private bool isGameActive = true;
    [SerializeField] private int alivePlayers = 4;

    [Header("UI")]
    [SerializeField] private Text playerCountText;
    [SerializeField] private Text gameStatusText;
    [SerializeField] private Canvas gameUI;

    [Header("Network")]
    [SerializeField] private float networkSyncInterval = 1f;
    private float lastNetworkSync = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Debug.Log("[GameManager] Game bắt đầu!");
        UpdatePlayerCount();
        UpdateGameStatus();

        // Tối ưu network settings
        PhotonNetwork.SendRate = 60; // 60 messages per second
        PhotonNetwork.SerializationRate = 60; // 60 serializations per second
    }

    private void Update()
    {
        // Cập nhật network sync
        if (Time.time - lastNetworkSync >= networkSyncInterval)
        {
            lastNetworkSync = Time.time;
            SyncGameState();
        }

        // Kiểm tra input tạm dừng
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    /// <summary>Cập nhật số lượng player còn lại</summary>
    private void UpdatePlayerCount()
    {
        if (PhotonNetwork.InRoom)
        {
            int activePlayers = CountActivePlayers();
            playerCountText.text = $"Player: {activePlayers}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
        }
    }

    /// <summary>Đếm số lượng player còn sống</summary>
    private int CountActivePlayers()
    {
        return PhotonNetwork.CurrentRoom.Players.Count;
    }

    /// <summary>Cập nhật trạng thái game</summary>
    private void UpdateGameStatus()
    {
        if (isGameActive)
        {
            gameStatusText.text = "Game - Đang chơi";
            gameStatusText.color = Color.green;
        }
        else
        {
            gameStatusText.text = "Game - Tạm dừng";
            gameStatusText.color = Color.yellow;
        }
    }

    /// <summary>Đồng bộ trạng thái game trên network</summary>
    private void SyncGameState()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        ExitGames.Client.Photon.Hashtable gameProps = new ExitGames.Client.Photon.Hashtable
        {
            { "activePlayers", CountActivePlayers() },
            { "gameActive", isGameActive }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(gameProps);
    }

    /// <summary>Tạm dừng/tiếp tục game</summary>
    public void TogglePause()
    {
        isGameActive = !isGameActive;
        Time.timeScale = isGameActive ? 1f : 0f;
        UpdateGameStatus();

        // Sync trạng thái pause tới tất cả player
        photonView.RPC("RPC_SetPause", RpcTarget.AllBuffered, !isGameActive);
    }

    [PunRPC]
    private void RPC_SetPause(bool paused)
    {
        isGameActive = !paused;
        Time.timeScale = isGameActive ? 1f : 0f;
        UpdateGameStatus();
    }

    /// <summary>Một player bị loại (chết/bị đuổi)</summary>
    public void OnPlayerEliminated(string playerName)
    {
        Debug.Log($"[GameManager] {playerName} đã bị loại!");

        alivePlayers--;
        UpdatePlayerCount();

        // Kiểm tra nếu chỉ còn 1 player
        if (alivePlayers <= 1)
        {
            EndGame();
        }
    }

    /// <summary>Kết thúc game</summary>
    public void EndGame()
    {
        isGameActive = false;
        gameStatusText.text = "Game - Kết thúc!";
        gameStatusText.color = Color.red;

        Debug.Log("[GameManager] Game kết thúc!");

        // Cho phép quay lại lobby sau 3 giây
        Invoke(nameof(ReturnToLobby), 3f);
    }

    /// <summary>Quay lại lobby</summary>
    private void ReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("LobbyScene"); // Tên scene lobby
    }

    /// <summary>Lấy trạng thái game</summary>
    public bool IsGameActive => isGameActive;
}

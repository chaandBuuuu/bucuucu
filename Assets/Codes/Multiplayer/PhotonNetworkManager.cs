using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Quản lý kết nối Photon Network cho game multiplayer
/// Xử lý: khởi tạo kết nối, tạo/join room, sync cài đặt network
/// </summary>
public class PhotonNetworkManager : MonoBehaviourPun
{
    public static PhotonNetworkManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private int maxPlayersPerRoom = 4;
    [SerializeField] private int maxRoomsOnServer = 100;

    [Header("Connection Settings")]
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private int connectionTimeoutMs = 5000;

    private string playerNickname;
    private string selectedCharacter;
    private int selectedCharacterIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Cấu hình Photon
        PhotonNetwork.GameVersion = gameVersion;

        if (autoConnect && !PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }
    }

    /// <summary>Kết nối tới Photon Cloud</summary>
    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
            return;

        Debug.Log("[NetworkManager] Đang kết nối tới Photon Cloud...");
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>Tạo Room mới (Host)</summary>
    public void CreateRoom(string roomName = null)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("[NetworkManager] Chưa kết nối tới Photon!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        string finalRoomName = string.IsNullOrEmpty(roomName) ? 
            "Room_" + Random.Range(1000, 9999) : roomName;

        PhotonNetwork.CreateRoom(finalRoomName, roomOptions, TypedLobby.Default);
        Debug.Log($"[NetworkManager] Tạo Room: {finalRoomName}");
    }

    /// <summary>Join vào Room có sẵn</summary>
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("[NetworkManager] Chưa kết nối tới Photon!");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[NetworkManager] Đang join Room: {roomName}");
    }

    /// <summary>Join random room</summary>
    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("[NetworkManager] Chưa kết nối tới Photon!");
            return;
        }

        PhotonNetwork.JoinRandomRoom();
        Debug.Log("[NetworkManager] Đang join random room...");
    }

    /// <summary>Rời khỏi Room hiện tại</summary>
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    /// <summary>Lấy danh sách Room từ Lobby</summary>
    public void GetRoomList()
    {
        // Đảm bảo đang ở Lobby
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    /// <summary>Thiết lập nickname người chơi</summary>
    public void SetPlayerNickname(string nickname)
    {
        playerNickname = nickname;
        PhotonNetwork.LocalPlayer.NickName = nickname;
        Debug.Log($"[NetworkManager] Đặt tên: {nickname}");
    }

    /// <summary>Thiết lập nhân vật được chọn</summary>
    public void SetSelectedCharacter(int characterIndex)
    {
        selectedCharacterIndex = characterIndex;
        selectedCharacter = GetCharacterName(characterIndex);
        
        // Gửi lên props của Player
        ExitGames.Client.Photon.Hashtable playerProps = new Hashtable
        {
            { "characterIndex", characterIndex },
            { "characterName", selectedCharacter },
            { "playerReady", true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
        
        Debug.Log($"[NetworkManager] Chọn nhân vật: {selectedCharacter} (Index: {characterIndex})");
    }

    /// <summary>Lấy tên nhân vật từ index</summary>
    private string GetCharacterName(int index)
    {
        return index switch
        {
            0 => "Hacker",
            1 => "Ghost_Hunter", 
            2 => "Priest",
            3 => "Scientist",
            _ => "Unknown"
        };
    }

    /// <summary>Kiểm tra tất cả player đã sẵn sàng chưa</summary>
    public bool AllPlayersReady()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.Players.Count < maxPlayersPerRoom)
            return false;

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.CustomProperties == null || 
                !player.CustomProperties.ContainsKey("playerReady") ||
                !(bool)player.CustomProperties["playerReady"])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Kiểm tra room có đủ 4 người chưa</summary>
    public bool IsRoomFull()
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Players.Count >= maxPlayersPerRoom;
    }

    public int MaxPlayers => maxPlayersPerRoom;
    public string SelectedCharacter => selectedCharacter;
    public int SelectedCharacterIndex => selectedCharacterIndex;
    public string PlayerNickname => playerNickname;
}

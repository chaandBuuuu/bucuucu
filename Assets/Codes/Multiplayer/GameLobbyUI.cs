using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Quản lý giao diện Lobby đơn giản
/// - Chọn/Tạo phòng
/// - Chọn nhân vật (4 lựa chọn)
/// </summary>
public class GameLobbyUI : MonoBehaviourPunCallbacks
{
    [Header("UI References - Room Selection")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private InputField roomNameInput;

    [Header("UI References - Character Select")]
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private Button[] characterButtons = new Button[4];
    [SerializeField] private Text selectedCharacterText;

    [Header("Character Data")]
    private string[] characterNameData = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private Color[] characterColors = { Color.red, Color.green, Color.yellow, new Color(0, 0, 1) };
    private int selectedCharacterIndex = -1;

    [Header("Status")]
    [SerializeField] private Text statusText;

    private void Start()
    {
        InitializeUI();
        ConnectToPhoton();
    }

    private void InitializeUI()
    {
        // Room Panel
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Character Select Panel
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        ShowRoomPanel();
    }

    private void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[GameLobbyUI] Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ==================== Panel Management ====================

    private void ShowRoomPanel()
    {
        roomPanel.SetActive(true);
        characterSelectPanel.SetActive(false);
    }

    private void ShowCharacterSelectPanel()
    {
        Debug.Log("[GameLobbyUI] Showing CharacterSelectPanel");
        
        if (characterSelectPanel == null)
        {
            Debug.LogError("[GameLobbyUI] characterSelectPanel is NOT assigned in Inspector!");
            return;
        }
        
        roomPanel.SetActive(false);
        characterSelectPanel.SetActive(true);
        Debug.Log("[GameLobbyUI] CharacterSelectPanel is now active");
        UpdateCharacterSelectUI();
    }

    // ==================== Room Management ====================

    private void OnHostClicked()
    {
        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
            roomName = "Room_" + Random.Range(1000, 9999);

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        UpdateStatus($"Creating room: {roomName}...");
    }

    private void OnJoinClicked()
    {
        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("Please enter room name!");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
        UpdateStatus($"Joining room: {roomName}...");
    }

    // ==================== Character Selection ====================

    private void SelectCharacter(int characterIndex)
    {
        selectedCharacterIndex = characterIndex;
        
        // Set player properties
        Hashtable playerProperties = new Hashtable
        {
            { "CharacterIndex", characterIndex }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        UpdateCharacterSelectUI();
        UpdateStatus($"Selected: {characterNameData[characterIndex]}");
    }

    private void UpdateCharacterSelectUI()
    {
        if (selectedCharacterIndex >= 0)
        {
            selectedCharacterText.text = $"Chon: {characterNameData[selectedCharacterIndex]}";
            selectedCharacterText.color = characterColors[selectedCharacterIndex];
        }
    }

    // ==================== Photon Callbacks ====================

    public override void OnConnected()
    {
        Debug.Log("[GameLobbyUI] Connected to Photon");
        UpdateStatus("Connected! Create or join a room");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[GameLobbyUI] Disconnected: {cause}");
        UpdateStatus($"Disconnected: {cause}");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[GameLobbyUI] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        UpdateStatus($"Room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/4)");
        ShowCharacterSelectPanel();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[GameLobbyUI] Failed to join room: {message}");
        UpdateStatus($"Failed to join: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[GameLobbyUI] Failed to create room: {message}");
        UpdateStatus($"Failed to create: {message}");
        ShowRoomPanel();
    }

    private void UpdateStatus(string message)
    {
        Debug.Log($"[GameLobbyUI] {message}");
        if (statusText != null)
            statusText.text = message;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ✅ NEW: Displays available rooms in the menu
/// - Uses Fusion's session list
/// - Shows room info and player count
/// - Manual refresh
/// </summary>
public class RoomListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private int maxRoomsToDisplay = 10;

    private RoomItemUI _selectedRoom = null;
    private List<RoomItemUI> _roomItems = new List<RoomItemUI>();
    private List<SessionInfo> _cachedSessions = new List<SessionInfo>();

    private void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
            joinButton.interactable = false;  // Disabled until room selected
        }

        // Register for session updates
        RegisterSessionUpdateCallbacks();

        // Initial refresh
        OnRefreshClicked();
    }

    private void OnDestroy()
    {
        UnregisterSessionUpdateCallbacks();
    }

    private void RegisterSessionUpdateCallbacks()
    {
        // ✅ UPDATED: Listen to SessionDiscoveryManager instead
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.OnSessionListUpdatedEvent += OnDiscoverySessionListUpdated;
        }

        // Also listen to FusionNetworkManager for join events
        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.OnJoinedSessionEvent += OnJoinedSession;
            FusionNetworkManager.Instance.OnJoinFailedEvent += OnJoinFailed;
        }
    }

    private void UnregisterSessionUpdateCallbacks()
    {
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.OnSessionListUpdatedEvent -= OnDiscoverySessionListUpdated;
        }

        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.OnJoinedSessionEvent -= OnJoinedSession;
            FusionNetworkManager.Instance.OnJoinFailedEvent -= OnJoinFailed;
        }
    }

    /// ✅ NEW: Called when SessionDiscoveryManager updates the session list
    private void OnDiscoverySessionListUpdated(List<SessionInfo> sessions)
    {
        Debug.Log($"[RoomListUI] Received updated session list: {sessions.Count} sessions");
        RefreshRoomList(sessions);
    }

    private void OnRefreshClicked()
    {
        if (statusText != null)
            statusText.text = "🔄 Đang tải danh sách phòng...";

        // ✅ UPDATED: Get sessions from SessionDiscoveryManager
        if (SessionDiscoveryManager.Instance == null)
        {
            if (statusText != null)
                statusText.text = "❌ SessionDiscoveryManager chưa khởi tạo!";
            return;
        }

        var sessions = SessionDiscoveryManager.Instance.GetDiscoveredSessions();
        RefreshRoomList(sessions);
    }

    /// ✅ NEW: Helper to refresh room list display
    private void RefreshRoomList(List<SessionInfo> sessions)
    {
        // Clear existing items
        foreach (var item in _roomItems)
        {
            Destroy(item.gameObject);
        }
        _roomItems.Clear();
        _selectedRoom = null;

        if (statusText != null)
            statusText.text = "🔄 Đang tải danh sách phòng...";

        if (joinButton != null)
            joinButton.interactable = false;

        if (sessions == null || sessions.Count == 0)
        {
            if (statusText != null)
                statusText.text = "📭 Chưa có phòng nào";
            return;
        }

        int displayCount = Mathf.Min(sessions.Count, maxRoomsToDisplay);
        for (int i = 0; i < displayCount; i++)
        {
            CreateRoomItem(sessions[i]);
        }

        if (statusText != null)
            statusText.text = $"✅ Tìm thấy {displayCount} phòng";
    }

    private void CreateRoomItem(SessionInfo sessionInfo)
    {
        if (roomItemPrefab == null)
        {
            Debug.LogError("[RoomListUI] roomItemPrefab not assigned!");
            return;
        }

        GameObject itemGO = Instantiate(roomItemPrefab, roomListContainer);
        RoomItemUI item = itemGO.GetComponent<RoomItemUI>();
        
        if (item == null)
        {
            item = itemGO.AddComponent<RoomItemUI>();  // Auto-add if missing
        }

        item.Initialize(sessionInfo, OnRoomSelected);
        _roomItems.Add(item);
    }

    private void OnRoomSelected(RoomItemUI roomItem)
    {
        // Deselect previous
        if (_selectedRoom != null && _selectedRoom != roomItem)
            _selectedRoom.SetSelected(false);

        _selectedRoom = roomItem;
        _selectedRoom.SetSelected(true);

        if (joinButton != null)
            joinButton.interactable = true;

        Debug.Log($"[RoomListUI] Selected room: {roomItem.SessionName}");
    }

    private async void OnJoinClicked()
    {
        if (_selectedRoom == null)
        {
            if (statusText != null)
                statusText.text = "❌ Chọn phòng trước!";
            return;
        }

        // ✅ NEW: Validate player name before joining
        var lobbyUI = FindObjectOfType<GameLobbyUI>();
        if (lobbyUI != null && !lobbyUI.ValidatePlayerNamePublic())
        {
            if (statusText != null)
                statusText.text = "❌ Vui lòng nhập tên người chơi!";
            Debug.LogError("[RoomListUI] Cannot join without valid player name");
            return;
        }

        SetButtonsInteractable(false);

        if (statusText != null)
            statusText.text = $"🚪 Đang vào {_selectedRoom.SessionName}...";

        // ✅ UPDATED: Stop discovery before joining
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }

        // ✅ NEW: Store player name before joining
        if (lobbyUI != null)
        {
            string playerName = lobbyUI.GetCurrentPlayerName();
            if (FusionNetworkManager.Instance != null)
            {
                FusionNetworkManager.Instance.SetStoredPlayerName(playerName);
            }
        }

        await FusionNetworkManager.Instance.JoinSession(_selectedRoom.SessionName);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (refreshButton != null) refreshButton.interactable = value;
        if (joinButton != null) joinButton.interactable = (_selectedRoom != null) && value;
    }

    private void OnJoinedSession()
    {
        if (statusText != null)
            statusText.text = "✅ Đã vào phòng!";
    }

    private void OnJoinFailed(string reason)
    {
        SetButtonsInteractable(true);
        if (statusText != null)
            statusText.text = $"❌ Thất bại: {reason}";
    }
}

/// <summary>
/// ✅ Individual room item in list
/// </summary>
public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text sessionNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image backgroundImage;

    private SessionInfo _sessionInfo;
    private System.Action<RoomItemUI> _onSelected;
    private bool _isSelected = false;

    private Color _normalColor = new Color(1, 1, 1, 0.7f);
    private Color _selectedColor = new Color(0, 1, 1, 1f);

    public string SessionName => _sessionInfo?.Name ?? "Unknown";

    public void Initialize(SessionInfo sessionInfo, System.Action<RoomItemUI> onSelected)
    {
        _sessionInfo = sessionInfo;
        _onSelected = onSelected;

        UpdateDisplay();

        if (selectButton != null)
            selectButton.onClick.AddListener(OnClicked);
    }

    private void UpdateDisplay()
    {
        if (_sessionInfo == null) return;

        // Display room name
        if (sessionNameText != null)
            sessionNameText.text = $"🏠 {_sessionInfo.Name}";

        // Display player count
        if (playerCountText != null)
            playerCountText.text = $"👥 {_sessionInfo.PlayerCount}/{_sessionInfo.MaxPlayers}";
    }

    private void OnClicked()
    {
        _onSelected?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? _selectedColor : _normalColor;
        }
    }
}

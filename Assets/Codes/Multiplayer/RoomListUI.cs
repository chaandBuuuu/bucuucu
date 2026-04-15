using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// FIXED:
///   - Refresh button chỉ làm mới danh sách, KHÔNG join
///   - Join button chỉ hiện/active khi đã chọn room
///   - Tách rõ OnRefreshClicked vs OnJoinClicked
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

    private void Start()
    {
        // FIX: Setup buttons trước, KHÔNG gọi OnRefreshClicked() trong Start
        // vì SessionDiscoveryManager chưa chắc đã có dữ liệu
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
            joinButton.interactable = false; // Disabled cho đến khi chọn room
        }

        // Register callbacks để nhận update tự động
        RegisterSessionUpdateCallbacks();

        // Hiện trạng thái chờ ban đầu
        UpdateStatus("🔍 Đang tìm phòng...");
    }

    private void OnDestroy()
    {
        UnregisterSessionUpdateCallbacks();
    }

    private void RegisterSessionUpdateCallbacks()
    {
        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.OnSessionListUpdatedEvent += OnDiscoverySessionListUpdated;

        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.OnJoinedSessionEvent += OnJoinedSession;
            FusionNetworkManager.Instance.OnJoinFailedEvent += OnJoinFailed;
        }
    }

    private void UnregisterSessionUpdateCallbacks()
    {
        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.OnSessionListUpdatedEvent -= OnDiscoverySessionListUpdated;

        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.OnJoinedSessionEvent -= OnJoinedSession;
            FusionNetworkManager.Instance.OnJoinFailedEvent -= OnJoinFailed;
        }
    }

    /// <summary>
    /// Được gọi tự động khi SessionDiscoveryManager có session mới
    /// </summary>
    private void OnDiscoverySessionListUpdated(List<SessionInfo> sessions)
    {
        Debug.Log($"[RoomListUI] Session list updated: {sessions.Count} sessions");
        RefreshRoomListDisplay(sessions);
    }

    /// <summary>
    /// FIX: Refresh CHỈ làm mới danh sách hiển thị từ discovery cache
    /// KHÔNG tự động join bất cứ thứ gì
    /// </summary>
    private void OnRefreshClicked()
    {
        UpdateStatus("🔄 Đang làm mới danh sách...");

        if (SessionDiscoveryManager.Instance == null)
        {
            UpdateStatus("❌ SessionDiscoveryManager chưa khởi tạo!");
            return;
        }

        // Chỉ lấy cached sessions và hiển thị lại — KHÔNG join
        var sessions = SessionDiscoveryManager.Instance.GetDiscoveredSessions();
        RefreshRoomListDisplay(sessions);
    }

    /// <summary>
    /// Làm mới UI danh sách phòng
    /// </summary>
    private void RefreshRoomListDisplay(List<SessionInfo> sessions)
    {
        // Xóa items cũ
        foreach (var item in _roomItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _roomItems.Clear();

        // FIX: Reset selected room và disable join button
        _selectedRoom = null;
        if (joinButton != null)
            joinButton.interactable = false;

        if (sessions == null || sessions.Count == 0)
        {
            UpdateStatus("📭 Chưa có phòng nào. Nhấn Refresh để tìm lại.");
            return;
        }

        int displayCount = Mathf.Min(sessions.Count, maxRoomsToDisplay);
        for (int i = 0; i < displayCount; i++)
            CreateRoomItem(sessions[i]);

        UpdateStatus($"✅ Tìm thấy {displayCount} phòng — chọn phòng rồi nhấn Join");
    }

    private void CreateRoomItem(SessionInfo sessionInfo)
    {
        if (roomItemPrefab == null || roomListContainer == null)
        {
            Debug.LogError("[RoomListUI] roomItemPrefab hoặc roomListContainer chưa được gán!");
            return;
        }

        GameObject itemGO = Instantiate(roomItemPrefab, roomListContainer);
        RoomItemUI item = itemGO.GetComponent<RoomItemUI>();
        if (item == null)
            item = itemGO.AddComponent<RoomItemUI>();

        item.Initialize(sessionInfo, OnRoomSelected);
        _roomItems.Add(item);
    }

    /// <summary>
    /// Được gọi khi người dùng click vào 1 room item
    /// FIX: Chỉ select room, KHÔNG tự động join
    /// </summary>
    private void OnRoomSelected(RoomItemUI roomItem)
    {
        // Deselect cái cũ
        if (_selectedRoom != null && _selectedRoom != roomItem)
            _selectedRoom.SetSelected(false);

        _selectedRoom = roomItem;
        _selectedRoom.SetSelected(true);

        // FIX: Chỉ enable Join button sau khi chọn room — KHÔNG join ngay
        if (joinButton != null)
            joinButton.interactable = true;

        UpdateStatus($"✅ Đã chọn: {roomItem.SessionName} — Nhấn Join để vào");
        Debug.Log($"[RoomListUI] Selected room: {roomItem.SessionName}");
    }

    /// <summary>
    /// FIX: Join chỉ được gọi khi người dùng bấm nút Join
    /// </summary>
    private async void OnJoinClicked()
    {
        if (_selectedRoom == null)
        {
            UpdateStatus("❌ Chọn phòng trước khi Join!");
            return;
        }

        // Validate player name
        var lobbyUI = FindObjectOfType<GameLobbyUI>();
        if (lobbyUI != null && !lobbyUI.ValidatePlayerNamePublic())
        {
            UpdateStatus("❌ Vui lòng nhập tên người chơi trước!");
            return;
        }

        if (FusionNetworkManager.Instance == null)
        {
            UpdateStatus("❌ FusionNetworkManager không tồn tại!");
            return;
        }

        SetButtonsInteractable(false);
        UpdateStatus($"🚪 Đang vào phòng '{_selectedRoom.SessionName}'...");

        // Stop discovery trước khi join
        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.StopDiscovery();

        // Lưu tên người chơi
        if (lobbyUI != null)
        {
            string playerName = lobbyUI.GetCurrentPlayerName();
            FusionNetworkManager.Instance.SetStoredPlayerName(playerName);
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
        UpdateStatus("✅ Đã vào phòng!");
    }

    private void OnJoinFailed(string reason)
    {
        SetButtonsInteractable(true);
        UpdateStatus($"❌ Vào phòng thất bại: {reason}");
    }

    private void UpdateStatus(string msg)
    {
        Debug.Log($"[RoomListUI] {msg}");
        if (statusText != null)
            statusText.text = msg;
    }
}

/// <summary>
/// Individual room item trong danh sách
/// </summary>
public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text sessionNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image backgroundImage;

    private SessionInfo _sessionInfo;
    private System.Action<RoomItemUI> _onSelected;

    private readonly Color _normalColor   = new Color(1f, 1f, 1f, 0.7f);
    private readonly Color _selectedColor = new Color(0f, 1f, 1f, 1f);

    public string SessionName => _sessionInfo?.Name ?? "Unknown";

    public void Initialize(SessionInfo sessionInfo, System.Action<RoomItemUI> onSelected)
    {
        _sessionInfo = sessionInfo;
        _onSelected  = onSelected;

        if (sessionNameText != null)
            sessionNameText.text = $"🏠 {_sessionInfo.Name}";

        if (playerCountText != null)
            playerCountText.text = $"👥 {_sessionInfo.PlayerCount}/{_sessionInfo.MaxPlayers}";

        if (selectButton != null)
            selectButton.onClick.AddListener(() => _onSelected?.Invoke(this));

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? _selectedColor : _normalColor;
    }
}
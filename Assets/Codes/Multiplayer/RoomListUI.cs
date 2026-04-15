using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;

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

    private async void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
            joinButton.interactable = false;
        }

        RegisterSessionUpdateCallbacks();
        UpdateStatus("🔍 Đang kết nối tìm phòng...");

        if (SessionDiscoveryManager.Instance != null)
            await SessionDiscoveryManager.Instance.StartDiscovery();
        else
            UpdateStatus("❌ SessionDiscoveryManager không tồn tại!");
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

    private void OnDiscoverySessionListUpdated(List<SessionInfo> sessions)
    {
        Debug.Log($"[RoomListUI] Session list updated: {sessions.Count} sessions");
        RefreshRoomListDisplay(sessions);
    }

    private async void OnRefreshClicked()
    {
        UpdateStatus("🔄 Đang làm mới danh sách...");

        if (SessionDiscoveryManager.Instance == null)
        {
            UpdateStatus("❌ SessionDiscoveryManager chưa khởi tạo!");
            return;
        }

        await SessionDiscoveryManager.Instance.StartDiscovery();
    }

    private void RefreshRoomListDisplay(List<SessionInfo> sessions)
    {
        foreach (var item in _roomItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _roomItems.Clear();

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

        // ✅ FIX: Lấy component có sẵn trên prefab, không AddComponent mới
        RoomItemUI item = itemGO.GetComponent<RoomItemUI>();
        if (item == null)
        {
            Debug.LogError("[RoomListUI] Prefab thiếu RoomItemUI component! Hãy add vào prefab trong Editor.");
            Destroy(itemGO);
            return;
        }

        item.Initialize(sessionInfo, OnRoomSelected);
        _roomItems.Add(item);
    }

    private void OnRoomSelected(RoomItemUI roomItem)
    {
        if (_selectedRoom != null && _selectedRoom != roomItem)
            _selectedRoom.SetSelected(false);

        _selectedRoom = roomItem;
        _selectedRoom.SetSelected(true);

        if (joinButton != null)
            joinButton.interactable = true;

        UpdateStatus($"✅ Đã chọn: {roomItem.SessionName} — Nhấn Join để vào");
    }

    private async void OnJoinClicked()
    {
        if (_selectedRoom == null)
        {
            UpdateStatus("❌ Chọn phòng trước khi Join!");
            return;
        }

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

        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.StopDiscovery();

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
/// Individual room item trong danh sách.
/// ✅ FIX: Dùng GetComponentInChildren thay vì [SerializeField] để không bị null
///         khi prefab chưa gán đủ refs trong Inspector.
/// </summary>
public class RoomItemUI : MonoBehaviour
{
    // Giữ [SerializeField] để vẫn gán được trong Inspector nếu muốn,
    // nhưng Awake sẽ tự tìm nếu null.
    [SerializeField] private TMP_Text sessionNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image backgroundImage;

    private SessionInfo _sessionInfo;
    private System.Action<RoomItemUI> _onSelected;

    private readonly Color _normalColor   = new Color(1f, 1f, 1f, 0.7f);
    private readonly Color _selectedColor = new Color(0f, 1f, 1f, 1f);

    public string SessionName => _sessionInfo?.Name ?? "Unknown";

    private void Awake()
    {
        // ✅ FIX: Tự tìm child components nếu chưa được gán trong Inspector
        if (selectButton == null)
            selectButton = GetComponentInChildren<Button>(includeInactive: true);

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // Tìm các TMP_Text theo thứ tự — index 0 = tên phòng, index 1 = số người
        if (sessionNameText == null || playerCountText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
            if (sessionNameText == null && texts.Length > 0) sessionNameText = texts[0];
            if (playerCountText  == null && texts.Length > 1) playerCountText  = texts[1];
        }

        // ✅ LOG để debug nếu vẫn còn null
        if (selectButton == null)
            Debug.LogError($"[RoomItemUI] '{gameObject.name}': Không tìm thấy Button! " +
                           "Hãy đảm bảo prefab có Button component.");
        if (sessionNameText == null)
            Debug.LogWarning($"[RoomItemUI] '{gameObject.name}': Không tìm thấy TMP_Text cho tên phòng.");
    }

    public void Initialize(SessionInfo sessionInfo, System.Action<RoomItemUI> onSelected)
    {
        _sessionInfo = sessionInfo;
        _onSelected  = onSelected;

        if (sessionNameText != null)
            sessionNameText.text = $"🏠 {_sessionInfo.Name}";

        if (playerCountText != null)
            playerCountText.text = $"👥 {_sessionInfo.PlayerCount}/{_sessionInfo.MaxPlayers}";

        if (selectButton != null)
        {
            // ✅ Xóa listener cũ trước để tránh đăng ký trùng nếu item bị reuse
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelected?.Invoke(this));
            Debug.Log($"[RoomItemUI] Listener đã đăng ký cho phòng: {_sessionInfo.Name}");
        }
        else
        {
            Debug.LogError($"[RoomItemUI] selectButton NULL — click sẽ không hoạt động cho '{_sessionInfo.Name}'!");
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? _selectedColor : _normalColor;
    }
}
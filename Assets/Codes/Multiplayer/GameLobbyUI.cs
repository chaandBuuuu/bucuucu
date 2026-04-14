using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅ UPDATED: Main menu with player name input + room listing
/// - Input player name (instead of server name)
/// - Show available rooms + join button
/// - Host current game button
/// </summary>
public class GameLobbyUI : MonoBehaviour
{
    [Header("Player Name Input")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text playerNameError;

    [Header("Room Listing")]
    [SerializeField] private RoomListUI roomListUI;

    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text statusText;

    [Header("References")]
    [SerializeField] private GameObject canvasToHide;

    private string _currentPlayerName = "";

    private void Start()
    {
        // Setup player name input
        if (playerNameError != null)
            playerNameError.gameObject.SetActive(false);

        // Auto-focus name input
        if (playerNameInput != null)
        {
            playerNameInput.text = "";
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
            playerNameInput.onSubmit.AddListener(_ => OnNameInputSubmitted());
        }

        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
            joinButton.interactable = false;  // Disabled until room selected
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        RegisterEvents();

        // ✅ NEW: Start session discovery
        StartSessionDiscovery();
    }

    private void OnDestroy()
    {
        UnregisterEvents();

        // ✅ NEW: Stop session discovery
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }
    }

    private void RegisterEvents()
    {
        if (FusionNetworkManager.Instance == null) return;
        FusionNetworkManager.Instance.OnJoinedSessionEvent += OnJoinedSession;
        FusionNetworkManager.Instance.OnJoinFailedEvent += OnJoinFailed;
    }

    private void UnregisterEvents()
    {
        if (FusionNetworkManager.Instance == null) return;
        FusionNetworkManager.Instance.OnJoinedSessionEvent -= OnJoinedSession;
        FusionNetworkManager.Instance.OnJoinFailedEvent -= OnJoinFailed;
    }

    /// ✅ NEW: Initialize session discovery to get available rooms
    private async void StartSessionDiscovery()
    {
        if (SessionDiscoveryManager.Instance == null)
        {
            Debug.LogWarning("[GameLobbyUI] SessionDiscoveryManager not found!");
            return;
        }

        UpdateStatus("🔍 Đang tìm phòng khả dụng...");
        await SessionDiscoveryManager.Instance.StartDiscovery();
    }

    private void OnNameInputSubmitted()
    {
        // After entering name, try to host
        OnHostClicked();
    }

    private bool ValidatePlayerName()
    {
        if (playerNameInput == null)
        {
            UpdateError("⚠️ Player name input not configured");
            return false;
        }

        _currentPlayerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(_currentPlayerName))
        {
            UpdateError("❌ Vui lòng nhập tên!");
            return false;
        }

        if (_currentPlayerName.Length < 2)
        {
            UpdateError("❌ Tên phải có ít nhất 2 ký tự!");
            return false;
        }

        if (_currentPlayerName.Length > 16)
        {
            UpdateError("❌ Tên không được quá 16 ký tự!");
            return false;
        }

        ClearError();
        return true;
    }

    private async void OnHostClicked()
    {
        if (!ValidatePlayerName()) return;

        // Store player name
        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.SetStoredPlayerName(_currentPlayerName);
        }

        // ✅ NEW: Stop discovery before hosting
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }

        string sessionName = _currentPlayerName + "_Room_" + Random.Range(1000, 9999);
        SetButtonsInteractable(false);
        UpdateStatus($"🎮 Tạo phòng '{sessionName}'...");

        if (FusionNetworkManager.Instance != null)
        {
            await FusionNetworkManager.Instance.CreateSession(sessionName);
        }
    }

    private async void OnJoinClicked()
    {
        if (!ValidatePlayerName()) return;

        // Store player name
        if (FusionNetworkManager.Instance != null)
        {
            FusionNetworkManager.Instance.SetStoredPlayerName(_currentPlayerName);
        }

        if (roomListUI == null)
        {
            UpdateError("❌ RoomListUI not configured");
            return;
        }

        SetButtonsInteractable(false);
        UpdateStatus("🚪 Đang vào phòng...");
    }

    private void OnRefreshClicked()
    {
        if (roomListUI != null)
        {
            // roomListUI will handle refresh
            Debug.Log("[GameLobbyUI] Refreshing room list (handled by RoomListUI)");
        }
    }

    private void OnJoinedSession()
    {
        UpdateStatus("✅ Đã vào phòng! Chuyển sang Lobby...");
        // Ẩn canvas menu
        if (canvasToHide != null)
            canvasToHide.SetActive(false);
    }

    private void OnJoinFailed(string reason)
    {
        UpdateStatus($"❌ Thất bại: {reason}");
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hostButton != null) hostButton.interactable = value;
        if (joinButton != null) joinButton.interactable = value && (roomListUI != null);
        if (refreshButton != null) refreshButton.interactable = value;
    }

    private void UpdateStatus(string msg)
    {
        Debug.Log($"[GameLobbyUI] {msg}");
        if (statusText != null)
            statusText.text = msg;
    }

    private void UpdateError(string msg)
    {
        if (playerNameError != null)
        {
            playerNameError.text = msg;
            playerNameError.gameObject.SetActive(true);
        }
    }

    private void ClearError()
    {
        if (playerNameError != null)
            playerNameError.gameObject.SetActive(false);
    }
}
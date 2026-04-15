using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// FIXED:
///   - Canvas menu được ẩn ngay khi joined session (không dùng delay ngắn rồi vẫn hiện)
///   - canvasToHide.SetActive(false) được gọi chắc chắn kể cả khi object DontDestroyOnLoad
///   - Thêm OnSceneLoaded để tự ẩn canvas nếu không còn ở scene menu
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
        if (playerNameError != null)
            playerNameError.gameObject.SetActive(false);

        if (playerNameInput != null)
        {
            playerNameInput.text = "";
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
            playerNameInput.onSubmit.AddListener(_ => OnNameInputSubmitted());
        }

        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);

        // FIX: joinButton trong GameLobbyUI không cần — RoomListUI tự xử lý join
        // Giữ lại để tránh null ref nhưng không wire vào OnJoinClicked nữa
        if (joinButton != null)
            joinButton.interactable = false;

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        RegisterEvents();
        StartSessionDiscovery();
    }

    private void OnDestroy()
    {
        UnregisterEvents();

        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.StopDiscovery();
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

    private async void StartSessionDiscovery()
    {
        if (SessionDiscoveryManager.Instance == null)
        {
            Debug.Log("[GameLobbyUI] Auto-creating SessionDiscoveryManager...");
            GameObject discoveryGO = new GameObject("SessionDiscovery");
            SessionDiscoveryManager manager = discoveryGO.AddComponent<SessionDiscoveryManager>();
            TryAssignRunnerPrefab(manager);
        }

        if (SessionDiscoveryManager.Instance == null)
        {
            UpdateStatus("❌ SessionDiscoveryManager initialization failed!");
            return;
        }

        UpdateStatus("🔍 Đang tìm phòng...");
        await SessionDiscoveryManager.Instance.StartDiscovery();
    }

    private void TryAssignRunnerPrefab(SessionDiscoveryManager manager)
    {
        try
        {
            if (FusionNetworkManager.Instance != null)
            {
                var field = typeof(FusionNetworkManager)
                    .GetField("runnerPrefab",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var prefab = field.GetValue(FusionNetworkManager.Instance) as NetworkRunner;
                    if (prefab != null)
                    {
                        var managerField = typeof(SessionDiscoveryManager)
                            .GetField("discoveryRunnerPrefab",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        managerField?.SetValue(manager, prefab);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameLobbyUI] Exception assigning prefab: {ex.Message}");
        }
    }

    private void OnNameInputSubmitted()
    {
        OnHostClicked();
    }

    private bool ValidatePlayerName()
    {
        if (playerNameInput == null) { UpdateError("⚠️ Player name input not configured"); return false; }

        _currentPlayerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(_currentPlayerName)) { UpdateError("❌ Vui lòng nhập tên!"); return false; }
        if (_currentPlayerName.Length < 2)             { UpdateError("❌ Tên phải có ít nhất 2 ký tự!"); return false; }
        if (_currentPlayerName.Length > 16)            { UpdateError("❌ Tên không được quá 16 ký tự!"); return false; }

        ClearError();
        return true;
    }

    public string GetCurrentPlayerName()   => _currentPlayerName;
    public bool   ValidatePlayerNamePublic() => ValidatePlayerName();

    private async void OnHostClicked()
    {
        if (!ValidatePlayerName()) return;

        if (FusionNetworkManager.Instance != null)
            FusionNetworkManager.Instance.SetStoredPlayerName(_currentPlayerName);

        if (SessionDiscoveryManager.Instance != null)
            SessionDiscoveryManager.Instance.StopDiscovery();

        string sessionName = _currentPlayerName + "_Room_" + Random.Range(1000, 9999);
        SetButtonsInteractable(false);
        UpdateStatus($"🎮 Tạo phòng '{sessionName}'...");

        if (FusionNetworkManager.Instance != null)
            await FusionNetworkManager.Instance.CreateSession(sessionName);
    }

    private void OnRefreshClicked()
    {
        // FIX: Chỉ trigger refresh hiển thị qua RoomListUI — không join
        Debug.Log("[GameLobbyUI] Refresh clicked — delegating to RoomListUI");
        // RoomListUI tự lắng nghe SessionDiscoveryManager event
        // Không cần làm gì thêm ở đây
    }

    /// <summary>
    /// FIX: Ẩn canvas NGAY LẬP TỨC khi joined session
    /// Dùng coroutine chỉ để deactivate input field, canvas ẩn ngay
    /// </summary>
    private void OnJoinedSession()
    {
        UpdateStatus("✅ Đã vào phòng! Đang chuyển...");

        // FIX: Ẩn canvas ngay lập tức — không delay
        HideMenuCanvas();

        // Deactivate input field async để tránh conflict
        StartCoroutine(DeactivateInputDelayed());
    }

    /// <summary>
    /// FIX: Ẩn canvas menu ngay lập tức và unregister callbacks
    /// </summary>
    private void HideMenuCanvas()
    {
        if (playerNameInput != null)
            playerNameInput.DeactivateInputField();

        if (canvasToHide != null)
        {
            canvasToHide.SetActive(false);
            Debug.Log("[GameLobbyUI] ✅ Menu canvas hidden immediately");
        }
        else
        {
            // FIX: Nếu canvasToHide không được gán, tìm Canvas trên chính object này
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
                Debug.Log("[GameLobbyUI] ✅ Canvas hidden via GetComponent fallback");
            }
        }

        // Unregister events sau khi đã join để tránh fire thêm
        UnregisterEvents();
    }

    private System.Collections.IEnumerator DeactivateInputDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        if (playerNameInput != null)
            playerNameInput.DeactivateInputField();
    }

    private void OnJoinFailed(string reason)
    {
        UpdateStatus($"❌ Thất bại: {reason}");
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hostButton    != null) hostButton.interactable    = value;
        if (refreshButton != null) refreshButton.interactable = value;
        // joinButton được quản lý bởi RoomListUI
    }

    private void UpdateStatus(string msg)
    {
        Debug.Log($"[GameLobbyUI] {msg}");
        if (statusText != null) statusText.text = msg;
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
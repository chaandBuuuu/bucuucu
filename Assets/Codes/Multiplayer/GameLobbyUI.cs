using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameLobbyUI : MonoBehaviour
{
    [Header("Room Panel")]
    [SerializeField] private Button         hostButton;
    [SerializeField] private Button         joinButton;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Text       statusText;

    [Header("References")]
    [SerializeField] private GameObject canvasToHide; // Kéo Canvas vào đây

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        RegisterEvents();
    }

    private void OnDestroy() => UnregisterEvents();

    private void RegisterEvents()
    {
        if (FusionNetworkManager.Instance == null) return;
        FusionNetworkManager.Instance.OnJoinedSessionEvent += OnJoinedSession;
        FusionNetworkManager.Instance.OnJoinFailedEvent    += OnJoinFailed;
    }

    private void UnregisterEvents()
    {
        if (FusionNetworkManager.Instance == null) return;
        FusionNetworkManager.Instance.OnJoinedSessionEvent -= OnJoinedSession;
        FusionNetworkManager.Instance.OnJoinFailedEvent    -= OnJoinFailed;
    }

    private async void OnHostClicked()
    {
        string name = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) name = "Room_" + Random.Range(1000, 9999);
        SetButtonsInteractable(false);
        UpdateStatus($"Đang tạo phòng: {name}...");
        await FusionNetworkManager.Instance.CreateSession(name);
    }

    private async void OnJoinClicked()
    {
        string name = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) { UpdateStatus("Vui lòng nhập tên phòng!"); return; }
        SetButtonsInteractable(false);
        UpdateStatus($"Đang vào phòng: {name}...");
        await FusionNetworkManager.Instance.JoinSession(name);
    }

    private void OnJoinedSession()
    {
        UpdateStatus("Đã vào phòng! Đang chuyển sang Lobby...");
        // Ẩn canvas Menu đi
        if (canvasToHide != null)
            canvasToHide.SetActive(false);
    }

    private void OnJoinFailed(string reason)
    {
        UpdateStatus($"Thất bại: {reason}");
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        hostButton.interactable = value;
        joinButton.interactable = value;
    }

    private void UpdateStatus(string msg)
    {
        Debug.Log($"[GameLobbyUI] {msg}");
        if (statusText != null) statusText.text = msg;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class LobbyCharacterSelectUI : MonoBehaviour
{
    [Header("Car Selection")]
    [SerializeField] private Button[] carButtons = new Button[4];
    [SerializeField] private TMP_Text selectedCarText;

    [Header("Ready Button")]
    [SerializeField] private Button readyButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Canvas Management")]
    [SerializeField] private GameObject lobbyCanvas;           // Kéo toàn bộ Canvas Lobby vào đây
    [SerializeField] private GameObject characterSelectPanel;  // Panel chứa nút chọn xe + Ready

    private readonly string[] carNames = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private readonly Color[] carColors = { Color.red, Color.green, Color.yellow, new Color(0f, 0.5f, 1f) };

    private int _selectedCarIndex = -1;
    private bool _isReady = false;

    private NetworkRunner _runner;

    private void Start()
    {
        _runner = FusionNetworkManager.Instance?.Runner;

        // Setup car buttons
        for (int i = 0; i < carButtons.Length; i++)
        {
            int idx = i;
            carButtons[i].onClick.AddListener(() => SelectCar(idx));
        }

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        // Hiển thị panel chọn xe
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(true);

        UpdateStatus("Chọn xe của bạn!");
    }

    private void SelectCar(int index)
    {
        _selectedCarIndex = index;

        if (selectedCarText != null)
        {
            selectedCarText.text = $"Xe: {carNames[index]}";
            selectedCarText.color = carColors[index];
        }

        UpdateStatus($"Đã chọn: {carNames[index]} - Nhấn Ready để xác nhận");
    }

    private void OnReadyClicked()
    {
        if (_selectedCarIndex < 0)
        {
            UpdateStatus("⚠️ Vui lòng chọn xe trước!");
            return;
        }

        if (_isReady) return;

        _isReady = true;

        // Gửi lựa chọn xe lên Server
        if (FusionNetworkManager.Instance?.Runner != null)
        {
            FusionNetworkManager.Instance.RPC_RegisterCarChoice(
                FusionNetworkManager.Instance.Runner.LocalPlayer, 
                _selectedCarIndex);
        }

        // Disable tương tác
        foreach (var btn in carButtons) btn.interactable = false;
        if (readyButton != null) readyButton.interactable = false;

        UpdateStatus($"✅ Sẵn sàng với {carNames[_selectedCarIndex]}!");

        // ẨN CANVAS / PANEL sau 0.8 giây
        Invoke(nameof(HideLobbyUI), 0.8f);

        // Nếu là Host → Tự động bắt đầu game (tùy chọn)
        if (_runner != null && _runner.IsServer)
        {
            Debug.Log("[LobbyCharacterSelectUI] Host đã ready - Có thể tự động start nếu đủ người");
            // Bạn có thể gọi GameStartController.OnStartRaceClicked() ở đây nếu muốn
        }
    }

    private void HideLobbyUI()
    {
        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(false);           // Ẩn toàn bộ Canvas Lobby
        else if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);  // Hoặc chỉ ẩn panel chọn xe

        Debug.Log("[LobbyCharacterSelectUI] Lobby UI đã được ẩn.");
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    // Public methods nếu cần kiểm tra từ script khác
    public bool IsReady() => _isReady;
    public int GetSelectedCarIndex() => _selectedCarIndex;
}
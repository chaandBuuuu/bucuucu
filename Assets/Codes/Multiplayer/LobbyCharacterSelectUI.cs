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

    [Header("UI Management")]
    [SerializeField] private GameObject lobbyCanvas;           // Kéo Canvas Lobby vào đây
    [SerializeField] private GameObject waitingForHostPanel;   // Panel hiện "Đang chờ Host bắt đầu"

    private readonly string[] carNames = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private readonly Color[] carColors = { Color.red, Color.green, Color.yellow, new Color(0f, 0.5f, 1f) };

    private int _selectedCarIndex = -1;
    private bool _isReady = false;

    private void Start()
    {
        for (int i = 0; i < carButtons.Length; i++)
        {
            int idx = i;
            carButtons[i].onClick.AddListener(() => SelectCar(idx));
        }

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        // Ẩn panel chờ Host lúc đầu
        if (waitingForHostPanel != null)
            waitingForHostPanel.SetActive(false);
    }

    private void SelectCar(int index)
    {
        _selectedCarIndex = index;
        if (selectedCarText != null)
        {
            selectedCarText.text = $"Xe: {carNames[index]}";
            selectedCarText.color = carColors[index];
        }
        UpdateStatus($"Đã chọn: {carNames[index]}");
    }

    private void OnReadyClicked()
    {
        if (_selectedCarIndex < 0)
        {
            UpdateStatus("⚠️ Vui lòng chọn xe trước!");
            return;
        }

        _isReady = true;

        // Gửi lựa chọn xe
        if (FusionNetworkManager.Instance?.Runner != null)
        {
            FusionNetworkManager.Instance.RPC_RegisterCarChoice(
                FusionNetworkManager.Instance.Runner.LocalPlayer, 
                _selectedCarIndex);
        }

        // Disable buttons
        foreach (var btn in carButtons) btn.interactable = false;
        if (readyButton != null) readyButton.interactable = false;

        UpdateStatus($"✅ Sẵn sàng với {carNames[_selectedCarIndex]}");

        // Nếu là Client → Hiện "Đang chờ Host bắt đầu"
        if (FusionNetworkManager.Instance?.Runner != null && 
            !FusionNetworkManager.Instance.Runner.IsServer)
        {
            if (waitingForHostPanel != null)
                waitingForHostPanel.SetActive(true);
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    // Public để GameStartController hoặc Host gọi
    public bool IsReady() => _isReady;
    public int GetSelectedCarIndex() => _selectedCarIndex;

    // Gọi hàm này khi Host bấm Start Game
    public void HideLobbyUI()
    {
        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(false);
        else
            gameObject.SetActive(false);

        Debug.Log("[LobbyCharacterSelectUI] Lobby UI đã ẩn.");
    }
}
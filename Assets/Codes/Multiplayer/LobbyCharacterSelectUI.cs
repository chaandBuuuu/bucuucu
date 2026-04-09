using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class LobbyCharacterSelectUI : MonoBehaviour
{
    [Header("Car Selection")]
    [SerializeField] private Button[] carButtons = new Button[4];
    [SerializeField] private TMP_Text selectedCarText;

    [Header("Status Text")]
    [SerializeField] private TMP_Text statusText;

    [Header("Panels")]
    [SerializeField] private GameObject characterSelectPanel;   // Panel chứa nút chọn xe
    [SerializeField] private GameObject waitingPanel;           // Panel "Đang chờ Host bắt đầu" (cho Client)

    private readonly string[] carNames = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private readonly Color[] carColors = { Color.red, Color.green, Color.yellow, new Color(0f, 0.5f, 1f) };

    private int _selectedCarIndex = -1;

    private void Start()
    {
        // Setup nút chọn xe
        for (int i = 0; i < carButtons.Length; i++)
        {
            int index = i;
            carButtons[i].onClick.AddListener(() => OnCarSelected(index));
        }

        // Reset panels
        if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
        if (waitingPanel != null) waitingPanel.SetActive(false);

        UpdateStatus("Chọn xe của bạn!");
    }

    private void OnCarSelected(int index)
    {
        _selectedCarIndex = index;

        // Hiển thị xe đã chọn
        if (selectedCarText != null)
        {
            selectedCarText.text = $"Xe: {carNames[index]}";
            selectedCarText.color = carColors[index];
        }

        UpdateStatus($"Đã chọn: {carNames[index]}");

        // Gửi lựa chọn xe lên server
        if (FusionNetworkManager.Instance?.Runner != null)
        {
            FusionNetworkManager.Instance.RPC_RegisterCarChoice(
                FusionNetworkManager.Instance.Runner.LocalPlayer, 
                _selectedCarIndex);
        }

        // TỰ ĐỘNG ẨN PANEL CHỌN XE
        Invoke(nameof(HideCharacterSelectPanel), 0.5f);
    }

    private void HideCharacterSelectPanel()
    {
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
            Debug.Log("[LobbyCharacterSelectUI] Panel chọn xe đã tự động ẩn.");
        }

        // Hiện panel chờ Host (nếu là Client)
        if (waitingPanel != null && FusionNetworkManager.Instance?.Runner != null && 
            !FusionNetworkManager.Instance.Runner.IsServer)
        {
            waitingPanel.SetActive(true);
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    // Public để các script khác lấy thông tin
    public int GetSelectedCarIndex() => _selectedCarIndex;
}
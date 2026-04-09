using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// Chọn loại xe & Ready cho Racing Game
/// </summary>
public class LobbyCharacterSelectUI : MonoBehaviour
{
    [Header("Car Selection")]
    [SerializeField] private Button[] carButtons = new Button[4];
    [SerializeField] private TMP_Text selectedCarText;

    [Header("Ready Button")]
    [SerializeField] private Button readyButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private readonly string[] _carNames = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private readonly Color[] _carColors =
    {
        Color.red,
        Color.green,
        Color.yellow,
        new Color(0f, 0.5f, 1f)
    };

    private int _selectedCarIndex = -1;
    private bool _isReady = false;

    private void Start()
    {
        // Setup car selection buttons
        for (int i = 0; i < carButtons.Length; i++)
        {
            int idx = i;
            carButtons[i].onClick.AddListener(() => SelectCar(idx));
        }

        // Setup ready button
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        UpdateStatus("Chọn loại xe của bạn!");
    }

    private void SelectCar(int index)
    {
        _selectedCarIndex = index;

        if (selectedCarText != null)
        {
            selectedCarText.text = $"Xe: {_carNames[index]}";
            selectedCarText.color = _carColors[index];
        }

        UpdateStatus($"Đã chọn: {_carNames[index]} - Nhấn Ready để xác nhận");
        Debug.Log($"[LobbyCharacterSelectUI] Selected car: {_carNames[index]}");
    }

    private void OnReadyClicked()
    {
        if (_selectedCarIndex < 0)
        {
            UpdateStatus("⚠️ Vui lòng chọn xe trước!");
            return;
        }

        _isReady = true;
        
        // Send to network manager
        if (FusionNetworkManager.Instance != null)
            FusionNetworkManager.Instance.SetSelectedCharacter(_selectedCarIndex);

        // Disable buttons when ready
        foreach (var btn in carButtons)
            btn.interactable = false;
        if (readyButton != null)
            readyButton.interactable = false;

        UpdateStatus($"✅ Đã sẵn sàng với {_carNames[_selectedCarIndex]}!");
        Debug.Log($"[LobbyCharacterSelectUI] Player ready with car: {_carNames[_selectedCarIndex]}");
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    public bool IsReady() => _isReady;
    public int GetSelectedCarIndex() => _selectedCarIndex;
}

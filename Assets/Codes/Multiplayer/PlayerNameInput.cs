using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Màn hình nhập tên trước khi chọn nhân vật
/// Setup: tạo Panel riêng trong Canvas Lobby, gắn script này vào
/// </summary>
public class PlayerNameInputUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button         confirmButton;
    [SerializeField] private TMP_Text       errorText;

    [Header("Navigation")]
    [SerializeField] private GameObject nameInputPanel;       // Panel nhập tên (chính nó)
    [SerializeField] private GameObject characterSelectPanel; // Panel chọn nhân vật (hiện sau)

    private void Start()
    {
        // Hiện panel nhập tên, ẩn panel chọn nhân vật
        if (nameInputPanel      != null) nameInputPanel.SetActive(true);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (errorText           != null) errorText.gameObject.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirmClicked);

        // Cho phép bấm Enter để xác nhận
        nameInputField.onSubmit.AddListener(_ => OnConfirmClicked());

        // Focus vào ô nhập tên ngay
        nameInputField.Select();
        nameInputField.ActivateInputField();
    }

    private void OnConfirmClicked()
    {
        string playerName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            ShowError("Vui lòng nhập tên!");
            return;
        }

        if (playerName.Length < 2)
        {
            ShowError("Tên phải có ít nhất 2 ký tự!");
            return;
        }

        if (playerName.Length > 16)
        {
            ShowError("Tên không được quá 16 ký tự!");
            return;
        }

        // Lưu tên vào FusionNetworkManager
        FusionNetworkManager.Instance?.SetPlayerName(playerName);
        Debug.Log($"[PlayerNameInputUI] Tên đã đặt: {playerName}");

        // Chuyển sang màn hình chọn nhân vật
        if (nameInputPanel      != null) nameInputPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
    }

    private void ShowError(string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }
}   
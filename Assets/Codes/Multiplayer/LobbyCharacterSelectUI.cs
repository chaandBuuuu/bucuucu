using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI chọn nhân vật ở scene Lobby
/// </summary>
public class LobbyCharacterSelectUI : MonoBehaviour
{
    [Header("Character Buttons")]
    [SerializeField] private Button[] characterButtons = new Button[4];
    [SerializeField] private TMP_Text selectedCharacterText;

    [Header("Host Only")]
    [SerializeField] private GameObject startButtonObj;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private readonly string[] _charNames  = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
    private readonly Color[]  _charColors =
    {
        Color.red,
        Color.green,
        Color.yellow,
        new Color(0f, 0.5f, 1f)
    };

    private void Start()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int idx = i;
            characterButtons[i].onClick.AddListener(() => SelectCharacter(idx));
        }

        // Chỉ Host mới thấy nút Start
        if (startButtonObj != null)
        {
            bool isHost = FusionNetworkManager.Instance != null && FusionNetworkManager.Instance.IsHost;
            startButtonObj.SetActive(isHost);
        }

        UpdateStatus("Chọn nhân vật của bạn!");
    }

    private void SelectCharacter(int index)
    {
        if (FusionNetworkManager.Instance != null)
            FusionNetworkManager.Instance.SetSelectedCharacter(index);

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text  = $"Đã chọn: {_charNames[index]}";
            selectedCharacterText.color = _charColors[index];
        }

        UpdateStatus($"Đã chọn: {_charNames[index]}");
        Debug.Log($"[LobbyCharacterSelectUI] Chọn: {_charNames[index]}");
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
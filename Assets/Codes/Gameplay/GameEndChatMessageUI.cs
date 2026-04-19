using UnityEngine;
using TMPro;

public class GameEndChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message)
    {
        if (playerNameText != null) playerNameText.text = $"<color=cyan>{playerName}:</color>";
        if (messageText    != null) messageText.text    = message;
    }
}

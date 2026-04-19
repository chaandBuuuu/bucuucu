using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message)
    {
        Debug.Log($"[ChatMessageUI] Initialize called: {playerName}: {message}");
        
        if (playerNameText != null)
        {
            playerNameText.text = $"{playerName}:";
            Debug.Log($"[ChatMessageUI] ✅ PlayerNameText set: {playerNameText.text}");
        }
        else
        {
            Debug.LogError("[ChatMessageUI] ❌ playerNameText is NULL!");
        }
        
        if (messageText != null)
        {
            messageText.text = message;
            Debug.Log($"[ChatMessageUI] ✅ MessageText set: {messageText.text}");
        }
        else
        {
            Debug.LogError("[ChatMessageUI] ❌ messageText is NULL!");
        }

        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            Debug.Log($"[ChatMessageUI] RectTransform size: {rectTransform.rect.size}");

        var layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
            Debug.Log($"[ChatMessageUI] ✅ LayoutElement found - Height: {layoutElement.preferredHeight}");
        else
            Debug.LogWarning("[ChatMessageUI] ⚠️ NO LayoutElement on prefab!");
    }
}

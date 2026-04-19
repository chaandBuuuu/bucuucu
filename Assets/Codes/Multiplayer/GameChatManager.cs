using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ FIXED GameChatManager:
///   - ChatNetworkHandler được spawn như NetworkObject riêng (không gắn vào Runner)
///   - RPC hoạt động đúng trên cả host lẫn client
///   - T để toggle chat
/// </summary>
public class GameChatManager : MonoBehaviour
{
    public static GameChatManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform       chatMessagesContainer;
    [SerializeField] private GameObject      chatMessagePrefab;
    [SerializeField] private TMP_InputField  chatInputField;
    [SerializeField] private Button          chatSendButton;
    [SerializeField] private ScrollRect      chatScrollRect;
    [SerializeField] private CanvasGroup     chatPanelCanvasGroup;

    [Header("Settings")]
    [SerializeField] private int  maxChatMessages  = 30;
    [SerializeField] private bool startChatEnabled = true;

    private List<string> _chatLog = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Debug.Log($"[GameChatManager] ✅ Started - Container={chatMessagesContainer != null}, Prefab={chatMessagePrefab != null}, Input={chatInputField != null}, Button={chatSendButton != null}");
        
        if (chatSendButton  != null) chatSendButton.onClick.AddListener(OnSendClicked);
        if (chatInputField  != null) chatInputField.onSubmit.AddListener(_ => OnSendClicked());
        if (chatPanelCanvasGroup != null) chatPanelCanvasGroup.alpha = startChatEnabled ? 1f : 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && chatPanelCanvasGroup != null)
            chatPanelCanvasGroup.alpha = chatPanelCanvasGroup.alpha > 0.5f ? 0f : 1f;
    }

    private void OnSendClicked()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
        {
            Debug.LogWarning("[GameChatManager] ⚠️ InputField is null or text is empty");
            return;
        }

        string msg     = chatInputField.text.Trim();
        string name    = FusionNetworkManager.Instance?.GetStoredPlayerName() ?? "Player";
        chatInputField.text = "";
        chatInputField.ActivateInputField();

        Debug.Log($"[GameChatManager] 📤 Sending: {name}: {msg}");

        // ✅ Gửi qua ChatNetworkHandler (NetworkObject được spawn sẵn)
        if (ChatNetworkHandler.Instance != null)
        {
            ChatNetworkHandler.Instance.SendChat(name, msg);
            Debug.Log($"[GameChatManager] ✅ Sent via ChatNetworkHandler");
        }
        else
        {
            Debug.LogWarning("[GameChatManager] ⚠️ ChatNetworkHandler.Instance is NULL! Using fallback...");
            AddMessageLocal(name, msg);   // Fallback nếu không có network
        }
    }

    /// <summary>
    /// Gọi bởi ChatNetworkHandler.RPC_Broadcast → chạy trên tất cả clients
    /// </summary>
    public void AddMessageLocal(string playerName, string message)
    {
        // ✅ Debug: kiểm tra references
        if (chatMessagesContainer == null)
        {
            Debug.LogError("[GameChatManager] ❌ chatMessagesContainer is NULL! Assign it in inspector.");
            return;
        }
        if (chatMessagePrefab == null)
        {
            Debug.LogError("[GameChatManager] ❌ chatMessagePrefab is NULL! Assign it in inspector.");
            return;
        }

        _chatLog.Add($"{playerName}: {message}");
        Debug.Log($"[GameChatManager] ✅ Adding message: {playerName}: {message}");
        Debug.Log($"[GameChatManager] Content current children: {chatMessagesContainer.childCount}");

        var go  = Instantiate(chatMessagePrefab, chatMessagesContainer);
        Debug.Log($"[GameChatManager] ✅ Prefab instantiated. New child count: {chatMessagesContainer.childCount}");
        
        var ui  = go.GetComponent<ChatMessageUI>();
        if (ui != null)
        {
            ui.Initialize(playerName, message);
            Debug.Log($"[GameChatManager] ✅ ChatMessageUI initialized");
        }
        else
        {
            Debug.LogError("[GameChatManager] ❌ ChatMessageUI component not found on prefab!");
        }

        // Giới hạn số tin nhắn
        while (chatMessagesContainer.childCount > maxChatMessages)
            Destroy(chatMessagesContainer.GetChild(0).gameObject);

        // Auto scroll xuống
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
            Debug.Log($"[GameChatManager] ✅ ScrollRect updated");
        }
        else
        {
            Debug.LogWarning("[GameChatManager] ⚠️ chatScrollRect is NULL!");
        }
    }

    public void ClearChat()
    {
        _chatLog.Clear();
        if (chatMessagesContainer != null)
            foreach (Transform c in chatMessagesContainer) Destroy(c.gameObject);
    }
}


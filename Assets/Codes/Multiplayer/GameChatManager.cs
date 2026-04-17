using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ✅ Global Chat System - Available throughout game (Lobby, Racing, etc.)
/// - Players can chat from when they join room until end of game
/// - Synced across all players via ChatNetworkHandler RPC
/// - Shows player name, message content
/// </summary>
public class GameChatManager : MonoBehaviour
{
    public static GameChatManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform chatMessagesContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button chatSendButton;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private CanvasGroup chatPanelCanvasGroup;  // For toggling chat visibility

    [Header("Settings")]
    [SerializeField] private int maxChatMessages = 30;
    [SerializeField] private bool startChatEnabled = true;

    private List<ChatMessage> _chatMessages = new List<ChatMessage>();
    private ChatNetworkHandler _networkHandler;
    private bool _isInitialized = false;

    private struct ChatMessage
    {
        public string playerName;
        public string message;
        public float timestamp;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Find or create network handler
        _networkHandler = FindAnyObjectByType<ChatNetworkHandler>();
        if (_networkHandler == null)
        {
            // Create network handler if doesn't exist
            var runnerObj = FindAnyObjectByType<NetworkRunner>();
            if (runnerObj != null)
            {
                _networkHandler = runnerObj.gameObject.AddComponent<ChatNetworkHandler>();
                Debug.Log("[GameChatManager] Created ChatNetworkHandler");
            }
            else
            {
                Debug.LogWarning("[GameChatManager] NetworkRunner not found");
                return;
            }
        }

        // Link network handler to this manager
        _networkHandler.SetChatManager(this);

        // Setup chat UI references
        if (chatSendButton != null)
            chatSendButton.onClick.AddListener(OnSendChatMessage);

        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(_ => OnSendChatMessage());
        }

        if (chatPanelCanvasGroup != null)
            chatPanelCanvasGroup.alpha = startChatEnabled ? 1f : 0f;

        _isInitialized = true;
        Debug.Log("[GameChatManager] ✅ Initialized and ready for chat");
    }

    private void Update()
    {
        if (!_isInitialized)
            Initialize();

        // Toggle chat visibility with T key
        if (Input.GetKeyDown(KeyCode.T) && chatPanelCanvasGroup != null)
        {
            chatPanelCanvasGroup.alpha = chatPanelCanvasGroup.alpha > 0.5f ? 0f : 1f;
        }
    }

    /// <summary>
    /// Send chat message to all players
    /// </summary>
    private void OnSendChatMessage()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
            return;

        string message = chatInputField.text;
        chatInputField.text = "";
        chatInputField.ActivateInputField();

        if (_networkHandler == null)
        {
            Debug.LogWarning("[GameChatManager] Network handler not available");
            return;
        }

        // Get player name
        string playerName = FusionNetworkManager.Instance?.GetStoredPlayerName() ?? "Player";

        // Send via network handler (will call RPC)
        _networkHandler.SendChatMessage(playerName, message);

        Debug.Log($"[GameChatManager] Message sent: {playerName}: {message}");
    }

    /// <summary>
    /// Add chat message to local display (called by ChatNetworkHandler)
    /// </summary>
    public void AddChatMessageLocal(string playerName, string message)
    {
        if (chatMessagesContainer == null || chatMessagePrefab == null)
            return;

        // Store message
        _chatMessages.Add(new ChatMessage
        {
            playerName = playerName,
            message = message,
            timestamp = Time.time
        });

        // Instantiate UI
        GameObject msgGO = Instantiate(chatMessagePrefab, chatMessagesContainer);
        var msgUI = msgGO.GetComponent<ChatMessageUI>();

        if (msgUI != null)
        {
            msgUI.Initialize(playerName, message);
        }

        // Limit messages
        while (chatMessagesContainer.childCount > maxChatMessages)
        {
            if (chatMessagesContainer.childCount > 0)
            {
                Destroy(chatMessagesContainer.GetChild(0).gameObject);
            }
        }

        // Auto-scroll to bottom
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Clear all chat messages
    /// </summary>
    public void ClearChat()
    {
        _chatMessages.Clear();
        if (chatMessagesContainer != null)
        {
            foreach (Transform child in chatMessagesContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}

/// <summary>
/// Network handler for chat messages - handles RPC calls
/// </summary>
public class ChatNetworkHandler : NetworkBehaviour
{
    private GameChatManager _chatManager;

    public void SetChatManager(GameChatManager manager)
    {
        _chatManager = manager;
    }

    /// <summary>
    /// Send chat message to all players
    /// </summary>
    public void SendChatMessage(string playerName, string message)
    {
        if (!HasInputAuthority)
        {
            Debug.LogWarning("[ChatNetworkHandler] Only input authority can send chat");
            return;
        }

        // Call RPC on all players
        RPC_BroadcastChatMessage(playerName, message);
    }

    /// <summary>
    /// RPC to broadcast chat message to all players
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_BroadcastChatMessage(string playerName, string message)
    {
        if (_chatManager != null)
        {
            _chatManager.AddChatMessageLocal(playerName, message);
        }
    }
}

/// <summary>
/// Individual chat message display
/// </summary>
public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message)
    {
        if (playerNameText != null)
            playerNameText.text = $"<color=cyan>{playerName}:</color>";

        if (messageText != null)
            messageText.text = message;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ✅ AUTO-SETUP: Automatically initializes Chat & Camera systems on scene load
/// - Creates GameChatManager if missing
/// - Creates complete Chat UI structure
/// - Assigns all references automatically
/// - Creates ChatMessagePrefab
/// - Ready to use without manual setup!
/// </summary>
public class ChatCameraAutoSetup : MonoBehaviour
{
    [Header("Auto Setup Settings")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createChatUI = true;
    [SerializeField] private bool logDetails = true;

    [Header("Chat UI Settings")]
    [SerializeField] private Vector2 chatPanelSize = new Vector2(300, 400);
    [SerializeField] private Vector2 chatPanelPosition = new Vector2(-150, -200);
    [SerializeField] private int maxChatMessages = 30;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupChatAndCamera();
        }
    }

    /// <summary>
    /// Main setup function - initializes chat and camera systems
    /// </summary>
    public void SetupChatAndCamera()
    {
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] 🚀 Starting Chat & Camera setup...");

        // Step 1: Setup Chat System
        SetupChatSystem();

        // Step 2: Verify Camera System
        VerifyCameraSystem();

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Chat & Camera setup complete!");
    }

    /// <summary>
    /// Setup Chat System - creates manager and UI
    /// </summary>
    private void SetupChatSystem()
    {
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] 📢 Setting up Chat System...");

        // Step 1: Check if GameChatManager exists
        GameChatManager existingChat = FindAnyObjectByType<GameChatManager>();
        if (existingChat != null)
        {
            if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ GameChatManager already exists!");
            return;
        }

        // Step 2: Create GameChatManager GameObject
        GameObject chatManagerObj = new GameObject("GameChatManager");
        GameChatManager chatManager = chatManagerObj.AddComponent<GameChatManager>();
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created GameChatManager");

        // Step 3: Create Chat UI structure
        if (createChatUI)
        {
            CreateChatUIStructure(chatManager);
        }
        else
        {
            if (logDetails) Debug.LogWarning("[ChatCameraAutoSetup] ⚠️ Chat UI creation disabled - assign manually in inspector!");
        }
    }

    /// <summary>
    /// Create complete Chat UI with all components
    /// </summary>
    private void CreateChatUIStructure(GameChatManager chatManager)
    {
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] 🎨 Creating Chat UI structure...");

        Canvas mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            if (logDetails) Debug.LogError("[ChatCameraAutoSetup] ❌ No Canvas found in scene! Create one first.");
            return;
        }

        // ── Create Chat Panel ─────────────────────────────────────────
        GameObject chatPanelObj = new GameObject("ChatPanel");
        chatPanelObj.transform.SetParent(mainCanvas.transform, false);
        RectTransform chatPanelRect = chatPanelObj.AddComponent<RectTransform>();
        Image chatPanelBg = chatPanelObj.AddComponent<Image>();
        CanvasGroup chatCanvasGroup = chatPanelObj.AddComponent<CanvasGroup>();

        chatPanelRect.sizeDelta = chatPanelSize;
        chatPanelRect.anchoredPosition = chatPanelPosition;
        chatPanelBg.color = new Color(0, 0, 0, 0.8f);

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created ChatPanel");

        // ── Create Scroll View for messages ───────────────────────────
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(chatPanelObj.transform, false);
        RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
        ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();
        Image scrollBg = scrollViewObj.AddComponent<Image>();

        scrollRect.sizeDelta = new Vector2(-10, -60);  // Padding for input
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(5, 5);
        scrollRect.offsetMax = new Vector2(-5, -55);
        scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // ── Create Viewport ──────────────────────────────────────────
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        Image viewportImage = viewportObj.AddComponent<Image>();
        Mask mask = viewportObj.AddComponent<Mask>();

        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportImage.color = Color.white;

        // ── Create Content ───────────────────────────────────────────
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();

        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 2;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        scroll.content = contentRect;
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created ScrollView for messages");

        // ── Create Input Field ───────────────────────────────────────
        GameObject inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(chatPanelObj.transform, false);
        RectTransform inputRect = inputFieldObj.AddComponent<RectTransform>();
        Image inputBg = inputFieldObj.AddComponent<Image>();
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();

        inputRect.sizeDelta = new Vector2(-10, 30);
        inputRect.anchorMin = new Vector2(0, 0);
        inputRect.anchorMax = new Vector2(1, 0);
        inputRect.offsetMin = new Vector2(5, 5);
        inputRect.offsetMax = new Vector2(-5, 35);

        inputBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        inputField.textComponent = CreateTextComponent(inputFieldObj, "Placeholder", Vector2.zero);

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created InputField");

        // ── Create Send Button ───────────────────────────────────────
        GameObject sendButtonObj = new GameObject("SendButton");
        sendButtonObj.transform.SetParent(chatPanelObj.transform, false);
        RectTransform buttonRect = sendButtonObj.AddComponent<RectTransform>();
        Image buttonBg = sendButtonObj.AddComponent<Image>();
        Button sendButton = sendButtonObj.AddComponent<Button>();

        buttonRect.sizeDelta = new Vector2(60, 30);
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.offsetMin = new Vector2(-65, 5);
        buttonRect.offsetMax = new Vector2(-5, 35);

        buttonBg.color = new Color(0, 0.5f, 1, 0.8f);
        sendButton.targetGraphic = buttonBg;

        TextMeshProUGUI buttonText = CreateTextComponent(sendButtonObj, "Send", Vector2.zero);
        if (buttonText != null) buttonText.text = "Send";

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created SendButton");

        // ── Create Chat Message Prefab ───────────────────────────────
        GameObject messagePrefab = CreateChatMessagePrefab();
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ Created ChatMessagePrefab");

        // ── Assign all references to GameChatManager ──────────────────
        AssignChatReferences(chatManager, contentObj, messagePrefab, inputField, sendButton, scroll, chatPanelObj);
    }

    /// <summary>
    /// Create Chat Message Prefab
    /// </summary>
    private GameObject CreateChatMessagePrefab()
    {
        GameObject prefab = new GameObject("ChatMessagePrefab");
        prefab.SetActive(false);

        // Create Layout Group
        LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;

        // Create Player Name Text
        GameObject nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(prefab.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();

        nameRect.sizeDelta = new Vector2(-10, 20);
        nameText.fontSize = 4;
        nameText.text = "Player:";

        // Create Message Text
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(prefab.transform);
        RectTransform msgRect = msgObj.AddComponent<RectTransform>();
        TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();

        msgRect.sizeDelta = new Vector2(-10, 20);
        msgText.fontSize = 4;
        msgText.text = "Message";

        // Attach ChatMessageUI script
        ChatMessageUI msgUI = prefab.AddComponent<ChatMessageUI>();

        // Get the private fields via reflection and set them
        var nameField = typeof(ChatMessageUI).GetField("playerNameText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var msgField = typeof(ChatMessageUI).GetField("messageText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (nameField != null) nameField.SetValue(msgUI, nameText);
        if (msgField != null) msgField.SetValue(msgUI, msgText);

        return prefab;
    }

    /// <summary>
    /// Create Text Component for UI elements
    /// </summary>
    private TextMeshProUGUI CreateTextComponent(GameObject parent, string name, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

        textRect.sizeDelta = sizeDelta;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        text.fontSize = 4;
        text.text = name;

        return text;
    }

    /// <summary>
    /// Assign all UI references to GameChatManager via reflection
    /// </summary>
    private void AssignChatReferences(GameChatManager chatManager, GameObject contentObj, GameObject msgPrefab,
        TMP_InputField inputField, Button sendButton, ScrollRect scrollRect, GameObject chatPanelObj)
    {
        var type = typeof(GameChatManager);

        // Assign references using reflection for SerializeFields
        AssignField(chatManager, type, "chatMessagesContainer", contentObj.transform);
        AssignField(chatManager, type, "chatMessagePrefab", msgPrefab);
        AssignField(chatManager, type, "chatInputField", inputField);
        AssignField(chatManager, type, "chatSendButton", sendButton);
        AssignField(chatManager, type, "chatScrollRect", scrollRect);
        AssignField(chatManager, type, "chatPanelCanvasGroup", chatPanelObj.GetComponent<CanvasGroup>());

        if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ All chat references assigned!");
    }

    /// <summary>
    /// Helper to assign field via reflection
    /// </summary>
    private void AssignField(GameChatManager manager, System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && value != null)
        {
            field.SetValue(manager, value);
            if (logDetails) Debug.Log($"[ChatCameraAutoSetup] ✅ Assigned {fieldName}");
        }
        else if (field == null)
        {
            Debug.LogWarning($"[ChatCameraAutoSetup] ⚠️ Field not found: {fieldName}");
        }
    }

    /// <summary>
    /// Verify Camera System is ready
    /// </summary>
    private void VerifyCameraSystem()
    {
        if (logDetails) Debug.Log("[ChatCameraAutoSetup] 📹 Verifying Camera System...");

        MultiCameraManager camManager = FindAnyObjectByType<MultiCameraManager>();
        if (camManager != null)
        {
            if (logDetails) Debug.Log("[ChatCameraAutoSetup] ✅ MultiCameraManager ready!");
        }
        else
        {
            if (logDetails) Debug.LogWarning("[ChatCameraAutoSetup] ⚠️ MultiCameraManager not found - camera system may not work!");
        }
    }
}

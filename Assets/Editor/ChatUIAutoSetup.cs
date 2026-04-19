using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// ✅ Editor tool tự động setup Chat UI trong scene GamePlay
/// Menu: RacingGame → Setup Chat UI
/// </summary>
public class ChatUIAutoSetup : EditorWindow
{
    [MenuItem("RacingGame/Setup Chat UI")]
    public static void ShowWindow()
    {
        GetWindow<ChatUIAutoSetup>("Chat UI Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chat UI Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Tool này sẽ tự động tạo:\n" +
            "1. GameChatManager UI (chat trong game)\n" +
            "2. GameEndChatManager UI (chat + vote sau race)\n" +
            "3. 3 NetworkObject prefabs (ChatNetworkHandler, GameEndVoteHandler, MultiCameraManager)\n\n" +
            "Yêu cầu: Scene GamePlay đang mở, có Canvas trong scene.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("▶ Setup tất cả", GUILayout.Height(40)))
            SetupAll();

        EditorGUILayout.Space();
        GUILayout.Label("Hoặc setup từng phần:", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup GameChatManager UI"))       SetupGameChatUI();
        if (GUILayout.Button("Setup GameEndChatManager UI"))    SetupGameEndChatUI();
        if (GUILayout.Button("Tạo NetworkObject Prefabs"))      CreateNetworkPrefabs();
        if (GUILayout.Button("Tạo ChatMessage Prefab"))         CreateChatMessagePrefab();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static void SetupAll()
    {
        SetupGameChatUI();
        SetupGameEndChatUI();
        CreateNetworkPrefabs();
        CreateChatMessagePrefab();
        Debug.Log("[ChatUIAutoSetup] ✅ Setup hoàn tất!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region GameChatManager UI

    private static void SetupGameChatUI()
    {
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindOrCreateCanvas("GameChatCanvas");

        // Panel chính
        GameObject chatPanel = CreateUIPanel(canvas.transform, "ChatPanel",
            new Rect(10, 10, 350, 400), new Color(0, 0, 0, 0.7f));

        CanvasGroup cg = chatPanel.AddComponent<CanvasGroup>();

        // Header
        CreateLabel(chatPanel.transform, "ChatHeader", "💬 CHAT",
            new Rect(0, -15, 350, 30), 14, Color.yellow);

        // Messages area (ScrollView)
        GameObject scrollView = CreateScrollView(chatPanel.transform, "ChatScrollView",
            new Rect(0, -30, 340, 300));

        // Input area
        GameObject inputArea = CreateUIPanel(chatPanel.transform, "InputArea",
            new Rect(0, -350, 340, 40), new Color(0.2f, 0.2f, 0.2f, 1f));

        TMP_InputField inputField = CreateInputField(inputArea.transform, "ChatInputField",
            new Rect(-30, 0, 270, 35), "Nhập tin nhắn... (T để toggle)");

        Button sendBtn = CreateButton(inputArea.transform, "SendButton", "Gửi",
            new Rect(145, 0, 60, 35), new Color(0.2f, 0.6f, 0.2f));

        // Thêm hoặc tìm GameChatManager
        GameChatManager mgr = FindObjectOfType<GameChatManager>();
        if (mgr == null)
        {
            GameObject mgrGO = new GameObject("GameChatManager");
            mgr = mgrGO.AddComponent<GameChatManager>();
        }

        // Gán references qua SerializedObject
        SerializedObject so = new SerializedObject(mgr);
        so.FindProperty("chatMessagesContainer").objectReferenceValue =
            scrollView.transform.Find("Viewport/Content");
        so.FindProperty("chatInputField").objectReferenceValue       = inputField;
        so.FindProperty("chatSendButton").objectReferenceValue       = sendBtn;
        so.FindProperty("chatScrollRect").objectReferenceValue       = scrollView.GetComponent<ScrollRect>();
        so.FindProperty("chatPanelCanvasGroup").objectReferenceValue = cg;
        so.ApplyModifiedProperties();

        Debug.Log("[ChatUIAutoSetup] ✅ GameChatManager UI created");
        EditorUtility.SetDirty(mgr);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region GameEndChatManager UI

    private static void SetupGameEndChatUI()
    {
        Canvas canvas = FindOrCreateCanvas("GameEndCanvas");
        canvas.gameObject.SetActive(false); // Ẩn mặc định

        CanvasGroup cg = canvas.gameObject.GetComponent<CanvasGroup>()
                      ?? canvas.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Background dim
        GameObject bg = CreateUIPanel(canvas.transform, "Background",
            new Rect(0, 0, 1920, 1080), new Color(0, 0, 0, 0.85f));
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Winner text
        TMP_Text winnerText = CreateLabel(canvas.transform, "WinnerText", "🏆 Winner",
            new Rect(0, 180, 800, 80), 48, Color.yellow);
        winnerText.alignment = TextAlignmentOptions.Center;

        // Stats text
        TMP_Text statsText = CreateLabel(canvas.transform, "StatsText", "📊 Bảng xếp hạng",
            new Rect(0, 80, 500, 200), 20, Color.white);
        statsText.alignment = TextAlignmentOptions.Left;

        // Chat panel (bên trái)
        GameObject chatPanel = CreateUIPanel(canvas.transform, "EndChatPanel",
            new Rect(-400, -100, 380, 350), new Color(0.1f, 0.1f, 0.1f, 0.9f));

        GameObject scrollView = CreateScrollView(chatPanel.transform, "EndChatScrollView",
            new Rect(0, 20, 370, 270));

        TMP_InputField inputField = CreateInputField(chatPanel.transform, "EndChatInput",
            new Rect(-25, -155, 290, 35), "Nhắn tin...");

        Button sendBtn = CreateButton(chatPanel.transform, "EndChatSend", "Gửi",
            new Rect(155, -155, 60, 35), new Color(0.2f, 0.6f, 0.2f));

        // Vote panel (bên phải)
        GameObject votePanel = CreateUIPanel(canvas.transform, "VotePanel",
            new Rect(400, -100, 350, 200), new Color(0.1f, 0.1f, 0.1f, 0.9f));

        TMP_Text voteLabel = CreateLabel(votePanel.transform, "VoteLabel", "🗳️ Vote:",
            new Rect(0, 70, 300, 30), 16, Color.white);

        TMP_Text voteCount = CreateLabel(votePanel.transform, "VoteCountText", "Restart: 0/2  Lobby: 0/2",
            new Rect(0, 40, 300, 25), 13, Color.cyan);

        Button restartBtn = CreateButton(votePanel.transform, "RestartButton", "🔄 Chơi lại",
            new Rect(-80, -10, 150, 45), new Color(0.2f, 0.5f, 0.9f));

        Button lobbyBtn = CreateButton(votePanel.transform, "LobbyButton", "🏠 Về Lobby",
            new Rect(80, -10, 150, 45), new Color(0.5f, 0.3f, 0.8f));

        Button menuBtn = CreateButton(votePanel.transform, "MainMenuButton", "❌ Main Menu",
            new Rect(0, -70, 200, 40), new Color(0.7f, 0.2f, 0.2f));

        // Gán GameEndChatManager
        GameEndChatManager mgr = FindObjectOfType<GameEndChatManager>();
        if (mgr == null)
        {
            GameObject mgrGO = new GameObject("GameEndChatManager");
            mgr = mgrGO.AddComponent<GameEndChatManager>();
        }

        SerializedObject so = new SerializedObject(mgr);
        so.FindProperty("gameEndCanvas").objectReferenceValue         = canvas;
        so.FindProperty("canvasGroup").objectReferenceValue           = cg;
        so.FindProperty("winnerText").objectReferenceValue            = winnerText;
        so.FindProperty("statsText").objectReferenceValue             = statsText;
        so.FindProperty("chatMessagesContainer").objectReferenceValue =
            scrollView.transform.Find("Viewport/Content");
        so.FindProperty("chatInputField").objectReferenceValue        = inputField;
        so.FindProperty("chatSendButton").objectReferenceValue        = sendBtn;
        so.FindProperty("chatScrollRect").objectReferenceValue        = scrollView.GetComponent<ScrollRect>();
        so.FindProperty("restartButton").objectReferenceValue         = restartBtn;
        so.FindProperty("lobbyButton").objectReferenceValue           = lobbyBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue        = menuBtn;
        so.FindProperty("voteCountText").objectReferenceValue         = voteCount;
        so.ApplyModifiedProperties();

        Debug.Log("[ChatUIAutoSetup] ✅ GameEndChatManager UI created");
        EditorUtility.SetDirty(mgr);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Network Prefabs

    private static void CreateNetworkPrefabs()
    {
        string prefabFolder = "Assets/Prefabs";

        // Đảm bảo folder tồn tại
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        CreateNetworkPrefab(prefabFolder, "ChatNetworkHandler",   typeof(ChatNetworkHandler));
        CreateNetworkPrefab(prefabFolder, "GameEndVoteHandler",   typeof(GameEndVoteHandler));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ChatUIAutoSetup] ✅ Network prefabs created in {prefabFolder}");
    }

    private static void CreateNetworkPrefab(string folder, string name, System.Type scriptType)
    {
        string path = $"{folder}/{name}.prefab";

        // Skip nếu đã tồn tại
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log($"[ChatUIAutoSetup] {name} prefab đã tồn tại, skip");
            return;
        }

        GameObject go = new GameObject(name);
        go.AddComponent<NetworkObject>();
        go.AddComponent(scriptType);

        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        DestroyImmediate(go);

        if (success)
            Debug.Log($"[ChatUIAutoSetup] ✅ Created prefab: {path}");
        else
            Debug.LogError($"[ChatUIAutoSetup] ❌ Failed to create prefab: {path}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ChatMessage Prefab

    private static void CreateChatMessagePrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // GameChat message prefab
        CreateMessagePrefab(folder, "ChatMessageItem", typeof(ChatMessageUI),
            "playerNameText", "messageText");

        // GameEnd chat message prefab
        CreateMessagePrefab(folder, "GameEndChatMessageItem", typeof(GameEndChatMessageUI),
            "playerNameText", "messageText");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChatUIAutoSetup] ✅ ChatMessage prefabs created");
    }

    private static void CreateMessagePrefab(string folder, string name,
        System.Type uiScript, string nameField, string msgField)
    {
        string path = $"{folder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log($"[ChatUIAutoSetup] {name} prefab đã tồn tại, skip");
            return;
        }

        // Root
        GameObject root = new GameObject(name);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(320, 30);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing            = 5;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childAlignment         = TextAnchor.MiddleLeft;

        var fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Name text
        GameObject nameGO = new GameObject("PlayerName");
        nameGO.transform.SetParent(root.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text      = "<color=cyan>Player:</color>";
        nameTMP.fontSize  = 12;
        nameTMP.color     = Color.white;
        nameTMP.enableWordWrapping = false;
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(80, 25);

        // Message text
        GameObject msgGO = new GameObject("Message");
        msgGO.transform.SetParent(root.transform, false);
        var msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
        msgTMP.text     = "Hello!";
        msgTMP.fontSize = 12;
        msgTMP.color    = Color.white;
        msgTMP.enableWordWrapping = true;
        var msgRect = msgGO.GetComponent<RectTransform>();
        msgRect.sizeDelta = new Vector2(230, 25);

        // Add UI script và gán references
        var uiComp = root.AddComponent(uiScript);
        SerializedObject so = new SerializedObject(uiComp);
        so.FindProperty(nameField).objectReferenceValue = nameTMP;
        so.FindProperty(msgField).objectReferenceValue  = msgTMP;
        so.ApplyModifiedProperties();

        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        DestroyImmediate(root);

        if (success) Debug.Log($"[ChatUIAutoSetup] ✅ Created: {path}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region UI Helpers

    private static Canvas FindOrCreateCanvas(string name)
    {
        // Tìm canvas đã có trong scene
        var existing = GameObject.Find(name);
        if (existing != null) return existing.GetComponent<Canvas>() ?? existing.AddComponent<Canvas>();

        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateUIPanel(Transform parent, string name, Rect rect, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);
        return go;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string text,
        Rect rect, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Rect rect, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var rt  = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);

        // Label text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return btn;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name,
        Rect rect, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var input = go.AddComponent<TMP_InputField>();
        var rt    = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.sizeDelta = new Vector2(-10, -6);
        var mask = textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text    = placeholder;
        phTMP.color   = new Color(0.5f, 0.5f, 0.5f);
        phTMP.fontSize = 12;
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.sizeDelta = Vector2.zero;

        // Input text
        GameObject inpGO = new GameObject("Text");
        inpGO.transform.SetParent(textArea.transform, false);
        var inpTMP = inpGO.AddComponent<TextMeshProUGUI>();
        inpTMP.color    = Color.white;
        inpTMP.fontSize = 12;
        var inpRT = inpGO.GetComponent<RectTransform>();
        inpRT.anchorMin = Vector2.zero; inpRT.anchorMax = Vector2.one;
        inpRT.sizeDelta = Vector2.zero;

        input.textViewport   = taRT;
        input.textComponent  = inpTMP;
        input.placeholder    = phTMP;

        return input;
    }

    private static GameObject CreateScrollView(Transform parent, string name, Rect rect)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

        var scroll = go.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(go.transform, false);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero; vpRT.anchoredPosition = Vector2.zero;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 3;
        vlg.padding                = new RectOffset(5, 5, 5, 5);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRT;
        scroll.content  = contentRT;

        return go;
    }

    #endregion
}

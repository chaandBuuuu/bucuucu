#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.IO;
using UnityEditor.SceneManagement;

public class RacingGameAutoSetup : EditorWindow
{
    private const string PREFAB_FOLDER   = "Assets/Prefabs/RacingGame/";
    private const string CAR_LIST_PATH   = "Assets/CarPrefabList.asset";
    private const string SCENES_FOLDER   = "Assets/Scenes/";

    [MenuItem("RacingGame/🚀 Auto Setup ALL Scenes (Menu + Lobby + Racing)")]
    public static void ShowWindow()
    {
        GetWindow<RacingGameAutoSetup>("Racing Auto Setup").minSize = new Vector2(560, 700);
    }

    private void OnGUI()
    {
        GUILayout.Space(15);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        GUILayout.Label("🏎 AUTO SETUP FULL RACING GAME", titleStyle);
        EditorGUILayout.HelpBox(
            "Tự động tạo 3 scene hoàn chỉnh:\n" +
            "• Menu (Index 0)\n• Lobby (Index 1)\n• GamePlay (Index 2)",
            MessageType.Info);
        GUILayout.Space(20);

        if (GUILayout.Button("▶ TẠO TOÀN BỘ 3 SCENE", GUILayout.Height(70)))
            CreateFullGame();

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Setup từng scene:", EditorStyles.boldLabel);

        if (GUILayout.Button("📜 Setup Menu Scene (Index 0)"))  SetupMenuScene();
        if (GUILayout.Button("📍 Setup Lobby Scene (Index 1)")) SetupLobbyScene();
        if (GUILayout.Button("🏁 Setup Racing Scene – GamePlay (Index 2)")) SetupRacingScene();

        GUILayout.Space(20);
        if (GUILayout.Button("🧹 Cleanup Current Scene", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xóa hết object trong scene hiện tại?", "Xóa", "Hủy"))
                CleanupScene();
        }

        EditorGUILayout.HelpBox(
            "Sau khi setup xong:\n" +
            "1. Lưu scene với tên chính xác:\n" +
            "   • Menu.unity\n   • Lobby.unity\n   • GamePlay.unity\n" +
            "2. Build Settings → Index 0 = Menu, 1 = Lobby, 2 = GamePlay",
            MessageType.Warning);
    }

    // =========================================================================
    //  FULL GAME
    // =========================================================================
    private void CreateFullGame()
    {
        EnsureScenesFolder();
        CreatePrefabsFolder();
        CreateCarPrefabs();

        SetupMenuScene(true);
        SetupLobbyScene(true);
        SetupRacingScene(true);

        EditorUtility.DisplayDialog("✅ HOÀN TẤT!",
            "Đã tạo đầy đủ 3 scene!\n\n" +
            "✅ Menu (Index 0)\n✅ Lobby (Index 1)\n✅ GamePlay (Index 2)\n\n" +
            "Hãy lưu scene và sắp xếp Build Settings theo thứ tự trên.", "OK");

        Debug.Log("<color=green>[RacingAutoSetup] ✅ FULL GAME SETUP HOÀN TẤT!</color>");
    }

    // =========================================================================
    //  MENU SCENE  (Index 0)
    // =========================================================================
    private void SetupMenuScene(bool createNewScene = false)
    {
        if (createNewScene)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(), SCENES_FOLDER + "Menu.unity");
        }

        // ── FusionNetworkManager (DontDestroyOnLoad carrier) ─────────────────
        if (FindAnyObjectByType<FusionNetworkManager>() == null)
        {
            var go = new GameObject("FusionNetworkManager");

            // NetworkRunner prefab – tạo inline nếu chưa có asset
            var runnerGO = new GameObject("NetworkRunnerPrefab");
            var runner   = runnerGO.AddComponent<NetworkRunner>();
            runnerGO.AddComponent<NetworkSceneManagerDefault>();
            string runnerPath = PREFAB_FOLDER + "NetworkRunner.prefab";
            CreatePrefabsFolder();
            var runnerPrefabAsset = SavePrefabIfNotExists(runnerGO, runnerPath);
            DestroyImmediate(runnerGO);

            var fnm = go.AddComponent<FusionNetworkManager>();
            var so  = new SerializedObject(fnm);
            so.FindProperty("runnerPrefab").objectReferenceValue = runnerPrefabAsset.GetComponent<NetworkRunner>();
            so.FindProperty("maxPlayers").intValue      = 4;
            so.FindProperty("lobbySceneIndex").intValue  = 1;
            so.FindProperty("racingSceneIndex").intValue = 2;

            // Assign CarPrefabList if it already exists
            var carList = CreateOrGetCarPrefabList();
            so.FindProperty("carPrefabList").objectReferenceValue = carList;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(go, "Create FusionNetworkManager (Menu)");
        }

        // ── InputHandler ──────────────────────────────────────────────────────
        if (FindAnyObjectByType<InputHandler>() == null)
        {
            var go = new GameObject("InputHandler");
            go.AddComponent<InputHandler>();
            Undo.RegisterCreatedObjectUndo(go, "Create InputHandler (Menu)");
        }

        // ── Menu Canvas ───────────────────────────────────────────────────────
        if (GameObject.Find("MenuCanvas") == null)
        {
            var canvasGO = new GameObject("MenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Attach GameLobbyUI (holds PlayerNameInput + Host/Join logic)
            var lobbyUI = canvasGO.AddComponent<GameLobbyUI>();

            // ── Title ────────────────────────────────────────────────────────
            CreateText(canvasGO, "Title", new Vector2(0, 350), "🏎 RACING MULTIPLAYER", 72, Color.yellow);

            // ── Player Name Panel ────────────────────────────────────────────
            var namePanel = CreatePanel(canvasGO, "NameInputPanel", new Vector2(0, 150), new Vector2(700, 200));

            CreateText(namePanel, "NameLabel", new Vector2(0, 60), "Nhập tên của bạn:", 36, Color.white);

            TMP_InputField nameInput = CreateInputField(namePanel, "PlayerNameInput", new Vector2(0, -10), new Vector2(600, 70));

            TMP_Text nameError = CreateText(namePanel, "NameError", new Vector2(0, -80), "", 28, Color.red);
            nameError.gameObject.SetActive(false);

            // ── Buttons ──────────────────────────────────────────────────────
            Button hostBtn    = CreateButton(canvasGO, "HostButton",    new Vector2(0,  -60), "🎮 HOST GAME");
            Button joinBtn    = CreateButton(canvasGO, "JoinButton",    new Vector2(0, -180), "🚪 JOIN GAME");
            Button refreshBtn = CreateButton(canvasGO, "RefreshButton", new Vector2(0, -300), "🔄 Refresh Rooms");

            TMP_Text statusTxt = CreateText(canvasGO, "StatusText", new Vector2(0, -390), "", 30, Color.cyan);

            // ── Room List ────────────────────────────────────────────────────
            var roomListGO    = new GameObject("RoomListUI");
            roomListGO.transform.SetParent(canvasGO.transform, false);
            var roomListRect  = roomListGO.AddComponent<RectTransform>();
            roomListRect.anchorMin        = new Vector2(0.5f, 0.5f);
            roomListRect.anchorMax        = new Vector2(0.5f, 0.5f);
            roomListRect.anchoredPosition = new Vector2(500, 0);
            roomListRect.sizeDelta        = new Vector2(450, 500);
            var roomListUI = roomListGO.AddComponent<RoomListUI>();

            // Container + item prefab for room list
            var containerGO = new GameObject("RoomContainer");
            containerGO.transform.SetParent(roomListGO.transform, false);
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero; containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero; containerRect.offsetMax = Vector2.zero;
            containerGO.AddComponent<VerticalLayoutGroup>().spacing = 5;

            var roomItemPrefab = CreateRoomItemPrefab();

            var rlSo = new SerializedObject(roomListUI);
            rlSo.FindProperty("roomListContainer").objectReferenceValue = containerGO.transform;
            rlSo.FindProperty("roomItemPrefab").objectReferenceValue    = roomItemPrefab;
            rlSo.FindProperty("joinButton").objectReferenceValue        = joinBtn;
            rlSo.FindProperty("statusText").objectReferenceValue        = statusTxt;
            rlSo.ApplyModifiedProperties();

            // ── Wire GameLobbyUI ─────────────────────────────────────────────
            var uiSo = new SerializedObject(lobbyUI);
            uiSo.FindProperty("playerNameInput").objectReferenceValue = nameInput;
            uiSo.FindProperty("playerNameError").objectReferenceValue = nameError;
            uiSo.FindProperty("roomListUI").objectReferenceValue      = roomListUI;
            uiSo.FindProperty("hostButton").objectReferenceValue      = hostBtn;
            uiSo.FindProperty("joinButton").objectReferenceValue      = joinBtn;
            uiSo.FindProperty("refreshButton").objectReferenceValue   = refreshBtn;
            uiSo.FindProperty("statusText").objectReferenceValue      = statusTxt;
            uiSo.FindProperty("canvasToHide").objectReferenceValue    = canvasGO;
            uiSo.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Menu Canvas");
        }

        if (!createNewScene)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[RacingAutoSetup] ✅ Menu Scene setup xong!</color>");
    }

    // =========================================================================
    //  LOBBY SCENE  (Index 1)
    // =========================================================================
    private void SetupLobbyScene(bool createNewScene = false)
    {
        if (createNewScene)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(), SCENES_FOLDER + "Lobby.unity");
        }

        CreatePrefabsFolder();
        CreateCarPrefabs();

        // ── FusionNetworkManager ──────────────────────────────────────────────
        // Lobby scene cũng cần FNM vì nó là scene đầu tiên Fusion load vào
        if (FindAnyObjectByType<FusionNetworkManager>() == null)
        {
            var go  = new GameObject("FusionNetworkManager");
            var fnm = go.AddComponent<FusionNetworkManager>();

            string runnerPath    = PREFAB_FOLDER + "NetworkRunner.prefab";
            var    runnerPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(runnerPath);
            if (runnerPrefab == null)
            {
                var runnerGO = new GameObject("NetworkRunnerPrefab");
                runnerGO.AddComponent<NetworkRunner>();
                runnerGO.AddComponent<NetworkSceneManagerDefault>();
                runnerPrefab = SavePrefabIfNotExists(runnerGO, runnerPath);
                DestroyImmediate(runnerGO);
            }

            var so = new SerializedObject(fnm);
            so.FindProperty("runnerPrefab").objectReferenceValue  = runnerPrefab.GetComponent<NetworkRunner>();
            so.FindProperty("maxPlayers").intValue                = 4;
            so.FindProperty("lobbySceneIndex").intValue           = 1;
            so.FindProperty("racingSceneIndex").intValue          = 2;
            so.FindProperty("carPrefabList").objectReferenceValue = CreateOrGetCarPrefabList();
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(go, "Create FusionNetworkManager (Lobby)");
        }

        // ── InputHandler ──────────────────────────────────────────────────────
        if (FindAnyObjectByType<InputHandler>() == null)
        {
            var go = new GameObject("InputHandler");
            go.AddComponent<InputHandler>();
            Undo.RegisterCreatedObjectUndo(go, "Create InputHandler (Lobby)");
        }

        // ── LobbySpawner ──────────────────────────────────────────────────────
        GameObject lobbySpawnerGO = null;
        if (FindAnyObjectByType<LobbySpawner>() == null)
        {
            lobbySpawnerGO = new GameObject("LobbySpawner");
            var spawner    = lobbySpawnerGO.AddComponent<LobbySpawner>();

            // Spawn points
            var spawnContainer = new GameObject("LobbySpawnPoints");
            spawnContainer.transform.SetParent(lobbySpawnerGO.transform);
            Vector3[] lobbyPos  = { new(-6,0,0), new(6,0,0), new(-6,4,0), new(6,4,0) };
            Transform[] spawnTr = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var sp = new GameObject($"LobbySpawn_{i}");
                sp.transform.SetParent(spawnContainer.transform);
                sp.transform.position = lobbyPos[i];
                spawnTr[i] = sp.transform;
            }

            var sSo = new SerializedObject(spawner);
            var spProp = sSo.FindProperty("spawnPoints");
            spProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTr[i];
            sSo.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(lobbySpawnerGO, "Create LobbySpawner");
        }

        // ── GameStartController ───────────────────────────────────────────────
        if (FindAnyObjectByType<GameStartController>() == null)
        {
            var go = new GameObject("GameStartController");
            go.AddComponent<NetworkObject>();
            go.AddComponent<GameStartController>();
            Undo.RegisterCreatedObjectUndo(go, "Create GameStartController");
        }

        // ── Lobby Canvas ──────────────────────────────────────────────────────
        if (GameObject.Find("LobbyCanvas") == null)
        {
            var canvasGO = new GameObject("LobbyCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            CreateText(canvasGO, "LobbyTitle", new Vector2(0, 420), "🏎 LOBBY", 64, Color.yellow);

            // ── Character Select Panel ────────────────────────────────────────
            var charPanel = CreatePanel(canvasGO, "CharacterSelectPanel", new Vector2(-400, 50), new Vector2(700, 600));
            CreateText(charPanel, "CharTitle", new Vector2(0, 250), "Chọn xe của bạn", 40, Color.white);

            string[] carNames  = { "Hacker", "Ghost Hunter", "Priest", "Scientist" };
            Color[]  carColors = { Color.red, Color.green, Color.yellow, new Color(0f,0.5f,1f) };
            Button[] carBtns   = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                carBtns[i] = CreateButton(charPanel, $"CarBtn_{i}",
                    new Vector2(0, 160 - i * 120), carNames[i], color: carColors[i]);
            }

            TMP_Text selectedTxt = CreateText(charPanel, "SelectedCarText",
                new Vector2(0, -280), "Chưa chọn xe", 32, Color.cyan);

            TMP_Text charStatusTxt = CreateText(charPanel, "StatusText",
                new Vector2(0, -340), "", 28, Color.white);

            var charSelectUI = charPanel.AddComponent<LobbyCharacterSelectUI>();
            var csSo = new SerializedObject(charSelectUI);
            var btnsProp = csSo.FindProperty("carButtons");
            btnsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = carBtns[i];
            csSo.FindProperty("selectedCarText").objectReferenceValue = selectedTxt;
            csSo.FindProperty("statusText").objectReferenceValue      = charStatusTxt;
            csSo.ApplyModifiedProperties();

            // ── Waiting Panel (hiển thị sau khi chọn xe) ─────────────────────
            var waitPanel = CreatePanel(canvasGO, "WaitingPanel", new Vector2(-400, 50), new Vector2(700, 200));
            waitPanel.SetActive(false);
            CreateText(waitPanel, "WaitText", new Vector2(0, 0),
                "✅ Đã chọn xe!\nĐang chờ Host bắt đầu...", 36, Color.cyan);

            // Link waitingPanel vào LobbyCharacterSelectUI
            csSo = new SerializedObject(charSelectUI);
            csSo.FindProperty("characterSelectPanel").objectReferenceValue = charPanel;
            csSo.FindProperty("waitingPanel").objectReferenceValue         = waitPanel;
            csSo.ApplyModifiedProperties();

            // ── Player List Panel ─────────────────────────────────────────────
            var playerListPanel = CreatePanel(canvasGO, "PlayerListPanel", new Vector2(400, 100), new Vector2(500, 500));
            CreateText(playerListPanel, "ListTitle", new Vector2(0, 210), "👥 Người chơi", 40, Color.white);

            TMP_Text readyCountTxt = CreateText(playerListPanel, "ReadyCountText",
                new Vector2(0, 140), "Sẵn sàng: 0/4", 32, Color.white);

            // ── Start Button (Host only) ──────────────────────────────────────
            var startGo = new GameObject("StartGameContainer");
            startGo.transform.SetParent(canvasGO.transform, false);
            var startRect = startGo.AddComponent<RectTransform>();
            startRect.anchorMin        = new Vector2(0.5f, 0);
            startRect.anchorMax        = new Vector2(0.5f, 0);
            startRect.anchoredPosition = new Vector2(0, 80);
            startRect.sizeDelta        = new Vector2(500, 100);

            Button startBtn = CreateButton(startGo, "StartRaceButton", Vector2.zero,
                "▶ BẮT ĐẦU ĐUA", color: new Color(0.2f, 0.8f, 0.2f));

            TMP_Text startStatusTxt = CreateText(canvasGO, "StartStatusText",
                new Vector2(0, -420), "", 30, Color.cyan);

            // Wire GameStartController
            var gsc    = FindAnyObjectByType<GameStartController>();
            var gscSo  = new SerializedObject(gsc);
            gscSo.FindProperty("statusText").objectReferenceValue      = readyCountTxt;
            gscSo.FindProperty("startRaceButton").objectReferenceValue = startBtn;
            gscSo.FindProperty("requiredPlayers").intValue             = 1; // 1 = host có thể tự start
            gscSo.ApplyModifiedProperties();

            // Wire LobbySpawner start button
            if (FindAnyObjectByType<LobbySpawner>() != null)
            {
                var ls   = FindAnyObjectByType<LobbySpawner>();
                var lsSo = new SerializedObject(ls);
                lsSo.FindProperty("startButtonObj").objectReferenceValue = startGo;
                lsSo.ApplyModifiedProperties();
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Lobby Canvas");
        }

        if (!createNewScene)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[RacingAutoSetup] ✅ Lobby Scene setup xong!</color>");
    }

    // =========================================================================
    //  RACING SCENE  (Index 2)
    // =========================================================================
    private void SetupRacingScene(bool createNewScene = false)
    {
        if (createNewScene)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(), SCENES_FOLDER + "GamePlay.unity");
        }

        CreatePrefabsFolder();
        CreateCarPrefabs();
        CreateRaceManager();
        CreateTrackObjects();
        CreateRacingCarSpawner();
        CreateRaceUICanvasFull();
        CreateMultiCameraManager();

        if (!createNewScene)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[RacingAutoSetup] ✅ Racing Scene (GamePlay) setup xong!</color>");
    }

    private void CreateRaceManager()
    {
        if (FindAnyObjectByType<RaceManager>() != null) return;
        var go = new GameObject("RaceManager");
        go.AddComponent<NetworkObject>();
        go.AddComponent<RaceManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create RaceManager");
    }

    private void CreateTrackObjects()
    {
        if (GameObject.Find("FinishLine") == null)
        {
            var fl  = new GameObject("FinishLine");
            fl.transform.position = new Vector3(0, -15, 0);
            var col = fl.AddComponent<BoxCollider2D>();
            col.size      = new Vector2(12, 2);
            col.isTrigger = true;
            var detector  = fl.AddComponent<FinishLineDetector>();
            var rm        = FindAnyObjectByType<RaceManager>();
            if (rm != null) detector.SetRaceManager(rm);
            Undo.RegisterCreatedObjectUndo(fl, "Create FinishLine");
        }

        if (GameObject.Find("SpawnPoints") == null)
        {
            var container   = new GameObject("SpawnPoints");
            Vector3[] pos   = { new(-6,-14,0), new(6,-14,0), new(-6,-10,0), new(6,-10,0) };
            for (int i = 0; i < 4; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(container.transform);
                sp.transform.position = pos[i];
            }
            Undo.RegisterCreatedObjectUndo(container, "Create SpawnPoints");
        }
    }

    private void CreateRacingCarSpawner()
    {
        if (FindAnyObjectByType<RacingCarSpawner>() != null) return;
        var go      = new GameObject("RacingCarSpawner");
        var spawner = go.AddComponent<RacingCarSpawner>();

        // Link spawn points
        var spawnParent = GameObject.Find("SpawnPoints");
        if (spawnParent != null)
        {
            int count      = spawnParent.transform.childCount;
            var so         = new SerializedObject(spawner);
            var spawnProp  = so.FindProperty("spawnPoints");
            spawnProp.arraySize = count;
            for (int i = 0; i < count; i++)
                spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnParent.transform.GetChild(i);
            so.ApplyModifiedProperties();
        }

        CarPrefabList list = CreateOrGetCarPrefabList();
        var sSo = new SerializedObject(spawner);
        sSo.FindProperty("carPrefabList").objectReferenceValue = list;
        sSo.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(go, "Create RacingCarSpawner");
    }

    private void CreateRaceUICanvasFull()
    {
        if (GameObject.Find("RaceUICanvas") != null) return;

        var canvasGO = new GameObject("RaceUICanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var raceUI = canvasGO.AddComponent<RaceUI>();
        var so     = new SerializedObject(raceUI);

        so.FindProperty("timerText").objectReferenceValue      = CreateText(canvasGO, "Timer",     new Vector2(0, 400),   "⏱️ 00:00",    48);
        so.FindProperty("statusText").objectReferenceValue     = CreateText(canvasGO, "Status",    new Vector2(0, 300),   "🏁 WAITING...",36);
        so.FindProperty("speedText").objectReferenceValue      = CreateText(canvasGO, "Speed",     new Vector2(600, 400), "💨 0.0",       32);
        so.FindProperty("countdownText").objectReferenceValue  = CreateText(canvasGO, "Countdown", new Vector2(0, 200),   "",            60);
        so.FindProperty("raceEndText").objectReferenceValue    = CreateText(canvasGO, "RaceEnd",   new Vector2(0, 0),     "",            80);
        so.FindProperty("raceResultText").objectReferenceValue = CreateText(canvasGO, "Results",   new Vector2(0, -150),  "",            28);

        so.FindProperty("mainMenuButton").objectReferenceValue = CreateButton(canvasGO, "MainMenuBtn", new Vector2(-200,-300), "Main Menu");
        so.FindProperty("restartButton").objectReferenceValue  = CreateButton(canvasGO, "RestartBtn",  new Vector2( 200,-300), "Restart Race");

        so.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create RaceUICanvas");
    }

    private void CreateMultiCameraManager()
    {
        if (FindAnyObjectByType<MultiCameraManager>() != null) return;
        var go = new GameObject("MultiCameraManager");
        go.AddComponent<MultiCameraManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create MultiCameraManager");
    }

    // =========================================================================
    //  COMMON HELPERS
    // =========================================================================
    private void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private void CreateCarPrefabs()
    {
        CreatePrefabsFolder();
        string[] names  = { "Car_Hacker", "Car_GhostHunter", "Car_Priest", "Car_Scientist" };
        Color[]  colors = { Color.red, Color.green, Color.yellow, new Color(0f,0.5f,1f) };

        CarPrefabList list = CreateOrGetCarPrefabList();

        for (int i = 0; i < 4; i++)
        {
            string path = PREFAB_FOLDER + names[i] + ".prefab";
            if (!File.Exists(path))
            {
                var go  = new GameObject(names[i]);
                go.AddComponent<NetworkObject>();
                go.AddComponent<CarController>();
                var rb  = go.AddComponent<Rigidbody2D>();
                rb.gravityScale   = 0f;
                rb.freezeRotation = true;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.4f, 2.2f);
                var sr  = go.AddComponent<SpriteRenderer>();
                sr.color        = colors[i];
                sr.sortingOrder = 10;
                go.tag = "Player";

                PrefabUtility.SaveAsPrefabAsset(go, path);
                DestroyImmediate(go);
            }

            if (list.carPrefabs[i] == null)
            {
                list.carPrefabs[i] = AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
                EditorUtility.SetDirty(list);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private CarPrefabList CreateOrGetCarPrefabList()
    {
        var list = AssetDatabase.LoadAssetAtPath<CarPrefabList>(CAR_LIST_PATH);
        if (list == null)
        {
            list = ScriptableObject.CreateInstance<CarPrefabList>();
            AssetDatabase.CreateAsset(list, CAR_LIST_PATH);
        }
        return list;
    }

    private void CreatePrefabsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/RacingGame"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "RacingGame");
    }

    /// <summary>Save prefab only if file doesn't exist yet.</summary>
    private GameObject SavePrefabIfNotExists(GameObject go, string path)
    {
        CreatePrefabsFolder();
        if (!File.Exists(path))
            PrefabUtility.SaveAsPrefabAsset(go, path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private void CleanupScene()
    {
        string[] names = {
            "MenuCanvas", "LobbyCanvas", "RaceUICanvas", "RaceManager", "FinishLine",
            "SpawnPoints", "RacingCarSpawner", "MultiCameraManager", "LobbySpawner",
            "FusionNetworkManager", "InputHandler", "GameStartController",
            "LobbySpawnPoints", "StartGameContainer"
        };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go) DestroyImmediate(go);
        }
    }

    // ── UI factory ────────────────────────────────────────────────────────────

    private GameObject CreatePanel(GameObject parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt   = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        var img  = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.45f);
        return go;
    }

    private TMP_InputField CreateInputField(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f);

        var field = go.AddComponent<TMP_InputField>();

        // Placeholder
        var placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        placeholder.transform.SetParent(go.transform, false);
        placeholder.text      = "Nhập tên...";
        placeholder.color     = new Color(1,1,1,0.4f);
        placeholder.fontSize  = 32;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        var phRect = placeholder.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10,0); phRect.offsetMax = new Vector2(-10,0);

        // Text
        var textComp = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        textComp.transform.SetParent(go.transform, false);
        textComp.color     = Color.white;
        textComp.fontSize  = 32;
        textComp.alignment = TextAlignmentOptions.MidlineLeft;
        var tRect = textComp.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(10,0); tRect.offsetMax = new Vector2(-10,0);

        field.textViewport   = tRect;
        field.textComponent  = textComp;
        field.placeholder    = placeholder;
        field.characterLimit = 16;

        return field;
    }

    private TMP_Text CreateText(GameObject parent, string name, Vector2 pos, string text,
                                int fontSize, Color? color = null)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(900, 120);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color ?? Color.white;
        return tmp;
    }

    private Button CreateButton(GameObject parent, string name, Vector2 pos, string text,
                                UnityEngine.Events.UnityAction onClick = null, Color? color = null)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(400, 100);

        var img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.15f, 0.15f, 0.15f);

        var btn = go.AddComponent<Button>();

        var tmp = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text      = text;
        tmp.fontSize  = 40;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var tRect = tmp.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;

        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }

    /// <summary>Tạo prefab đơn giản cho một room item trong RoomListUI.</summary>
    private GameObject CreateRoomItemPrefab()
    {
        string path = PREFAB_FOLDER + "RoomItem.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go  = new GameObject("RoomItem");
        var rt  = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(430, 70);
        var img = go.AddComponent<Image>();
        img.color = new Color(1,1,1,0.7f);
        go.AddComponent<Button>();
        go.AddComponent<RoomItemUI>();

        // Session name text
        var nameGO  = new GameObject("SessionName").AddComponent<TextMeshProUGUI>();
        nameGO.transform.SetParent(go.transform, false);
        nameGO.text     = "Room Name";
        nameGO.fontSize = 28;
        nameGO.color    = Color.black;
        nameGO.alignment = TextAlignmentOptions.MidlineLeft;
        var nr = nameGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0,0); nr.anchorMax = new Vector2(0.7f,1);
        nr.offsetMin = new Vector2(10,0); nr.offsetMax = Vector2.zero;

        // Player count text
        var countGO = new GameObject("PlayerCount").AddComponent<TextMeshProUGUI>();
        countGO.transform.SetParent(go.transform, false);
        countGO.text     = "0/4";
        countGO.fontSize = 28;
        countGO.color    = Color.black;
        countGO.alignment = TextAlignmentOptions.MidlineRight;
        var cr = countGO.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.7f,0); cr.anchorMax = Vector2.one;
        cr.offsetMin = Vector2.zero; cr.offsetMax = new Vector2(-10,0);

        // Wire RoomItemUI serialized fields
        var itemUI = go.GetComponent<RoomItemUI>();
        var soItem = new SerializedObject(itemUI);
        soItem.FindProperty("sessionNameText").objectReferenceValue = nameGO;
        soItem.FindProperty("playerCountText").objectReferenceValue = countGO;
        soItem.FindProperty("selectButton").objectReferenceValue    = go.GetComponent<Button>();
        soItem.FindProperty("backgroundImage").objectReferenceValue = img;
        soItem.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
#endif
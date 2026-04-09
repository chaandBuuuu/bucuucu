using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Automatic Setup Wizard for Devour 2D Gameplay System
/// Tự động setup toàn bộ système gameplay
/// </summary>
public class DevourGameplaySetupWizard : EditorWindow
{
    private GUIStyle headerStyle;
    private Vector2 scrollPos;
    private bool createDatabase = true;
    private bool createPrefabs = true;
    private bool setupScene = true;
    private bool setupUI = true;

    [MenuItem("Devour/Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<DevourGameplaySetupWizard>("Devour Setup");
    }

    private void OnGUI()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 10, 10)
            };
        }

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("🎮 DEVOUR 2D GAMEPLAY - AUTO SETUP", headerStyle);
        EditorGUILayout.HelpBox("Tự động setup toàn bộ hệ thống gameplay", MessageType.Info);

        GUILayout.Space(10);

        // Options
        GUILayout.Label("Setup Options:", headerStyle);
        createDatabase = EditorGUILayout.Toggle("Create Character Database", createDatabase);
        createPrefabs = EditorGUILayout.Toggle("Create Character Prefabs", createPrefabs);
        setupScene = EditorGUILayout.Toggle("Setup Scene (add managers)", setupScene);
        setupUI = EditorGUILayout.Toggle("Setup UI Canvas", setupUI);

        GUILayout.Space(20);

        // Setup Button
        if (GUILayout.Button("▶ START SETUP", GUILayout.Height(50)))
        {
            StartSetup();
        }

        GUILayout.Space(20);

        // Info
        EditorGUILayout.HelpBox("Setup sẽ:\n" +
            "• Tạo CharacterDatabase.asset\n" +
            "• Tạo 6 character prefabs\n" +
            "• Thêm GameplayStateManager, GameStartController, etc vào scene\n" +
            "• Setup UI Canvas với managers", MessageType.Info);

        GUILayout.EndScrollView();
    }

    private void StartSetup()
    {
        Debug.Log("[DevourGameplaySetupWizard] Starting auto setup...");

        if (createDatabase)
            CreateCharacterDatabase();

        if (createPrefabs)
            CreateCharacterPrefabs();

        if (setupScene)
            SetupGameplayScene();

        if (setupUI)
            SetupUICanvas();

        Debug.Log("[DevourGameplaySetupWizard] ✅ Setup complete!");
        EditorUtility.DisplayDialog("Setup Complete", "Devour Gameplay System setup hoàn tất!\n\nXem Assets/Resources/ để kiểm tra", "OK");
    }

    /// <summary>
    /// Tạo CharacterDatabase ScriptableObject
    /// </summary>
    private void CreateCharacterDatabase()
    {
        Debug.Log("[Setup] Creating CharacterDatabase...");

        // Ensure Resources folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var db = ScriptableObject.CreateInstance<CharacterDatabase>();

        // Use reflection để set characters array
        var charactersField = typeof(CharacterDatabase).GetField("characters", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (charactersField != null)
        {
            var characters = new CharacterConfig[]
            {
                new CharacterConfig
                {
                    characterId = CharacterID.Hunt1,
                    characterName = "Hunt #1 - Root",
                    role = CharacterRole.Hunter,
                    maxHealth = 100f,
                    moveSpeed = 4.5f,
                    uiColor = new Color(0.8f, 0.4f, 0.4f),
                    description = "Slower but leaves root trails. Pull survivors with vines."
                },
                new CharacterConfig
                {
                    characterId = CharacterID.Hunt2,
                    characterName = "Hunt #2 - Eyes",
                    role = CharacterRole.Hunter,
                    maxHealth = 100f,
                    moveSpeed = 5f,
                    uiColor = new Color(0.9f, 0.7f, 0.2f),
                    description = "Cone vision. Can narrow vision to shoot deadly beams."
                },
                new CharacterConfig
                {
                    characterId = CharacterID.Survival1,
                    characterName = "Survival #1 - Marksman",
                    role = CharacterRole.Survivor,
                    maxHealth = 80f,
                    moveSpeed = 5.5f,
                    uiColor = new Color(0.4f, 0.8f, 0.4f),
                    description = "Mark bullets stun, Tiger bullets do more damage."
                },
                new CharacterConfig
                {
                    characterId = CharacterID.Survival2,
                    characterName = "Survival #2 - Boombox",
                    role = CharacterRole.Survivor,
                    maxHealth = 85f,
                    moveSpeed = 5.2f,
                    uiColor = new Color(0.4f, 0.4f, 0.8f),
                    description = "Place boombox to grant speed. Clap to stun hunters."
                },
                new CharacterConfig
                {
                    characterId = CharacterID.Survival3,
                    characterName = "Survival #3 - Lumberjack",
                    role = CharacterRole.Survivor,
                    maxHealth = 90f,
                    moveSpeed = 5f,
                    uiColor = new Color(0.8f, 0.6f, 0.2f),
                    description = "Speed boost when holding wood. Throw wood at hunters."
                },
                new CharacterConfig
                {
                    characterId = CharacterID.Survival4,
                    characterName = "Survival #4 - Support",
                    role = CharacterRole.Survivor,
                    maxHealth = 75f,
                    moveSpeed = 5f,
                    uiColor = new Color(0.6f, 0.4f, 0.8f),
                    description = "Support teammates with buffs and area stuns."
                }
            };

            charactersField.SetValue(db, characters);
        }

        AssetDatabase.CreateAsset(db, "Assets/Resources/CharacterDatabase.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Setup] ✅ CharacterDatabase created at Assets/Resources/CharacterDatabase.asset");
    }

    /// <summary>
    /// Tạo 6 character prefabs
    /// </summary>
    private void CreateCharacterPrefabs()
    {
        Debug.Log("[Setup] Creating character prefabs...");

        // Ensure Prefabs folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs/Characters"))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "Characters");

        string[] characterNames = { "Hunt1", "Hunt2", "Survival1", "Survival2", "Survival3", "Survival4" };
        CharacterID[] characterIds = { CharacterID.Hunt1, CharacterID.Hunt2, CharacterID.Survival1, CharacterID.Survival2, CharacterID.Survival3, CharacterID.Survival4 };

        for (int i = 0; i < characterNames.Length; i++)
        {
            CreateCharacterPrefab(characterNames[i], characterIds[i]);
        }

        Debug.Log("[Setup] ✅ Character prefabs created");
    }

    private void CreateCharacterPrefab(string prefabName, CharacterID characterId)
    {
        // Create root GameObject
        GameObject prefab = new GameObject($"{prefabName}_Character");

        // Add Rigidbody2D
        var rb = prefab.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // Add Collider
        var collider = prefab.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        // Add SpriteRenderer
        var spriteRenderer = prefab.AddComponent<SpriteRenderer>();
        spriteRenderer.color = Color.white;

        // Add NetworkObject
        prefab.AddComponent<NetworkObject>();

        // Add NetworkTransform
        prefab.AddComponent<NetworkTransform>();

        // Add NetworkCharacterController
        var controller = prefab.AddComponent<NetworkCharacterController>();

        // Add AbilityManager
        var abilityManager = prefab.AddComponent<AbilityManager>();

        // Add StatusEffectManager
        var statusEffectManager = prefab.AddComponent<StatusEffectManager>();

        // Add role-specific abilities
        if (characterId == CharacterID.Hunt1)
        {
            prefab.AddComponent<Hunt1AbilityE>();
            prefab.AddComponent<Hunt1AbilityR>();
            prefab.AddComponent<Hunt1AbilityF>();
            prefab.AddComponent<Hunt1Passive>();
        }
        else if (characterId == CharacterID.Hunt2)
        {
            prefab.AddComponent<Hunt2Passive>();
            prefab.AddComponent<Hunt2AbilityE>();
            prefab.AddComponent<Hunt2AbilityR>();
            prefab.AddComponent<Hunt2AbilityF>();
        }
        else if (characterId == CharacterID.Survival1)
        {
            prefab.AddComponent<Survival1Passive>();
            prefab.AddComponent<Survival1AbilityE>();
            prefab.AddComponent<Survival1AbilityR>();
        }
        else if (characterId == CharacterID.Survival2)
        {
            prefab.AddComponent<Survival2Passive>();
            prefab.AddComponent<Survival2AbilityE>();
            prefab.AddComponent<Survival2AbilityR>();
        }
        else if (characterId == CharacterID.Survival3)
        {
            prefab.AddComponent<Survival3Passive>();
            prefab.AddComponent<Survival3AbilityE>();
            prefab.AddComponent<Survival3AbilityR>();
        }
        else if (characterId == CharacterID.Survival4)
        {
            prefab.AddComponent<Survival4Passive>();
            prefab.AddComponent<Survival4AbilityE>();
            prefab.AddComponent<Survival4AbilityR>();
        }

        // Save as prefab
        string prefabPath = $"Assets/Resources/Prefabs/Characters/{prefabName}_Character.prefab";
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        
        // Cleanup
        DestroyImmediate(prefab);

        Debug.Log($"[Setup] Created prefab: {prefabName}");
    }

    /// <summary>
    /// Setup scene với tất cả managers
    /// </summary>
    private void SetupGameplayScene()
    {
        Debug.Log("[Setup] Setting up scene...");

        // Get active scene
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == "UntitledScene" || string.IsNullOrEmpty(scene.name))
        {
            EditorUtility.DisplayDialog("Warning", "Vui lòng save scene trước!\nFile → New Scene → Save", "OK");
            return;
        }

        // Create GameplayStateManager
        CreateManagerInScene<GameplayStateManager>("GameplayStateManager");

        // Create GameStartController
        CreateManagerInScene<GameStartController>("GameStartController");

        // Create WoodSystem
        CreateManagerInScene<WoodSystem>("WoodSystem");

        // Create spawn points
        CreateSpawners();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Setup] ✅ Scene setup complete");
    }

    private void CreateManagerInScene<T>(string name) where T : MonoBehaviour
    {
        var existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Debug.Log($"[Setup] {name} already exists");
            return;
        }

        var go = new GameObject(name);
        go.AddComponent<T>();
        Debug.Log($"[Setup] Created {name}");
    }

    private void CreateSpawners()
    {
        var existing = GameObject.Find("Spawners");
        if (existing != null)
            return;

        var spawnersParent = new GameObject("Spawners");
        
        Vector2[] positions = { Vector2.zero, new Vector2(5, 5), new Vector2(-5, 5), new Vector2(0, -5) };
        
        for (int i = 0; i < 4; i++)
        {
            var spawner = new GameObject($"Spawn_{i}");
            spawner.transform.SetParent(spawnersParent.transform);
            spawner.transform.position = positions[i];
        }

        Debug.Log("[Setup] Created 4 spawn points");
    }

    /// <summary>
    /// Setup UI Canvas
    /// </summary>
    private void SetupUICanvas()
    {
        Debug.Log("[Setup] Setting up UI Canvas...");

        var existing = FindObjectOfType<Canvas>();
        if (existing != null)
        {
            Debug.Log("[Setup] Canvas already exists");
            return;
        }

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Add GameplayUIManager
        canvasGO.AddComponent<GameplayUIManager>();

        // Add GameEndUIManager
        canvasGO.AddComponent<GameEndUIManager>();

        // Create simple panels
        CreateUIPanel(canvasGO, "GameplayPanel");
        CreateUIPanel(canvasGO, "GameEndPanel");

        Debug.Log("[Setup] ✅ UI Canvas created");
    }

    private void CreateUIPanel(GameObject parent, string panelName)
    {
        var panelGO = new GameObject(panelName);
        panelGO.transform.SetParent(parent.transform);

        var image = panelGO.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        var rectTransform = panelGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

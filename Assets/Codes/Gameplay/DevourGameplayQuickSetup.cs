using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// One-click setup for Devour 2D Gameplay System
/// </summary>
public class DevourGameplayQuickSetup
{
    [MenuItem("Devour/Quick Setup/1. Create Character Database")]
    public static void CreateCharacterDatabase()
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

        Debug.Log("✅ [Setup] CharacterDatabase created at Assets/Resources/CharacterDatabase.asset");
        EditorUtility.DisplayDialog("Done", "✅ CharacterDatabase created!\n\nNext: Create Character Prefabs", "OK");
    }

    [MenuItem("Devour/Quick Setup/2. Create Character Prefabs")]
    public static void CreateCharacterPrefabs()
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

        AssetDatabase.Refresh();
        Debug.Log("✅ [Setup] Character prefabs created");
        EditorUtility.DisplayDialog("Done", "✅ Character Prefabs created!\n\nLocation: Assets/Resources/Prefabs/Characters/\n\nNext: Setup Scene (or use Auto Setup Wizard)", "OK");
    }

    private static void CreateCharacterPrefab(string prefabName, CharacterID characterId)
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
        Object.DestroyImmediate(prefab);

        Debug.Log($"[Setup] Created prefab: {prefabName}");
    }

    [MenuItem("Devour/Quick Setup/3. Add Managers to Scene")]
    public static void SetupSceneManagers()
    {
        Debug.Log("[Setup] Adding managers to scene...");

        FindOrCreateManager<GameplayStateManager>("GameplayStateManager");
        FindOrCreateManager<GameStartController>("GameStartController");
        FindOrCreateManager<WoodSystem>("WoodSystem");
        FindOrCreateManager<GameplayNetworkManager>("GameplayNetworkManager");

        // Create Spawners
        if (GameObject.Find("Spawners") == null)
        {
            var spawnersParent = new GameObject("Spawners");
            
            Vector2[] positions = { Vector2.zero, new Vector2(5, 5), new Vector2(-5, 5), new Vector2(0, -5) };
            
            for (int i = 0; i < 4; i++)
            {
                var spawner = new GameObject($"Spawn_{i}");
                spawner.transform.SetParent(spawnersParent.transform);
                spawner.transform.position = positions[i];
            }
        }

        Debug.Log("✅ [Setup] Managers added to scene");
        EditorUtility.DisplayDialog("Done", "✅ Scene Managers added!\n\nNext: Setup UI Canvas", "OK");
    }

    [MenuItem("Devour/Quick Setup/4. Setup UI Canvas")]
    public static void SetupUICanvasMenu()
    {
        Debug.Log("[Setup] Setting up UI Canvas...");

        if (Object.FindAnyObjectByType<Canvas>() != null)
        {
            EditorUtility.DisplayDialog("Warning", "Canvas already exists in scene", "OK");
            return;
        }

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Add managers
        canvasGO.AddComponent<GameplayUIManager>();
        canvasGO.AddComponent<GameEndUIManager>();

        // Create panels
        CreateUIPanel(canvasGO, "GameplayPanel");
        CreateUIPanel(canvasGO, "GameEndPanel");

        Debug.Log("✅ [Setup] UI Canvas created");
        EditorUtility.DisplayDialog("Done", "✅ UI Canvas created!\n\n🎮 Setup complete! Ready to test.", "OK");
    }

    private static void CreateUIPanel(GameObject parent, string panelName)
    {
        var panelGO = new GameObject(panelName);
        panelGO.transform.SetParent(parent.transform);

        panelGO.AddComponent<Image>();
        var image = panelGO.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        var rectTransform = panelGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void FindOrCreateManager<T>(string name) where T : MonoBehaviour
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null)
        {
            Debug.Log($"[Setup] {name} already exists");
            return;
        }

        var go = new GameObject(name);
        go.AddComponent<T>();
        Debug.Log($"[Setup] Created {name}");
    }

    [MenuItem("Devour/Quick Setup/---")]
    public static void Separator1() { }

    [MenuItem("Devour/Quick Setup/All-in-One Setup")]
    public static void AllInOneSetup()
    {
        Debug.Log("[Setup] Starting All-in-One Setup...");

        CreateCharacterDatabase();
        CreateCharacterPrefabs();
        SetupSceneManagers();
        SetupUICanvasMenu();

        Debug.Log("✅ [Setup] All-in-One Setup COMPLETE!");
        EditorUtility.DisplayDialog("Complete!", "✅ Tất cả setup xong!\n\n🎮 Ready to play!", "OK");
    }
}

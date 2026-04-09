using UnityEngine;
using UnityEditor;

/// <summary>
/// Asset database và config builder cho game
/// </summary>
#if UNITY_EDITOR
public class GameSetupWizard
{
    [MenuItem("Devour/Setup/Create Character Database")]
    public static void CreateCharacterDatabase()
    {
        var db = ScriptableObject.CreateInstance<CharacterDatabase>();
        
        var characters = new CharacterConfig[]
        {
            // Hunters
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
            
            // Survivors
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

        // Gán characters vào database (thông qua reflection vì CharacterDatabase là private)
        var field = typeof(CharacterDatabase).GetField("characters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(db, characters);
        }

        // Save database
        AssetDatabase.CreateAsset(db, "Assets/Resources/CharacterDatabase.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("[GameSetupWizard] Character database created at Assets/Resources/CharacterDatabase.asset");
    }

    [MenuItem("Devour/Setup/Create Gameplay Prefabs")]
    public static void CreateGameplayPrefabs()
    {
        Debug.Log("[GameSetupWizard] Please create character prefabs manually in Prefabs/Characters/ folder");
        Debug.Log("Each prefab should have NetworkCharacterController + AbilityManager + StatusEffectManager components");
    }
}
#endif

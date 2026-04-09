using UnityEngine;

/// <summary>
/// Định nghĩa các vai trò và nhân vật trong trò chơi
/// </summary>
public enum CharacterRole
{
    /// <summary>Kẻ đi săn</summary>
    Hunter,
    /// <summary>Người sống sót</summary>
    Survivor
}

/// <summary>
/// ID các nhân vật cụ thể
/// </summary>
public enum CharacterID
{
    // Hunters
    Hunt1 = 0,
    Hunt2 = 1,
    
    // Survivors
    Survival1 = 2,
    Survival2 = 3,
    Survival3 = 4,
    Survival4 = 5
}

[System.Serializable]
public class CharacterConfig
{
    public CharacterID characterId;
    public string characterName;
    public CharacterRole role;
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public Color uiColor;
    public string description;
}

/// <summary>
/// Quản lý cấu hình nhân vật
/// </summary>
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private CharacterConfig[] characters;
    private static CharacterDatabase _instance;

    public static CharacterDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<CharacterDatabase>("CharacterDatabase");
            return _instance;
        }
    }

    public CharacterConfig GetCharacter(CharacterID id)
    {
        foreach (var character in characters)
            if (character.characterId == id)
                return character;
        return null;
    }

    public CharacterRole GetRole(CharacterID id)
    {
        var config = GetCharacter(id);
        return config != null ? config.role : CharacterRole.Survivor;
    }
}

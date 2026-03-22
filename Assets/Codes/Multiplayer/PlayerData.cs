using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Fusion/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string playerName     = "Player";
    public int    characterIndex = 0;
    public bool   isReady        = false;

    public void Reset()
    {
        playerName     = "Player";
        characterIndex = 0;
        isReady        = false;
    }

    public string GetCharacterName() => characterIndex switch
    {
        0 => "Hacker",
        1 => "Ghost_Hunter",
        2 => "Priest",
        3 => "Scientist",
        _ => "Unknown"
    };

    public Color GetCharacterColor() => characterIndex switch
    {
        0 => new Color(0.8f, 0.3f, 0.3f),
        1 => new Color(0.3f, 0.8f, 0.3f),
        2 => new Color(0.8f, 0.8f, 0.3f),
        3 => new Color(0.3f, 0.3f, 0.8f),
        _ => Color.white
    };
}

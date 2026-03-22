using UnityEngine;

[CreateAssetMenu(fileName = "MultiplayerConfig", menuName = "Fusion/MultiplayerConfig")]
public class MultiplayerConfig : ScriptableObject
{
    public int    maxPlayersPerRoom  = 4;
    public int    minPlayersToStart  = 2;
    public int    maxWaitTimeSeconds = 30;
    public string gameVersion        = "1.0";

    public string[] characterNames = { "Hacker", "Ghost_Hunter", "Priest", "Scientist" };

    public Color[] characterColors = new Color[4]
    {
        new Color(0.8f, 0.3f, 0.3f),
        new Color(0.3f, 0.8f, 0.3f),
        new Color(0.8f, 0.8f, 0.3f),
        new Color(0.3f, 0.3f, 0.8f)
    };

    public float   moveSpeed            = 5f;
    public float   acceleration         = 15f;
    public float   deceleration         = 20f;
    public float   cameraFollowSpeed    = 5f;
    public Vector3 cameraOffset         = new Vector3(0, 0, -10);
    public Vector2 minCameraBounds      = new Vector2(-50, -50);
    public Vector2 maxCameraBounds      = new Vector2(50, 50);
    public bool    debugLogs            = true;

    public Vector3[] spawnPoints = new Vector3[4]
    {
        new Vector3(-5, 0, 0),
        new Vector3( 5, 0, 0),
        new Vector3(-5, 5, 0),
        new Vector3( 5, 5, 0)
    };

    public string GetCharacterName(int i) =>
        i >= 0 && i < characterNames.Length ? characterNames[i] : "Unknown";

    public Color GetCharacterColor(int i) =>
        i >= 0 && i < characterColors.Length ? characterColors[i] : Color.white;

    public Vector3 GetSpawnPoint(int i) =>
        i >= 0 && i < spawnPoints.Length ? spawnPoints[i] : Vector3.zero;

    private void OnValidate()
    {
        maxPlayersPerRoom = Mathf.Max(2, maxPlayersPerRoom);
        minPlayersToStart = Mathf.Clamp(minPlayersToStart, 1, maxPlayersPerRoom);
        moveSpeed         = Mathf.Max(0.1f, moveSpeed);
        if (spawnPoints.Length != maxPlayersPerRoom)
            System.Array.Resize(ref spawnPoints, maxPlayersPerRoom);
        if (characterColors.Length != characterNames.Length)
            System.Array.Resize(ref characterColors, characterNames.Length);
    }
}

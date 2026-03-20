using UnityEngine;
using Photon.Pun;

/// <summary>
/// Cấu hình centralized cho toàn bộ hệ thống multiplayer
/// Chứa các tham số tối ưu hóa network và game settings
/// </summary>
[CreateAssetMenu(fileName = "MultiplayerConfig", menuName = "Multiplayer/Config")]
public class MultiplayerConfig : ScriptableObject
{
    [Header("🌐 Network Settings")]
    [SerializeField] public string photonAppVersion = "1.0";
    [SerializeField] public int maxPlayersPerRoom = 4;
    [SerializeField] public int minPlayersToStart = 2;
    [SerializeField] public int maxWaitTimeSeconds = 30;

    [Header("📡 Photon Optimization")]
    [SerializeField] public int sendRate = 60;              // Messages/sec
    [SerializeField] public int serializationRate = 60;     // Updates/sec
    [SerializeField] public float networkUpdateInterval = 0.1f; // Sync interval

    [Header("👥 Character Settings")]
    [SerializeField] public string[] characterNames = new string[4]
    {
        "Hacker",
        "Ghost_Hunter", 
        "Priest",
        "Scientist"
    };

    [SerializeField] public Color[] characterColors = new Color[4]
    {
        new Color(0.8f, 0.3f, 0.3f, 1f), // Hacker - Red
        new Color(0.3f, 0.8f, 0.3f, 1f), // Ghost Hunter - Green
        new Color(0.8f, 0.8f, 0.3f, 1f), // Priest - Yellow
        new Color(0.3f, 0.3f, 0.8f, 1f)  // Scientist - Blue
    };

    [Header("🎮 Game Settings")]
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] public float acceleration = 15f;
    [SerializeField] public float deceleration = 20f;
    [SerializeField] public bool faceMovementDirection = true;

    [Header("🎥 Camera Settings")]
    [SerializeField] public float cameraFollowSpeed = 5f;
    [SerializeField] public Vector3 cameraOffset = new Vector3(0, 0, -10);
    [SerializeField] public Vector2 minCameraBounds = new Vector2(-50, -50);
    [SerializeField] public Vector2 maxCameraBounds = new Vector2(50, 50);

    [Header("🎯 Spawn Points")]
    [SerializeField] public Vector3[] spawnPoints = new Vector3[4]
    {
        new Vector3(-5, 0, 0),
        new Vector3(5, 0, 0),
        new Vector3(-5, 5, 0),
        new Vector3(5, 5, 0)
    };

    [Header("🔧 Debug")]
    [SerializeField] public bool debugLogs = true;
    [SerializeField] public bool simulateNetwork = false;
    [SerializeField] public float simulatedLatency = 0f; // Milliseconds

    // ==================== Methods ====================

    /// <summary>Lấy tên nhân vật theo index</summary>
    public string GetCharacterName(int index)
    {
        return index >= 0 && index < characterNames.Length ? characterNames[index] : "Unknown";
    }

    /// <summary>Lấy màu nhân vật theo index</summary>
    public Color GetCharacterColor(int index)
    {
        return index >= 0 && index < characterColors.Length ? characterColors[index] : Color.white;
    }

    /// <summary>Lấy spawn point theo index</summary>
    public Vector3 GetSpawnPoint(int index)
    {
        return index >= 0 && index < spawnPoints.Length ? spawnPoints[index] : Vector3.zero;
    }

    /// <summary>Áp dụng tất cả settings</summary>
    public void ApplySettings()
    {
        // Photon settings
        Photon.Pun.PhotonNetwork.SendRate = sendRate;
        Photon.Pun.PhotonNetwork.SerializationRate = serializationRate;

        if (debugLogs)
        {
            Debug.Log($"[MultiplayerConfig] Applied settings: SendRate={sendRate}, SerRate={serializationRate}");
        }
    }

    /// <summary>Validate settings</summary>
    private void OnValidate()
    {
        // Đảm bảo các giá trị hợp lệ
        maxPlayersPerRoom = Mathf.Max(2, maxPlayersPerRoom);
        minPlayersToStart = Mathf.Min(minPlayersToStart, maxPlayersPerRoom);
        sendRate = Mathf.Max(10, sendRate);
        serializationRate = Mathf.Max(10, serializationRate);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);

        // Đảm bảo có đủ spawn points
        if (spawnPoints.Length != maxPlayersPerRoom)
        {
            System.Array.Resize(ref spawnPoints, maxPlayersPerRoom);
        }

        // Đảm bảo có đủ character colors
        if (characterColors.Length != characterNames.Length)
        {
            System.Array.Resize(ref characterColors, characterNames.Length);
        }
    }
}

using UnityEngine;
using Fusion;
using TMPro;

/// <summary>
/// Tự động thiết lập cảnh racing với tất cả các thành phần cần thiết
/// Chỉ cần gắn script này vào một GameObject rỗng và chạy OnRuntimeSetup()
/// </summary>
public class RacingGameAutoSetup : MonoBehaviour
{
    [Header("Racing Scene Setup")]
    [SerializeField] private string sceneName = "Racing";
    [SerializeField] private Vector2 trackCenter = Vector2.zero;
    [SerializeField] private float trackRadius = 20f;

    [Header("Prefabs")]
    [SerializeField] private CarController carPrefab;
    [SerializeField] private PowerupPickup powerupPrefab;

    [Header("UI Canvas")]
    [SerializeField] private Canvas raceCanvasPrefab;

    public void SetupRacingScene()
    {
        Debug.Log("[RacingGameAutoSetup] Bắt đầu thiết lập cảnh racing...");

        // 1. Tạo RaceManager
        CreateRaceManager();

        // 2. Tạo FinishLine
        CreateFinishLine();

        // 3. Tạo Spawn Points
        CreateSpawnPoints();

        // 4. Tạo Powerup Items
        CreatePowerupItems();

        // 5. Tạo UI Canvas
        CreateRaceUICanvas();

        Debug.Log("[RacingGameAutoSetup] ✅ Thiết lập hoàn tất!");
    }

    private void CreateRaceManager()
    {
        GameObject raceManagerObj = new GameObject("RaceManager");
        RaceManager raceManager = raceManagerObj.AddComponent<RaceManager>();
        
        // Cấu hình RaceManager
        raceManager.SetLapsToWin(4);
        
        Debug.Log("✅ Tạo RaceManager");
    }

    private void CreateFinishLine()
    {
        GameObject finishLineObj = new GameObject("FinishLine");
        finishLineObj.transform.position = new Vector3(0, 0, 0);

        BoxCollider2D collider = finishLineObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3, 8);
        collider.isTrigger = true;

        FinishLineDetector detector = finishLineObj.AddComponent<FinishLineDetector>();
        
        // Tìm RaceManager
        RaceManager raceManager = FindObjectOfType<RaceManager>();
        if (raceManager != null)
        {
            detector.raceManager = raceManager;
        }

        Debug.Log("✅ Tạo FinishLine");
    }

    private void CreateSpawnPoints()
    {
        GameObject spawnPointsContainer = new GameObject("SpawnPoints");

        Vector3[] spawnPositions = new Vector3[]
        {
            new Vector3(-5, 5, 0),
            new Vector3(5, 5, 0),
            new Vector3(-5, -5, 0),
            new Vector3(5, -5, 0)
        };

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
            spawnPoint.transform.parent = spawnPointsContainer.transform;
            spawnPoint.transform.position = spawnPositions[i];
        }

        // Gắn RacingCarSpawner vào container
        RacingCarSpawner spawner = spawnPointsContainer.AddComponent<RacingCarSpawner>();
        
        Debug.Log("✅ Tạo 4 Spawn Points");
    }

    private void CreatePowerupItems()
    {
        GameObject powerupsContainer = new GameObject("Powerups");

        // Vị trí ngẫu nhiên 4 powerup trên track
        Vector3[] powerupPositions = new Vector3[]
        {
            new Vector3(10, 0, 0),
            new Vector3(-10, 0, 0),
            new Vector3(0, 10, 0),
            new Vector3(0, -10, 0)
        };

        PowerupType[] powerupTypes = new PowerupType[]
        {
            PowerupType.Shield,
            PowerupType.Gun,
            PowerupType.SpeedBoost,
            PowerupType.Trap
        };

        for (int i = 0; i < powerupPositions.Length; i++)
        {
            GameObject powerupObj = new GameObject($"Powerup_{powerupTypes[i]}");
            powerupObj.transform.parent = powerupsContainer.transform;
            powerupObj.transform.position = powerupPositions[i];

            // Thêm components
            CircleCollider2D collider = powerupObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            SpriteRenderer renderer = powerupObj.AddComponent<SpriteRenderer>();
            renderer.color = GetPowerupColor(powerupTypes[i]);

            // Thêm PowerupPickup script
            powerupObj.AddComponent<PowerupPickup>();
        }

        Debug.Log("✅ Tạo 4 Powerup Items");
    }

    private void CreateRaceUICanvas()
    {
        GameObject canvasObj = new GameObject("RaceUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Tạo LapCounter Text
        GameObject lapCounterObj = new GameObject("LapCounter");
        lapCounterObj.transform.parent = canvasObj.transform;
        RectTransform lapRect = lapCounterObj.AddComponent<RectTransform>();
        lapRect.anchoredPosition = new Vector2(20, -20);
        lapRect.sizeDelta = new Vector2(300, 100);

        TextMeshProUGUI lapText = lapCounterObj.AddComponent<TextMeshProUGUI>();
        lapText.text = "Vòng: 1/4";
        lapText.fontSize = 36;
        lapText.color = Color.white;

        // Tạo Timer Text
        GameObject timerObj = new GameObject("Timer");
        timerObj.transform.parent = canvasObj.transform;
        RectTransform timerRect = timerObj.AddComponent<RectTransform>();
        timerRect.anchoredPosition = new Vector2(0, -20);
        timerRect.sizeDelta = new Vector2(300, 100);

        TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "Thời gian: 0:00";
        timerText.fontSize = 36;
        timerText.alignment = TextAlignmentOptions.TopCenter;
        timerText.color = Color.white;

        Debug.Log("✅ Tạo RaceUI Canvas");
    }

    private Color GetPowerupColor(PowerupType type)
    {
        return type switch
        {
            PowerupType.Shield => Color.cyan,
            PowerupType.Gun => Color.red,
            PowerupType.SpeedBoost => Color.yellow,
            PowerupType.Trap => Color.magenta,
            _ => Color.white
        };
    }

    // Gọi từ menu hoặc button
    public void OnSetupButtonClicked()
    {
        SetupRacingScene();
    }
}

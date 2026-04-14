using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

/// <summary>
/// FIX:
///   - Xóa Reflection để set raceManager trong FinishLineDetector
///     → dùng public SetRaceManager() method thay thế
///   - Thêm warning rõ ràng: RaceManager là NetworkBehaviour, KHÔNG dùng AddComponent()
///     để tạo runtime — phải đặt sẵn trong scene hierarchy có NetworkObject component
///   - CreateRaceManager() chỉ cảnh báo chứ không tạo mới để tránh tạo sai
/// </summary>
public class RacingGameAutoSetup : MonoBehaviour
{
    [Header("Racing Scene Setup")]
    [SerializeField] private Vector2 trackCenter = Vector2.zero;
    [SerializeField] private float   trackRadius = 20f;

    [Header("Prefabs")]
    [SerializeField] private PowerupPickup powerupPrefab;

    [Header("UI Canvas")]
    [SerializeField] private Canvas raceCanvasPrefab;

    public void SetupRacingScene()
    {
        Debug.Log("[RacingGameAutoSetup] Bắt đầu thiết lập cảnh racing...");

        CheckRaceManager();    // 1. Kiểm tra (không tạo)
        CreateFinishLine();    // 2. FinishLine
        CreateSpawnPoints();   // 3. Spawn Points
        // ✅ DISABLED: CreatePowerupItems();  // 4. Powerup Items (removed system)
        CreateRaceUICanvas();  // 5. UI Canvas

        Debug.Log("[RacingGameAutoSetup] ✅ Thiết lập hoàn tất!");
    }

    /// <summary>
    /// FIX: RaceManager là NetworkBehaviour — KHÔNG thể tạo bằng AddComponent().
    /// Phải đặt sẵn trong scene với NetworkObject component đính kèm.
    /// Hàm này chỉ kiểm tra và cảnh báo nếu thiếu.
    /// </summary>
    private void CheckRaceManager()
    {
        RaceManager existing = FindAnyObjectByType<RaceManager>();
        if (existing != null)
        {
            Debug.Log("[RacingGameAutoSetup] ✅ RaceManager đã tồn tại trong scene.");
            return;
        }

        Debug.LogError(
            "[RacingGameAutoSetup] ❌ THIẾU RaceManager trong scene!\n" +
            "Hãy tạo GameObject → thêm NetworkObject + RaceManager component → lưu vào scene.\n" +
            "KHÔNG dùng AddComponent() runtime vì RaceManager là NetworkBehaviour.");
    }

    private void CreateFinishLine()
    {
        FinishLineDetector existing = FindAnyObjectByType<FinishLineDetector>();
        if (existing != null)
        {
            Debug.Log("[RacingGameAutoSetup] ⚠️ FinishLine đã tồn tại, bỏ qua.");
            return;
        }

        GameObject finishLineObj = new GameObject("FinishLine");
        finishLineObj.transform.position = Vector3.zero;

        BoxCollider2D col = finishLineObj.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(3f, 8f);
        col.isTrigger = true;

        FinishLineDetector detector = finishLineObj.AddComponent<FinishLineDetector>();

        // FIX: Dùng public SetRaceManager() thay vì Reflection
        RaceManager raceManager = FindAnyObjectByType<RaceManager>();
        if (raceManager != null)
            detector.SetRaceManager(raceManager);
        else
            Debug.LogWarning("[RacingGameAutoSetup] FinishLine tạo xong nhưng chưa có RaceManager!");

        Debug.Log("[RacingGameAutoSetup] ✅ FinishLine tạo xong.");
    }

    private void CreateSpawnPoints()
    {
        if (GameObject.Find("SpawnPoints") != null)
        {
            Debug.Log("[RacingGameAutoSetup] ⚠️ SpawnPoints đã tồn tại, bỏ qua.");
            return;
        }

        GameObject container = new GameObject("SpawnPoints");

        Vector3[] positions =
        {
            new Vector3(-5f,  5f, 0f),
            new Vector3( 5f,  5f, 0f),
            new Vector3(-5f, -5f, 0f),
            new Vector3( 5f, -5f, 0f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var sp = new GameObject($"SpawnPoint_{i}");
            sp.transform.SetParent(container.transform);
            sp.transform.position = positions[i];
        }

        container.AddComponent<RacingCarSpawner>();
        Debug.Log("[RacingGameAutoSetup] ✅ 4 SpawnPoints tạo xong.");
    }

    private void CreatePowerupItems()
    {
        if (GameObject.Find("Powerups") != null)
        {
            Debug.Log("[RacingGameAutoSetup] ⚠️ Powerups đã tồn tại, bỏ qua.");
            return;
        }

        GameObject container = new GameObject("Powerups");

        Vector3[]     positions = { new Vector3(10,0,0), new Vector3(-10,0,0),
                                    new Vector3(0,10,0), new Vector3(0,-10,0) };
        PowerupType[] types     = { PowerupType.Shield, PowerupType.Gun,
                                    PowerupType.SpeedBoost, PowerupType.Trap };

        for (int i = 0; i < positions.Length; i++)
        {
            var obj = new GameObject($"Powerup_{types[i]}");
            obj.transform.SetParent(container.transform);
            obj.transform.position = positions[i];

            var col = obj.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;
            col.isTrigger = true;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = GetPowerupColor(types[i]);

            obj.AddComponent<PowerupPickup>();
        }

        Debug.Log("[RacingGameAutoSetup] ✅ 4 Powerup Items tạo xong.");
    }

    private void CreateRaceUICanvas()
    {
        if (GameObject.Find("RaceUICanvas") != null)
        {
            Debug.Log("[RacingGameAutoSetup] ⚠️ RaceUICanvas đã tồn tại, bỏ qua.");
            return;
        }

        var canvasObj = new GameObject("RaceUICanvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var lapCounterText     = CreateUIText(canvasObj, "LapCounter",      new Vector2(-300f, -30f), "Vòng: 0/4",       TextAlignmentOptions.Left);
        var timerText          = CreateUIText(canvasObj, "Timer",           new Vector2(   0f, -30f), "Thời gian: 0:00", TextAlignmentOptions.Center);
        var speedText          = CreateUIText(canvasObj, "SpeedDisplay",    new Vector2( 300f, -30f), "Speed: 0.0",      TextAlignmentOptions.Right);
        var powerupDisplayText = CreateUIText(canvasObj, "PowerupDisplay",  new Vector2(0f, -80f),   "No Powerup",      TextAlignmentOptions.Center);

        // ✅ Create and reference main UI texts
        var raceEndTextObj  = CreateUIText(canvasObj, "RaceEndText", Vector2.zero, "", TextAlignmentOptions.Center);
        raceEndTextObj.fontSize = 60;
        raceEndTextObj.gameObject.SetActive(false);

        var raceResultTextObj = CreateUIText(canvasObj, "RaceResultText", new Vector2(0f, 100f), "", TextAlignmentOptions.TopLeft);
        raceResultTextObj.fontSize = 20;
        raceResultTextObj.gameObject.SetActive(false);

        // ✅ Tạo Buttons
        var mainMenuBtn = CreateUIButton(canvasObj, "MainMenuBtn", new Vector2(-200f, -300f), "📋 Menu");
        var restartBtn  = CreateUIButton(canvasObj, "RestartBtn",  new Vector2( 200f, -300f), "🔄 Restart");

        // ✅ Attach RaceUI script và wire buttons (direct assignment)
        var raceUI = canvasObj.AddComponent<RaceUI>();
        raceUI.timerText       = timerText;
        raceUI.statusText      = speedText;  // Re-using speed text for status
        raceUI.speedText       = speedText;
        raceUI.countdownText   = powerupDisplayText;
        raceUI.raceEndText     = raceEndTextObj;
        raceUI.raceResultText  = raceResultTextObj;
        raceUI.mainMenuButton  = mainMenuBtn;
        raceUI.restartButton   = restartBtn;

        Debug.Log("[RacingGameAutoSetup] ✅ RaceUI Canvas tạo xong (với buttons + RaceUI script).");
    }

    private TextMeshProUGUI CreateUIText(GameObject parent, string name,
                                         Vector2 anchoredPos, string defaultText,
                                         TextAlignmentOptions alignment)
    {
        var obj  = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 1f);
        rt.anchorMax       = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(400f, 60f);

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = defaultText;
        tmp.fontSize  = 32;
        tmp.color     = Color.white;
        tmp.alignment = alignment;
        return tmp;
    }

    private UnityEngine.UI.Button CreateUIButton(GameObject parent, string name,
                                                  Vector2 anchoredPos, string buttonText)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(300f, 60f);

        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f);

        var btn = obj.AddComponent<UnityEngine.UI.Button>();

        // Text child
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = buttonText;
        tmp.fontSize  = 28;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private Color GetPowerupColor(PowerupType type) => type switch
    {
        PowerupType.Shield     => Color.cyan,
        PowerupType.Gun        => Color.red,
        PowerupType.SpeedBoost => Color.yellow,
        PowerupType.Trap       => Color.magenta,
        _                      => Color.white
    };
}
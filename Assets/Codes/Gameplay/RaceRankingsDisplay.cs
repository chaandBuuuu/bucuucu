using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ✅ Real-Time Rankings Display Lúc Đang Race
/// - Hiển thị vị trí người chơi, tốc độ, khoảng cách tới finish
/// - Update mỗi 0.2s
/// - Toggle bằng phím TAB
/// - Chỉ host/server mới có thể Restart
/// </summary>
public class RaceRankingsDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform rankingsContainer;     // Content để chứa ranking items
    [SerializeField] private GameObject rankingItemPrefab;    // Prefab item (TMP_Text simple)
    [SerializeField] private CanvasGroup panelCanvasGroup;    // CanvasGroup để ẩn/hiện
    [SerializeField] private TextMeshProUGUI titleText;       // "BẢNG XẾP HẠNG" title
    [SerializeField] private Button restartButton;            // Nút Restart (chỉ host)
    [SerializeField] private Button menuButton;               // Nút Menu

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private bool startVisible = false;

    private RaceManager _raceManager;
    private List<CarController> _cachedCars = new List<CarController>();
    private Dictionary<CarController, GameObject> _rankingItems = new Dictionary<CarController, GameObject>();
    private float _lastUpdateTime = 0f;
    private bool _isVisible = false;

    private void Start()
    {
        _raceManager = RaceManager.Instance;

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = startVisible ? 1f : 0f;

        _isVisible = startVisible;

        if (titleText != null)
            titleText.text = "📊 BẢNG XẾP HẠNG";

        // ✅ Gán button listeners
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);

        Debug.Log("[RaceRankingsDisplay] ✅ Initialized");
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void Update()
    {
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            _isVisible = !_isVisible;
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = _isVisible ? 1f : 0f;
        }

        if (!_isVisible) return;

        // Update rankings periodically
        float elapsed = Time.time - _lastUpdateTime;
        if (elapsed >= updateInterval)
        {
            _lastUpdateTime = Time.time;
            UpdateRankings();
        }
    }

    private void UpdateRankings()
    {
        if (_raceManager == null) return;

        // Cache cars nếu chưa có
        if (_cachedCars.Count == 0)
        {
            _cachedCars = new List<CarController>(FindObjectsByType<CarController>(FindObjectsSortMode.None));
            if (_cachedCars.Count == 0) return;
        }

        // Sắp xếp theo vị trí (tính toán distance to finish)
        var sortedCars = CalculateRankings();

        // Update hoặc create items
        int position = 1;
        foreach (var car in sortedCars)
        {
            if (!_rankingItems.TryGetValue(car, out var itemGO))
            {
                // Tạo item mới
                itemGO = Instantiate(rankingItemPrefab, rankingsContainer);
                _rankingItems[car] = itemGO;
            }

            UpdateRankingItem(itemGO, car, position);
            position++;
        }

        // Xóa items của cars không tồn tại
        var deadCars = _rankingItems.Keys.Where(c => c == null || !_cachedCars.Contains(c)).ToList();
        foreach (var deadCar in deadCars)
        {
            if (_rankingItems.TryGetValue(deadCar, out var deadItem))
            {
                Destroy(deadItem);
                _rankingItems.Remove(deadCar);
            }
        }
    }

    private List<CarController> CalculateRankings()
    {
        if (_raceManager == null) return _cachedCars;

        // Sắp xếp: người finished đầu tiên → finished nhưng gần nhất → chưa finished nhưng gần nhất
        return _cachedCars.OrderBy(car =>
        {
            if (car == null) return float.MaxValue;

            // Nếu đã finish
            if (car.IsFinished)
                return _raceManager.GetPlayerFinishTime(car);

            // Nếu chưa finish: tính distance tới finish line
            var finishLine = _raceManager.GetFinishLineTransform();
            if (finishLine != null)
            {
                float dist = Vector3.Distance(car.transform.position, finishLine.position);
                return 999999f + dist; // Đẩy xuống dưới những người đã finish
            }

            return float.MaxValue;
        }).ToList();
    }

    private void UpdateRankingItem(GameObject itemGO, CarController car, int position)
    {
        if (car == null) return;

        var tmpText = itemGO.GetComponent<TextMeshProUGUI>();
        if (tmpText == null) return;

        // Lấy tên người chơi
        string playerName = GetPlayerName(car);

        // Lấy status (finish time hoặc distance)
        string status = "";
        if (car.IsFinished)
        {
            float finishTime = _raceManager.GetPlayerFinishTime(car);
            status = $"✅ {finishTime:F2}s";
        }
        else
        {
            var finishLine = _raceManager.GetFinishLineTransform();
            if (finishLine != null)
            {
                float dist = Vector3.Distance(car.transform.position, finishLine.position);
                status = $"📍 {dist:F0}m";
            }
        }

        // Color cho top 3
        Color textColor = position switch
        {
            1 => new Color(1f, 0.84f, 0f),      // Gold
            2 => new Color(0.75f, 0.75f, 0.75f),// Silver
            3 => new Color(0.8f, 0.5f, 0.2f),   // Bronze
            _ => Color.white
        };

        tmpText.text = $"{position:D1}. {playerName,-20} {status}";
        tmpText.color = textColor;
    }

    private string GetPlayerName(CarController car)
    {
        if (car == null) return "Unknown";

        // ✅ Try to get cached player name from CarController
        try
        {
            string name = car.GetPlayerName();
            if (!string.IsNullOrEmpty(name))
                return name;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[RaceRankingsDisplay] Lỗi lấy tên: {ex.Message}");
        }
        
        return "Unknown";
    }

    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = visible ? 1f : 0f;

        // ✅ Nếu show, check xem có phải host không để enable Restart button
        if (visible && restartButton != null)
        {
            bool isHost = FusionNetworkManager.Instance?.Runner?.IsServer ?? false;
            restartButton.interactable = isHost;
            Debug.Log($"[RaceRankingsDisplay] Restart button interactable: {isHost}");
        }
    }

    // ✅ Xử lý khi click Restart button (chỉ host gọi)
    private void OnRestartClicked()
    {
        Debug.Log("[RaceRankingsDisplay] 🔄 Restart clicked");

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner == null || !runner.IsServer)
        {
            Debug.LogError("[RaceRankingsDisplay] ❌ Chỉ host mới có thể restart!");
            return;
        }

        // Gọi RPC restart race
        if (RaceManager.Instance != null)
            RaceManager.Instance.RPC_RestartRace();
    }

    // ✅ Xử lý khi click Menu button
    private void OnMenuClicked()
    {
        Debug.Log("[RaceRankingsDisplay] 🏠 Menu clicked");

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null)
        {
            runner.Shutdown();
            Debug.Log("[RaceRankingsDisplay] Shutdown Fusion");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}

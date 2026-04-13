using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ✅ Minimap system để hiển thị vị trí của tất cả players trên track
/// - Hiển thị trên canvas corner nhỏ
/// - Theo dõi vị trí real-time của player
/// - Color coding cho mỗi player
/// </summary>
public class MiniMapManager : MonoBehaviour
{
    [Header("Minimap UI")]
    [SerializeField] private Canvas minimapCanvas;
    [SerializeField] private RectTransform minimapPanel;  // Panel chứa map
    [SerializeField] private Image minimapBackground;

    [Header("Track Settings")]
    [SerializeField] private Vector2 trackSize = new Vector2(100, 60);  // Kích thước track thực tế
    [SerializeField] private Vector2 minimapSize = new Vector2(200, 150);  // Kích thước minimap trên canvas
    [SerializeField] private Color32[] playerColors = new Color32[4]
    {
        new Color32(255, 0, 0, 255),     // Player 1 - Red
        new Color32(0, 255, 0, 255),     // Player 2 - Green
        new Color32(255, 255, 0, 255),   // Player 3 - Yellow
        new Color32(0, 150, 255, 255)    // Player 4 - Blue
    };

    private Dictionary<PlayerRef, Image> _playerDots = new Dictionary<PlayerRef, Image>();
    private Dictionary<PlayerRef, CarController> _playerCars = new Dictionary<PlayerRef, CarController>();
    private Vector3 _trackCenter = Vector3.zero;
    private NetworkRunner _runner;

    private void Start()
    {
        _runner = FindAnyObjectByType<NetworkRunner>();
        
        // Nếu chưa có minimapPanel, tạo nó
        if (minimapPanel == null)
        {
            CreateMinimapUI();
        }

        InitializePlayerDots();
    }

    private void CreateMinimapUI()
    {
        if (minimapCanvas == null)
        {
            var canvasGO = new GameObject("MinimapCanvas");
            minimapCanvas = canvasGO.AddComponent<Canvas>();
            minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Tạo background panel
        var panelGO = new GameObject("MinimapPanel");
        panelGO.transform.SetParent(minimapCanvas.transform);
        minimapPanel = panelGO.AddComponent<RectTransform>();
        minimapBackground = panelGO.AddComponent<Image>();

        // ✅ Position ở top-right corner
        minimapPanel.anchorMin = new Vector2(1, 1);
        minimapPanel.anchorMax = new Vector2(1, 1);
        minimapPanel.pivot = new Vector2(1, 1);
        minimapPanel.offsetMin = new Vector2(-220, -170);  // 20px from right/top
        minimapPanel.offsetMax = Vector2.zero;

        minimapBackground.color = new Color(0, 0, 0, 0.7f);  // Semi-transparent black

        Debug.Log("[MiniMapManager] ✅ Created minimap UI");
    }

    private void InitializePlayerDots()
    {
        if (_runner == null) return;

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            CreatePlayerDot(player);
        }
    }

    private void CreatePlayerDot(PlayerRef player)
    {
        if (minimapPanel == null) return;

        var dotGO = new GameObject($"PlayerDot_{player.PlayerId}");
        dotGO.transform.SetParent(minimapPanel);
        
        var dotImage = dotGO.AddComponent<Image>();
        dotImage.color = playerColors[Mathf.Min(player.PlayerId, playerColors.Length - 1)];

        var dotRect = dotGO.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(12, 12);  // Size of dot
        dotRect.anchoredPosition = Vector2.zero;

        _playerDots[player] = dotImage;
        Debug.Log($"[MiniMapManager] Created dot for Player {player.PlayerId}");
    }

    public void RegisterPlayerCar(PlayerRef player, CarController car)
    {
        _playerCars[player] = car;
    }

    private void LateUpdate()
    {
        if (minimapPanel == null) return;

        UpdatePlayerDots();
    }

    private void UpdatePlayerDots()
    {
        foreach (var kvp in _playerCars)
        {
            PlayerRef player = kvp.Key;
            CarController car = kvp.Value;

            if (car == null || !_playerDots.TryGetValue(player, out var dot)) continue;

            // ✅ Calculate minimap position
            Vector3 carPos = car.transform.position;
            Vector2 minimapPos = WorldToMinimapPos(carPos);
            dot.GetComponent<RectTransform>().anchoredPosition = minimapPos;
        }
    }

    private Vector2 WorldToMinimapPos(Vector3 worldPos)
    {
        // ✅ Map world coordinates to minimap coordinates
        // Normalize position relative to track center
        float normalizedX = (worldPos.x - _trackCenter.x) / trackSize.x;
        float normalizedY = (worldPos.y - _trackCenter.y) / trackSize.y;

        // Scale to minimap size
        float minimapX = normalizedX * minimapSize.x;
        float minimapY = normalizedY * minimapSize.y;

        return new Vector2(minimapX, minimapY);
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        CreatePlayerDot(player);
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (_playerDots.TryGetValue(player, out var dot) && dot != null)
        {
            Destroy(dot.gameObject);
        }
        _playerDots.Remove(player);
        _playerCars.Remove(player);
    }
}

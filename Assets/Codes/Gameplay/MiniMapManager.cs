using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ❌ DEPRECATED: Minimap bị loại bỏ (chế độ camera fixed toàn bộ track)
/// - Giữ lại script để tránh reference errors
/// - Tất cả chức năng bị vô hiệu hóa
/// </summary>
public class MiniMapManager : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[MiniMapManager] ❌ DEPRECATED - Minimap system disabled (using fixed top-down camera instead)");
        gameObject.SetActive(false);  // Vô hiệu hóa component
    }

    #pragma warning disable CS0414  // Tắt warning về unused fields
    [Header("Minimap UI")]
    [SerializeField] private Canvas minimapCanvas;
    [SerializeField] private RectTransform minimapPanel;
    [SerializeField] private Image minimapBackground;

    [Header("Track Settings")]
    [SerializeField] private Vector2 trackSize = new Vector2(100, 60);
    [SerializeField] private Vector2 minimapSize = new Vector2(200, 150);
    [SerializeField] private Color32[] playerColors = new Color32[4]
    {
        new Color32(255, 0, 0, 255),
        new Color32(0, 255, 0, 255),
        new Color32(255, 255, 0, 255),
        new Color32(0, 150, 255, 255)
    };

    private Dictionary<PlayerRef, Image> _playerDots = new Dictionary<PlayerRef, Image>();
    private Dictionary<PlayerRef, CarController> _playerCars = new Dictionary<PlayerRef, CarController>();
    private Vector3 _trackCenter = Vector3.zero;
    private NetworkRunner _runner;
    #pragma warning restore CS0414

    // ✅ Dummy methods để tránh reference errors
    public void RegisterPlayerCar(PlayerRef player, CarController car) { }
    public void OnPlayerJoined(PlayerRef player) { }
    public void OnPlayerLeft(PlayerRef player) { }
}


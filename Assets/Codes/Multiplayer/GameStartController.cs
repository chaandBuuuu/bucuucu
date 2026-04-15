using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

/// <summary>
/// Quản lý start race - chờ tất cả player sẵn sàng
/// </summary>
public class GameStartController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int requiredPlayers = 2;   // ✅ FIX: Đổi default xuống 2 cho dễ test
    [SerializeField] private float checkInterval = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startRaceButton;

    [Networked] private int ReadyCount { get; set; }
    [Networked] private bool RaceStarting { get; set; }

    private bool _isSpawned = false;
    private float _lastCheck = 0f;

    public override void Spawned()
    {
        _isSpawned = true;
        Debug.Log("[GameStartController] Chờ player sẵn sàng...");

        if (startRaceButton != null)
        {
            startRaceButton.onClick.AddListener(OnStartRaceClicked);
            bool isHost = HasStateAuthority;
            startRaceButton.gameObject.SetActive(isHost);
        }

        Debug.Log("[GameStartController] Spawned!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer || RaceStarting) return;
        if (Runner.SimulationTime - _lastCheck < checkInterval) return;
        _lastCheck = Runner.SimulationTime;
        CheckIfCanStart();
    }

    private void Update()
    {
        if (!_isSpawned) return;

        if (statusText != null)
        {
            // ✅ Hiện số player thực tế thay vì requiredPlayers cứng
            int playerCount = 0;
            foreach (var _ in Runner.ActivePlayers) playerCount++;
            statusText.text = $"Sẵn sàng: {ReadyCount}/{playerCount} (cần {requiredPlayers})";
        }

        // ✅ FIX: Cập nhật interactable của nút theo trạng thái thực tế
        if (startRaceButton != null && HasStateAuthority)
        {
            int playerCount = 0;
            foreach (var _ in Runner.ActivePlayers) playerCount++;
            startRaceButton.interactable = !RaceStarting && ReadyCount >= requiredPlayers && playerCount >= requiredPlayers;
        }
    }

    private void CheckIfCanStart()
    {
        int readyCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject _))
                readyCount++;
        }

        ReadyCount = readyCount;

        int playerCount = 0;
        foreach (var _ in Runner.ActivePlayers) playerCount++;

        Debug.Log($"[GameStartController] Players: {playerCount}, Ready: {readyCount}/{requiredPlayers}");
    }

    private void OnStartRaceClicked()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("[GameStartController] Chỉ Host mới được bắt đầu game!");
            return;
        }

        if (RaceStarting)
        {
            Debug.LogWarning("[GameStartController] Race đang trong quá trình bắt đầu, bỏ qua...");
            return;
        }

        // ✅ FIX: Đếm player thực tế trong phòng
        int playerCount = 0;
        foreach (var _ in Runner.ActivePlayers) playerCount++;

        // ✅ FIX: Dùng ReadyCount (networked, đã tính trong CheckIfCanStart)
        //         thay vì đếm lại không có điều kiện gì
        if (playerCount < requiredPlayers)
        {
            Debug.LogWarning($"[GameStartController] Cần ít nhất {requiredPlayers} người. Hiện có {playerCount}.");
            if (statusText != null)
                statusText.text = $"❌ Cần {requiredPlayers} người! (hiện {playerCount})";
            return;
        }

        if (ReadyCount < requiredPlayers)
        {
            Debug.LogWarning($"[GameStartController] Chưa đủ người sẵn sàng: {ReadyCount}/{requiredPlayers}");
            if (statusText != null)
                statusText.text = $"❌ Chưa đủ sẵn sàng: {ReadyCount}/{requiredPlayers}";
            return;
        }

        Debug.Log("[GameStartController] ✅ Host bắt đầu game → Load Racing Scene");
        RaceStarting = true;                        // ✅ Đặt cờ để tránh bấm nhiều lần
        Runner.LoadScene(SceneRef.FromIndex(2));    // Racing Scene Index = 2
    }
}
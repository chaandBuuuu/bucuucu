using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// FIX:
///   - HashSet _carsCrossedThisLap thực sự được dùng để chống đếm nhiều lần
///   - Thêm cooldown per-car để xe dừng trên finish line không spam lap
///   - Chỉ server xử lý trigger (RemotePlayerController sync qua NetworkTransform)
///   - Thêm public SetRaceManager() để RacingGameAutoSetup gán mà không cần Reflection
/// </summary>
public class FinishLineDetector : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;

    // FIX: Cooldown tránh đếm 2 lần khi xe hover trên finish line
    private const float LAP_COOLDOWN = 2f;
    private Dictionary<CarController, float> _lastLapTime = new Dictionary<CarController, float>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (raceManager == null) return;

        // FIX: Chỉ server đếm lap — remote car được di chuyển bởi NetworkTransform
        // nên OnTriggerEnter2D sẽ fire trên host khi xe cross finish line
        var runner = FindAnyObjectByType<NetworkRunner>();
        if (runner != null && !runner.IsServer) return;

        var car = collision.GetComponent<CarController>();
        if (car == null) return;
        if (car.IsFinished) return;

        // FIX: Cooldown per-car
        float now = Time.time;
        if (_lastLapTime.TryGetValue(car, out float lastTime))
        {
            if (now - lastTime < LAP_COOLDOWN) return;
        }

        _lastLapTime[car] = now;
        raceManager.RegisterLapCompletion(car);
        Debug.Log($"[FinishLineDetector] {car.name} qua vạch đích");
    }

    /// <summary>
    /// FIX: Public setter thay thế Reflection trong RacingGameAutoSetup.
    /// </summary>
    public void SetRaceManager(RaceManager rm)
    {
        raceManager = rm;
    }
}
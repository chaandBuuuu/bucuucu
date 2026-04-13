using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

/// <summary>
/// FIX:
///   - RegisterLapCompletion() thêm HasStateAuthority check → tránh client ghi trực tiếp
///     vào [Networked] state (sẽ throw exception trong Fusion)
///   - Spawned() chỉ reset state khi race chưa bắt đầu → an toàn khi object respawn
///   - _carLapCount dùng NetworkId làm key để tránh stale reference
///   - ✅ Thêm distance calculation để xác định leader và position
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private int lapsToWin = RacingConstants.RACE_LAPS_TO_WIN;
    
    [Header("Finish Line")]
    [SerializeField] private Transform finishLineTransform;  // ✅ Sẽ được set bởi FinishLineDetector

    [Networked] public bool  RaceStarted      { get; private set; }
    [Networked] public bool  RaceFinished     { get; private set; }
    [Networked] public float RaceTimer        { get; private set; }
    [Networked] public int   CountdownCounter { get; private set; } = -1;  // ✅ -1 = không countdown, 3,2,1,0

    // FIX: Dùng NetworkId làm key để tránh stale reference
    private Dictionary<NetworkId, int> _carLapCount = new Dictionary<NetworkId, int>();
    // ✅ Lưu distance của từng player để calculate position
    private Dictionary<NetworkId, float> _carDistanceToFinish = new Dictionary<NetworkId, float>();
    // ✅ OPTIMIZE: Cache cars để tránh FindObjectsByType mỗi frame
    private List<CarController> _cachedCars = new List<CarController>();
    private CarController _winner = null;
    private bool _isSpawned = false;
    private float _countdownTimer = 0f;  // ✅ Timer cho countdown
    private float _lastCarCacheTime = 0f;  // ✅ Cache update throttle
    private const float CAR_CACHE_UPDATE_INTERVAL = 0.5f;  // ✅ Update cache every 0.5s

    public event System.Action                   OnRaceStart;
    public event System.Action<CarController>    OnRaceEnd;
    public event System.Action<CarController, int> OnLapComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _isSpawned = true;

        if (HasStateAuthority && !RaceStarted)
        {
            RaceStarted  = false;
            RaceFinished = false;
            RaceTimer    = 0f;
            Debug.Log("[RaceManager] ✅ Race initialized!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        
        // ✅ Handle countdown
        if (CountdownCounter >= 0)
        {
            _countdownTimer += Runner.DeltaTime;
            int desiredCount = Mathf.Max(0, 3 - Mathf.FloorToInt(_countdownTimer));
            
            if (desiredCount != CountdownCounter)
            {
                CountdownCounter = desiredCount;
                Debug.Log($"[RaceManager] Countdown: {CountdownCounter}");
            }

            // Khi countdown kết thúc (đạt 0), bắt đầu race
            if (_countdownTimer >= 3f)
            {
                _countdownTimer = 0f;
                CountdownCounter = -1;
                RaceStarted = true;
                Debug.Log("[RaceManager] 🏁 RACE STARTED!");
                OnRaceStart?.Invoke();
            }
        }
        
        // Bình thường update race timer
        if (RaceStarted && !RaceFinished)
            RaceTimer += Runner.DeltaTime;

        // ✅ Update distance tracking mỗi frame
        UpdatePlayerDistances();
    }

    // ✅ NEW: Set finish line position (called by FinishLineDetector)
    public void SetFinishLinePosition(Transform finishLine)
    {
        finishLineTransform = finishLine;
    }

    // ✅ NEW: Update distance từ từng player đến finish line (optimized with caching)
    private void UpdatePlayerDistances()
    {
        if (finishLineTransform == null) return;

        // ✅ OPTIMIZE: Throttle car cache update to avoid FindObjectsByType every frame
        float currentTime = Time.time;
        if (currentTime - _lastCarCacheTime > CAR_CACHE_UPDATE_INTERVAL)
        {
            _lastCarCacheTime = currentTime;
            _cachedCars.Clear();
            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
            {
                if (car.Object != null)
                    _cachedCars.Add(car);
            }
        }

        // ✅ Update distance using cached cars
        Vector3 finishPos = finishLineTransform.position;
        foreach (var car in _cachedCars)
        {
            if (car.Object == null) continue;
            NetworkId carId = car.Object.Id;

            float distance = Vector3.Distance(car.transform.position, finishPos);
            _carDistanceToFinish[carId] = distance;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        // ✅ Bắt đầu countdown thay vì race ngay lập tức
        CountdownCounter = 3;
        _countdownTimer = 0f;
        Debug.Log("[RaceManager] 🎬 Bắt đầu countdown 3 giây!");
    }

    public void RegisterLapCompletion(CarController car)
    {
        if (!HasStateAuthority) return;
        if (!RaceStarted || RaceFinished) return;

        NetworkId carId = car.Object.Id;

        if (!_carLapCount.ContainsKey(carId))
            _carLapCount[carId] = 0;

        _carLapCount[carId]++;
        int laps = _carLapCount[carId];

        OnLapComplete?.Invoke(car, laps);
        Debug.Log($"[RaceManager] {car.name} chạm đích!");

        if (laps >= lapsToWin)
            FinishRace(car);
    }

    private void FinishRace(CarController winner)
    {
        if (RaceFinished) return;
        RaceFinished = true;
        _winner      = winner;
        OnRaceEnd?.Invoke(winner);
        Debug.Log($"[RaceManager] 🏆 {winner.name} chiến thắng sau {RaceTimer:F2}s!");
    }

    // ✅ NEW: Lấy vị trí của player trong cuộc đua
    public int GetPlayerPosition(CarController car)
    {
        if (car?.Object == null) return 0;

        // Lấy tất cả players và sort theo distance (gần finish line nhất = position 1)
        var carsByDistance = _carDistanceToFinish
            .OrderBy(x => x.Value)  // Sort theo distance tăng dần
            .ToList();

        for (int i = 0; i < carsByDistance.Count; i++)
        {
            if (carsByDistance[i].Key == car.Object.Id)
                return i + 1;  // Position 1-based
        }

        return 0;
    }

    // ✅ NEW: Lấy thông tin leader (optimized)
    public CarController GetLeaderCar()
    {
        if (_carDistanceToFinish.Count == 0) return null;

        float minDistance = float.MaxValue;
        NetworkId leaderId = default;

        foreach (var kvp in _carDistanceToFinish)
        {
            if (kvp.Value < minDistance)
            {
                minDistance = kvp.Value;
                leaderId = kvp.Key;
            }
        }

        // ✅ Find car by cached list
        foreach (var car in _cachedCars)
        {
            if (car.Object != null && car.Object.Id == leaderId)
                return car;
        }

        return null;
    }

    // ✅ NEW: Lấy all players sorted by position (optimized - no LINQ)
    public List<(CarController car, int position)> GetRankings()
    {
        var rankings = new List<(CarController, int)>();
        var sortedDistances = new List<(NetworkId id, float distance)>(_carDistanceToFinish.Count);

        // Collect all distances
        foreach (var kvp in _carDistanceToFinish)
        {
            sortedDistances.Add((kvp.Key, kvp.Value));
        }

        // ✅ Simple bubble sort instead of LINQ (less GC)
        for (int i = 0; i < sortedDistances.Count - 1; i++)
        {
            for (int j = i + 1; j < sortedDistances.Count; j++)
            {
                if (sortedDistances[j].distance < sortedDistances[i].distance)
                {
                    var temp = sortedDistances[i];
                    sortedDistances[i] = sortedDistances[j];
                    sortedDistances[j] = temp;
                }
            }
        }

        // Map back to cars
        for (int i = 0; i < sortedDistances.Count; i++)
        {
            foreach (var car in _cachedCars)
            {
                if (car.Object != null && car.Object.Id == sortedDistances[i].id)
                {
                    rankings.Add((car, i + 1));
                    break;
                }
            }
        }

        return rankings;
    }

    public int GetLapCount(CarController car)
    {
        if (car == null || car.Object == null) return 0;
        return _carLapCount.TryGetValue(car.Object.Id, out int count) ? count : 0;
    }

    public float GetRaceTime()
    {
        if (!_isSpawned) return 0f;
        return RaceTimer;
    }

    public CarController GetWinner() => _winner;
}
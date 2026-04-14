using UnityEngine;

/// <summary>
/// ✅ NEW: Locks/Unlocks game input when race ends
/// - Prevents player movement
/// - Allows UI interaction only
/// - Singleton pattern
/// </summary>
public class GameInputLocker : MonoBehaviour
{
    public static GameInputLocker Instance { get; private set; }

    private bool _inputLocked = false;
    private CarController[] _allCars;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Find all cars in scene
        UpdateCarCache();
    }

    /// ✅ Lock or unlock input
    public void LockInput(bool locked)
    {
        _inputLocked = locked;
        Debug.Log($"[GameInputLocker] Input locked: {locked}");

        UpdateCarCache();

        // Disable/enable all car controls
        foreach (var car in _allCars)
        {
            if (car != null)
            {
                car.SetInputEnabled(!locked);
            }
        }
    }

    /// ✅ Is input locked?
    public bool IsInputLocked()
    {
        return _inputLocked;
    }

    /// ✅ Update car cache
    private void UpdateCarCache()
    {
        _allCars = FindObjectsOfType<CarController>();
    }

    /// ✅ Check if can move
    public bool CanPlayerMove()
    {
        return !_inputLocked;
    }
}

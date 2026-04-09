using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// FIX:
///   - FindLocalCar() dùng Coroutine poll thay vì gọi FindObjectsByType mỗi frame
///     → tránh CPU spike khi scene mới load và xe chưa spawn
///   - Unsubscribe event trong OnDestroy để tránh memory leak
/// </summary>
public class RaceUI : MonoBehaviour
{
    [Header("Race Info")]
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Powerup Display")]
    [SerializeField] private TextMeshProUGUI powerupText;
    [SerializeField] private Image           powerupIcon;

    [Header("Race End")]
    [SerializeField] private TextMeshProUGUI raceEndText;
    [SerializeField] private Button          mainMenuButton;
    [SerializeField] private Button          restartButton;

    private RaceManager   _raceManager;
    private CarController _localCar;

    private void Start()
    {
        _raceManager = RaceManager.Instance;

        if (_raceManager != null)
        {
            _raceManager.OnLapComplete += OnLapComplete;
            _raceManager.OnRaceEnd     += OnRaceEnd;
        }

        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButton  != null) restartButton .onClick.AddListener(OnRestartClicked);

        if (raceEndText != null) raceEndText.gameObject.SetActive(false);

        // FIX: Dùng coroutine poll thay vì FindObjectsByType mỗi frame
        StartCoroutine(FindLocalCarRoutine());
    }

    private void OnDestroy()
    {
        // FIX: Unsubscribe để tránh memory leak khi UI bị destroy
        if (_raceManager != null)
        {
            _raceManager.OnLapComplete -= OnLapComplete;
            _raceManager.OnRaceEnd     -= OnRaceEnd;
        }
    }

    /// <summary>
    /// FIX: Poll mỗi 0.5 giây đến khi tìm được xe của local player.
    /// Tránh FindObjectsByType chạy 60 lần/giây trong Update().
    /// </summary>
    private IEnumerator FindLocalCarRoutine()
    {
        while (_localCar == null)
        {
            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
            {
                if (car.HasInputAuthority)
                {
                    _localCar = car;
                    Debug.Log($"[RaceUI] Tìm thấy local car: {car.name}");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (_localCar != null)
        {
            UpdateLapUI();
            UpdateSpeedUI();
            UpdatePowerupUI();
        }

        UpdateTimerUI();
    }

    private void UpdateLapUI()
    {
        if (lapText == null || _raceManager == null) return;
        int lap = _raceManager.GetLapCount(_localCar);
        lapText.text = string.Format(RacingConstants.LAP_FORMAT, lap, RacingConstants.RACE_LAPS_TO_WIN);
    }

    private void UpdateTimerUI()
    {
        if (timerText == null || _raceManager == null) return;
        float time    = _raceManager.GetRaceTime();
        int   minutes = (int)(time / 60f);
        int   seconds = (int)(time % 60f);
        timerText.text = string.Format(RacingConstants.TIMER_FORMAT, minutes, seconds);
    }

    private void UpdateSpeedUI()
    {
        if (speedText == null || _localCar == null) return;
        speedText.text = string.Format(RacingConstants.SPEED_FORMAT, _localCar.GetSpeed());
    }

    private void UpdatePowerupUI()
    {
        if (powerupText == null || _localCar == null) return;

        var inventory = _localCar.GetPowerupInventory();
        if (inventory != null && inventory.HasPowerup())
        {
            var powerup = inventory.GetCurrentPowerup();
            powerupText.text  = $"Powerup: {powerup}";
            powerupText.color = GetPowerupColor(powerup.Value);
        }
        else
        {
            powerupText.text  = "No Powerup";
            powerupText.color = Color.white;
        }
    }

    private void OnLapComplete(CarController car, int lap)
    {
        if (_localCar == null || car != _localCar) return;
        if (lapText == null) return;
        lapText.color = Color.yellow;
        Invoke(nameof(ResetLapColor), 0.5f);
    }

    private void ResetLapColor()
    {
        if (lapText != null) lapText.color = Color.white;
    }

    private void OnRaceEnd(CarController winner)
    {
        if (raceEndText == null) return;
        bool isWinner = (winner == _localCar);
        raceEndText.text  = isWinner ? "YOU WIN!" : $"{winner.name} thắng!";
        raceEndText.color = isWinner ? Color.green : Color.red;
        raceEndText.gameObject.SetActive(true);
    }

    private Color GetPowerupColor(PowerupType type) => type switch
    {
        PowerupType.Shield     => Color.cyan,
        PowerupType.Gun        => Color.yellow,
        PowerupType.SpeedBoost => Color.green,
        PowerupType.Trap       => Color.red,
        _                      => Color.white
    };

    private void OnMainMenuClicked()
        => UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");

    private void OnRestartClicked()
        => UnityEngine.SceneManagement.SceneManager.LoadScene(
               UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
}
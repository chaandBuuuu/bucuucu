using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

/// <summary>
/// UI cho cuộc đua
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
    [SerializeField] private Image powerupIcon;

    [Header("Race End")]
    [SerializeField] private TextMeshProUGUI raceEndText;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;

    private RaceManager _raceManager;
    private CarController _localCar;
    private NetworkRunner _runner;

    private void Start()
    {
        _raceManager = RaceManager.Instance;
        _runner = FindAnyObjectByType<NetworkRunner>();
        
        if (_raceManager != null)
        {
            _raceManager.OnLapComplete += OnLapComplete;
            _raceManager.OnRaceEnd += OnRaceEnd;
        }

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void Update()
    {
        if (_localCar == null)
            FindLocalCar();

        if (_localCar != null)
        {
            UpdateLapUI();
            UpdateSpeedUI();
            UpdatePowerupUI();
        }

        UpdateTimerUI();
    }

    private void FindLocalCar()
    {
        var allCars = FindObjectsByType<CarController>();
        foreach (var car in allCars)
        {
            if (car.HasInputAuthority)
            {
                _localCar = car;
                break;
            }
        }
    }

    private void UpdateLapUI()
    {
        if (lapText != null && _raceManager != null)
        {
            int lap = _raceManager.GetLapCount(_localCar);
            lapText.text = $"Lap: {lap}/4";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null && _raceManager != null)
        {
            float time = _raceManager.GetRaceTime();
            int minutes = (int)(time / 60f);
            int seconds = (int)(time % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    private void UpdateSpeedUI()
    {
        if (speedText != null && _localCar != null)
        {
            float speed = _localCar.GetSpeed();
            speedText.text = $"Speed: {speed:F1}";
        }
    }

    private void UpdatePowerupUI()
    {
        if (powerupText != null && _localCar != null)
        {
            var inventory = _localCar.GetPowerupInventory();
            if (inventory != null && inventory.HasPowerup())
            {
                var powerup = inventory.GetCurrentPowerup();
                powerupText.text = $"Powerup: {powerup}";
                powerupText.color = GetPowerupColor(powerup.Value);
            }
            else
            {
                powerupText.text = "No Powerup";
                powerupText.color = Color.white;
            }
        }
    }

    private void OnLapComplete(CarController car, int lap)
    {
        if (_localCar == null) return;
        
        if (car == _localCar)
        {
            lapText.color = Color.yellow;
            Invoke(nameof(ResetLapColor), 0.5f);
        }
    }

    private void ResetLapColor()
    {
        if (lapText != null)
            lapText.color = Color.white;
    }

    private void OnRaceEnd(CarController winner)
    {
        if (raceEndText != null)
        {
            bool isWinner = (winner == _localCar);
            raceEndText.text = isWinner ? "YOU WIN!" : $"{winner.name} won!";
            raceEndText.color = isWinner ? Color.green : Color.red;
            raceEndText.gameObject.SetActive(true);
        }
    }

    private Color GetPowerupColor(PowerupType type)
    {
        return type switch
        {
            PowerupType.Shield => Color.cyan,
            PowerupType.Gun => Color.yellow,
            PowerupType.SpeedBoost => Color.green,
            PowerupType.Trap => Color.red,
            _ => Color.white
        };
    }

    private void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    private void OnRestartClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}

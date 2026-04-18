using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ UPDATED: Display finish-line based racing UI
///   - Timer: Race countdown
///   - Status: Running / Finished / Final Countdown
///   - Speed: Current speed
///   - Final Countdown: 10s để tính toán kết quả
///   - Rankings: Final results
/// </summary>
public class RaceUI : MonoBehaviour
{
    [Header("Race Info")]
[SerializeField] public TextMeshProUGUI timerText;
[SerializeField] public TextMeshProUGUI statusText;
[SerializeField] public TextMeshProUGUI speedText;
[SerializeField] public TextMeshProUGUI countdownText;

[Header("Race End")]
[SerializeField] public TextMeshProUGUI raceEndText;
[SerializeField] public TextMeshProUGUI raceResultText;
[SerializeField] public Button mainMenuButton;
[SerializeField] public Button restartButton;

    private RaceManager   _raceManager;
    private CarController _localCar;
    private bool _raceStarted = false;
    private bool _raceFinished = false;
    private float _lastUIUpdateTime = 0f;
    private const float UI_UPDATE_INTERVAL = 0.1f;  // ✅ Update UI every 100ms instead of every frame

    private void Start()
    {
        _raceManager = RaceManager.Instance;

        if (_raceManager != null)
        {
            _raceManager.OnRaceStart += OnRaceStart;
            _raceManager.OnPlayerFinish += OnPlayerFinish;
            _raceManager.OnFinalRankings += OnFinalRankings;
            _raceManager.OnRaceEnd += OnRaceEnd;
        }

        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButton  != null) restartButton .onClick.AddListener(OnRestartClicked);

        if (raceEndText != null) raceEndText.gameObject.SetActive(false);
        if (raceResultText != null) raceResultText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        
        // ✅ NEW: Set initial status
        if (statusText != null)
            statusText.text = "🔄 WAITING FOR COUNTDOWN...";

        StartCoroutine(FindLocalCarRoutine());
    }

    private void OnDestroy()
    {
        if (_raceManager != null)
        {
            _raceManager.OnRaceStart -= OnRaceStart;
            _raceManager.OnPlayerFinish -= OnPlayerFinish;
            _raceManager.OnFinalRankings -= OnFinalRankings;
            _raceManager.OnRaceEnd -= OnRaceEnd;
        }
    }

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
                    Debug.Log($"[RaceUI] ✅ Found local car: {car.name}");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnRaceStart()
    {
        _raceStarted = true;
        if (statusText != null)
            statusText.text = "🚩 RACING...";
        Debug.Log("[RaceUI] Race started!");
    }

    private void OnPlayerFinish(CarController car)
    {
        if (car == _localCar)
        {
            if (statusText != null)
                statusText.text = "✅ YOU FINISHED!";
        }
        Debug.Log($"[RaceUI] {car.name} finished!");
    }

    private void OnFinalRankings(List<(CarController, int, float, float)> rankings)
    {
        _raceFinished = true;
        DisplayFinalResults(rankings);
    }

    private void OnRaceEnd(CarController winner)
    {
        if (raceEndText == null) return;
        
        bool isWinner = (winner == _localCar);
        raceEndText.text  = isWinner ? "🏆 YOU WIN!" : $"🥈 {winner.name} won!";
        raceEndText.color = isWinner ? Color.yellow : Color.cyan;
        raceEndText.gameObject.SetActive(true);
    }

    private void DisplayFinalResults(List<(CarController car, int position, float time, float distance)> rankings)
    {
        if (raceResultText == null) return;

        string results = "=== FINAL RESULTS ===\n";
        for (int i = 0; i < rankings.Count; i++)
        {
            var (car, pos, time, dist) = rankings[i];
            results += $"{pos}. {car.name} - {time:F2}s ({dist:F2}m away)\n";
        }

        raceResultText.text = results;
        raceResultText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_raceManager == null) return;

        // ✅ OPTIMIZE: Throttle UI updates to reduce CPU usage
        float timeSinceLastUpdate = Time.time - _lastUIUpdateTime;
        if (timeSinceLastUpdate < UI_UPDATE_INTERVAL)
            return;

        _lastUIUpdateTime = Time.time;

        UpdateTimerUI();
        UpdateCountdownUI();

        if (_localCar != null && !_raceFinished)
        {
            UpdateSpeedUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null || _raceManager == null) return;
        float time    = _raceManager.GetRaceTime();
        int   minutes = (int)(time / 60f);
        int   seconds = (int)(time % 60f);
        timerText.text = $"⏱️ {minutes:00}:{seconds:00}";
    }

    private void UpdateCountdownUI()
    {
        if (_raceManager == null || !_raceManager.IsSpawned)
        {
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
            return;
        }

        // ✅ Check pre-race countdown (3,2,1,0)
        int preRaceCountdown = _raceManager.CountdownCounter;
        if (preRaceCountdown >= 0)
        {
            countdownText.gameObject.SetActive(true);
            if (preRaceCountdown == 0)
                countdownText.text = "🚩 GO!";
            else
                countdownText.text = preRaceCountdown.ToString();
            
            countdownText.color = Color.yellow;
            countdownText.fontSize = 80;  // Bigger for pre-race
            return;
        }

        // ✅ Check post-finish countdown (10s)
        float finishCountdown = _raceManager.FinishCountdown;
        if (finishCountdown >= 0f)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = $"⏳ Finish in {finishCountdown:F1}s";
            countdownText.color = finishCountdown < 5f ? Color.yellow : Color.white;
            countdownText.fontSize = 40;  // Normal size
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void UpdateSpeedUI()
    {
        if (speedText == null || _localCar == null) return;
        speedText.text = $"💨 {_localCar.GetSpeed():F1} unit/s";
    }

    private void OnMainMenuClicked()
        => UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");

    private void OnRestartClicked()
        => UnityEngine.SceneManagement.SceneManager.LoadScene(
               UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
}
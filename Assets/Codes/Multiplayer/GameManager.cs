using UnityEngine;
using TMPro;
using Fusion;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Networked] public bool IsGameActive  { get; private set; }
    [Networked] public bool IsPaused      { get; private set; }
    [Networked] public int  AlivePlayers  { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text gameStatusText;

    private ChangeDetector _changes;
    private float _lastSync = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            AlivePlayers = CountPlayers();
            IsGameActive = true;
            IsPaused     = false;
        }
        Debug.Log($"[GameManager] Bắt đầu với {AlivePlayers} người chơi!");
        UpdateUI();
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsPaused):
                    Time.timeScale = IsPaused ? 0f : 1f;
                    UpdateUI();
                    break;
                case nameof(IsGameActive):
                    UpdateUI();
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (Runner.SimulationTime - _lastSync >= 1f)
        {
            _lastSync    = Runner.SimulationTime;
            AlivePlayers = CountPlayers();
        }

        if (GetInput(out NetworkInputData input) && input.IsPausing)
            IsPaused = !IsPaused;
    }

    private void Update() => UpdateUI();

    public void OnPlayerEliminated(string playerName)
    {
        if (!HasStateAuthority) return;
        Debug.Log($"[GameManager] {playerName} bị loại!");
        AlivePlayers--;
        if (AlivePlayers <= 1) EndGame();
    }

    public void EndGame()
    {
        if (!HasStateAuthority) return;
        IsGameActive = false;
        Debug.Log("[GameManager] Game kết thúc!");
        Invoke(nameof(ReturnToLobby), 3f);
    }

    private void ReturnToLobby()
    {
        if (!HasStateAuthority) return;
        Runner.LoadScene(SceneRef.FromIndex(0));
    }

    private int CountPlayers()
    {
        int c = 0;
        foreach (var _ in Runner.ActivePlayers) c++;
        return c;
    }

    private void UpdateUI()
    {
        if (playerCountText != null && Runner != null)
            playerCountText.text = $"Players: {AlivePlayers}";

        if (gameStatusText == null) return;

        if (!IsGameActive)
        { gameStatusText.text = "Game - Kết thúc!"; gameStatusText.color = Color.red; }
        else if (IsPaused)
        { gameStatusText.text = "Game - Tạm dừng";  gameStatusText.color = Color.yellow; }
        else
        { gameStatusText.text = "Game - Đang chơi"; gameStatusText.color = Color.green; }
    }
}
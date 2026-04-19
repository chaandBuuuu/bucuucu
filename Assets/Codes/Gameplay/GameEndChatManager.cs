using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ FIXED GameEndChatManager:
///   - Restart/BackToLobby dùng RaceManager.RPC_RestartRace / RPC_BackToLobby
///   - Vote count dùng Runner.ActivePlayers thay vì MaxPlayers cứng
///   - Vote được sync qua ChatNetworkHandler RPC
///   - Chỉ host mới thực sự load scene
/// </summary>
public class GameEndChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas       gameEndCanvas;
    [SerializeField] private CanvasGroup  canvasGroup;
    [SerializeField] private TMP_Text     winnerText;
    [SerializeField] private TMP_Text     statsText;

    [Header("Chat")]
    [SerializeField] private Transform      chatMessagesContainer;
    [SerializeField] private GameObject     chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button         chatSendButton;
    [SerializeField] private ScrollRect     chatScrollRect;

    [Header("Vote Buttons")]
    [SerializeField] private Button   restartButton;
    [SerializeField] private Button   lobbyButton;
    [SerializeField] private Button   mainMenuButton;
    [SerializeField] private TMP_Text voteCountText;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeInDelay    = 1f;
    [SerializeField] private int   maxMessages    = 20;

    // ── Runtime ───────────────────────────────────────────────────────────────
    public static GameEndChatManager Instance { get; private set; }

    private CarController _winner;
    private bool _isGameEnded = false;
    private List<(CarController, int, float, float)> _finalRankings;

    // Vote: key = playerId, value = "restart" | "lobby"
    private Dictionary<int, string> _votes = new Dictionary<int, string>();
    private int _totalPlayers = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameEndCanvas != null) gameEndCanvas.enabled = false;
        if (canvasGroup   != null) canvasGroup.alpha     = 0f;

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnd       += OnRaceEnd;
            RaceManager.Instance.OnFinalRankings += OnFinalRankings;
        }

        if (chatSendButton  != null) chatSendButton.onClick.AddListener(OnSendChat);
        if (chatInputField  != null) chatInputField.onSubmit.AddListener(_ => OnSendChat());
        if (restartButton   != null) restartButton.onClick.AddListener(() => OnVote("restart"));
        if (lobbyButton     != null) lobbyButton.onClick.AddListener(() => OnVote("lobby"));
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnd       -= OnRaceEnd;
            RaceManager.Instance.OnFinalRankings -= OnFinalRankings;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Race End

    private void OnRaceEnd(CarController winner)
    {
        _winner    = winner;
        _isGameEnded = true;

        GameInputLocker.Instance?.LockInput(true);

        // Lấy số player thực tế
        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null)
        {
            _totalPlayers = 0;
            foreach (var _ in runner.ActivePlayers) _totalPlayers++;
        }

        StartCoroutine(ShowUI());
    }

    private void OnFinalRankings(List<(CarController, int, float, float)> rankings)
    {
        _finalRankings = rankings;
        UpdateStats();
    }

    private IEnumerator ShowUI()
    {
        yield return new WaitForSeconds(fadeInDelay);

        if (gameEndCanvas != null) gameEndCanvas.enabled = true;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void UpdateStats()
    {
        if (_winner != null && winnerText != null)
            winnerText.text = $"🏆 {_winner.name} Chiến Thắng!";

        if (_finalRankings != null && statsText != null)
        {
            string s = "📊 Bảng Xếp Hạng:\n";
            foreach (var (car, pos, time, dist) in _finalRankings)
                s += $"{pos}. {car.name} – {time:F1}s\n";
            statsText.text = s;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Chat

    private void OnSendChat()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text)) return;

        string msg  = chatInputField.text.Trim();
        string name = FusionNetworkManager.Instance?.GetStoredPlayerName() ?? "Player";
        chatInputField.text = "";

        // Dùng ChatNetworkHandler để broadcast
        if (ChatNetworkHandler.Instance != null)
            ChatNetworkHandler.Instance.RPC_Broadcast(name, $"[END] {msg}");
        else
            AddChatLocal(name, msg);
    }

    public void AddChatLocal(string playerName, string message)
    {
        if (chatMessagesContainer == null || chatMessagePrefab == null) return;

        var go = Instantiate(chatMessagePrefab, chatMessagesContainer);
        var ui = go.GetComponent<ChatMessageUI>();
        if (ui != null) ui.Initialize(playerName, message);

        while (chatMessagesContainer.childCount > maxMessages)
            Destroy(chatMessagesContainer.GetChild(0).gameObject);

        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Vote

    private void OnVote(string voteType)
    {
        if (!_isGameEnded) return;

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner == null) return;

        int playerId = runner.LocalPlayer.PlayerId;
        _votes[playerId] = voteType;

        string name = FusionNetworkManager.Instance?.GetStoredPlayerName() ?? "Player";

        // Broadcast vote qua chat
        if (ChatNetworkHandler.Instance != null)
            ChatNetworkHandler.Instance.RPC_Broadcast("System", $"🗳️ {name} voted: {voteType}");

        // ✅ Sync vote count qua RPC
        if (GameEndVoteHandler.Instance != null)
            GameEndVoteHandler.Instance.RPC_RegisterVote(playerId, voteType);

        UpdateVoteDisplay();
    }

    public void RegisterRemoteVote(int playerId, string voteType)
    {
        _votes[playerId] = voteType;
        UpdateVoteDisplay();
        CheckVoteResult();
    }

    private void UpdateVoteDisplay()
    {
        if (voteCountText == null) return;

        int restartCount = 0, lobbyCount = 0;
        foreach (var v in _votes.Values)
        {
            if (v == "restart") restartCount++;
            else if (v == "lobby") lobbyCount++;
        }

        voteCountText.text = $"🔄 Restart: {restartCount}/{_totalPlayers}  🏠 Lobby: {lobbyCount}/{_totalPlayers}";
    }

    private void CheckVoteResult()
    {
        // Majority vote
        int restartCount = 0, lobbyCount = 0;
        foreach (var v in _votes.Values)
        {
            if (v == "restart") restartCount++;
            else if (v == "lobby") lobbyCount++;
        }

        int majority = Mathf.CeilToInt(_totalPlayers / 2f);

        if (restartCount >= majority)
            ExecuteRestart();
        else if (lobbyCount >= majority)
            ExecuteBackToLobby();
    }

    private void ExecuteRestart()
    {
        Debug.Log("[GameEndChatManager] ✅ Majority voted restart");
        GameInputLocker.Instance?.LockInput(false);

        // ✅ Chỉ host load scene
        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null && runner.IsServer)
            RaceManager.Instance?.RPC_RestartRace();
    }

    private void ExecuteBackToLobby()
    {
        Debug.Log("[GameEndChatManager] ✅ Majority voted lobby");
        GameInputLocker.Instance?.LockInput(false);

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null && runner.IsServer)
            RaceManager.Instance?.RPC_BackToLobby();
    }

    private void OnMainMenu()
    {
        Debug.Log("[GameEndChatManager] Main Menu");
        GameInputLocker.Instance?.LockInput(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    #endregion
}


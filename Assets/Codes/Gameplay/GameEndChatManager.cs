using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ FIXED GameEndChatManager:
///   - Sửa lỗi cú pháp (method nằm trong method)
///   - Thêm fields rankingsContainer + rankingItemPrefab (public SerializeField)
///   - RankingItemUI nhận đúng tên người chơi từ FusionNetworkManager
///   - Restart/Lobby dùng RaceManager RPC
///   - Vote sync qua GameEndVoteHandler RPC
/// </summary>
public class GameEndChatManager : MonoBehaviour
{
    public static GameEndChatManager Instance { get; private set; }

    [Header("UI – Tổng quan")]
    [SerializeField] private Canvas       gameEndCanvas;
    [SerializeField] private CanvasGroup  canvasGroup;
    [SerializeField] private TMP_Text     winnerText;
    [SerializeField] private TMP_Text     statsText;

    [Header("Rankings")]
    [SerializeField] private Transform  rankingsContainer;   // ScrollView Content chứa ranking items
    [SerializeField] private GameObject rankingItemPrefab;   // Prefab có RankingItemUI component

    [Header("Chat")]
    [SerializeField] private Transform      chatMessagesContainer;
    [SerializeField] private GameObject     chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button         chatSendButton;
    [SerializeField] private ScrollRect     chatScrollRect;

    [Header("Vote")]
    // ✅ MOVED to RaceRankingsDisplay: restartButton, lobbyButton
    [SerializeField] private Button   mainMenuButton;  // ✅ Only keep Menu button
    [SerializeField] private TMP_Text voteCountText;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeInDelay    = 1f;
    [SerializeField] private int   maxMessages    = 20;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private CarController _winner;
    private bool          _isGameEnded = false;
    private List<(CarController car, int pos, float time, float dist)> _finalRankings;
    private Dictionary<int, string> _votes       = new Dictionary<int, string>();
    private int                     _totalPlayers = 1;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameEndCanvas != null) gameEndCanvas.enabled = false;
        if (canvasGroup   != null) canvasGroup.alpha     = 0f;

        // Subscribe events (RaceManager có thể chưa spawn ngay)
        StartCoroutine(SubscribeWhenReady());

        if (chatSendButton != null) chatSendButton.onClick.AddListener(OnSendChat);
        if (chatInputField != null) chatInputField.onSubmit.AddListener(_ => OnSendChat());
        // ✅ MOVED: Vote buttons now handled by RaceRankingsDisplay
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Chờ RaceManager spawn xong (NetworkBehaviour)
        while (RaceManager.Instance == null)
            yield return new WaitForSeconds(0.2f);

        RaceManager.Instance.OnRaceEnd       += OnRaceEnd;
        RaceManager.Instance.OnFinalRankings += OnFinalRankings;
        Debug.Log("[GameEndChatManager] ✅ Subscribed to RaceManager events");

        // ✅ Late-joiner replay: Nếu race đã kết thúc trước khi subscribe, replay sự kiện
        if (RaceManager.Instance.RaceFinished)
        {
            Debug.Log("[GameEndChatManager] ⚡ Race already finished - replaying events for late joiner");
            var rankings = RaceManager.Instance.GetCachedFinalRankings();
            var winner = RaceManager.Instance.GetCachedWinner();

            if (rankings != null && rankings.Count > 0)
            {
                OnFinalRankings(rankings);
            }
            if (winner != null)
            {
                OnRaceEnd(winner);
            }
        }
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
        _winner      = winner;
        _isGameEnded = true;
        _votes.Clear();

        GameInputLocker.Instance?.LockInput(true);

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null)
        {
            _totalPlayers = 0;
            foreach (var _ in runner.ActivePlayers) _totalPlayers++;
        }

        UpdateVoteDisplay();
        StartCoroutine(ShowUI());
    }

    private void OnFinalRankings(List<(CarController, int, float, float)> rankings)
    {
        // Convert to named tuple
        _finalRankings = new List<(CarController, int, float, float)>();
        foreach (var r in rankings) _finalRankings.Add(r);
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
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void UpdateStats()
    {
        // Winner text
        if (_winner != null && winnerText != null)
        {
            string winnerName = GetPlayerDisplayName(_winner);
            winnerText.text = $"🏆 {winnerName} Chiến Thắng!";
        }

        // Stats text (plain text summary)
        if (_finalRankings != null && statsText != null)
        {
            string s = "📊 Bảng Xếp Hạng:\n";
            foreach (var (car, pos, time, dist) in _finalRankings)
                s += $"{pos}. {GetPlayerDisplayName(car)} – {time:F1}s\n";
            statsText.text = s;
        }

        // ✅ Ranking items UI
        if (_finalRankings != null && rankingsContainer != null && rankingItemPrefab != null)
        {
            // Xoá items cũ
            foreach (Transform child in rankingsContainer)
                Destroy(child.gameObject);

            // Tạo item mới cho từng ranking
            foreach (var (car, pos, time, dist) in _finalRankings)
            {
                var go = Instantiate(rankingItemPrefab, rankingsContainer);
                go.SetActive(true);

                var ui = go.GetComponent<RankingItemUI>();
                if (ui != null)
                {
                    // ✅ FIX: Dùng tên người chơi thực, không phải car.name
                    string playerName = GetPlayerDisplayName(car);
                    ui.Initialize(pos, playerName, time);
                }
                else
                {
                    Debug.LogError("[GameEndChatManager] rankingItemPrefab thiếu RankingItemUI component!");
                }
            }
        }
        else if (rankingsContainer == null)
        {
            Debug.LogWarning("[GameEndChatManager] rankingsContainer chưa gán trong Inspector!");
        }
        else if (rankingItemPrefab == null)
        {
            Debug.LogWarning("[GameEndChatManager] rankingItemPrefab chưa gán trong Inspector!");
        }
    }

    /// <summary>
    /// ✅ Lấy tên người chơi từ FusionNetworkManager thay vì dùng car.name
    /// </summary>
    private string GetPlayerDisplayName(CarController car)
    {
        if (car == null) return "Unknown";
        if (car.Object == null) return car.name;

        if (FusionNetworkManager.Instance != null)
        {
            string name = FusionNetworkManager.Instance.GetPlayerName(car.Object.InputAuthority);
            if (!string.IsNullOrEmpty(name)) return name;
        }
        return car.name;
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

        int    playerId = runner.LocalPlayer.PlayerId;
        string name     = FusionNetworkManager.Instance?.GetStoredPlayerName() ?? "Player";

        // Broadcast thông báo vote qua chat
        if (ChatNetworkHandler.Instance != null)
            ChatNetworkHandler.Instance.RPC_Broadcast("System", $"🗳️ {name} voted: {voteType}");

        // Sync vote tới tất cả clients
        if (GameEndVoteHandler.Instance != null)
            GameEndVoteHandler.Instance.RPC_RegisterVote(playerId, voteType);
        else
        {
            // Fallback nếu VoteHandler chưa spawn
            RegisterRemoteVote(playerId, voteType);
        }
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

        int r = 0, l = 0;
        foreach (var v in _votes.Values)
        {
            if (v == "restart") r++;
            else if (v == "lobby") l++;
        }
        voteCountText.text = $"🔄 Restart: {r}/{_totalPlayers}  🏠 Lobby: {l}/{_totalPlayers}";
    }

    private void CheckVoteResult()
    {
        int r = 0, l = 0;
        foreach (var v in _votes.Values)
        {
            if (v == "restart") r++;
            else if (v == "lobby") l++;
        }

        int majority = Mathf.CeilToInt(_totalPlayers / 2f);
        if      (r >= majority) ExecuteRestart();
        else if (l >= majority) ExecuteBackToLobby();
    }

    private void ExecuteRestart()
    {
        Debug.Log("[GameEndChatManager] ✅ Restart voted");
        GameInputLocker.Instance?.LockInput(false);
        _isGameEnded = false;

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null && runner.IsServer)
            RaceManager.Instance?.RPC_RestartRace();
    }

    private void ExecuteBackToLobby()
    {
        Debug.Log("[GameEndChatManager] ✅ Back to Lobby voted");
        GameInputLocker.Instance?.LockInput(false);
        _isGameEnded = false;

        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null && runner.IsServer)
            RaceManager.Instance?.RPC_BackToLobby();
    }

    private void OnMainMenu()
    {
        Debug.Log("[GameEndChatManager] Main Menu");
        GameInputLocker.Instance?.LockInput(false);
        _isGameEnded = false;

        // Shutdown Fusion trước khi load menu
        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner != null) runner.Shutdown();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Reset (gọi khi scene reload)

    /// <summary>Gọi khi restart race để reset state UI</summary>
    public void ResetForNewRace()
    {
        _winner      = null;
        _isGameEnded = false;
        _finalRankings = null;
        _votes.Clear();

        if (gameEndCanvas != null) gameEndCanvas.enabled = false;
        if (canvasGroup   != null) canvasGroup.alpha     = 0f;

        // Xoá ranking items cũ
        if (rankingsContainer != null)
            foreach (Transform child in rankingsContainer)
                Destroy(child.gameObject);
    }

    #endregion
}
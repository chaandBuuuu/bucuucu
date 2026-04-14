using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ✅ NEW: Game End UI with Chat + Vote Restart
/// - Shows when race ends
/// - Displays winner + game stats
/// - Chat system for players
/// - Vote buttons to restart race
/// - Locks game input
/// </summary>
public class GameEndChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas gameEndCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statsText;

    [Header("Chat System")]
    [SerializeField] private Transform chatMessagesContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button chatSendButton;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Vote System")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToLobbyButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TMP_Text voteCountText;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeInDelay = 0.5f;
    [SerializeField] private int maxChatMessages = 20;

    private List<ChatMessage> _chatMessages = new List<ChatMessage>();
    private Dictionary<PlayerRef, bool> _restartVotes = new Dictionary<PlayerRef, bool>();
    private CarController _winner;
    private float _raceDuration;
    private bool _isGameEnded = false;
    private List<(CarController, int, float, float)> _finalRankings;

    private void Start()
    {
        // Initialize canvas (disabled initially)
        if (gameEndCanvas != null)
            gameEndCanvas.enabled = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // Setup event listeners
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnd += OnRaceEnd;
            RaceManager.Instance.OnFinalRankings += OnFinalRankings;
        }

        // Setup chat
        if (chatSendButton != null)
            chatSendButton.onClick.AddListener(OnSendChatMessage);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(_ => OnSendChatMessage());

        // Setup vote buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(OnVoteRestart);

        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobby);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);

        Debug.Log("[GameEndChatManager] Initialized");
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnd -= OnRaceEnd;
            RaceManager.Instance.OnFinalRankings -= OnFinalRankings;
        }
    }

    /// ✅ Called when race ends
    private void OnRaceEnd(CarController winner)
    {
        _winner = winner;
        _isGameEnded = true;

        Debug.Log($"[GameEndChatManager] Race ended! Winner: {winner.name}");

        // ✅ NEW: Lock game input
        GameInputLocker.Instance?.LockInput(true);

        // Show UI with delay and fade
        StartCoroutine(ShowGameEndUI());
    }

    /// ✅ Called with final rankings
    private void OnFinalRankings(List<(CarController, int, float, float)> rankings)
    {
        _finalRankings = rankings;
        UpdateStatsDisplay();
    }

    /// ✅ Fade in game end UI
    private System.Collections.IEnumerator ShowGameEndUI()
    {
        yield return new WaitForSeconds(fadeInDelay);

        if (gameEndCanvas != null)
            gameEndCanvas.enabled = true;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Debug.Log("[GameEndChatManager] Game end UI faded in");
    }

    /// ✅ Update stats display
    private void UpdateStatsDisplay()
    {
        if (_winner != null && winnerText != null)
        {
            winnerText.text = $"🏆 {_winner.name} Chiến Thắng!";
        }

        if (_finalRankings != null && statsText != null)
        {
            string stats = "📊 Bảng Xếp Hạng:\n";
            for (int i = 0; i < _finalRankings.Count; i++)
            {
                var (car, pos, time, dist) = _finalRankings[i];
                stats += $"{pos}. {car.name} - {time:F1}s\n";
            }
            statsText.text = stats;
        }
    }

    /// ✅ Send chat message
    private void OnSendChatMessage()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
            return;

        string message = chatInputField.text;
        chatInputField.text = "";

        // Send to all players
        if (FusionNetworkManager.Instance != null)
        {
            var playerName = FusionNetworkManager.Instance.GetStoredPlayerName();
            AddChatMessage(playerName, message);

            // ✅ TODO: Send via RPC to sync across network
            // RPC_BroadcastChatMessage(playerName, message);
        }
    }

    /// ✅ Add chat message to display
    private void AddChatMessage(string playerName, string message)
    {
        if (chatMessagesContainer == null || chatMessagePrefab == null)
            return;

        GameObject msgGO = Instantiate(chatMessagePrefab, chatMessagesContainer);
        var msgUI = msgGO.GetComponent<ChatMessageUI>();

        if (msgUI != null)
        {
            msgUI.Initialize(playerName, message);
        }

        // Limit messages
        while (chatMessagesContainer.childCount > maxChatMessages)
        {
            Destroy(chatMessagesContainer.GetChild(0).gameObject);
        }

        // Auto-scroll to bottom
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// ✅ Vote to restart
    private void OnVoteRestart()
    {
        if (!_isGameEnded) return;

        PlayerRef localPlayer = FusionNetworkManager.Instance?.Runner?.LocalPlayer ?? default;
        _restartVotes[localPlayer] = true;

        Debug.Log($"[GameEndChatManager] Player voted to restart");
        UpdateVoteDisplay();

        // ✅ TODO: RPC to sync votes
        // RPC_RegisterRestartVote(localPlayer);

        // Check if all voted
        int totalPlayers = FusionNetworkManager.Instance?.Runner?.SessionInfo.MaxPlayers ?? 1;
        if (_restartVotes.Count >= totalPlayers)
        {
            RestartRace();
        }
    }

    /// ✅ Update vote display
    private void UpdateVoteDisplay()
    {
        if (voteCountText != null)
        {
            int totalPlayers = 4; // TODO: Get from Runner
            voteCountText.text = $"💬 Vote Restart: {_restartVotes.Count}/{totalPlayers}";
        }
    }

    /// ✅ Restart race
    private void RestartRace()
    {
        Debug.Log("[GameEndChatManager] Restarting race...");

        // ✅ NEW: Unlock input
        GameInputLocker.Instance?.LockInput(false);

        // Reset
        _isGameEnded = false;
        _restartVotes.Clear();
        _chatMessages.Clear();

        // Clear chat display
        if (chatMessagesContainer != null)
        {
            foreach (Transform child in chatMessagesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Hide UI
        if (gameEndCanvas != null)
            gameEndCanvas.enabled = false;

        // TODO: Reload scene or reset race state
    }

    /// ✅ Back to Lobby
    private void OnBackToLobby()
    {
        Debug.Log("[GameEndChatManager] Back to Lobby");
        GameInputLocker.Instance?.LockInput(false);
        // TODO: Load Lobby scene
    }

    /// ✅ Back to Main Menu
    private void OnMainMenu()
    {
        Debug.Log("[GameEndChatManager] Back to Main Menu");
        GameInputLocker.Instance?.LockInput(false);
        // TODO: Load Main Menu scene
    }
}

/// <summary>
/// ✅ Individual chat message display
/// </summary>
public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message)
    {
        if (playerNameText != null)
            playerNameText.text = $"<color=cyan>{playerName}:</color>";

        if (messageText != null)
            messageText.text = message;
    }
}

/// <summary>
/// ✅ Internal chat message data
/// </summary>
public struct ChatMessage
{
    public string PlayerName;
    public string Message;
    public float Timestamp;
}

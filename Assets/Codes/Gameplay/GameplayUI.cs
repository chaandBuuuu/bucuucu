using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI cho lobby và character selection
/// </summary>
public class LobbyCharacterSelectManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GridLayoutGroup characterGrid;
    [SerializeField] private TextMeshProUGUI selectedCharacterText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;

    private CharacterID _selectedCharacter = CharacterID.Hunt1;
    private Button[] _characterButtons;

    private void Start()
    {
        // Create character selection buttons
        var characterDb = CharacterDatabase.Instance;
        
        for (int i = 0; i < 6; i++)
        {
            CharacterID id = (CharacterID)i;
            var config = characterDb.GetCharacter(id);
            
            if (config != null)
            {
                CreateCharacterButton(config);
            }
        }

        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private void CreateCharacterButton(CharacterConfig config)
    {
        var buttonGO = new GameObject(config.characterName);
        buttonGO.transform.SetParent(characterGrid.transform);

        var button = buttonGO.AddComponent<Button>();
        var image = buttonGO.AddComponent<Image>();
        image.color = config.uiColor;

        var textGO = new GameObject("Name");
        textGO.transform.SetParent(buttonGO.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = config.characterName;

        button.onClick.AddListener(() => SelectCharacter(config.characterId));
    }

    private void SelectCharacter(CharacterID id)
    {
        _selectedCharacter = id;
        var config = CharacterDatabase.Instance.GetCharacter(id);
        selectedCharacterText.text = $"Selected: {config.characterName}";
        
        Debug.Log($"[LobbyCharacterSelectManager] Selected {config.characterName}");
    }

    private void OnReadyClicked()
    {
        // Mark player as ready
        Debug.Log($"[LobbyCharacterSelectManager] Ready with {_selectedCharacter}");
        
        // Send to game manager / network
        var gameStartController = FindAnyObjectByType<GameStartController>();
        if (gameStartController != null)
        {
            gameStartController.RPC_PlayerReadyWithCharacter((int)_selectedCharacter);
        }
    }

    private void OnStartGameClicked()
    {
        var gameManager = GameplayStateManager.Instance;
        if (gameManager != null)
        {
            gameManager.RPC_StartGame();
        }
    }
}

/// <summary>
/// Quản lý UI lúc đang chơi
/// </summary>
public class GameplayUIManager : NetworkBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Bonfire UI")]
    [SerializeField] private Transform bonfireContainer;
    [SerializeField] private GameObject bonfireUIPrefab;

    [Header("Status Effects UI")]
    [SerializeField] private Transform statusEffectContainer;
    [SerializeField] private GameObject statusEffectIconPrefab;

    [Header("Objective UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI timerText;

    private NetworkCharacterController _localCharacter;
    private GameplayStateManager _gameManager;

    private void Start()
    {
        _gameManager = GameplayStateManager.Instance;

        // Subscribe to events
        if (_gameManager != null)
        {
            _gameManager.OnBonfireLit += OnBonfireLit;
            _gameManager.OnPhaseChanged += OnGamePhaseChanged;
        }
    }

    private void Update()
    {
        if (_localCharacter == null)
            FindLocalCharacter();

        if (_localCharacter != null)
        {
            UpdateHealthUI();
            UpdateStatusEffectsUI();
        }

        UpdateObjectiveUI();
        UpdateTimerUI();
    }

    private void FindLocalCharacter()
    {
        var allCharacters = FindObjectsByType<NetworkCharacterController>();
        foreach (var character in allCharacters)
        {
            if (character.HasInputAuthority)
            {
                _localCharacter = character;
                break;
            }
        }
    }

    private void UpdateHealthUI()
    {
        healthSlider.value = _localCharacter.CurrentHealth / 100f;
        healthText.text = $"HP: {_localCharacter.CurrentHealth:F0}/100";
    }

    private void UpdateStatusEffectsUI()
    {
        var statusManager = _localCharacter.GetStatusEffectManager();
        var effects = statusManager.GetActiveEffects();

        // Clear old UI
        foreach (Transform child in statusEffectContainer)
            Destroy(child.gameObject);

        // Create new UI for active effects
        foreach (var effect in effects)
        {
            var icon = Instantiate(statusEffectIconPrefab, statusEffectContainer);
            var text = icon.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{effect.effectType}\n{effect.RemainingTime:F1}s";
        }
    }

    private void UpdateObjectiveUI()
    {
        if (_localCharacter == null) return;

        if (_localCharacter.IsHunter)
        {
            objectiveText.text = "Objective: Eliminate all survivors!";
        }
        else
        {
            objectiveText.text = "Objective: Collect wood and escape!";
        }
    }

    private void UpdateTimerUI()
    {
        if (_gameManager != null)
        {
            timerText.text = $"Time: {_gameManager.GameTimer:F0}s";
        }
    }

    private void OnBonfireLit()
    {
        Debug.Log("[GameplayUIManager] Bonfire lit!");
        // Update bonfire UI
    }

    private void OnGamePhaseChanged(GamePhase phase)
    {
        Debug.Log($"[GameplayUIManager] Phase changed to {phase}");
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnBonfireLit -= OnBonfireLit;
            _gameManager.OnPhaseChanged -= OnGamePhaseChanged;
        }
    }
}

/// <summary>
/// Hiển thị kết quả game
/// </summary>
public class GameEndUIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        var gameManager = GameplayStateManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnGameEnd += OnGameEnd;
        }

        restartButton.onClick.AddListener(RestartGame);
        gameEndPanel.SetActive(false);
    }

    private void OnGameEnd(GameWinner winner)
    {
        gameEndPanel.SetActive(true);

        if (winner == GameWinner.Hunter)
        {
            resultText.text = "HUNTER WINS!\nAll survivors eliminated!";
        }
        else if (winner == GameWinner.Survivors)
        {
            resultText.text = "SURVIVORS WIN!\nEscaped successfully!";
        }
    }

    private void RestartGame()
    {
        // Reload scene or return to lobby
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    private void OnDestroy()
    {
        var gameManager = GameplayStateManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnGameEnd -= OnGameEnd;
        }
    }
}

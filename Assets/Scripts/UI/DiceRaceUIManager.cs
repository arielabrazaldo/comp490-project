using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements and interactions for the Dice Race game.
/// Handles game panel display, dice rolling, turn management, and game state displays.
/// Similar architecture to BattleshipsUIManager for consistency.
/// </summary>
public class DiceRaceUIManager : MonoBehaviour
{
    private static DiceRaceUIManager instance;
    public static DiceRaceUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DiceRaceUIManager>();
            }
            return instance;
        }
    }

    #region UI References

    [Header("Main Panels")]
    [SerializeField] private GameObject diceRaceGamePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game UI Elements")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private TextMeshProUGUI diceResultText;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Button leaveGameButton;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button playAgainButton;

    [Header("Player Status Display")]
    [SerializeField] private Transform playerStatusContainer;
    [SerializeField] private GameObject playerStatusPrefab;

    #endregion

    #region Private State

    private bool isSubscribedToEvents = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeButtons();
    }

    private void Start()
    {
        // Start with panels hidden
        HideAllPanels();
        Debug.Log("[DiceRaceUIManager] Initialized");
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    #endregion

    #region Initialization

    private void InitializeButtons()
    {
        // Game buttons
        if (rollDiceButton) rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        if (leaveGameButton) leaveGameButton.onClick.AddListener(OnLeaveGameClicked);

        // Game over buttons
        if (returnToMenuButton) returnToMenuButton.onClick.AddListener(OnLeaveGameClicked);
        if (playAgainButton) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    #endregion

    #region Event Subscription

    /// <summary>
    /// Subscribe to NetworkGameManager events for UI updates
    /// </summary>
    public void SubscribeToGameEvents()
    {
        if (isSubscribedToEvents) return;

        NetworkGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
        NetworkGameManager.OnPlayerMoved += OnPlayerMoved;
        NetworkGameManager.OnGameStateChanged += OnGameStateChanged;

        isSubscribedToEvents = true;
        Debug.Log("[DiceRaceUIManager] ? Subscribed to game events");
    }

    /// <summary>
    /// Unsubscribe from NetworkGameManager events
    /// </summary>
    public void UnsubscribeFromGameEvents()
    {
        if (!isSubscribedToEvents) return;

        NetworkGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
        NetworkGameManager.OnPlayerMoved -= OnPlayerMoved;
        NetworkGameManager.OnGameStateChanged -= OnGameStateChanged;

        isSubscribedToEvents = false;
        Debug.Log("[DiceRaceUIManager] Unsubscribed from game events");
    }

    #endregion

    #region Panel Management

    private void HideAllPanels()
    {
        if (diceRaceGamePanel) diceRaceGamePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Show the main Dice Race game panel
    /// </summary>
    public void ShowGamePanel()
    {
        HideAllPanels();
        
        if (diceRaceGamePanel)
        {
            diceRaceGamePanel.SetActive(true);
        }

        // Subscribe to events when showing panel
        SubscribeToGameEvents();

        // Update UI with initial values
        UpdateGameUI();

        Debug.Log("[DiceRaceUIManager] Showing game panel");
    }

    /// <summary>
    /// Show the game over panel with winner info
    /// </summary>
    public void ShowGameOverPanel(int winnerId)
    {
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
        }

        if (winnerText)
        {
            winnerText.text = $"Player {winnerId + 1} Wins!";
        }

        Debug.Log($"[DiceRaceUIManager] Game Over - Player {winnerId + 1} wins!");
    }

    /// <summary>
    /// Hide all panels and cleanup
    /// </summary>
    public void HideAndCleanup()
    {
        UnsubscribeFromGameEvents();
        HideAllPanels();
        Debug.Log("[DiceRaceUIManager] Hidden and cleaned up");
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Update the game UI with current state
    /// </summary>
    private void UpdateGameUI()
    {
        if (NetworkGameManager.Instance == null) return;

        // Update current player text
        int currentPlayerId = NetworkGameManager.Instance.GetCurrentPlayerId();
        UpdateCurrentPlayerDisplay(currentPlayerId);

        // Update game status
        var gameState = NetworkGameManager.Instance.GetGameState();
        UpdateGameStatusDisplay(gameState);

        // Update roll button state
        UpdateRollButtonState();

        // Clear dice result
        if (diceResultText)
        {
            diceResultText.text = "";
        }
    }

    /// <summary>
    /// Update the current player display
    /// </summary>
    private void UpdateCurrentPlayerDisplay(int playerId)
    {
        if (currentPlayerText != null)
        {
            bool isMyTurn = NetworkGameManager.Instance?.IsMyTurn() ?? false;
            string turnIndicator = isMyTurn ? " (Your Turn!)" : "";
            currentPlayerText.text = $"Player {playerId + 1}'s Turn{turnIndicator}";
            
            // Change color based on whose turn
            currentPlayerText.color = isMyTurn ? Color.green : Color.white;
        }
    }

    /// <summary>
    /// Update the game status display
    /// </summary>
    private void UpdateGameStatusDisplay(NetworkGameManager.GameState state)
    {
        if (gameStatusText == null) return;

        switch (state)
        {
            case NetworkGameManager.GameState.WaitingToStart:
                gameStatusText.text = "Waiting for game to start...";
                break;
            case NetworkGameManager.GameState.InProgress:
                gameStatusText.text = "Game in progress - Roll the dice!";
                break;
            case NetworkGameManager.GameState.GameOver:
                gameStatusText.text = "Game Over!";
                break;
        }
    }

    /// <summary>
    /// Update the roll button interactability
    /// </summary>
    private void UpdateRollButtonState()
    {
        if (rollDiceButton == null) return;

        bool canRoll = NetworkGameManager.Instance != null && 
                       NetworkGameManager.Instance.IsMyTurn() &&
                       NetworkGameManager.Instance.GetGameState() == NetworkGameManager.GameState.InProgress;

        rollDiceButton.interactable = canRoll;
    }

    /// <summary>
    /// Display dice roll result
    /// </summary>
    public void ShowDiceResult(int result)
    {
        if (diceResultText != null)
        {
            diceResultText.text = $"Rolled: {result}";
        }
    }

    /// <summary>
    /// Show a status message
    /// </summary>
    public void ShowMessage(string message, Color? color = null)
    {
        if (gameStatusText != null)
        {
            gameStatusText.text = message;
            if (color.HasValue)
            {
                gameStatusText.color = color.Value;
            }
        }
        Debug.Log($"[DiceRaceUIManager] {message}");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle player turn change
    /// </summary>
    private void OnPlayerTurnChanged(int newPlayerId)
    {
        Debug.Log($"[DiceRaceUIManager] Turn changed to player {newPlayerId}");

        UpdateCurrentPlayerDisplay(newPlayerId);
        UpdateRollButtonState();

        // Clear previous dice result on turn change
        if (diceResultText)
        {
            diceResultText.text = "";
        }
    }

    /// <summary>
    /// Handle player movement
    /// </summary>
    private void OnPlayerMoved(int playerId, int newPosition)
    {
        Debug.Log($"[DiceRaceUIManager] Player {playerId} moved to position {newPosition}");

        if (gameStatusText != null)
        {
            gameStatusText.text = $"Player {playerId + 1} moved to tile {newPosition}";
        }
    }

    /// <summary>
    /// Handle game state change
    /// </summary>
    private void OnGameStateChanged(NetworkGameManager.GameState newState)
    {
        Debug.Log($"[DiceRaceUIManager] Game state changed to: {newState}");

        UpdateGameStatusDisplay(newState);

        if (newState == NetworkGameManager.GameState.GameOver)
        {
            // Get winner and show game over panel
            // For now, just show the panel - winner detection handled elsewhere
            if (gameOverPanel)
            {
                gameOverPanel.SetActive(true);
            }
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handle roll dice button click
    /// </summary>
    private void OnRollDiceClicked()
    {
        Debug.Log("[DiceRaceUIManager] Roll Dice button clicked");

        if (NetworkGameManager.Instance == null)
        {
            ShowMessage("Error: Game manager not found!", Color.red);
            return;
        }

        if (!NetworkGameManager.Instance.IsMyTurn())
        {
            ShowMessage("Not your turn!", Color.red);
            return;
        }

        // Disable button while processing
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = false;
        }

        // Roll the dice
        NetworkGameManager.Instance.RollDice();
    }

    /// <summary>
    /// Handle leave game button click
    /// </summary>
    private async void OnLeaveGameClicked()
    {
        Debug.Log("[DiceRaceUIManager] Leave Game button clicked");

        // Cleanup
        UnsubscribeFromGameEvents();
        HideAllPanels();

        // Leave lobby and return to main menu
        try
        {
            if (LobbyManager.Instance != null)
            {
                await LobbyManager.Instance.LeaveLobby();
            }

            // Return to main menu via UIManager_Streamlined
            if (UIManager_Streamlined.Instance != null)
            {
                UIManager_Streamlined.Instance.ShowMainMenuPublic();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DiceRaceUIManager] Error leaving game: {e.Message}");
            
            // Still try to show main menu
            if (UIManager_Streamlined.Instance != null)
            {
                UIManager_Streamlined.Instance.ShowMainMenuPublic();
            }
        }
    }

    /// <summary>
    /// Handle play again button click
    /// </summary>
    private void OnPlayAgainClicked()
    {
        Debug.Log("[DiceRaceUIManager] Play Again button clicked");

        // For now, just return to saved games to host a new game
        HideAllPanels();
        
        if (UIManager_Streamlined.Instance != null)
        {
            UIManager_Streamlined.Instance.ShowGameModeSelectionPublic();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initialize and show the Dice Race game UI
    /// Called by UIManager_Streamlined or NetworkGameManager when starting a Dice Race game
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[DiceRaceUIManager] Starting Dice Race game UI");
        
        // Hide UIManager_Streamlined panels (lobby, menu, etc.)
        if (UIManager_Streamlined.Instance != null)
        {
            UIManager_Streamlined.Instance.HideAllPanelsPublic();
            Debug.Log("[DiceRaceUIManager] Hidden UIManager_Streamlined panels");
        }
        
        ShowGamePanel();
    }

    /// <summary>
    /// End the game and show results
    /// </summary>
    public void EndGame(int winnerId)
    {
        Debug.Log($"[DiceRaceUIManager] Ending game - Winner: Player {winnerId + 1}");
        ShowGameOverPanel(winnerId);
    }

    /// <summary>
    /// Check if the UI manager is ready
    /// </summary>
    public bool IsReady()
    {
        return diceRaceGamePanel != null;
    }

    #endregion
}

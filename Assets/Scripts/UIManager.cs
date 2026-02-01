using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIManager>();
            }
            return instance;
        }
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject savedGamesPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject diceRaceGamePanel;
    [SerializeField] private GameObject monopolyGamePanel;

    [Header("Main Menu")]
    [SerializeField] private Button createCustomGameButton;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button exitButton;

    [Header("Saved Games Panel")]
    [SerializeField] private Transform savedGamesListContent;
    [SerializeField] private GameObject savedGameItemPrefab;
    [SerializeField] private Button backFromSavedGamesButton;
    [SerializeField] private TextMeshProUGUI noSavedGamesText;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button submitJoinButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI lobbyTitleText;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;

    [Header("Dice Race Game Panel - UI Elements")]
    [SerializeField] private TextMeshProUGUI diceRaceCurrentPlayerText;
    [SerializeField] private TextMeshProUGUI diceRaceGameStatusText;
    [SerializeField] private Button diceRaceRollDiceButton;
    [SerializeField] private TextMeshProUGUI diceRaceDiceResultText;
    [SerializeField] private Button diceRaceLeaveGameButton;

    [Header("Monopoly Game Panel - UI Elements")]
    [SerializeField] private TextMeshProUGUI monopolyCurrentPlayerText;
    [SerializeField] private TextMeshProUGUI monopolyGameStatusText;
    [SerializeField] private Button monopolyRollDiceButton;
    [SerializeField] private Button purchasePropertyButton;
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private TextMeshProUGUI gameMessagesText;
    [SerializeField] private TextMeshProUGUI cardMessagesText;
    [SerializeField] private Button monopolyLeaveGameButton;

    [Header("Property Ownership UI")]
    [SerializeField] private GameObject propertyOwnershipPanel;
    [SerializeField] private TextMeshProUGUI propertyOwnershipText;
    [SerializeField] private Button togglePropertyListButton;

    [Header("Game Mode Settings")]
    [SerializeField] private bool isMonopolyMode = false;
    [SerializeField] private bool isBattleshipsMode = false;

    private Coroutine statusClearCoroutine;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 2f;
    private bool isSubscribedToNetworkEvents = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        HideAllPanels();
    }

    private void Start()
    {
        ValidateUIReferences();
        SetupButtonListeners();
        ShowMainMenu();
        SetupPlayerListLayout();

        // Subscribe to network events if NetworkGameManager already exists
        if (NetworkGameManager.Instance != null)
        {
            SubscribeToNetworkEvents();
            isSubscribedToNetworkEvents = true;
        }
    }

    private void OnDestroy()
    {
        if (isSubscribedToNetworkEvents)
        {
            UnsubscribeFromNetworkEvents();
            isSubscribedToNetworkEvents = false;
        }
    }

    #region Network Event Subscriptions

    private void SubscribeToNetworkEvents()
    {
        Debug.Log("Subscribing to network events...");

        if (isMonopolyMode)
        {
            // Subscribe to Monopoly events
            MonopolyGameManager.OnGameStarted += OnGameStarted;
            MonopolyGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
            MonopolyGameManager.OnPlayerMoved += OnPlayerMoved;
            MonopolyGameManager.OnPlayerMoneyChanged += OnPlayerMoneyChanged;
            MonopolyGameManager.OnPropertyPurchased += OnPropertyPurchased;
            MonopolyGameManager.OnGameStateChanged += OnMonopolyGameStateChanged;
            MonopolyGameManager.OnGameMessage += OnGameMessage;

            // Subscribe to trade events
            MonopolyTradeManager.OnGameMessage += OnGameMessage;
        }
        else
        {
            // Subscribe to regular dice race events
            NetworkGameManager.OnGameStarted += OnGameStarted;
            NetworkGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
            NetworkGameManager.OnPlayerMoved += OnPlayerMoved;
            NetworkGameManager.OnGameStateChanged += OnGameStateChanged;
        }
    }

    private void UnsubscribeFromNetworkEvents()
    {
        Debug.Log("Unsubscribing from network events...");

        if (isMonopolyMode)
        {
            // Unsubscribe from Monopoly events
            MonopolyGameManager.OnGameStarted -= OnGameStarted;
            MonopolyGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
            MonopolyGameManager.OnPlayerMoved -= OnPlayerMoved;
            MonopolyGameManager.OnPlayerMoneyChanged -= OnPlayerMoneyChanged;
            MonopolyGameManager.OnPropertyPurchased -= OnPropertyPurchased;
            MonopolyGameManager.OnGameStateChanged -= OnMonopolyGameStateChanged;
            MonopolyGameManager.OnGameMessage -= OnGameMessage;
        }
        else
        {
            // Unsubscribe from regular dice race events
            NetworkGameManager.OnGameStarted -= OnGameStarted;
            NetworkGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
            NetworkGameManager.OnPlayerMoved -= OnPlayerMoved;
            NetworkGameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    #endregion

    #region Network Event Handlers

    private void OnGameStarted()
    {
        Debug.Log("Game started - showing game panel");
        ShowGamePanel();
    }

    /// <summary>
    /// Called directly by NetworkGameManager when game starts (for all clients)
    /// </summary>
    public void OnNetworkGameStarted()
    {
        Debug.Log("Network game started - switching to game panel");
        ShowGamePanel();
    }

    private void OnPlayerTurnChanged(int currentPlayerId)
    {
        UpdateCurrentPlayerDisplay(currentPlayerId);
        UpdateGameButtons();
    }

    private void OnPlayerMoved(int playerId, int newPosition)
    {
        string locationText = isMonopolyMode ? "space" : "position";

        if (isMonopolyMode && gameMessagesText != null)
        {
            gameMessagesText.text += $"Player {playerId + 1} moved to {locationText} {newPosition}\n";
        }
        else if (!isMonopolyMode && diceRaceDiceResultText != null)
        {
            diceRaceDiceResultText.text = $"Player {playerId + 1} moved to {locationText} {newPosition}";
        }
    }

    private void OnPlayerMoneyChanged(int playerId, int newMoney)
    {
        if (isMonopolyMode)
        {
            UpdatePlayerMoneyDisplay();
        }
    }

    private void OnPropertyPurchased(int playerId, int propertyId)
    {
        if (gameMessagesText != null)
        {
            gameMessagesText.text = $"Player {playerId + 1} purchased property {propertyId}";
        }

        // Update property ownership list
        UpdatePropertyOwnershipDisplay();
    }

    private void OnGameStateChanged(NetworkGameManager.GameState newState)
    {
        if (diceRaceGameStatusText != null)
        {
            switch (newState)
            {
                case NetworkGameManager.GameState.WaitingToStart:
                    diceRaceGameStatusText.text = "Waiting to start...";
                    break;
                case NetworkGameManager.GameState.InProgress:
                    diceRaceGameStatusText.text = "Dice Race in progress";
                    break;
                case NetworkGameManager.GameState.GameOver:
                    diceRaceGameStatusText.text = "Game Over!";
                    UpdateGameButtons();
                    break;
            }
        }
    }

    private void OnMonopolyGameStateChanged(MonopolyGameManager.GameState newState)
    {
        if (monopolyGameStatusText != null)
        {
            switch (newState)
            {
                case MonopolyGameManager.GameState.WaitingToStart:
                    monopolyGameStatusText.text = "Waiting to start...";
                    break;
                case MonopolyGameManager.GameState.InProgress:
                    monopolyGameStatusText.text = "Monopoly in progress";
                    break;
                case MonopolyGameManager.GameState.GameOver:
                    monopolyGameStatusText.text = "Game Over!";
                    UpdateGameButtons();
                    break;
            }
        }
    }

    private void OnGameMessage(string message)
    {
        // Check if this is a card-related message (starts with arrow)
        if (message.StartsWith("  ?"))
        {
            // Card message - route to card text if available, otherwise append to game messages
            if (cardMessagesText != null)
            {
                // For card messages, build a multi-line display
                if (message.Contains("Drew Chance:") || message.Contains("Drew Community Chest:"))
                {
                    // Start fresh for new card draw
                    cardMessagesText.text = message;
                }
                else
                {
                    // Append effect to existing card message
                    cardMessagesText.text += "\n" + message;
                }
            }
            else
            {
                // Fallback: append to game messages
                if (gameMessagesText != null)
                {
                    gameMessagesText.text += "\n" + message;
                }
            }
        }
        else
        {
            // Regular game message - replace in game messages text
            if (gameMessagesText != null)
            {
                gameMessagesText.text = message;
            }
        }
    }

    #endregion

    private void SetupPlayerListLayout()
    {
        if (playerListContent != null)
        {
            var verticalLayoutGroup = playerListContent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
            {
                verticalLayoutGroup = playerListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.spacing = 5f;

            var contentSizeFitter = playerListContent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = playerListContent.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Debug.Log("Player list layout components configured");
        }
    }

    private void ValidateUIReferences()
    {
        if (mainMenuPanel == null) Debug.LogError("MainMenuPanel is not assigned in UIManager!");
        if (savedGamesPanel == null) Debug.LogError("SavedGamesPanel is not assigned in UIManager!");
        if (lobbyPanel == null) Debug.LogError("LobbyPanel is not assigned in UIManager!");
        if (joinPanel == null) Debug.LogError("JoinPanel is not assigned in UIManager!");
        if (diceRaceGamePanel == null) Debug.LogError("DiceRaceGamePanel is not assigned in UIManager!");
        if (monopolyGamePanel == null) Debug.LogError("MonopolyGamePanel is not assigned in UIManager!");

        // Main menu validation
        if (createCustomGameButton == null) Debug.LogError("CreateCustomGameButton is not assigned in UIManager!");
        if (hostGameButton == null) Debug.LogError("HostGameButton is not assigned in UIManager!");
        if (joinGameButton == null) Debug.LogError("JoinGameButton is not assigned in UIManager!");
        if (exitButton == null) Debug.LogError("ExitButton is not assigned in UIManager!");

        // Saved Games Panel validation
        if (savedGamesListContent == null) Debug.LogError("SavedGamesListContent is not assigned in UIManager!");
        if (savedGameItemPrefab == null) Debug.LogError("SavedGameItemPrefab is not assigned in UIManager!");
        if (backFromSavedGamesButton == null) Debug.LogError("BackFromSavedGamesButton is not assigned in UIManager!");
        if (noSavedGamesText == null) Debug.LogWarning("NoSavedGamesText is not assigned (optional - will show when no games saved)");

        // Join panel validation
        if (submitJoinButton == null) Debug.LogError("SubmitJoinButton is not assigned in UIManager!");
        if (backToMenuButton == null) Debug.LogError("BackToMenuButton is not assigned in UIManager!");
        if (joinCodeInput == null) Debug.LogError("JoinCodeInput is not assigned in UIManager!");

        // Lobby panel validation
        if (lobbyCodeText == null) Debug.LogError("LobbyCodeText is not assigned in UIManager!");
        if (lobbyTitleText == null) Debug.LogWarning("LobbyTitleText is not assigned (optional - defaults to 'BoardSmith Lobby')");
        if (playerListContent == null) Debug.LogError("PlayerListContent is not assigned in UIManager!");
        if (playerListItemPrefab == null) Debug.LogError("PlayerListItemPrefab is not assigned in UIManager!");
        if (statusText == null) Debug.LogError("StatusText is not assigned in UIManager!");
        if (startGameButton == null) Debug.LogError("StartGameButton is not assigned in UIManager!");
        if (leaveLobbyButton == null) Debug.LogError("LeaveLobbyButton is not assigned in UIManager!");

        // Dice Race panel validation
        if (diceRaceCurrentPlayerText == null) Debug.LogError("DiceRaceCurrentPlayerText is not assigned in UIManager!");
        if (diceRaceGameStatusText == null) Debug.LogError("DiceRaceGameStatusText is not assigned in UIManager!");
        if (diceRaceRollDiceButton == null) Debug.LogError("DiceRaceRollDiceButton is not assigned in UIManager!");
        if (diceRaceDiceResultText == null) Debug.LogError("DiceRaceDiceResultText is not assigned in UIManager!");
        if (diceRaceLeaveGameButton == null) Debug.LogError("DiceRaceLeaveGameButton is not assigned in UIManager!");

        // Monopoly panel validation
        if (monopolyCurrentPlayerText == null) Debug.LogError("MonopolyCurrentPlayerText is not assigned in UIManager!");
        if (monopolyGameStatusText == null) Debug.LogError("MonopolyGameStatusText is not assigned in UIManager!");
        if (monopolyRollDiceButton == null) Debug.LogError("MonopolyRollDiceButton is not assigned in UIManager!");
        if (purchasePropertyButton == null) Debug.LogError("PurchasePropertyButton is not assigned in UIManager!");
        if (playerMoneyText == null) Debug.LogError("PlayerMoneyText is not assigned in UIManager!");
        if (gameMessagesText == null) Debug.LogError("GameMessagesText is not assigned in UIManager!");
        if (cardMessagesText == null) Debug.LogWarning("CardMessagesText is not assigned (optional - cards will show in game messages)");
        if (monopolyLeaveGameButton == null) Debug.LogError("MonopolyLeaveGameButton is not assigned in UIManager!");

        // Property ownership UI validation (optional)
        if (propertyOwnershipPanel == null) Debug.LogWarning("PropertyOwnershipPanel is not assigned (optional feature)");
        if (propertyOwnershipText == null) Debug.LogWarning("PropertyOwnershipText is not assigned (optional feature)");
        if (togglePropertyListButton == null) Debug.LogWarning("TogglePropertyListButton is not assigned (optional feature)");
    }

    private void SetupButtonListeners()
    {
        // Main Menu
        if (createCustomGameButton != null) createCustomGameButton.onClick.AddListener(OnCreateCustomGameClicked);
        if (hostGameButton != null) hostGameButton.onClick.AddListener(OnHostGameClicked);
        if (joinGameButton != null) joinGameButton.onClick.AddListener(OnJoinGameClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);

        // Saved Games Panel
        if (backFromSavedGamesButton != null) backFromSavedGamesButton.onClick.AddListener(OnBackFromSavedGamesClicked);

        // Join Panel
        if (submitJoinButton != null) submitJoinButton.onClick.AddListener(OnSubmitJoinClicked);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

        // Lobby Panel
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveLobbyButton != null) leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

        // Dice Race game panel button listeners
        if (diceRaceRollDiceButton != null) diceRaceRollDiceButton.onClick.AddListener(OnDiceRaceRollDiceClicked);
        if (diceRaceLeaveGameButton != null) diceRaceLeaveGameButton.onClick.AddListener(OnLeaveGameClicked);

        // Monopoly game panel button listeners
        if (monopolyRollDiceButton != null) monopolyRollDiceButton.onClick.AddListener(OnMonopolyRollDiceClicked);
        if (purchasePropertyButton != null) purchasePropertyButton.onClick.AddListener(OnPurchasePropertyClicked);
        if (monopolyLeaveGameButton != null) monopolyLeaveGameButton.onClick.AddListener(OnLeaveGameClicked);

        // Property ownership UI button listener
        if (togglePropertyListButton != null) togglePropertyListButton.onClick.AddListener(OnTogglePropertyListClicked);
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (savedGamesPanel != null) savedGamesPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(false);
        if (diceRaceGamePanel != null) diceRaceGamePanel.SetActive(false);
        if (monopolyGamePanel != null) monopolyGamePanel.SetActive(false);

        // Hide Battleships setup panel managed by BattleshipsSetupManager
        if (BattleshipsSetupManager.Instance != null)
        {
            BattleshipsSetupManager.Instance.HideBattleshipsGameSetup();
        }
    }

    private void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            Debug.Log("Showing Main Menu");
        }
        ClearStatus();
    }

    public void ShowLobbyPublic()
    {
        ShowLobby();
    }

    private void ShowLobby()
    {
        HideAllPanels();
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("Showing Lobby Panel");

            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
            }

            // Update lobby title based on game mode
            UpdateLobbyTitle();
        }
        ClearStatus();
    }

    /// <summary>
    /// Updates the lobby title text based on the current game mode
    /// </summary>
    private void UpdateLobbyTitle()
    {
        if (lobbyTitleText != null)
        {
            string gameModeTitle = GetGameModeDisplayName();
            lobbyTitleText.text = $"{gameModeTitle} Lobby";
            Debug.Log($"Updated lobby title to: {lobbyTitleText.text}");
        }
    }

    /// <summary>
    /// Gets the display name for the current game mode
    /// </summary>
    private string GetGameModeDisplayName()
    {
        if (isBattleshipsMode)
        {
            return "Battleships";
        }
        else if (isMonopolyMode)
        {
            return "Monopoly";
        }
        else
        {
            return "Dice Race";
        }
    }

    private void ShowJoinPanel()
    {
        HideAllPanels();
        if (joinPanel != null)
        {
            joinPanel.SetActive(true);
            Debug.Log("Showing Join Panel");
        }
        ClearStatus();
    }

    /// <summary>
    /// Show the saved games panel with list of user's saved custom games
    /// </summary>
    private void ShowSavedGamesPanel()
    {
        HideAllPanels();
        if (savedGamesPanel != null)
        {
            savedGamesPanel.SetActive(true);
            Debug.Log("Showing Saved Games Panel");
            LoadSavedGamesList();
        }
        ClearStatus();
    }

    /// <summary>
    /// Load and display the list of saved custom games
    /// </summary>
    private void LoadSavedGamesList()
    {
        if (savedGamesListContent == null || savedGameItemPrefab == null)
        {
            Debug.LogError("Saved games list content or prefab not assigned!");
            return;
        }

        // Clear existing list
        foreach (Transform child in savedGamesListContent)
        {
            Destroy(child.gameObject);
        }

        // TODO: Load saved games from disk (will implement later)
        // For now, show "no saved games" message
        List<SavedGameInfo> savedGames = new List<SavedGameInfo>(); // Placeholder

        if (savedGames.Count == 0)
        {
            // Show "no saved games" message
            if (noSavedGamesText != null)
            {
                noSavedGamesText.gameObject.SetActive(true);
                noSavedGamesText.text = "No saved games found.\nCreate a custom game first!";
            }
            Debug.Log("No saved games found");
        }
        else
        {
            // Hide "no saved games" message
            if (noSavedGamesText != null)
            {
                noSavedGamesText.gameObject.SetActive(false);
            }

            // Create list items for each saved game
            foreach (var savedGame in savedGames)
            {
                GameObject listItem = Instantiate(savedGameItemPrefab, savedGamesListContent);

                // Setup list item (will need to populate with game info)
                SetupSavedGameListItem(listItem, savedGame);
            }

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(savedGamesListContent as RectTransform);
        }
    }

    /// <summary>
    /// Setup a saved game list item with game info and host button
    /// </summary>
    private void SetupSavedGameListItem(GameObject listItem, SavedGameInfo gameInfo)
    {
        // Find components in the list item
        TextMeshProUGUI gameNameText = listItem.transform.Find("GameNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI gameDetailsText = listItem.transform.Find("GameDetailsText")?.GetComponent<TextMeshProUGUI>();
        Button hostButton = listItem.transform.Find("HostButton")?.GetComponent<Button>();

        if (gameNameText != null)
        {
            gameNameText.text = gameInfo.gameName;
        }

        if (gameDetailsText != null)
        {
            gameDetailsText.text = $"{gameInfo.gameType} • {gameInfo.playerCount} Players";
        }

        if (hostButton != null)
        {
            hostButton.onClick.AddListener(() => OnHostSavedGameClicked(gameInfo));
        }
    }

    private void ShowGamePanel()
    {
        HideAllPanels();

        if (isBattleshipsMode)
        {
            // Battleships mode - show ship placement panel
            Debug.Log("Showing Battleships Ship Placement Panel");

            // Hide other game boards
            if (BoardGenerator.Instance != null && BoardGenerator.Instance.GetBoardParent() != null)
            {
                BoardGenerator.Instance.GetBoardParent().gameObject.SetActive(false);
            }

            if (MonopolyBoardManager.Instance != null && MonopolyBoardManager.Instance.boardParent != null)
            {
                MonopolyBoardManager.Instance.boardParent.gameObject.SetActive(false);
            }

            // CRITICAL FIX: Generate Battleships boards FIRST (which activates parents)
            BattleshipsBoardGenerator boardGenerator = FindFirstObjectByType<BattleshipsBoardGenerator>();
            if (boardGenerator != null)
            {
                Debug.Log("Generating Battleships boards (this will activate board parents)...");

                boardGenerator.GenerateBoards();
                Debug.Log("? Battleships boards generated and activated");
            }
            else
            {
                Debug.LogError("? BattleshipsBoardGenerator not found in scene!");
            }

            // Show Battleships ship placement UI AFTER boards are generated
            if (BattleshipsUIManager.Instance != null)
            {
                BattleshipsUIManager.Instance.ShowShipPlacementPanel();
                Debug.Log("? Showing Battleships ship placement panel");
            }
            else
            {
                Debug.LogError("? BattleshipsUIManager instance not found!");
            }
        }
        else if (isMonopolyMode && monopolyGamePanel != null)
        {
            monopolyGamePanel.SetActive(true);
            Debug.Log($"Showing Monopoly Game Panel - Mode: {isMonopolyMode}");

            // Hide dice race board
            if (BoardGenerator.Instance != null && BoardGenerator.Instance.GetBoardParent() != null)
            {
                BoardGenerator.Instance.GetBoardParent().gameObject.SetActive(false);
                Debug.Log("?? Hid Dice Race board");
            }

            // Show Monopoly board FIRST, before UI updates
            if (MonopolyBoardManager.Instance != null && MonopolyBoardManager.Instance.boardParent != null)
            {
                MonopolyBoardManager.Instance.boardParent.gameObject.SetActive(true);

                // CRITICAL FIX: Ensure board is behind UI elements
                Canvas boardCanvas = MonopolyBoardManager.Instance.boardParent.GetComponent<Canvas>();
                if (boardCanvas != null)
                {
                    boardCanvas.sortingOrder = -1; // Put board behind UI
                    Debug.Log("?? Set Monopoly board canvas sorting order to -1");
                }
                else
                {
                    // If the board parent doesn't have a canvas, check if parent does
                    boardCanvas = MonopolyBoardManager.Instance.boardParent.GetComponentInParent<Canvas>();
                    if (boardCanvas != null)
                    {
                        // Don't change the main canvas, instead add sorting to board parent
                        Debug.Log("?? Board is on main canvas - layout should be correct");
                    }
                }

                Debug.Log("?? Showed Monopoly board");
            }

            // Initialize game UI
            UpdateCurrentPlayerDisplay(0);
            UpdateGameButtons();

            // Set status messages
            if (monopolyGameStatusText != null) monopolyGameStatusText.text = "Monopoly in progress";
            if (gameMessagesText != null) gameMessagesText.text = "Monopoly started! Roll the dice when it's your turn.";

            // Make sure all Monopoly UI is visible and interactable
            if (monopolyCurrentPlayerText != null) monopolyCurrentPlayerText.gameObject.SetActive(true);
            if (monopolyGameStatusText != null) monopolyGameStatusText.gameObject.SetActive(true);
            if (monopolyRollDiceButton != null)
            {
                monopolyRollDiceButton.gameObject.SetActive(true);
                monopolyRollDiceButton.transform.SetAsLastSibling(); // Ensure button is on top
            }
            if (purchasePropertyButton != null)
            {
                purchasePropertyButton.gameObject.SetActive(true);
                purchasePropertyButton.transform.SetAsLastSibling();
            }
            if (playerMoneyText != null) playerMoneyText.gameObject.SetActive(true);
            if (gameMessagesText != null) gameMessagesText.gameObject.SetActive(true);
            if (cardMessagesText != null)
            {
                cardMessagesText.gameObject.SetActive(true);
                cardMessagesText.text = ""; // Clear card messages
            }
            if (monopolyLeaveGameButton != null)
            {
                monopolyLeaveGameButton.gameObject.SetActive(true);
                monopolyLeaveGameButton.transform.SetAsLastSibling();
            }

            // Initialize property ownership panel
            if (propertyOwnershipPanel != null)
            {
                propertyOwnershipPanel.SetActive(false); // Hidden by default
            }

            UpdatePlayerMoneyDisplay();
            UpdatePropertyOwnershipDisplay(); // Initial update

            // Force button update after a short delay
            StartCoroutine(DelayedButtonUpdate());

            Debug.Log($"?? Monopoly Game Panel configured and active");
        }
        else if (!isMonopolyMode && !isBattleshipsMode && diceRaceGamePanel != null)
        {
            diceRaceGamePanel.SetActive(true);
            Debug.Log("Showing Dice Race Game Panel");

            // Hide Monopoly board
            if (MonopolyBoardManager.Instance != null && MonopolyBoardManager.Instance.boardParent != null)
            {
                MonopolyBoardManager.Instance.boardParent.gameObject.SetActive(false);
            }

            // Show dice race board
            if (BoardGenerator.Instance != null && BoardGenerator.Instance.GetBoardParent() != null)
            {
                BoardGenerator.Instance.GetBoardParent().gameObject.SetActive(true);
            }

            UpdateCurrentPlayerDisplay(0);
            UpdateGameButtons();

            if (diceRaceGameStatusText != null) diceRaceGameStatusText.text = "Dice Race in progress";
            if (diceRaceDiceResultText != null) diceRaceDiceResultText.text = "Dice Race started! Roll the dice when it's your turn.";
        }

        ClearStatus();
    }

    private IEnumerator DelayedButtonUpdate()
    {
        yield return new WaitForSeconds(0.5f); // Wait for game manager to be fully initialized
        UpdateGameButtons();
        Debug.Log("?? Forced button update after delay");
    }

    #region Game Panel Methods

    private void UpdateCurrentPlayerDisplay(int currentPlayerId)
    {
        if (isMonopolyMode)
        {
            UpdateMonopolyCurrentPlayerDisplay(currentPlayerId);
        }
        else
        {
            UpdateDiceRaceCurrentPlayerDisplay(currentPlayerId);
        }
    }

    private void UpdateDiceRaceCurrentPlayerDisplay(int currentPlayerId)
    {
        if (diceRaceCurrentPlayerText != null)
        {
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsMyTurn())
            {
                diceRaceCurrentPlayerText.text = $"Current Turn: Player {currentPlayerId + 1} (Your Turn!)";
                diceRaceCurrentPlayerText.color = Color.green;
            }
            else
            {
                diceRaceCurrentPlayerText.text = $"Current Turn: Player {currentPlayerId + 1}";
                diceRaceCurrentPlayerText.color = Color.white;
            }
        }
    }

    private void UpdateMonopolyCurrentPlayerDisplay(int currentPlayerId)
    {
        if (monopolyCurrentPlayerText != null)
        {
            if (MonopolyGameManager.Instance != null && MonopolyGameManager.Instance.IsMyTurn())
            {
                monopolyCurrentPlayerText.text = $"Current Turn: Player {currentPlayerId + 1} (Your Turn!)";
                monopolyCurrentPlayerText.color = Color.green;
            }
            else
            {
                monopolyCurrentPlayerText.text = $"Current Turn: Player {currentPlayerId + 1}";
                monopolyCurrentPlayerText.color = Color.white;
            }
        }
    }

    private void UpdateGameButtons()
    {
        if (isMonopolyMode)
        {
            UpdateMonopolyButtons();
        }
        else
        {
            UpdateDiceRaceButtons();
        }
    }

    private void UpdateDiceRaceButtons()
    {
        if (diceRaceRollDiceButton != null && NetworkGameManager.Instance != null)
        {
            bool isMyTurn = NetworkGameManager.Instance.IsMyTurn();
            bool gameInProgress = NetworkGameManager.Instance.GetGameState() == NetworkGameManager.GameState.InProgress;

            diceRaceRollDiceButton.interactable = isMyTurn && gameInProgress;

            TextMeshProUGUI buttonText = diceRaceRollDiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (isMyTurn && gameInProgress)
                {
                    buttonText.text = "Roll Dice!";
                }
                else if (!gameInProgress)
                {
                    buttonText.text = "Game Over";
                }
                else
                {
                    buttonText.text = "Wait Your Turn";
                }
            }
        }
    }

    private void UpdateMonopolyButtons()
    {
        if (MonopolyGameManager.Instance == null)
        {
            Debug.LogWarning("?? MonopolyGameManager.Instance is null in UpdateMonopolyButtons");
            if (monopolyRollDiceButton != null) monopolyRollDiceButton.interactable = false;
            if (purchasePropertyButton != null) purchasePropertyButton.interactable = false;
            return;
        }

        bool isMyTurn = MonopolyGameManager.Instance.IsMyTurn();
        bool gameInProgress = MonopolyGameManager.Instance.GetGameState() == MonopolyGameManager.GameState.InProgress;

        // Debug logging
        Debug.Log($"?? UpdateMonopolyButtons: IsMyTurn={isMyTurn}, GameInProgress={gameInProgress}, CurrentTurn={MonopolyGameManager.Instance.GetCurrentPlayerId()}, MyPlayerId={MonopolyGameManager.Instance.GetMyPlayerId()}");

        // Roll Dice Button
        if (monopolyRollDiceButton != null)
        {
            monopolyRollDiceButton.interactable = isMyTurn && gameInProgress;

            TextMeshProUGUI buttonText = monopolyRollDiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (isMyTurn && gameInProgress)
                {
                    buttonText.text = "Roll Dice!";
                    buttonText.color = Color.white;
                }
                else if (!gameInProgress)
                {
                    buttonText.text = "Game Over";
                    buttonText.color = Color.red;
                }
                else
                {
                    buttonText.text = "Wait Your Turn";
                    buttonText.color = Color.gray;
                }
            }

            Debug.Log($"?? Roll Dice Button: interactable={monopolyRollDiceButton.interactable}");
        }

        // Purchase Property Button
        if (purchasePropertyButton != null)
        {
            purchasePropertyButton.interactable = isMyTurn && gameInProgress;
            Debug.Log($"?? Purchase Button: interactable={purchasePropertyButton.interactable}");
        }
    }

    private void UpdatePlayerMoneyDisplay()
    {
        if (playerMoneyText != null && MonopolyGameManager.Instance != null)
        {
            int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
            if (myPlayerId >= 0)
            {
                var playerData = MonopolyGameManager.Instance.GetPlayer(myPlayerId);
                playerMoneyText.text = $"Money: ${playerData.money}";
            }
        }
    }

    private void OnPurchasePropertyClicked()
    {
        Debug.Log("Purchase property button clicked");

        if (MonopolyGameManager.Instance != null)
        {
            MonopolyGameManager.Instance.PurchaseProperty();
        }
        else
        {
            Debug.LogError("MonopolyGameManager instance not found!");
        }
    }

    private async void OnLeaveGameClicked()
    {
        Debug.Log("Leave game button clicked");

        // CRITICAL: Clean up Battleships boards BEFORE leaving lobby
        if (isBattleshipsMode)
        {
            Debug.Log("?? Cleaning up Battleships game...");

            // Use BattleshipsUIManager's cleanup method (cleaner than reflection)
            if (BattleshipsUIManager.Instance != null)
            {
                BattleshipsUIManager.Instance.CleanupForMainMenu();
            }
            else
            {
                Debug.LogWarning("?? BattleshipsUIManager instance not found");
            }

            // Reset Battleships mode flag
            isBattleshipsMode = false;
            Debug.Log("? Reset Battleships mode flag");
        }

        // Return to main menu and disconnect
        await LobbyManager.Instance.LeaveLobby();

        ShowMainMenu();
    }

    private void OnDiceRaceRollDiceClicked()
    {
        Debug.Log("Dice Race roll dice button clicked");

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RollDice();
        }
        else
        {
            Debug.LogError("NetworkGameManager instance not found!");
        }
    }

    private void OnMonopolyRollDiceClicked()
    {
        Debug.Log("Monopoly roll dice button clicked");

        if (MonopolyGameManager.Instance != null)
        {
            MonopolyGameManager.Instance.RollDice();
        }
        else
        {
            Debug.LogError("MonopolyGameManager instance not found!");
        }
    }

    private void OnTogglePropertyListClicked()
    {
        Debug.Log("Toggle property list button clicked");

        if (propertyOwnershipPanel != null)
        {
            bool isActive = propertyOwnershipPanel.activeSelf;
            propertyOwnershipPanel.SetActive(!isActive);

            if (isActive)
            {
                Debug.Log("? Hid property ownership panel");
            }
            else
            {
                Debug.Log("? Showed property ownership panel");

                // Update property ownership text
                UpdatePropertyOwnershipDisplay();
            }
        }
    }

    private void UpdatePropertyOwnershipDisplay()
    {
        if (propertyOwnershipText != null && MonopolyGameManager.Instance != null)
        {
            int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
            if (myPlayerId >= 0)
            {
                var playerProperties = MonopolyGameManager.Instance.GetPlayerProperties(myPlayerId);
                if (playerProperties != null && playerProperties.Count > 0)
                {
                    // Group properties by property group
                    Dictionary<PropertyGroup, List<MonopolyGameManager.PropertyOwnership>> groupedProperties =
                        new Dictionary<PropertyGroup, List<MonopolyGameManager.PropertyOwnership>>();

                    foreach (var ownership in playerProperties)
                    {
                        var space = MonopolyGameManager.Instance.GetSpace(ownership.propertyId);
                        if (space != null)
                        {
                            if (!groupedProperties.ContainsKey(space.group))
                            {
                                groupedProperties[space.group] = new List<MonopolyGameManager.PropertyOwnership>();
                            }
                            groupedProperties[space.group].Add(ownership);
                        }
                    }

                    // Define group order (matches board layout)
                    PropertyGroup[] groupOrder = {
                        PropertyGroup.Brown,
                        PropertyGroup.LightBlue,
                        PropertyGroup.Pink,
                        PropertyGroup.Orange,
                        PropertyGroup.Red,
                        PropertyGroup.Yellow,
                        PropertyGroup.Green,
                        PropertyGroup.DarkBlue,
                        PropertyGroup.Railroad,
                        PropertyGroup.Utility
                    };

                    // Create formatted string with grouped properties
                    string propertiesList = $"<b>Your Properties ({playerProperties.Count}):</b>\n\n";

                    foreach (var group in groupOrder)
                    {
                        if (groupedProperties.ContainsKey(group))
                        {
                            var properties = groupedProperties[group];
                            string groupColor = GetPropertyGroupColorHex(group);

                            // Add properties in this group with color (no header, no spacing between groups)
                            foreach (var ownership in properties)
                            {
                                var space = MonopolyGameManager.Instance.GetSpace(ownership.propertyId);
                                if (space != null)
                                {
                                    propertiesList += $"<color={groupColor}>• {space.spaceName}</color>";

                                    // Add building info
                                    if (ownership.hasHotel)
                                    {
                                        propertiesList += " [HOTEL]";
                                    }
                                    else if (ownership.houseCount > 0)
                                    {
                                        propertiesList += $" [{ownership.houseCount} House{(ownership.houseCount > 1 ? "s" : "")}]";
                                    }

                                    // Add mortgage status
                                    if (ownership.isMortgaged)
                                    {
                                        propertiesList += " (Mortgaged)";
                                    }
                                    propertiesList += "\n";
                                }
                            }
                        }
                    }

                    propertyOwnershipText.text = propertiesList;
                }
                else
                {
                    propertyOwnershipText.text = "You don't own any properties yet.";
                }
            }
        }
    }

    /// <summary>
    /// Get hex color code for property group
    /// </summary>
    private string GetPropertyGroupColorHex(PropertyGroup group)
    {
        switch (group)
        {
            case PropertyGroup.Brown:
                return "#8B4513"; // Saddle Brown
            case PropertyGroup.LightBlue:
                return "#87CEEB"; // Sky Blue
            case PropertyGroup.Pink:
                return "#FF69B4"; // Hot Pink
            case PropertyGroup.Orange:
                return "#FFA500"; // Orange
            case PropertyGroup.Red:
                return "#FF0000"; // Red
            case PropertyGroup.Yellow:
                return "#FFFF00"; // Yellow
            case PropertyGroup.Green:
                return "#00FF00"; // Lime Green
            case PropertyGroup.DarkBlue:
                return "#00008B"; // Dark Blue
            case PropertyGroup.Railroad:
                return "#000000"; // Black
            case PropertyGroup.Utility:
                return "#808080"; // Gray
            default:
                return "#FFFFFF"; // White
        }
    }

    private void SetStatus(string message, Color? color = null)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color ?? Color.white;
            statusText.gameObject.SetActive(true);
            Debug.Log($"Status: {message}");
        }
    }

    private void SetStatusError(string message)
    {
        SetStatus(message, Color.red);
    }

    private void SetStatusSuccess(string message)
    {
        SetStatus(message, Color.green);
    }

    private void SetStatusInfo(string message)
    {
        SetStatus(message, Color.white);
    }

    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }

        if (statusClearCoroutine != null)
        {
            StopCoroutine(statusClearCoroutine);
            statusClearCoroutine = null;
        }
    }

    private void SetStatusTemporary(string message, Color? color = null, float duration = 5f)
    {
        SetStatus(message, color);

        if (statusClearCoroutine != null)
        {
            StopCoroutine(statusClearCoroutine);
        }

        statusClearCoroutine = StartCoroutine(ClearStatusAfterDelay(duration));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearStatus();
        statusClearCoroutine = null;
    }

    #endregion

    #region Button Click Handlers

    private void OnCreateCustomGameClicked()
    {
        Debug.Log("Create Custom Game button clicked");

        // Check if RuleEditorManager exists
        if (RuleEditorManager.Instance == null)
        {
            Debug.LogError("[UIManager] RuleEditorManager instance not found!");
            SetStatusError("Error: RuleEditorManager not found in scene!");
            return;
        }

        // Check if RuleEditorUI exists
        RuleEditorUI ruleEditorUI = FindFirstObjectByType<RuleEditorUI>();
        if (ruleEditorUI == null)
        {
            Debug.LogError("[UIManager] RuleEditorUI not found in scene!");
            SetStatusError("Error: RuleEditorUI not found in scene!");
            return;
        }

        // Hide main menu panel before showing rule editor
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            Debug.Log("[UIManager] Hidden main menu panel");
        }

        // Show rule editor
        RuleEditorManager.Instance.ShowRuleEditor();
        SetStatusInfo("Configure your custom game rules");
    }

    /// <summary>
    /// Called when "Host Game" button is clicked on main menu
    /// Shows list of saved custom games
    /// </summary>
    private void OnHostGameClicked()
    {
        Debug.Log("Host Game button clicked");
        ShowSavedGamesPanel();
        SetStatusInfo("Select a saved game to host");
    }

    /// <summary>
    /// Called when user clicks "Host" button on a saved game
    /// Loads the game rules and creates a lobby
    /// </summary>
    private async void OnHostSavedGameClicked(SavedGameInfo gameInfo)
    {
        Debug.Log($"Hosting saved game: {gameInfo.gameName}");

        // TODO: Load rules from disk (will implement later)
        // For now, use placeholder rules
        GameRules rules = gameInfo.rules;

        if (rules == null)
        {
            SetStatusError("Failed to load game rules!");
            return;
        }

        SetStatusInfo($"Creating lobby for {gameInfo.gameName}...");

        try
        {
            // Set game mode flags based on game type
            switch (gameInfo.gameType)
            {
                case "Monopoly":
                    isMonopolyMode = true;
                    isBattleshipsMode = false;
                    break;
                case "Battleships":
                    isBattleshipsMode = true;
                    isMonopolyMode = false;
                    break;
                case "Dice Race":
                    isMonopolyMode = false;
                    isBattleshipsMode = false;
                    break;
                case "Hybrid":
                    isMonopolyMode = false;
                    isBattleshipsMode = false;
                    break;
            }

            // Create lobby with game name
            string lobbyCode = await LobbyManager.Instance.CreateLobby(gameInfo.gameName);

            if (!string.IsNullOrEmpty(lobbyCode))
            {
                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                }

                if (lobbyTitleText != null)
                {
                    lobbyTitleText.text = gameInfo.gameName;
                }

                // Store rules in RuleEditorManager so they can be used when starting the game
                if (RuleEditorManager.Instance != null)
                {
                    RuleEditorManager.Instance.SetRules(rules);
                }

                SetStatusSuccess($"Lobby created! Code: {lobbyCode}");
                ShowLobby();
            }
            else
            {
                SetStatusError("Failed to create lobby");
            }
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error: {e.Message}");
            Debug.LogError($"[UIManager] Lobby creation error: {e}");
        }
    }

    /// <summary>
    /// Called when "Back" button is clicked on saved games panel
    /// </summary>
    private void OnBackFromSavedGamesClicked()
    {
        Debug.Log("Back from saved games clicked");
        ShowMainMenu();
    }

    private void OnJoinGameClicked()
    {
        Debug.Log("Join Game button clicked");

        if (joinCodeInput != null)
        {
            joinCodeInput.text = "";
        }
        ShowJoinPanel();
        SetStatusInfo("Enter 6-character lobby code");
    }

    private async void OnSubmitJoinClicked()
    {
        Debug.Log("Submit join button clicked");

        if (joinCodeInput == null) return;

        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatusError("Please enter a lobby code");
            return;
        }

        if (submitJoinButton != null) submitJoinButton.interactable = false;
        if (joinCodeInput != null) joinCodeInput.interactable = false;

        SetStatusInfo($"Joining lobby with code: {code}...");

        try
        {
            SetStatusInfo("Connecting to lobby service...");
            bool joined = await LobbyManager.Instance.JoinLobby(code);

            if (joined)
            {
                SetStatusSuccess($"Successfully joined lobby: {code}");

                // Detect game mode from lobby name
                var lobby = await LobbyManager.Instance.GetCurrentLobby();
                if (lobby != null && lobby.Name != null)
                {
                    if (lobby.Name.Contains("Battleships"))
                    {
                        isBattleshipsMode = true;
                        isMonopolyMode = false;
                        Debug.Log("Detected Battleships mode from lobby name");
                    }
                    else if (lobby.Name.Contains("Monopoly"))
                    {
                        isMonopolyMode = true;
                        isBattleshipsMode = false;
                        Debug.Log("Detected Monopoly mode from lobby name");
                    }
                    else
                    {
                        isMonopolyMode = false;
                        isBattleshipsMode = false;
                        Debug.Log("Detected Dice Race mode from lobby name");
                    }
                }

                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {code}";
                }

                SetStatusInfo("Finalizing connection...");
                await System.Threading.Tasks.Task.Delay(1000);

                ShowLobby();
                UpdatePlayerList();
                SetStatusSuccess("Connected to lobby!");
            }
            else
            {
                SetStatusError($"Failed to join lobby with code: {code}. Please check the code and try again.");
            }
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error joining lobby: {e.Message}");
            Debug.LogError($"Exception in OnSubmitJoinClicked: {e}");
        }
        finally
        {
            if (submitJoinButton != null) submitJoinButton.interactable = true;
            if (joinCodeInput != null) joinCodeInput.interactable = true;
        }
    }

    private void OnBackToMenuClicked()
    {
        Debug.Log("Back to menu button clicked");
        ShowMainMenu();
    }

    private void OnExitButtonClicked()
    {
        Debug.Log("Exit button clicked");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnStartGameClicked()
    {
        Debug.Log("Start game button clicked");

        if (!LobbyManager.Instance.IsLobbyHost())
        {
            SetStatusError("Only the host can start the game!");
            return;
        }

        if (startGameButton != null) startGameButton.interactable = false;

        string gameMode = isBattleshipsMode ? "Battleships" : (isMonopolyMode ? "Monopoly" : "Dice Race");
        SetStatusInfo($"Starting {gameMode}...");

        try
        {
            // Check if this is a custom game (has custom rules set)
            bool isCustomGame = RuleEditorManager.Instance != null &&
                              RuleEditorManager.Instance.GetCurrentRules() != null;

            if (isCustomGame && CustomGameSpawner.Instance != null)
            {
                // Use custom game spawner for rule-based games
                Debug.Log("[UIManager] Starting custom game with rule-based spawner");
                StartCustomGame();
            }
            else if (isBattleshipsMode)
            {
                // Battleships mode
                SpawnBattleshipsGameManager();

                if (BattleshipsGameManager.Instance != null)
                {
                    int actualPlayerCount = LobbyManager.Instance.GetPlayersInfo().Count;
                    Debug.Log($"Starting Battleships with {actualPlayerCount} actual players in lobby");

                    // Initialize game with board configuration from BattleshipsSetupManager
                    if (BattleshipsSetupManager.Instance != null)
                    {
                        int rows = BattleshipsSetupManager.Instance.GetMaxRows();
                        int cols = BattleshipsSetupManager.Instance.GetMaxColumns();
                        bool[,] tileStates = BattleshipsSetupManager.Instance.GetTileStates();

                        Debug.Log($"Board config from setup: {rows}x{cols}, IsCustomized={BattleshipsSetupManager.Instance.IsCustomized()}, tileStates={(tileStates != null ? "NOT NULL" : "NULL")}");
                        if (tileStates != null && BattleshipsSetupManager.Instance.IsCustomized())
                        {
                            // Use custom board layout
                            BattleshipsGameManager.Instance.InitializeGame(actualPlayerCount, rows, cols, tileStates);
                            Debug.Log($"? Initialized Battleships with custom board: {rows}x{cols}");

                            // Clear grid state after successful initialization
                            BattleshipsSetupManager.Instance.ClearGridState();
                        }
                        else
                        {
                            // Use default board (all tiles active)
                            BattleshipsGameManager.Instance.InitializeGameWithDefaultBoard(actualPlayerCount, rows, cols);
                            Debug.Log($"? Initialized Battleships with default board: {rows}x{cols}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("BattleshipsSetupManager not found! Using default 10x10 board");
                        BattleshipsGameManager.Instance.InitializeGameWithDefaultBoard(actualPlayerCount, 10, 10);
                    }

                    Debug.Log("BattleshipsGameManager initialized - board generation will happen in ShowGamePanel");

                    // Explicitly show game panel (which will generate boards and show UI)
                    ShowGamePanel();
                }
            }
            else if (isMonopolyMode)
            {
                SpawnMonopolyGameManager();

                if (MonopolyGameManager.Instance != null)
                {
                    int actualPlayerCount = LobbyManager.Instance.GetPlayersInfo().Count;
                    Debug.Log($"Starting Monopoly with {actualPlayerCount} actual players in lobby");

                    MonopolyGameManager.Instance.InitializeGame(actualPlayerCount);

                    // Explicitly show game panel
                    ShowGamePanel();
                }
            }
            else
            {
                GameSetupManager.Instance.GenerateBoard();
                SpawnNetworkGameManager();

                if (NetworkGameManager.Instance != null)
                {
                    int actualPlayerCount = LobbyManager.Instance.GetPlayersInfo().Count;
                    Debug.Log($"Starting dice race with {actualPlayerCount} actual players in lobby");

                    NetworkGameManager.Instance.InitializeGame(actualPlayerCount);

                    // Explicitly show game panel
                    ShowGamePanel();
                }
            }

            SetStatusSuccess($"{gameMode} started!");
            Debug.Log($"{gameMode} generated and network game initialized!");
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error starting {gameMode}: {e.Message}");
            Debug.LogError($"Exception in OnStartGameClicked: {e}");
        }
        finally
        {
            if (startGameButton != null) startGameButton.interactable = true;
        }
    }

    /// <summary>
    /// Start a custom game using CustomGameSpawner
    /// </summary>
    private async void StartCustomGame()
    {
        try
        {
            int actualPlayerCount = LobbyManager.Instance.GetPlayersInfo().Count;
            Debug.Log($"[UIManager] Starting custom game with {actualPlayerCount} players");

            bool success = await CustomGameSpawner.Instance.SpawnGameFromCurrentRules(actualPlayerCount);

            if (success)
            {
                SetStatusSuccess("Custom game started!");
                ShowGamePanel();
            }
            else
            {
                SetStatusError("Failed to start custom game - check console for details");
            }
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error starting custom game: {e.Message}");
            Debug.LogError($"Exception in StartCustomGame: {e}");
        }
    }

    private async void OnLeaveLobbyClicked()
    {
        Debug.Log("Leave lobby button clicked");

        if (leaveLobbyButton != null) leaveLobbyButton.interactable = false;
        SetStatusInfo("Leaving lobby...");

        try
        {
            await LobbyManager.Instance.LeaveLobby();
            SetStatusSuccess("Left lobby");
            ShowMainMenu();
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error leaving lobby: {e.Message}");
            Debug.LogError($"Exception in OnLeaveLobbyClicked: {e}");
        }
        finally
        {
            if (leaveLobbyButton != null) leaveLobbyButton.interactable = true;
        }
    }

    #endregion

    #region Spawning Game Managers

    private void SpawnBattleshipsGameManager()
    {
        if (BattleshipsGameManager.Instance != null)
        {
            Debug.Log("BattleshipsGameManager already exists");
            return;
        }

        GameObject managerObj = new GameObject("BattleshipsGameManager");
        managerObj.AddComponent<BattleshipsGameManager>();
        Debug.Log("BattleshipsGameManager spawned");
    }

    private void SpawnMonopolyGameManager()
    {
        if (MonopolyGameManager.Instance != null)
        {
            Debug.Log("MonopolyGameManager already exists");
            return;
        }

        GameObject managerObj = new GameObject("MonopolyGameManager");
        managerObj.AddComponent<MonopolyGameManager>();
        Debug.Log("MonopolyGameManager spawned");
    }

    private void SpawnNetworkGameManager()
    {
        if (NetworkGameManager.Instance != null)
        {
            Debug.Log("NetworkGameManager already exists");
            return;
        }

        GameObject managerObj = new GameObject("NetworkGameManager");
        managerObj.AddComponent<NetworkGameManager>();
        Debug.Log("NetworkGameManager spawned");
    }

    #endregion

    #region Player List Updates

    private void UpdatePlayerList()
    {
        if (playerListContent == null || playerListItemPrefab == null) return;

        // Clear existing list
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // Get players from lobby
        List<Dictionary<string, string>> players = LobbyManager.Instance.GetPlayersInfo();

        if (players.Count == 0) return;

        // Create player list items
        foreach (var playerInfo in players)
        {
            GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);

            TextMeshProUGUI playerText = playerItem.GetComponent<TextMeshProUGUI>();
            if (playerText != null)
            {
                string hostIndicator = playerInfo["IsHost"] == "True" ? " (Host)" : "";
                playerText.text = $"{playerInfo["DisplayName"]}{hostIndicator}";
                playerText.fontSize = 14;
                playerText.alignment = TextAlignmentOptions.Center;
            }
        }

        // Force layout rebuild
        if (playerListContent != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent as RectTransform);
        }
    }

    #endregion

    #region Public API for RuleEditorUI

    /// <summary>
    /// Called from RuleEditorUI after user finishes configuring rules
    /// Now prompts user to name and save the game
    /// </summary>
    public async void OnRulesConfigured(GameRules rules)
    {
        Debug.Log("[UIManager] Rules configured, prompting user to save game...");

        // TODO: Show dialog to name and save the game
        // For now, auto-generate name and create lobby directly

        // Analyze rules to determine game type
        string gameType = "Custom Game";
        string lobbyName = "Custom Game";

        if (CustomGameAnalyzer.Instance != null)
        {
            var detectedType = CustomGameAnalyzer.Instance.AnalyzeGameRules(rules);
            int playerCount = CustomGameAnalyzer.Instance.GetRecommendedPlayerCount(detectedType, rules);
            gameType = detectedType.ToString();
            lobbyName = $"{detectedType} Game ({playerCount}P)";

            // Set game mode flags based on detected type
            switch (detectedType)
            {
                case CustomGameAnalyzer.DetectedGameType.Monopoly:
                    isMonopolyMode = true;
                    isBattleshipsMode = false;
                    break;
                case CustomGameAnalyzer.DetectedGameType.Battleships:
                    isBattleshipsMode = true;
                    isMonopolyMode = false;
                    break;
                case CustomGameAnalyzer.DetectedGameType.DiceRace:
                    isMonopolyMode = false;
                    isBattleshipsMode = false;
                    break;
                case CustomGameAnalyzer.DetectedGameType.Hybrid:
                    isMonopolyMode = false;
                    isBattleshipsMode = false;
                    break;
            }
        }

        // TODO: Save game to disk here (will implement later)
        // SavedGameInfo savedGame = new SavedGameInfo(lobbyName, gameType, rules);
        // SaveGameToDisk(savedGame);

        SetStatusInfo($"Creating {lobbyName}...");

        try
        {
            string lobbyCode = await LobbyManager.Instance.CreateLobby(lobbyName);

            if (!string.IsNullOrEmpty(lobbyCode))
            {
                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                }

                if (lobbyTitleText != null)
                {
                    lobbyTitleText.text = lobbyName;
                }

                SetStatusSuccess($"Lobby created! Code: {lobbyCode}");
                ShowLobby();
            }
            else
            {
                SetStatusError("Failed to create lobby");
            }
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error: {e.Message}");
            Debug.LogError($"[UIManager] Lobby creation error: {e}");
        }
    }

    #endregion

    #region Public API for Other Scripts

    /// <summary>
    /// DEPRECATED: Show game mode selection (kept for backwards compatibility with BattleshipsSetupManager)
    /// Now redirects to main menu since game mode selection is removed
    /// </summary>
    public void ShowGameModeSelectionPublic()
    {
        Debug.LogWarning("[UIManager] ShowGameModeSelectionPublic is deprecated - redirecting to main menu");
        ShowMainMenu();
    }

    /// <summary>
    /// Public API for setting status info message (for other managers)
    /// </summary>
    public void SetStatusInfoPublic(string message)
    {
        SetStatusInfo(message);
    }

    /// <summary>
    /// Public API for setting status success message (for other managers)
    /// </summary>
    public void SetStatusSuccessPublic(string message)
    {
        SetStatusSuccess(message);
    }

    /// <summary>
    /// Public API for setting status error message (for other managers)
    /// </summary>
    public void SetStatusErrorPublic(string message)
    {
        SetStatusError(message);
    }

    /// <summary>
    /// Public API for setting lobby code text (for other managers)
    /// </summary>
    public void SetLobbyCode(string code)
    {
        if (lobbyCodeText != null)
        {
            lobbyCodeText.text = $"Lobby Code: {code}";
        }
    }

    /// <summary>
    /// Public API for updating player list (for other managers)
    /// </summary>
    public void UpdatePlayerListPublic()
    {
        UpdatePlayerList();
    }

    #endregion
}
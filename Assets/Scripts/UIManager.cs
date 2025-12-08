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
    [SerializeField] private GameObject gameModeSelectionPanel; // NEW: Game mode selection panel
    [SerializeField] private GameObject gameSetupPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject diceRaceGamePanel; // Dice Race game panel
    [SerializeField] private GameObject monopolyGamePanel; // Monopoly game panel

    [Header("Main Menu")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button exitButton;

    [Header("Game Mode Selection Panel")]
    [SerializeField] private Button diceRaceButton; // NEW: Button for Dice Race mode
    [SerializeField] private Button monopolyButton; // NEW: Button for Monopoly mode
    [SerializeField] private Button battleshipsButton; // NEW: Button for Battleships mode (placeholder)
    [SerializeField] private Button backFromGameModeButton; // NEW: Back button

    [Header("Game Setup Panel")]
    [SerializeField] private TMP_InputField tileCountInput;
    [SerializeField] private TMP_InputField playerCountInput;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button backFromSetupButton;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button submitJoinButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
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
    [SerializeField] private TextMeshProUGUI cardMessagesText; // NEW: Separate text for card messages
    [SerializeField] private Button monopolyLeaveGameButton;
    
    [Header("Property Ownership UI")]
    [SerializeField] private GameObject propertyOwnershipPanel; // Panel to show owned properties
    [SerializeField] private TextMeshProUGUI propertyOwnershipText; // Text showing owned properties
    [SerializeField] private Button togglePropertyListButton; // Button to show/hide property list

    [Header("Game Mode Settings")]
    [SerializeField] private bool isMonopolyMode = false; // Track game mode - now visible in inspector
    [SerializeField] private bool isBattleshipsMode = false; // Track Battleships mode

    private Coroutine statusClearCoroutine;
    private float lastPlayerListUpdate = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 2f;
    private bool isSubscribedToNetworkEvents = false;

    // Game setup data
    private int configuredTileCount = 20;
    private int configuredPlayerCount = 2;
    
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
        // Note: If NetworkGameManager doesn't exist yet, we'll subscribe later in Update()
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
        if (gameModeSelectionPanel == null) Debug.LogError("GameModeSelectionPanel is not assigned in UIManager!");
        if (gameSetupPanel == null) Debug.LogError("GameSetupPanel is not assigned in UIManager!");
        if (lobbyPanel == null) Debug.LogError("LobbyPanel is not assigned in UIManager!");
        if (joinPanel == null) Debug.LogError("JoinPanel is not assigned in UIManager!");
        if (diceRaceGamePanel == null) Debug.LogError("DiceRaceGamePanel is not assigned in UIManager!");
        if (monopolyGamePanel == null) Debug.LogError("MonopolyGamePanel is not assigned in UIManager!");
        if (hostButton == null) Debug.LogError("HostButton is not assigned in UIManager!");
        if (joinButton == null) Debug.LogError("JoinButton is not assigned in UIManager!");
        if (exitButton == null) Debug.LogError("ExitButton is not assigned in UIManager!");
        
        // Game Mode Selection Panel validation
        if (diceRaceButton == null) Debug.LogError("DiceRaceButton is not assigned in UIManager!");
        if (monopolyButton == null) Debug.LogError("MonopolyButton is not assigned in UIManager!");
        if (battleshipsButton == null) Debug.LogError("BattleshipsButton is not assigned in UIManager!");
        if (backFromGameModeButton == null) Debug.LogError("BackFromGameModeButton is not assigned in UIManager!");
        
        if (tileCountInput == null) Debug.LogError("TileCountInput is not assigned in UIManager!");
        if (playerCountInput == null) Debug.LogError("PlayerCountInput is not assigned in UIManager!");
        if (createLobbyButton == null) Debug.LogError("CreateLobbyButton is not assigned in UIManager!");
        if (backFromSetupButton == null) Debug.LogError("BackFromSetupButton is not assigned in UIManager!");
        
        if (submitJoinButton == null) Debug.LogError("SubmitJoinButton is not assigned in UIManager!");
        if (backToMenuButton == null) Debug.LogError("BackToMenuButton is not assigned in UIManager!");
        if (startGameButton == null) Debug.LogError("StartGameButton is not assigned in UIManager!");
        if (leaveLobbyButton == null) Debug.LogError("LeaveLobbyButton is not assigned in UIManager!");
        if (joinCodeInput == null) Debug.LogError("JoinCodeInput is not assigned in UIManager!");
        if (lobbyCodeText == null) Debug.LogError("LobbyCodeText is not assigned in UIManager!");
        if (playerListContent == null) Debug.LogError("PlayerListContent is not assigned in UIManager!");
        if (playerListItemPrefab == null) Debug.LogError("PlayerListItemPrefab is not assigned in UIManager!");
        if (statusText == null) Debug.LogError("StatusText is not assigned in UIManager!");
        
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
        if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
        
        // Game Mode Selection Panel button listeners
        if (diceRaceButton != null) diceRaceButton.onClick.AddListener(OnDiceRaceButtonClicked);
        if (monopolyButton != null) monopolyButton.onClick.AddListener(OnMonopolyButtonClicked);
        if (battleshipsButton != null) battleshipsButton.onClick.AddListener(OnBattleshipsButtonClicked);
        if (backFromGameModeButton != null) backFromGameModeButton.onClick.AddListener(OnBackFromGameModeClicked);
        
        if (createLobbyButton != null) createLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
        if (backFromSetupButton != null) backFromSetupButton.onClick.AddListener(OnBackFromSetupClicked);
        
        if (submitJoinButton != null) submitJoinButton.onClick.AddListener(OnSubmitJoinClicked);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
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
        if (gameModeSelectionPanel != null) gameModeSelectionPanel.SetActive(false);
        if (gameSetupPanel != null) gameSetupPanel.SetActive(false);
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

    public void ShowGameModeSelectionPublic()
    {
        ShowGameModeSelection();
    }

    private void ShowGameModeSelection()
    {
        HideAllPanels();
        if (gameModeSelectionPanel != null) 
        {
            gameModeSelectionPanel.SetActive(true);
            Debug.Log("Showing Game Mode Selection Panel");
        }
        ClearStatus();
    }

    private void ShowGameSetup()
    {
        HideAllPanels();
        if (gameSetupPanel != null) 
        {
            gameSetupPanel.SetActive(true);
            Debug.Log("Showing Game Setup Panel");
            
            // Set default values for player and tile space prompts
            if (playerCountInput != null) playerCountInput.text = "2";
            
            // Show/hide tile count input based on mode (only for Dice Race)
            if (tileCountInput != null) 
            {
                tileCountInput.gameObject.SetActive(!isMonopolyMode);
                if (!isMonopolyMode) tileCountInput.text = "20";
            }
            
            // Update the create lobby button text based on mode
            if (createLobbyButton != null)
            {
                TextMeshProUGUI buttonText = createLobbyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isMonopolyMode ? "Create Monopoly Lobby" : "Create Dice Race Lobby";
                }
            }
        }
        else
        {
            Debug.LogError("GameSetupPanel is NULL! Please assign it in the Inspector.");
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
        }
        ClearStatus();
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

    /// <summary>
    /// Get display name for property group
    /// </summary>
    private string GetGroupName(PropertyGroup group)
    {
        switch (group)
        {
            case PropertyGroup.Brown:
                return "Brown Properties";
            case PropertyGroup.LightBlue:
                return "Light Blue Properties";
            case PropertyGroup.Pink:
                return "Pink Properties";
            case PropertyGroup.Orange:
                return "Orange Properties";
            case PropertyGroup.Red:
                return "Red Properties";
            case PropertyGroup.Yellow:
                return "Yellow Properties";
            case PropertyGroup.Green:
                return "Green Properties";
            case PropertyGroup.DarkBlue:
                return "Dark Blue Properties";
            case PropertyGroup.Railroad:
                return "Railroads";
            case PropertyGroup.Utility:
                return "Utilities";
            default:
                return "Other Properties";
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

    private void OnHostButtonClicked()
    {
        Debug.Log("Host button clicked - showing game mode selection");
        ShowGameModeSelection();
        SetStatusInfo("Select a game mode");
    }

    private void OnDiceRaceButtonClicked()
    {
        Debug.Log("Dice Race button clicked");
        isMonopolyMode = false;
        isBattleshipsMode = false;
        ShowGameSetup();
        SetStatusInfo("Configure your Dice Race game settings");
    }

    private async void OnMonopolyButtonClicked()
    {
        Debug.Log("Monopoly button clicked");
        isMonopolyMode = true;
        isBattleshipsMode = false;
        
        // Disable the button while creating lobby
        if (monopolyButton != null) monopolyButton.interactable = false;
        
        // Use default player count of 2 for Monopoly
        configuredPlayerCount = 2;
        
        SetStatusInfo($"Creating Monopoly lobby for {configuredPlayerCount} players...");
        
        try
        {
            string lobbyCode = await LobbyManager.Instance.CreateLobby($"Monopoly - {configuredPlayerCount} players");
            if (!string.IsNullOrEmpty(lobbyCode))
            {
                MonopolyBoardManager.Instance.ConfigureGame(configuredPlayerCount);
                
                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                }
                SetStatusSuccess($"Monopoly lobby created! Code: {lobbyCode}");
                ShowLobby();
                UpdatePlayerList();
            }
            else
            {
                SetStatusError("Failed to create Monopoly lobby. Check console for details.");
                // Stay on game mode selection panel on error
            }
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error creating Monopoly lobby: {e.Message}");
            Debug.LogError($"Exception in OnMonopolyButtonClicked: {e}");
        }
        finally
        {
            // Re-enable the button
            if (monopolyButton != null) monopolyButton.interactable = true;
        }
    }

    private void OnBattleshipsButtonClicked()
    {
        Debug.Log("Battleships button clicked");
        
        // Set Battleships mode
        isBattleshipsMode = true;
        isMonopolyMode = false;
        
        // Hide all panels first
        HideAllPanels();
        
        // Let BattleshipsSetupManager handle showing its panel
        if (BattleshipsSetupManager.Instance != null)
        {
            BattleshipsSetupManager.Instance.ShowBattleshipsGameSetup();
            SetStatusInfo("Configure your Battleships game settings");
        }
        else
        {
            Debug.LogError("BattleshipsSetupManager instance not found!");
            SetStatusError("Battleships setup not available");
        }
    }

    private void OnBackFromGameModeClicked()
    {
        Debug.Log("Back from game mode selection clicked");
        ShowMainMenu();
    }

    private void OnBackFromSetupClicked()
    {
        Debug.Log("Back from setup button clicked");
        ShowGameModeSelection();
    }

    private void OnJoinButtonClicked()
    {
        Debug.Log("Join button clicked");
        
        if (joinCodeInput != null)
        {
            joinCodeInput.text = "";
        }
        ShowJoinPanel();
        SetStatusInfo("6 character lobby code");
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
        Debug.Log("Exit button clicked - quitting application");
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
            if (isBattleshipsMode)
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
                        
                        Debug.Log($"Board config from setup: {rows}x{cols}, IsCustomized={BattleshipsSetupManager.Instance.IsCustomized()}, tileStates={(tileStates != null ? "NOT NULL" : "NULL")}")
;
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

    private void SpawnMonopolyGameManager()
    {
        if (MonopolyGameManager.Instance != null && MonopolyGameManager.Instance.IsSpawned)
        {
            Debug.Log("MonopolyGameManager already spawned");
            return;
        }

        if (NetworkManager.Instance.IsHost())
        {
            Debug.Log("Spawning MonopolyGameManager...");
            
            var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
            if (unityNetworkManager == null)
            {
                Debug.LogError("Unity's NetworkManager component not found!");
                return;
            }

            GameObject monopolyGameManagerPrefab = null;
            
            if (unityNetworkManager.NetworkConfig != null && unityNetworkManager.NetworkConfig.Prefabs != null)
            {
                foreach (var networkPrefab in unityNetworkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    if (networkPrefab.Prefab != null && networkPrefab.Prefab.GetComponent<MonopolyGameManager>() != null)
                    {
                        monopolyGameManagerPrefab = networkPrefab.Prefab;
                        break;
                    }
                }
            }
            
            if (monopolyGameManagerPrefab != null)
            {
                GameObject instance = Instantiate(monopolyGameManagerPrefab);
                Unity.Netcode.NetworkObject networkObject = instance.GetComponent<Unity.Netcode.NetworkObject>();
                networkObject.Spawn();
                Debug.Log("MonopolyGameManager spawned from Network Prefabs list");
            }
            else
            {
                Debug.LogError("MonopolyGameManager prefab not found in Network Prefabs list!");
                
                GameObject monopolyGameManagerGO = new GameObject("MonopolyGameManager");
                MonopolyGameManager monopolyGameManager = monopolyGameManagerGO.AddComponent<MonopolyGameManager>();
                Unity.Netcode.NetworkObject networkObject = monopolyGameManagerGO.AddComponent<Unity.Netcode.NetworkObject>();
                
                networkObject.Spawn();
                Debug.LogWarning("MonopolyGameManager spawned dynamically as fallback");
            }
            
            SpawnMonopolyTradeManager();
        }
    }
    
    private void SpawnMonopolyTradeManager()
    {
        if (MonopolyTradeManager.Instance != null && MonopolyTradeManager.Instance.IsSpawned)
        {
            Debug.Log("MonopolyTradeManager already spawned");
            return;
        }

        Debug.Log("Spawning MonopolyTradeManager...");
        
        var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
        if (unityNetworkManager == null)
        {
            Debug.LogError("Unity's NetworkManager component not found!");
            return;
        }

        GameObject tradeManagerPrefab = null;
        
        if (unityNetworkManager.NetworkConfig != null && unityNetworkManager.NetworkConfig.Prefabs.Prefabs != null)
        {
            foreach (var networkPrefab in unityNetworkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab != null && networkPrefab.Prefab.GetComponent<MonopolyTradeManager>() != null)
                {
                    tradeManagerPrefab = networkPrefab.Prefab;
                    break;
                }
            }
        }
        
        if (tradeManagerPrefab != null)
        {
            GameObject instance = Instantiate(tradeManagerPrefab);
            Unity.Netcode.NetworkObject networkObject = instance.GetComponent<Unity.Netcode.NetworkObject>();
            networkObject.Spawn();
            Debug.Log("MonopolyTradeManager spawned from Network Prefabs list");
        }
        else
        {
            Debug.LogError("MonopolyTradeManager prefab not found in Network Prefabs list!");
            
            GameObject tradeManagerGO = new GameObject("MonopolyTradeManager");
            MonopolyTradeManager tradeManager = tradeManagerGO.AddComponent<MonopolyTradeManager>();
            Unity.Netcode.NetworkObject networkObject = tradeManagerGO.AddComponent<Unity.Netcode.NetworkObject>();
            
            networkObject.Spawn();
            Debug.LogWarning("MonopolyTradeManager spawned dynamically as fallback");
        }
    }

    private void SpawnBattleshipsGameManager()
    {
        if (BattleshipsGameManager.Instance != null && BattleshipsGameManager.Instance.IsSpawned)
        {
            Debug.Log("BattleshipsGameManager already spawned");
            return;
        }

        if (NetworkManager.Instance.IsHost())
        {
            Debug.Log("Spawning BattleshipsGameManager...");
            
            var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
            if (unityNetworkManager == null)
            {
                Debug.LogError("Unity's NetworkManager component not found!");
                return;
            }

            GameObject battleshipsGameManagerPrefab = null;
            
            if (unityNetworkManager.NetworkConfig != null && unityNetworkManager.NetworkConfig.Prefabs != null)
            {
                foreach (var networkPrefab in unityNetworkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    if (networkPrefab.Prefab != null && networkPrefab.Prefab.GetComponent<BattleshipsGameManager>() != null)
                    {
                        battleshipsGameManagerPrefab = networkPrefab.Prefab;
                        break;
                    }
                }
            }
            
            if (battleshipsGameManagerPrefab != null)
            {
                GameObject instance = Instantiate(battleshipsGameManagerPrefab);
                Unity.Netcode.NetworkObject networkObject = instance.GetComponent<Unity.Netcode.NetworkObject>();
                networkObject.Spawn();
                Debug.Log("BattleshipsGameManager spawned from Network Prefabs list");
            }
            else
            {
                Debug.LogWarning("BattleshipsGameManager prefab not found in Network Prefabs list - using fallback!");
                
                GameObject battleshipsGameManagerGO = new GameObject("BattleshipsGameManager");
                BattleshipsGameManager battleshipsGameManager = battleshipsGameManagerGO.AddComponent<BattleshipsGameManager>();
                Unity.Netcode.NetworkObject networkObject = battleshipsGameManagerGO.AddComponent<Unity.Netcode.NetworkObject>();
                
                networkObject.Spawn();
                Debug.Log("BattleshipsGameManager spawned dynamically (fallback method working)");
            }
        }
    }

    private void SpawnNetworkGameManager()
    {
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsSpawned)
        {
            Debug.Log("NetworkGameManager already spawned");
            return;
        }

        if (NetworkManager.Instance.IsHost())
        {
            Debug.Log("Spawning NetworkGameManager...");
            
            var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
            if (unityNetworkManager == null)
            {
                Debug.LogError("Unity's NetworkManager component not found!");
                return;
            }

            GameObject networkGameManagerPrefab = null;
            
            if (unityNetworkManager.NetworkConfig != null && unityNetworkManager.NetworkConfig.Prefabs != null)
            {
                foreach (var networkPrefab in unityNetworkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    if (networkPrefab.Prefab != null && networkPrefab.Prefab.GetComponent<NetworkGameManager>() != null)
                    {
                        networkGameManagerPrefab = networkPrefab.Prefab;
                        break;
                    }
                }
            }
            
            if (networkGameManagerPrefab != null)
            {
                GameObject instance = Instantiate(networkGameManagerPrefab);
                Unity.Netcode.NetworkObject networkObject = instance.GetComponent<Unity.Netcode.NetworkObject>();
                networkObject.Spawn();
                Debug.Log("NetworkGameManager spawned from Network Prefabs list");
            }
            else
            {
                Debug.LogError("NetworkGameManager prefab not found in Network Prefabs list!");
                
                GameObject networkGameManagerGO = new GameObject("NetworkGameManager");
                NetworkGameManager networkGameManager = networkGameManagerGO.AddComponent<NetworkGameManager>();
                Unity.Netcode.NetworkObject networkObject = networkGameManagerGO.AddComponent<Unity.Netcode.NetworkObject>();
                
                networkObject.Spawn();
                Debug.LogWarning("NetworkGameManager spawned dynamically as fallback");
            }
        }
    }

    private async void OnCreateLobbyButtonClicked()
    {
        Debug.Log($"Create lobby button clicked - Mode: {(isMonopolyMode ? "Monopoly" : "Dice Race")}");

        // Parse player count (used by both modes)
        if (int.TryParse(playerCountInput.text, out int playerCount))
        {
            configuredPlayerCount = Mathf.Clamp(playerCount, 2, 4);
        }
        else
        {
            configuredPlayerCount = 2;
        }

        if (createLobbyButton != null) createLobbyButton.interactable = false;
        
        if (isMonopolyMode)
        {
            // Monopoly mode - no tile count needed
            SetStatusInfo($"Creating Monopoly lobby for {configuredPlayerCount} players...");
            
            try
            {
                string lobbyCode = await LobbyManager.Instance.CreateLobby($"Monopoly - {configuredPlayerCount} players");
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                    MonopolyBoardManager.Instance.ConfigureGame(configuredPlayerCount);
                    
                    if (lobbyCodeText != null)
                    {
                        lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                    }
                    SetStatusSuccess($"Monopoly lobby created! Code: {lobbyCode}");
                    ShowLobby();
                    UpdatePlayerList();
                }
                else
                {
                    SetStatusError("Failed to create Monopoly lobby. Check console for details.");
                }
            }
            catch (System.Exception e)
            {
                SetStatusError($"Error creating Monopoly lobby: {e.Message}");
                Debug.LogError($"Exception in OnCreateLobbyButtonClicked (Monopoly): {e}");
            }
        }
        else
        {
            // Dice Race mode - parse tile count
            if (int.TryParse(tileCountInput.text, out int tileCount))
            {
                configuredTileCount = Mathf.Clamp(tileCount, 10, 100);
            }
            else
            {
                configuredTileCount = 20;
            }
            
            SetStatusInfo($"Creating dice race lobby for {configuredPlayerCount} players with {configuredTileCount} tiles...");
            
            try
            {
                string lobbyCode = await LobbyManager.Instance.CreateLobby($"Dice Race - {configuredTileCount} tiles");
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                    GameSetupManager.Instance.ConfigureGame(configuredTileCount, configuredPlayerCount);
                    
                    if (lobbyCodeText != null)
                    {
                        lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                    }
                    SetStatusSuccess($"Dice race lobby created! Code: {lobbyCode}");
                    ShowLobby();
                    UpdatePlayerList();
                }
                else
                {
                    SetStatusError("Failed to create lobby. Check console for details.");
                }
            }
            catch (System.Exception e)
            {
                SetStatusError($"Error creating lobby: {e.Message}");
                Debug.LogError($"Exception in OnCreateLobbyButtonClicked (Dice Race): {e}");
            }
        }
        
        // Re-enable button
        if (createLobbyButton != null) createLobbyButton.interactable = true;
    }

    private async void OnLeaveLobbyClicked()
    {
        Debug.Log("Leave lobby button clicked");
        
        if (leaveLobbyButton != null) leaveLobbyButton.interactable = false;
        
        SetStatusInfo("Leaving lobby...");
        
        try
        {
            await LobbyManager.Instance.LeaveLobby();
            SetStatusSuccess("Left lobby successfully");
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

    private void UpdatePlayerList()
    {
        if (playerListContent == null || playerListItemPrefab == null) 
        {
            return;
        }

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        List<Dictionary<string, string>> players = LobbyManager.Instance.GetPlayersInfo();
        
        if (players.Count == 0)
        {
            SetStatusInfo("Waiting for players to join...");
            return;
        }

        foreach (var playerInfo in players)
        {
            GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);
            
            TextMeshProUGUI playerText = playerItem.GetComponent<TextMeshProUGUI>();
            if (playerText != null)
            {
                string hostIndicator = playerInfo["IsHost"] == "True" ? " (Host)" : "";
                playerText.text = $"{playerInfo["DisplayName"]}{hostIndicator}";
                
                playerText.enableAutoSizing = false;
                playerText.fontSize = 14;
                playerText.alignment = TextAlignmentOptions.Center;
            }

            RectTransform rectTransform = playerItem.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                rectTransform.sizeDelta = new Vector2(0, 30);
            }
        }

        if (playerListContent != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent as RectTransform);
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        }

        if (players.Count > 0)
        {
            SetStatusTemporary($"Lobby has {players.Count} player(s)", Color.cyan, 3f);
        }
    }

    private void Update()
    {
        if (lobbyPanel != null && lobbyPanel.activeSelf)
        {
            if (Time.time - lastPlayerListUpdate > PLAYER_LIST_UPDATE_INTERVAL)
            {
                UpdatePlayerList();
                lastPlayerListUpdate = Time.time;
            }
        }
        
        if (isMonopolyMode && monopolyGamePanel != null && monopolyGamePanel.activeSelf)
        {
            if (MonopolyGameManager.Instance != null)
            {
                UpdateGameButtons();
                UpdatePlayerMoneyDisplay();
            }
        }
        else if (!isMonopolyMode && diceRaceGamePanel != null && diceRaceGamePanel.activeSelf)
        {
            if (NetworkGameManager.Instance != null)
            {
                UpdateGameButtons();
            }
        }
        
        if (isMonopolyMode)
        {
            if (MonopolyGameManager.Instance != null && MonopolyGameManager.Instance.IsSpawned)
            {
                EnsureNetworkEventSubscription();
            }
        }
        else
        {
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsSpawned)
            {
                EnsureNetworkEventSubscription();
            }
        }
    }

    private void EnsureNetworkEventSubscription()
    {
        if (!isSubscribedToNetworkEvents)
        {
            if ((isMonopolyMode && MonopolyGameManager.Instance != null) || 
                (!isMonopolyMode && NetworkGameManager.Instance != null))
            {
                SubscribeToNetworkEvents();
                isSubscribedToNetworkEvents = true;
            }
        }
    }

    #region Public API for External Managers

    /// <summary>
    /// Public method to set status info message
    /// </summary>
    public void SetStatusInfoPublic(string message) => SetStatusInfo(message);

    /// <summary>
    /// Public method to set status error message
    /// </summary>
    public void SetStatusErrorPublic(string message) => SetStatusError(message);

    /// <summary>
    /// Public method to set status success message
    /// </summary>
    public void SetStatusSuccessPublic(string message) => SetStatusSuccess(message);

    /// <summary>
    /// Public method to set lobby code text
    /// </summary>
    public void SetLobbyCode(string code)
    {
        if (lobbyCodeText != null)
        {
            lobbyCodeText.text = $"Lobby Code: {code}";
        }
    }

    /// <summary>
    /// Public method to update player list
    /// </summary>
    public void UpdatePlayerListPublic() => UpdatePlayerList();

    #endregion

    [ContextMenu("Validate All UI References")]
    public void ValidateAllUIReferences()
    {
        Debug.Log("=== UIManager Reference Validation ===");
        
        int missingCount = 0;
        
        if (mainMenuPanel == null) { Debug.LogError("MainMenuPanel is missing!"); missingCount++; }
        else Debug.Log("MainMenuPanel assigned");
        
        if (gameModeSelectionPanel == null) { Debug.LogError("GameModeSelectionPanel is missing!"); missingCount++; }
        else Debug.Log("GameModeSelectionPanel assigned");
        
        if (gameSetupPanel == null) { Debug.LogError("GameSetupPanel is missing!"); missingCount++; }
        else Debug.Log("GameSetupPanel assigned");
        
        if (diceRaceGamePanel == null) { Debug.LogError("DiceRaceGamePanel is missing!"); missingCount++; }
        else Debug.Log("DiceRaceGamePanel assigned");
        
        if (monopolyGamePanel == null) { Debug.LogError("MonopolyGamePanel is missing!"); missingCount++; }
        else Debug.Log("MonopolyGamePanel assigned");
        
        if (hostButton == null) { Debug.LogError("HostButton is missing!"); missingCount++; }
        else Debug.Log("HostButton assigned");
        
        if (joinButton == null) { Debug.LogError("JoinButton is missing!"); missingCount++; }
        else Debug.Log("JoinButton assigned");
        
        if (exitButton == null) { Debug.LogError("ExitButton is missing!"); missingCount++; }
        else Debug.Log("ExitButton assigned");
        
        // Game Mode Selection Panel validation
        if (diceRaceButton == null) { Debug.LogError("DiceRaceButton is missing!"); missingCount++; }
        else Debug.Log("DiceRaceButton assigned");
        
        if (monopolyButton == null) { Debug.LogError("MonopolyButton is missing!"); missingCount++; }
        else Debug.Log("MonopolyButton assigned");
        
        if (battleshipsButton == null) { Debug.LogError("BattleshipsButton is missing!"); missingCount++; }
        else Debug.Log("BattleshipsButton assigned");
        
        if (backFromGameModeButton == null) { Debug.LogError("BackFromGameModeButton is missing!"); missingCount++; }
        else Debug.Log("BackFromGameModeButton assigned");
        
        if (tileCountInput == null) { Debug.LogError("TileCountInput is missing!"); missingCount++; }
        else Debug.Log("TileCountInput assigned");
        
        if (playerCountInput == null) { Debug.LogError("PlayerCountInput is missing!"); missingCount++; }
        else Debug.Log("PlayerCountInput assigned");
        
        if (createLobbyButton == null) { Debug.LogError("CreateLobbyButton is missing!"); missingCount++; }
        else Debug.Log("CreateLobbyButton assigned");
        
        if (backFromSetupButton == null) { Debug.LogError("BackFromSetupButton is missing!"); missingCount++; }
        else Debug.Log("BackFromSetupButton assigned");
        
        if (submitJoinButton == null) { Debug.LogError("SubmitJoinButton is missing!"); missingCount++; }
        else Debug.Log("SubmitJoinButton assigned");
        
        if (backToMenuButton == null) { Debug.LogError("BackToMenuButton is missing!"); missingCount++; }
        else Debug.Log("BackToMenuButton assigned");
        
        if (startGameButton == null) { Debug.LogError("StartGameButton is missing!"); missingCount++; }
        else Debug.Log("StartGameButton assigned");
        
        if (leaveLobbyButton == null) { Debug.LogError("LeaveLobbyButton is missing!"); missingCount++; }
        else Debug.Log("LeaveLobbyButton assigned");
        
        if (joinCodeInput == null) { Debug.LogError("JoinCodeInput is missing!"); missingCount++; }
        else Debug.Log("JoinCodeInput assigned");
        
        if (lobbyCodeText == null) { Debug.LogError("LobbyCodeText is missing!"); missingCount++; }
        else Debug.Log("LobbyCodeText assigned");
        
        if (playerListContent == null) { Debug.LogError("PlayerListContent is missing!"); missingCount++; }
        else Debug.Log("PlayerListContent assigned");
        
        if (playerListItemPrefab == null) { Debug.LogError("PlayerListItemPrefab is missing!"); missingCount++; }
        else Debug.Log("PlayerListItemPrefab assigned");
        
        if (statusText == null) { Debug.LogError("StatusText is missing!"); missingCount++; }
        else Debug.Log("StatusText assigned");
        
        // Dice Race panel validation
        if (diceRaceCurrentPlayerText == null) { Debug.LogError("DiceRaceCurrentPlayerText is missing!"); missingCount++; }
        else Debug.Log("DiceRaceCurrentPlayerText assigned");
        
        if (diceRaceGameStatusText == null) { Debug.LogError("DiceRaceGameStatusText is missing!"); missingCount++; }
        else Debug.Log("DiceRaceGameStatusText assigned");
        
        if (diceRaceRollDiceButton == null) { Debug.LogError("DiceRaceRollDiceButton is missing!"); missingCount++; }
        else Debug.Log("DiceRaceRollDiceButton assigned");
        
        if (diceRaceDiceResultText == null) { Debug.LogError("DiceRaceDiceResultText is missing!"); missingCount++; }
        else Debug.Log("DiceRaceDiceResultText assigned");
        
        if (diceRaceLeaveGameButton == null) { Debug.LogError("DiceRaceLeaveGameButton is missing!"); missingCount++; }
        else Debug.Log("DiceRaceLeaveGameButton assigned");
        
        // Monopoly panel validation
        if (monopolyCurrentPlayerText == null) { Debug.LogError("MonopolyCurrentPlayerText is missing!"); missingCount++; }
        else Debug.Log("MonopolyCurrentPlayerText assigned");
        
        if (monopolyGameStatusText == null) { Debug.LogError("MonopolyGameStatusText is missing!"); missingCount++; }
        else Debug.Log("MonopolyGameStatusText assigned");
        
        if (monopolyRollDiceButton == null) { Debug.LogError("MonopolyRollDiceButton is missing!"); missingCount++; }
        else Debug.Log("MonopolyRollDiceButton assigned");
        
        if (purchasePropertyButton == null) { Debug.LogError("PurchasePropertyButton is missing!"); missingCount++; }
        else Debug.Log("PurchasePropertyButton assigned");
        
        if (playerMoneyText == null) { Debug.LogError("PlayerMoneyText is missing!"); missingCount++; }
        else Debug.Log("PlayerMoneyText assigned");
        
        if (gameMessagesText == null) { Debug.LogError("GameMessagesText is missing!"); missingCount++; }
        else Debug.Log("GameMessagesText assigned");
        
        if (cardMessagesText == null) Debug.LogWarning("CardMessagesText is not assigned (optional - cards will show in game messages)");
        
        Debug.Log($"=== Validation Complete: {missingCount} missing references ===");
        
        if (missingCount > 0)
        {
            Debug.LogWarning("You need to assign the missing UI elements in the Inspector!");
            Debug.LogWarning("Tip: Look for NEW fields in the Game Mode Selection Panel section");
        }
        else
        {
            Debug.Log("All UI references are properly assigned!");
        }
    }

    [ContextMenu("Toggle Game Mode (Testing)")]
    public void ToggleGameMode()
    {
        isMonopolyMode = !isMonopolyMode;
        string mode = isMonopolyMode ? "Monopoly" : "Dice Race";
        Debug.Log($"Game mode switched to: {mode}");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    #endregion
}
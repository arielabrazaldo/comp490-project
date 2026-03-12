using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;


/// <summary>
/// Streamlined UIManager focused on core menu flow:
/// Main Menu -> Create Custom Game (Rule Editor) OR Host Game (Saved Games List) OR Join Game
/// Note: Dice Race game UI is handled by DiceRaceUIManager
/// Note: Battleships game UI is handled by BattleshipsUIManager
/// </summary>
public class UIManager_Streamlined : MonoBehaviour
{
    private static UIManager_Streamlined instance;
    public static UIManager_Streamlined Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIManager_Streamlined>();
            }
            return instance;
        }
    }

    #region UI Panels
    [Header("Core Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject savedGamesPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinPanel;
    
    [Header("Game Setup Panels")]
    [SerializeField] private GameObject diceRaceSetupPanel;
    
    // Note: Dice Race game panel is now managed by DiceRaceUIManager
    // Note: Battleships panels are managed by BattleshipsUIManager/BattleshipsSetupManager
    #endregion

    #region Main Menu Buttons
    [Header("Main Menu Buttons")]
    [SerializeField] private Button createCustomGameButton;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button exitButton;
    #endregion

    #region Saved Games Panel
    [Header("Saved Games Panel")]
    [SerializeField] private Transform savedGamesListContent;
    [SerializeField] private GameObject savedGameItemPrefab;
    [SerializeField] private Button backFromSavedGamesButton;
    [SerializeField] private TextMeshProUGUI noSavedGamesText;
    [SerializeField] private TMPro.TMP_Dropdown filterDropdown;
    [SerializeField] private TMP_FontAsset headerFont;
    #endregion

    #region Join Panel
    [Header("Join Panel")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button submitJoinButton;
    [SerializeField] private Button backFromJoinButton;
    #endregion

    #region Lobby Panel
    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI lobbyTitleText;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;
    #endregion

    #region Dice Race Setup Panel
    [Header("Dice Race Setup Panel")]
    [SerializeField] private TMP_InputField diceRaceTileCountInput;
    [SerializeField] private TMP_InputField diceRacePlayerCountInput;
    [SerializeField] private Button diceRaceCreateButton;
    [SerializeField] private Button diceRaceBackButton;
    #endregion

    #region Status Display
    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    private Coroutine statusClearCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            
            // CRITICAL FIX: DontDestroyOnLoad only works for root GameObjects
            // Unparent the GameObject first if it has a parent
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
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
        SetupButtonListeners();
        ShowMainMenu();

        SetupPlayerListLayout();
        
        SetupFilterDropdown();
        
        // Subscribe to lobby player changes
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayersChanged += OnLobbyPlayersChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from lobby events
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayersChanged -= OnLobbyPlayersChanged;
        }
    }
    
    /// <summary>
    /// Called when players join or leave the lobby
    /// </summary>
    private void OnLobbyPlayersChanged()
    {
        Debug.Log("[UIManager_Streamlined] Lobby players changed - refreshing player list");
        
        // Only update if lobby panel is active
        if (lobbyPanel != null && lobbyPanel.activeInHierarchy)
        {
            UpdatePlayerList();
        }
    }

    private void SetupFilterDropdown()
    {
        if (filterDropdown == null)
        {
            return;
        }

        filterDropdown.ClearOptions();
        filterDropdown.AddOptions(new List<string> { "STANDARD GAMES", "CUSTOM GAMES" });

        filterDropdown.onValueChanged.RemoveAllListeners();
        filterDropdown.onValueChanged.AddListener(_ =>
        {
            // only rebuild if the saved games panel is currently open 
            if (savedGamesPanel != null && savedGamesPanel.activeInHierarchy)
            {
                LoadSavedGamesList();
            }
        });

        filterDropdown.value = 0; // default to standard
        filterDropdown.RefreshShownValue();
    }
    #endregion

    #region Setup Methods
    private void SetupButtonListeners()
    {
        // Main Menu
        createCustomGameButton?.onClick.AddListener(OnCreateCustomGameClicked);
        hostGameButton?.onClick.AddListener(OnHostGameClicked);
        joinGameButton?.onClick.AddListener(OnJoinGameClicked);
        exitButton?.onClick.AddListener(OnExitClicked);

        // Saved Games Panel
        backFromSavedGamesButton?.onClick.AddListener(OnBackFromSavedGamesClicked);

        // Join Panel
        submitJoinButton?.onClick.AddListener(OnSubmitJoinClicked);
        backFromJoinButton?.onClick.AddListener(OnBackFromJoinClicked);

        // Lobby Panel
        startGameButton?.onClick.AddListener(OnStartGameClicked);
        leaveLobbyButton?.onClick.AddListener(OnLeaveLobbyClicked);

        // Dice Race Setup Panel
        diceRaceCreateButton?.onClick.AddListener(OnDiceRaceCreateClicked);
        diceRaceBackButton?.onClick.AddListener(OnDiceRaceBackClicked);

        // Note: Dice Race Game Panel buttons are handled by DiceRaceUIManager
        // Note: Battleships Setup/Game buttons are handled by BattleshipsSetupManager/BattleshipsUIManager
    }

    private void SetupPlayerListLayout()
    {
        if (playerListContent == null) return;

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
    }
    #endregion

    #region Panel Management
    private void HideAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        savedGamesPanel?.SetActive(false);
        lobbyPanel?.SetActive(false);
        joinPanel?.SetActive(false);
        diceRaceSetupPanel?.SetActive(false);
        // Note: Dice Race game panel is managed by DiceRaceUIManager
        // Note: Battleships panels are managed by BattleshipsUIManager/BattleshipsSetupManager
    }

    private void ShowMainMenu()
    {
        HideAllPanels();
        
        // CRITICAL: Clean up any leftover Battleships panels
        // This ensures no panels from previous games remain visible
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.CleanupForMainMenu();
        }
        
        // Also ensure BattleshipsSetupManager panels are hidden
        if (BattleshipsSetupManager.Instance != null)
        {
            BattleshipsSetupManager.Instance.HideBattleshipsGameSetup();
        }
        
        // Also clean up DiceRaceUIManager if it exists
        if (DiceRaceUIManager.Instance != null)
        {
            DiceRaceUIManager.Instance.HideAndCleanup();
        }
        
        mainMenuPanel?.SetActive(true);
        ClearStatus();
        Debug.Log("Showing Main Menu");
    }

    private void ShowSavedGamesPanel()
    {
        HideAllPanels();
        savedGamesPanel?.SetActive(true);
        ClearStatus();
        LoadSavedGamesList();
        Debug.Log("Showing Saved Games Panel");
    }

    private void ShowJoinPanel()
    {
        HideAllPanels();
        joinPanel?.SetActive(true);
        ClearStatus();
        if (joinCodeInput != null) joinCodeInput.text = "";
        Debug.Log("Showing Join Panel");
    }

    private void ShowLobby()
    {
        HideAllPanels();
        
        // CRITICAL: Also ensure Battleships panels are hidden when showing lobby
        // This prevents leftover panels from previous games
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.CleanupForMainMenu();
        }
        
        lobbyPanel?.SetActive(true);
        ClearStatus();

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        }
        
        // Update player list when showing lobby
        UpdatePlayerList();

        Debug.Log("Showing Lobby Panel");
    }

    private void ShowDiceRaceSetupPanel()
    {
        HideAllPanels();
        diceRaceSetupPanel?.SetActive(true);
        ClearStatus();
        Debug.Log("Showing Dice Race Setup Panel");
    }

    private void ShowBattleshipsSetupPanel()
    {
        HideAllPanels();
        
        // Delegate entirely to BattleshipsSetupManager
        if (BattleshipsSetupManager.Instance != null)
        {
            Debug.Log("[UIManager_Streamlined] Delegating to BattleshipsSetupManager");
            BattleshipsSetupManager.Instance.ShowBattleshipsGameSetup();
        }
        else
        {
            Debug.LogError("[UIManager_Streamlined] BattleshipsSetupManager not found!");
            SetStatus("Error: Battleships setup not available", Color.red);
            ShowSavedGamesPanel();
            return;
        }
        
        ClearStatus();
        Debug.Log("Showing Battleships Setup Panel");
    }
    #endregion

    #region Button Click Handlers - Main Menu
    private void OnCreateCustomGameClicked()
    {
        Debug.Log("Create Custom Game button clicked");

        // Check if RuleEditorManager exists
        if (RuleEditorManager.Instance == null)
        {
            Debug.LogError("[UIManager_Streamlined] RuleEditorManager instance not found!");
            Debug.LogError("[UIManager_Streamlined] Please add a GameObject named 'RuleEditorManager' to your scene hierarchy.");
            Debug.LogError("[UIManager_Streamlined] Then add the RuleEditorManager component to it.");
            SetStatus("Error: RuleEditorManager not found in scene! Check console.", Color.red);
            return;
        }

        // Check if RuleEditorUI exists
        RuleEditorUI ruleEditorUI = FindFirstObjectByType<RuleEditorUI>();
        if (ruleEditorUI == null)
        {
            Debug.LogError("[UIManager_Streamlined] RuleEditorUI not found in scene!");
            Debug.LogError("[UIManager_Streamlined] Please add a GameObject with RuleEditorUI component to your scene.");
            SetStatus("Error: RuleEditorUI not found in scene! Check console.", Color.red);
            return;
        }

        // Hide main menu panel before showing rule editor
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            Debug.Log("[UIManager_Streamlined] Hidden main menu panel");
        }

        // Show rule editor
        RuleEditorManager.Instance.ShowRuleEditor();
        SetStatus("Configure your custom game rules", Color.cyan);
    }

    private void OnHostGameClicked()
    {
        Debug.Log("Host Game button clicked");
        ShowSavedGamesPanel();
        SetStatus("Select a saved game to host", Color.cyan);
    }

    private void OnJoinGameClicked()
    {
        Debug.Log("Join Game button clicked");
        ShowJoinPanel();
        SetStatus("Enter 6-character lobby code", Color.cyan);
    }

    private void OnExitClicked()
    {
        Debug.Log("Exit button clicked");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    #endregion

    #region Button Click Handlers - Saved Games Panel
    private void OnBackFromSavedGamesClicked()
    {
        Debug.Log("Back from saved games clicked");
        ShowMainMenu();
    }

    private async void OnHostSavedGameClicked(SavedGameInfo gameInfo)
    {
        Debug.Log($"Hosting saved game: {gameInfo.gameName}");

        // DICE RACE: Show Dice Race setup panel
        if (gameInfo.gameType == 3 || gameInfo.gameName == "Dice Race")
        {
            Debug.Log("[UIManager_Streamlined] Dice Race detected - showing setup panel");
            
            // Store the game info for later use after setup
            StoreGameInfoForCustomization(gameInfo);
            
            // Set default values in input fields
            if (diceRaceTileCountInput != null)
            {
                diceRaceTileCountInput.text = gameInfo.rules?.tilesPerSide.ToString() ?? "20";
            }
            if (diceRacePlayerCountInput != null)
            {
                diceRacePlayerCountInput.text = gameInfo.playerCount.ToString();
            }
            
            // Show Dice Race setup panel
            ShowDiceRaceSetupPanel();
            SetStatus("Configure Dice Race settings", Color.cyan);
            return;
        }
        
        // BATTLESHIPS: Show Battleships setup panel
        if (gameInfo.gameType == 2 || gameInfo.gameName == "Classic Battleships")
        {
            Debug.Log("[UIManager_Streamlined] Battleships detected - showing setup panel");
            
            // Store the game info for later use after setup
            StoreGameInfoForCustomization(gameInfo);
            
            // Show Battleships setup panel
            ShowBattleshipsSetupPanel();
            SetStatus("Configure Battleships settings", Color.cyan);
            return;
        }

        // Standard hosting flow (for Monopoly and other games)
        await HostStandardGame(gameInfo);
    }

    /// <summary>
    /// Store game info temporarily for use after customization
    /// </summary>
    private SavedGameInfo pendingGameInfo;
    private void StoreGameInfoForCustomization(SavedGameInfo gameInfo)
    {
        pendingGameInfo = gameInfo;
        Debug.Log($"[UIManager_Streamlined] Stored game info for customization: {gameInfo.gameName}");
    }

    /// <summary>
    /// Called by PresetUIManager after user confirms customization
    /// </summary>
    public async void OnPresetCustomizationComplete(SerializableGameData customizedData)
    {
        Debug.Log("[UIManager_Streamlined] Preset customization complete - creating lobby");

        if (customizedData == null)
        {
            Debug.LogError("[UIManager_Streamlined] Customized data is null!");
            SetStatus("Error: Invalid customization data", Color.red);
            ShowMainMenu();
            return;
        }

        // Convert back to SavedGameInfo
        SavedGameInfo customizedGameInfo = customizedData.ToSavedGameInfo();
        
        // Host the customized game
        await HostStandardGame(customizedGameInfo);
    }

    /// <summary>
    /// Standard game hosting logic (extracted for reuse)
    /// </summary>
    private async System.Threading.Tasks.Task HostStandardGame(SavedGameInfo gameInfo)
    {
        if (gameInfo.rules == null)
        {
            SetStatus("Failed to load game rules!", Color.red);
            Debug.LogError($"[UIManager_Streamlined] gameInfo.rules is NULL for game: {gameInfo.gameName}");
            return;
        }

        SetStatus($"Creating lobby for {gameInfo.gameName}...", Color.yellow);

        try
        {
            // Store FULL game info (including gameType) BEFORE creating lobby
            if (RuleEditorManager.Instance != null)
            {
                Debug.Log($"[UIManager_Streamlined] Setting game info: {gameInfo.gameName} (GameType: {gameInfo.gameType})");
                Debug.Log($"[UIManager_Streamlined] Rules preview: EnableCurrency={gameInfo.rules.enableCurrency}, StartingMoney={gameInfo.rules.startingMoney}");
                
                // CRITICAL FIX: Use SetCurrentGameInfo to preserve gameType for spawning
                RuleEditorManager.Instance.SetCurrentGameInfo(gameInfo);
                
                // Verify game info was actually set
                SavedGameInfo storedInfo = RuleEditorManager.Instance.GetCurrentGameInfo();
                if (storedInfo == null)
                {
                    Debug.LogError("[UIManager_Streamlined] GameInfo was NOT stored in RuleEditorManager! (returned null)");
                    SetStatus("Error: Failed to store game info!", Color.red);
                    return;
                }
                else
                {
                    Debug.Log($"[UIManager_Streamlined] GameInfo verified: GameType={storedInfo.gameType}, Name={storedInfo.gameName}");
                }
            }
            else
            {
                Debug.LogError("[UIManager_Streamlined] RuleEditorManager.Instance is NULL!");
                SetStatus("Error: RuleEditorManager not found!", Color.red);
                return;
            }

            // Now create lobby
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

                SetStatus($"Lobby created! Code: {lobbyCode}", Color.green);
                ShowLobby();
            }
            else
            {
                SetStatus("Failed to create lobby", Color.red);
            }
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}", Color.red);
            Debug.LogError($"Lobby creation error: {e}");
        }
    }
    #endregion

    #region Button Click Handlers - Join Panel
    private async void OnSubmitJoinClicked()
    {
        Debug.Log("Submit join button clicked");

        if (joinCodeInput == null) return;

        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Please enter a lobby code", Color.red);
            return;
        }

        if (submitJoinButton != null) submitJoinButton.interactable = false;
        if (joinCodeInput != null) joinCodeInput.interactable = false;

        SetStatus($"Joining lobby with code: {code}...", Color.yellow);

        try
        {
            bool joined = await LobbyManager.Instance.JoinLobby(code);

            if (joined)
            {
                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {code}";
                }

                SetStatus("Connected to lobby!", Color.green);
                await System.Threading.Tasks.Task.Delay(500);

                ShowLobby();
                UpdatePlayerList();
            }
            else
            {
                SetStatus($"Failed to join lobby with code: {code}", Color.red);
            }
        }
        catch (System.Exception e)
        {
            SetStatus($"Error joining lobby: {e.Message}", Color.red);
            Debug.LogError($"Exception in OnSubmitJoinClicked: {e}");
        }
        finally
        {
            if (submitJoinButton != null) submitJoinButton.interactable = true;
            if (joinCodeInput != null) joinCodeInput.interactable = true;
        }
    }

    private void OnBackFromJoinClicked()
    {
        Debug.Log("Back from join clicked");
        ShowMainMenu();
    }
    #endregion

    #region Button Click Handlers - Dice Race Setup Panel
    private async void OnDiceRaceCreateClicked()
    {
        Debug.Log("[UIManager_Streamlined] Dice Race Create button clicked");

        if (pendingGameInfo == null)
        {
            Debug.LogError("[UIManager_Streamlined] No pending game info for Dice Race!");
            SetStatus("Error: No game info!", Color.red);
            ShowSavedGamesPanel();
            return;
        }

        // Get customized values from input fields
        int tileCount = 20; // Default
        int playerCount = 4; // Default

        if (diceRaceTileCountInput != null && int.TryParse(diceRaceTileCountInput.text, out int parsedTiles))
        {
            tileCount = Mathf.Clamp(parsedTiles, 10, 50);
        }

        if (diceRacePlayerCountInput != null && int.TryParse(diceRacePlayerCountInput.text, out int parsedPlayers))
        {
            playerCount = Mathf.Clamp(parsedPlayers, 2, 8);
        }

        Debug.Log($"[UIManager_Streamlined] Dice Race settings: {tileCount} tiles, {playerCount} players");

        // Update the game info with customized values
        if (pendingGameInfo.rules != null)
        {
            pendingGameInfo.rules.tilesPerSide = tileCount;
            pendingGameInfo.rules.maxPlayers = playerCount;
        }
        pendingGameInfo.playerCount = playerCount;

        SetStatus($"Creating Dice Race lobby ({tileCount} tiles, {playerCount} players)...", Color.yellow);

        // Host the game with updated settings
        await HostStandardGame(pendingGameInfo);
    }

    private void OnDiceRaceBackClicked()
    {
        Debug.Log("[UIManager_Streamlined] Dice Race Back button clicked");
        pendingGameInfo = null;
        ShowSavedGamesPanel();
    }
    #endregion

    #region Button Click Handlers - Lobby Panel
    private async void OnStartGameClicked()
    {
        Debug.Log("Start game button clicked");

        if (!LobbyManager.Instance.IsLobbyHost())
        {
            SetStatus("Only the host can start the game!", Color.red);
            return;
        }

        if (startGameButton != null) startGameButton.interactable = false;
        SetStatus("Starting game...", Color.yellow);

        try
        {
            // Check if RuleEditorManager exists
            if (RuleEditorManager.Instance == null)
            {
                Debug.LogError("[UIManager_Streamlined] ? RuleEditorManager.Instance is NULL!");
                SetStatus("Error: RuleEditorManager not found!", Color.red);
                return;
            }

            // Get current game info to check game type
            SavedGameInfo currentGameInfo = RuleEditorManager.Instance.GetCurrentGameInfo();
            GameRules currentRules = RuleEditorManager.Instance.GetCurrentRules();
            
            if (currentRules == null)
            {
                Debug.LogError("[UIManager_Streamlined] ? GetCurrentRules() returned NULL!");
                SetStatus("Error: No game rules configured!", Color.red);
                return;
            }

            int playerCount = LobbyManager.Instance.GetPlayersInfo().Count;
            Debug.Log($"[UIManager_Streamlined] ? Rules found: {currentRules.GetRulesSummary()}");

            // DICE RACE: Use NetworkGameManager directly (bypass CustomGameSpawner)
            int gameType = currentGameInfo?.gameType ?? 0;
            if (gameType == 3 || (currentGameInfo != null && currentGameInfo.gameName.Contains("Dice Race")))
            {
                Debug.Log("[UIManager_Streamlined] Dice Race detected - using NetworkGameManager directly");
                bool diceRaceSuccess = await StartDiceRaceGame(currentRules, playerCount);
                
                if (diceRaceSuccess)
                {
                    SetStatus("Dice Race started!", Color.green);
                }
                else
                {
                    SetStatus("Failed to start Dice Race - check console", Color.red);
                }
                return;
            }

            // OTHER GAMES: Use CustomGameSpawner
            if (CustomGameSpawner.Instance == null)
            {
                Debug.LogError("[UIManager_Streamlined] ? CustomGameSpawner.Instance is NULL!");
                SetStatus("Error: CustomGameSpawner not found!", Color.red);
                return;
            }

            Debug.Log("[UIManager_Streamlined] Starting custom game with rule-based spawner");
            bool success = await CustomGameSpawner.Instance.SpawnGameFromCurrentRules(playerCount);

            if (success)
            {
                SetStatus("Custom game started!", Color.green);
            }
            else
            {
                SetStatus("Failed to start custom game - check console", Color.red);
            }
        }
        catch (System.Exception e)
        {
            SetStatus($"Error starting game: {e.Message}", Color.red);
            Debug.LogError($"Exception in OnStartGameClicked: {e}");
        }
        finally
        {
            if (startGameButton != null) startGameButton.interactable = true;
        }
    }

    /// <summary>
    /// Start a Dice Race game directly using NetworkGameManager (bypasses CustomGameSpawner)
    /// </summary>
    private async System.Threading.Tasks.Task<bool> StartDiceRaceGame(GameRules rules, int playerCount)
    {
        Debug.Log("[UIManager_Streamlined] Starting Dice Race game directly...");

        try
        {
            // Get tile count from rules
            int tileCount = Mathf.Clamp(rules.tilesPerSide, 10, 100);
            Debug.Log($"[UIManager_Streamlined] Dice Race config: {tileCount} tiles, {playerCount} players");

            // Configure and generate board via GameSetupManager
            if (GameSetupManager.Instance != null)
            {
                GameSetupManager.Instance.ConfigureGame(tileCount, playerCount);
                GameSetupManager.Instance.GenerateBoard();
                Debug.Log("[UIManager_Streamlined] ? Board generated via GameSetupManager");
            }
            else
            {
                Debug.LogWarning("[UIManager_Streamlined] GameSetupManager not found - board may not be generated");
            }

            // Wait a moment for board generation
            await System.Threading.Tasks.Task.Delay(200);

            // Initialize NetworkGameManager if it exists
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.InitializeGame(playerCount);
                Debug.Log("[UIManager_Streamlined] ? NetworkGameManager initialized");
                
                // Hide menu panels
                HideAllPanels();
                
                // Delegate to DiceRaceUIManager for game UI
                if (DiceRaceUIManager.Instance != null)
                {
                    DiceRaceUIManager.Instance.StartGame();
                    Debug.Log("[UIManager_Streamlined] ? Delegated to DiceRaceUIManager");
                }
                else
                {
                    Debug.LogError("[UIManager_Streamlined] ? DiceRaceUIManager.Instance is NULL!");
                    Debug.LogError("[UIManager_Streamlined] Make sure DiceRaceUIManager exists in the scene");
                    return false;
                }
                
                return true;
            }
            else
            {
                Debug.LogError("[UIManager_Streamlined] ? NetworkGameManager.Instance is NULL!");
                Debug.LogError("[UIManager_Streamlined] Make sure NetworkGameManager prefab is in the scene or registered as NetworkPrefab");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager_Streamlined] Error starting Dice Race: {e}");
            return false;
        }
    }

    private async void OnLeaveLobbyClicked()
    {
        Debug.Log("Leave lobby button clicked");

        if (leaveLobbyButton != null) leaveLobbyButton.interactable = false;
        SetStatus("Leaving lobby...", Color.yellow);

        try
        {
            await LobbyManager.Instance.LeaveLobby();
            SetStatus("Left lobby", Color.green);
            ShowMainMenu();
        }
        catch (System.Exception e)
        {
            SetStatus($"Error leaving lobby: {e.Message}", Color.red);
            Debug.LogError($"Exception in OnLeaveLobbyClicked: {e}");
        }
        finally
        {
            if (leaveLobbyButton != null) leaveLobbyButton.interactable = true;
        }
    }
    #endregion

    #region Saved Games Management
    private void LoadSavedGamesList()
    {
        Debug.Log("[UIManager_Streamlined] ========== LoadSavedGamesList START ==========");
        
        if (savedGamesListContent == null || savedGameItemPrefab == null)
        {
            Debug.LogError("[UIManager_Streamlined] ? Saved games list content or prefab not assigned!");
            if (savedGamesListContent == null) Debug.LogError("  - savedGamesListContent is NULL");
            if (savedGameItemPrefab == null) Debug.LogError("  - savedGameItemPrefab is NULL");
            return;
        }

        Debug.Log("[UIManager_Streamlined] ? UI references valid");

        // Clear existing list
        foreach (Transform child in savedGamesListContent)
        {
            if (filterDropdown != null && child == filterDropdown.transform)
            {
                continue;
            }
            Destroy(child.gameObject);
        }

        Debug.Log("[UIManager_Streamlined] Cleared existing list items");

        // Get standard games from library
        List<SavedGameInfo> standardGames = new List<SavedGameInfo>();
        
        Debug.Log($"[UIManager_Streamlined] Checking StandardGameLibrary.Instance... (null? {StandardGameLibrary.Instance == null})");
        
        if (StandardGameLibrary.Instance != null)
        {
            Debug.Log("[UIManager_Streamlined] ? StandardGameLibrary instance found!");
            standardGames = StandardGameLibrary.Instance.GetAllStandardGames();
            Debug.Log($"[UIManager_Streamlined] ? Loaded {standardGames.Count} standard games from library");
            
            foreach (var game in standardGames)
            {
                Debug.Log($"  - {game.gameName} ({game.gameType}, {game.playerCount}P)");
            }
        }
        else
        {
            Debug.LogError("[UIManager_Streamlined] ? StandardGameLibrary instance is NULL!");
            Debug.LogError("[UIManager_Streamlined] Please ensure StandardGameLibrary GameObject exists in hierarchy!");
        }

        // Load custom saved games from disk
        List<SavedGameInfo> customGames = new List<SavedGameInfo>();
        
        Debug.Log($"[UIManager_Streamlined] Checking GameSaveManager.Instance... (null? {GameSaveManager.Instance == null})");
        
        if (GameSaveManager.Instance != null)
        {
            Debug.Log("[UIManager_Streamlined] ? GameSaveManager instance found!");
            customGames = GameSaveManager.Instance.LoadAllGames();
            Debug.Log($"[UIManager_Streamlined] ? Loaded {customGames.Count} custom games from disk");
            
            foreach (var game in customGames)
            {
                Debug.Log($"  - {game.gameName} ({game.gameType}, {game.playerCount}P)");
            }
        }
        else
        {
            Debug.LogError("[UIManager_Streamlined] ? GameSaveManager instance is NULL!");
            Debug.LogError("[UIManager_Streamlined] Please ensure GameSaveManager GameObject exists in hierarchy!");
        }

        // Check if we have any games to display
        int totalGames = standardGames.Count + customGames.Count;
        Debug.Log($"[UIManager_Streamlined] Total games to display: {totalGames} (Standard: {standardGames.Count}, Custom: {customGames.Count})");
        
        if (totalGames == 0)
        {
            Debug.LogWarning("[UIManager_Streamlined] ?? No games found - showing 'no games' message");
            
            if (noSavedGamesText != null)
            {
                noSavedGamesText.gameObject.SetActive(true);
                noSavedGamesText.text = "No games available.\nCreate a custom game to get started!";
                Debug.Log("[UIManager_Streamlined] ? 'No games' message displayed");
            }
            else
            {
                Debug.LogError("[UIManager_Streamlined] ? noSavedGamesText is NULL!");
            }
            
            Debug.Log("[UIManager_Streamlined] ========== LoadSavedGamesList END (No Games) ==========");
            return;
        }

        // Hide "no games" text
        if (noSavedGamesText != null)
        {
            noSavedGamesText.gameObject.SetActive(false);
            Debug.Log("[UIManager_Streamlined] Hidden 'no games' message");
        }

        // decide which list to show based on dropdown
        bool showStandard = (filterDropdown == null || filterDropdown.value == 0);

        if (showStandard)
        {
            // Show only Standard Games
            if (standardGames.Count > 0)


            {
                AddSectionHeader("STANDARD GAMES");
                foreach (var game in standardGames)
                {
                    GameObject listItem = Instantiate(savedGameItemPrefab, savedGamesListContent);
                    SetupSavedGameListItem(listItem, game);
                }
            }
            else
            {
                // Optional: show message if no standard games
                AddSectionHeader("STANDARD GAMES");
            }
        
        }
        else
        {
            // Show only Custom Games
            if (customGames.Count > 0)
            {
                AddSectionHeader("CUSTOM GAMES");
                foreach (var game in customGames)
                {
                    GameObject listItem = Instantiate(savedGameItemPrefab, savedGamesListContent);
                    SetupSavedGameListItem(listItem, game);
                }
            }
            else
            {
                // Optional: show message if no custom games
                AddSectionHeader("CUSTOM GAMES");
            }
        }

        // Force layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(savedGamesListContent as RectTransform);
        
        Debug.Log("[UIManager_Streamlined] ? Layout rebuilt");
        Debug.Log("[UIManager_Streamlined] ========== LoadSavedGamesList END (Success) ==========");
    }

    /// <summary>
    /// Add a section header to the saved games list
    /// </summary>
    private void AddSectionHeader(string headerText)
    {
        // Create a simple text object as section header
        GameObject headerObject = new GameObject($"Header_{headerText}");
        headerObject.transform.SetParent(savedGamesListContent, false);
        
         TextMeshProUGUI headerTextComponent = headerObject.AddComponent<TextMeshProUGUI>();
        headerTextComponent.text = headerText;
        headerTextComponent.fontSize = 18;
        headerTextComponent.fontStyle = TMPro.FontStyles.Bold;
        headerTextComponent.alignment = TextAlignmentOptions.Left;
        headerTextComponent.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        headerTextComponent.margin = new Vector4(10, 10, 10, 5);

        
        // assign TMP font asset here through script and font size
        if (headerFont != null)
        {
            headerTextComponent.font = headerFont;
            headerTextComponent.fontSize = 30;
        }
        
        // Add layout element
        var layoutElement = headerObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.preferredHeight = 30;
    }

    private void SetupSavedGameListItem(GameObject listItem, SavedGameInfo gameInfo)
     {
        // This will find TMP texts anywhere under this prefab even if it is nested
        TextMeshProUGUI gameNameText = null;
        TextMeshProUGUI gameDetailsText = null;

        foreach (var t in listItem.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.gameObject.name == "GameNameText") gameNameText = t;
            else if (t.gameObject.name == "GameDetailsText") gameDetailsText = t;
        }

        if (gameNameText != null)


        {
            gameNameText.text = gameInfo.gameName;
            gameNameText.color = Color.white;



        }


        else Debug.LogWarning("[UIManager_Streamlined] GameNameText not found in prefab instance.");

        if (gameDetailsText != null)
        {
            string description = gameInfo.GetDescription();

            // Replace "Modified ..." part with yellow rich text
            if (!gameInfo.isStandardGame)
            {
                description = description.Replace(
                    $"Modified {gameInfo.lastModifiedDate:MMM dd}",
                    $"<color=#FFD700>Modified {gameInfo.lastModifiedDate:MMM dd}</color>"
                );
            }

            gameDetailsText.text = description;
        }
        else Debug.LogWarning("[UIManager_Streamlined] GameDetailsText not found in prefab instance.");

        Button button = listItem.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnHostSavedGameClicked(gameInfo));
        }
        else
        {
            Debug.LogWarning("[UIManager_Streamlined] No Button found on SavedGameItemPrefab root. Add a Button component to the prefab root.");
        }


    }

    /// <summary>
    /// Public method to trigger hosting a saved game (called by UI elements or external scripts)
    /// </summary>
    public void HostSavedGame(SavedGameInfo gameInfo)
    {
        OnHostSavedGameClicked(gameInfo);
    }
    #endregion

    #region Player List Management
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
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerListContent as RectTransform);
    }
    #endregion

    #region Status Management
    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
            statusText.gameObject.SetActive(true);
            Debug.Log($"Status: {message}");
        }
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
    #endregion

    #region Public API - Called by RuleEditorUI
    /// <summary>
    /// Called from RuleEditorUI after user finishes configuring rules.
    /// Saves the custom game and returns to main menu (does NOT create lobby).
    /// </summary>
    public void OnRulesConfigured(GameRules rules)
    {
        // Generate a default name based on game type
        int gameType = DetermineGameTypeFromRules(rules);
        int recommendedPlayerCount = DeterminePlayerCountFromRules(rules);
        string gameName = $"{GetGameTypeName(gameType)} ({recommendedPlayerCount}P)";
        
        OnRulesConfigured(rules, gameName);
    }
    
    /// <summary>
    /// Called from RuleEditorUI after user finishes configuring rules with a custom name.
    /// Saves the custom game and returns to main menu (does NOT create lobby).
    /// </summary>
    public void OnRulesConfigured(GameRules rules, string gameName)
    {
        Debug.Log($"[UIManager_Streamlined] Rules configured, saving game as '{gameName}'...");

        // Determine game type from rules (no analyzer needed)
        int gameType = DetermineGameTypeFromRules(rules);
        int recommendedPlayerCount = DeterminePlayerCountFromRules(rules);

        // Create SavedGameInfo
        SavedGameInfo savedGame = new SavedGameInfo(
            gameName,
            gameType,
            recommendedPlayerCount,
            rules,
            isStandardGame: false // Custom game
        );

        // Save game to disk
        if (GameSaveManager.Instance != null)
        {
            bool saved = GameSaveManager.Instance.SaveGame(savedGame);
            if (saved)
            {
                Debug.Log($"? Custom game '{gameName}' saved to disk");
                SetStatus($"Game '{gameName}' saved! Go to 'Host Game' to create a lobby.", Color.green);
            }
            else
            {
                Debug.LogWarning($"?? Failed to save custom game '{gameName}'");
                SetStatus("Warning: Game not saved to disk", Color.yellow);
            }
        }
        else
        {
            Debug.LogWarning("[UIManager_Streamlined] GameSaveManager not found - game not saved!");
            SetStatus("Warning: GameSaveManager not found", Color.yellow);
        }

        // CRITICAL FIX: Don't create lobby here - just return to main menu
        // User will create lobby by clicking "Host Game" button
        Debug.Log("[UIManager_Streamlined] Returning to main menu. User can now host the game via 'Host Game' button.");
        
        // Delay showing main menu to let user see the success message
        StartCoroutine(ReturnToMainMenuAfterDelay(2f));
    }
    
    /// <summary>
    /// Called from RuleEditorUI to save the game without navigating away.
    /// This allows the RuleEditorUI to show its own confirmation panel.
    /// </summary>
    /// <param name="rules">The game rules to save</param>
    /// <param name="gameName">The name for the game</param>
    /// <param name="overwrite">If true, overwrite existing game with same name</param>
    public void OnRulesConfiguredWithoutNavigation(GameRules rules, string gameName, bool overwrite = false)
    {
        Debug.Log($"[UIManager_Streamlined] Rules configured (no navigation), saving game as '{gameName}' (overwrite={overwrite})...");

        // Determine game type from rules
        int gameType = DetermineGameTypeFromRules(rules);
        int recommendedPlayerCount = DeterminePlayerCountFromRules(rules);

        // Create SavedGameInfo
        SavedGameInfo savedGame = new SavedGameInfo(
            gameName,
            gameType,
            recommendedPlayerCount,
            rules,
            isStandardGame: false // Custom game
        );

        // Save game to disk
        if (GameSaveManager.Instance != null)
        {
            bool saved = GameSaveManager.Instance.SaveGame(savedGame, overwrite);
            if (saved)
            {
                string action = overwrite ? "updated" : "saved";
                Debug.Log($"? Custom game '{gameName}' {action} to disk");
                SetStatus($"Game '{gameName}' {action}!", Color.green);
            }
            else
            {
                Debug.LogWarning($"?? Failed to save custom game '{gameName}'");
                SetStatus("Warning: Game not saved to disk", Color.yellow);
            }
        }
        else
        {
            Debug.LogWarning("[UIManager_Streamlined] GameSaveManager not found - game not saved!");
            SetStatus("Warning: GameSaveManager not found", Color.yellow);
        }
        
        // Don't navigate away - let RuleEditorUI handle the confirmation panel
        Debug.Log("[UIManager_Streamlined] Game saved. RuleEditorUI will handle navigation.");
    }

    /// <summary>
    /// Get display name for game type int
    /// </summary>
    private string GetGameTypeName(int gameType)
    {
        return gameType switch
        {
            1 => "Monopoly",
            2 => "Battleships",
            3 => "Dice Race",
            4 => "Hybrid",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determine game type from rules without using CustomGameAnalyzer.
    /// Uses explicit feature checks instead of scoring algorithm.
    /// Returns: 1=Monopoly, 2=Battleships, 3=DiceRace, 4=Hybrid
    /// </summary>
    private int DetermineGameTypeFromRules(GameRules rules)
    {
        // Check for Monopoly-specific features
        if (rules.enableCurrency && rules.canPurchaseProperties && rules.enableRentCollection)
        {
            return 1; // Monopoly
        }
        
        // Check for Battleships-specific features
        if (rules.separatePlayerBoards && rules.enableCombat && rules.enableShipPlacement)
        {
            return 2; // Battleships
        }
        
        // Check for simple Dice Race (no complex features)
        if (!rules.enableCurrency && !rules.canPurchaseProperties && !rules.enableCombat &&
            (rules.winCondition == WinCondition.ReachGoal || rules.winCondition == WinCondition.ReachSpecificTile))
        {
            return 3; // Dice Race
        }
        
        // Default to Hybrid for mixed/custom rules
        return 4; // Hybrid
    }

    /// <summary>
    /// Determine recommended player count from rules.
    /// </summary>
    private int DeterminePlayerCountFromRules(GameRules rules)
    {
        // Battleships is typically 2 players
        if (rules.separatePlayerBoards && rules.enableCombat)
        {
            return Mathf.Clamp(rules.maxPlayers, 2, 2);
        }
        
        // Use the rules' max players, clamped to reasonable range
        return Mathf.Clamp(rules.maxPlayers, 2, 4);
    }

    /// <summary>
    /// Return to main menu after a delay (so user can see save confirmation)
    /// </summary>
    private System.Collections.IEnumerator ReturnToMainMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowMainMenuPublic();
    }
    #endregion

    #region Public API - General
    public void UpdatePlayerListPublic()
    {
        UpdatePlayerList();
    }

    public void SetLobbyCode(string code)
    {
        if (lobbyCodeText != null)
        {
            lobbyCodeText.text = $"Lobby Code: {code}";
        }
    }

    /// <summary>
    /// Set the lobby title text
    /// </summary>
    public void SetLobbyTitle(string title)
    {
        if (lobbyTitleText != null)
        {
            lobbyTitleText.text = title;
        }
    }

    public void ShowLobbyPublic()
    {
        ShowLobby();
    }

    /// <summary>
    /// Public method to show main menu (called by RuleEditorUI when closing)
    /// </summary>
    public void ShowMainMenuPublic()
    {
        ShowMainMenu();
    }
    
    /// <summary>
    /// Public method to show saved games panel (for returning from game setup panels)
    /// Called by BattleshipsSetupManager when user clicks back
    /// </summary>
    public void ShowGameModeSelectionPublic()
    {
        ShowSavedGamesPanel();
    }
    
    /// <summary>
    /// Hide all UIManager_Streamlined panels (called when game UI takes over)
    /// </summary>
    public void HideAllPanelsPublic()
    {
        HideAllPanels();
    }
    
    /// <summary>
    /// Set status message with info color (cyan)
    /// Called by BattleshipsSetupManager
    /// </summary>
    public void SetStatusInfoPublic(string message)
    {
        SetStatus(message, Color.cyan);
    }
    
    /// <summary>
    /// Set status message with success color (green)
    /// Called by BattleshipsSetupManager
    /// </summary>
    public void SetStatusSuccessPublic(string message)
    {
        SetStatus(message, Color.green);
    }
    
    /// <summary>
    /// Set status message with error color (red)
    /// Called by BattleshipsSetupManager
    /// </summary>
    public void SetStatusErrorPublic(string message)
    {
        SetStatus(message, Color.red);
    }
    #endregion

    #region Public API - Custom Games List
    /// <summary>
    /// Populates a scroll view content with custom games only.
    /// Called by RuleEditorUI to reuse the same list logic.
    /// </summary>
    /// <param name="contentTransform">The Content transform of the ScrollView to populate</param>
    /// <param name="itemPrefab">The prefab to use for each list item</param>
    /// <param name="onItemClicked">Callback when an item is clicked, receives SavedGameInfo</param>
    /// <returns>List of custom games that were loaded</returns>
    public List<SavedGameInfo> PopulateCustomGamesList(Transform contentTransform, GameObject itemPrefab, System.Action<SavedGameInfo> onItemClicked)
    {
        List<SavedGameInfo> customGames = new List<SavedGameInfo>();
        
        if (contentTransform == null || itemPrefab == null)
        {
            Debug.LogWarning("[UIManager_Streamlined] PopulateCustomGamesList: contentTransform or itemPrefab is null");
            return customGames;
        }
        
        // Clear existing items
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }
        
        // Load custom games from GameSaveManager
        if (GameSaveManager.Instance != null)
        {
            List<SavedGameInfo> allGames = GameSaveManager.Instance.LoadAllGames();
            
            // Filter to only custom games
            foreach (var game in allGames)
            {
                if (!game.isStandardGame)
                {
                    customGames.Add(game);
                }
            }
            
            Debug.Log($"[UIManager_Streamlined] PopulateCustomGamesList: Loaded {customGames.Count} custom games");
        }
        
        // Create list items
        foreach (var game in customGames)
        {
            GameObject listItem = Instantiate(itemPrefab, contentTransform);
            SetupSavedGameListItemWithCallback(listItem, game, onItemClicked);
        }
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform as RectTransform);
        
        return customGames;
    }
    
    /// <summary>
    /// Setup a saved game list item with a custom click callback
    /// </summary>
    private void SetupSavedGameListItemWithCallback(GameObject listItem, SavedGameInfo gameInfo, System.Action<SavedGameInfo> onItemClicked)
    {
        // Find TMP texts
        TextMeshProUGUI gameNameText = null;
        TextMeshProUGUI gameDetailsText = null;

        foreach (var t in listItem.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.gameObject.name == "GameNameText") gameNameText = t;
            else if (t.gameObject.name == "GameDetailsText") gameDetailsText = t;
        }

        if (gameNameText != null)
        {
            gameNameText.text = gameInfo.gameName;
            gameNameText.color = Color.white;
        }

        if (gameDetailsText != null)
        {
            string description = gameInfo.GetDescription();
            
            // Add yellow color to modified date for custom games
            if (!gameInfo.isStandardGame)
            {
                description = description.Replace(
                    $"Modified {gameInfo.lastModifiedDate:MMM dd}",
                    $"<color=#FFD700>Modified {gameInfo.lastModifiedDate:MMM dd}</color>"
                );
            }
            
            gameDetailsText.text = description;
        }

        // Setup button click handler
        Button button = listItem.GetComponent<Button>();
        if (button != null && onItemClicked != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onItemClicked(gameInfo));
        }
    }
    #endregion
}

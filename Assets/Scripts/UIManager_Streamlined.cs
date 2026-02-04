using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Streamlined UIManager focused on core menu flow:
/// Main Menu -> Create Custom Game (Rule Editor) OR Host Game (Saved Games List) OR Join Game
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
    }

    private void ShowMainMenu()
    {
        HideAllPanels();
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
        lobbyPanel?.SetActive(true);
        ClearStatus();

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        }

        Debug.Log("Showing Lobby Panel");
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

        if (gameInfo.rules == null)
        {
            SetStatus("Failed to load game rules!", Color.red);
            Debug.LogError($"[UIManager_Streamlined] gameInfo.rules is NULL for game: {gameInfo.gameName}");
            return;
        }

        SetStatus($"Creating lobby for {gameInfo.gameName}...", Color.yellow);

        try
        {
            // Store rules BEFORE creating lobby
            if (RuleEditorManager.Instance != null)
            {
                Debug.Log($"[UIManager_Streamlined] Setting rules for game: {gameInfo.gameName}");
                Debug.Log($"[UIManager_Streamlined] Rules preview: EnableCurrency={gameInfo.rules.enableCurrency}, StartingMoney={gameInfo.rules.startingMoney}");
                
                RuleEditorManager.Instance.SetRules(gameInfo.rules);
                
                // Verify rules were actually set
                GameRules storedRules = RuleEditorManager.Instance.GetCurrentRules();
                if (storedRules == null)
                {
                    Debug.LogError("[UIManager_Streamlined] ? Rules were NOT stored in RuleEditorManager! (returned null)");
                    SetStatus("Error: Failed to store game rules!", Color.red);
                    return;
                }
                else
                {
                    Debug.Log($"[UIManager_Streamlined] ? Rules verified: EnableCurrency={storedRules.enableCurrency}, StartingMoney={storedRules.startingMoney}");
                }
            }
            else
            {
                Debug.LogError("[UIManager_Streamlined] ? RuleEditorManager.Instance is NULL!");
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

            // Check if rules are configured
            GameRules currentRules = RuleEditorManager.Instance.GetCurrentRules();
            if (currentRules == null)
            {
                Debug.LogError("[UIManager_Streamlined] ? GetCurrentRules() returned NULL!");
                Debug.LogError("[UIManager_Streamlined] This means no rules were set when hosting the game.");
                Debug.LogError("[UIManager_Streamlined] Please check the OnHostSavedGameClicked logs above for issues.");
                SetStatus("Error: No game rules configured!", Color.red);
                return;
            }

            Debug.Log($"[UIManager_Streamlined] ? Rules found: {currentRules.GetRulesSummary()}");

            // Check if CustomGameSpawner exists
            if (CustomGameSpawner.Instance == null)
            {
                Debug.LogError("[UIManager_Streamlined] ? CustomGameSpawner.Instance is NULL!");
                SetStatus("Error: CustomGameSpawner not found!", Color.red);
                return;
            }

            // Start custom game
            Debug.Log("[UIManager_Streamlined] Starting custom game with rule-based spawner");
            int playerCount = LobbyManager.Instance.GetPlayersInfo().Count;
            bool success = await CustomGameSpawner.Instance.SpawnGameFromCurrentRules(playerCount);

            if (success)
            {
                SetStatus("Custom game started!", Color.green);
                // CustomGameSpawner will handle showing the appropriate game panel
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

        // Add Standard Games Section
        if (standardGames.Count > 0)
        {
            Debug.Log("[UIManager_Streamlined] Adding 'Standard Games' section...");
            AddSectionHeader("Standard Games");
            
            foreach (var game in standardGames)
            {
                Debug.Log($"[UIManager_Streamlined] Creating list item for: {game.gameName}");
                GameObject listItem = Instantiate(savedGameItemPrefab, savedGamesListContent);
                SetupSavedGameListItem(listItem, game);
            }
            
            Debug.Log("[UIManager_Streamlined] ? Standard games section complete");
        }

        // Add Custom Games Section
        if (customGames.Count > 0)
        {
            Debug.Log("[UIManager_Streamlined] Adding 'Custom Games' section...");
            AddSectionHeader("Custom Games");
            
            foreach (var game in customGames)
            {
                Debug.Log($"[UIManager_Streamlined] Creating list item for: {game.gameName}");
                GameObject listItem = Instantiate(savedGameItemPrefab, savedGamesListContent);
                SetupSavedGameListItem(listItem, game);
            }
            
            Debug.Log("[UIManager_Streamlined] ? Custom games section complete");
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

        // Find the host button anywhere under this prefab even if it is nested
        Button hostButton = null;
        foreach (var b in listItem.GetComponentsInChildren<Button>(true))
        {
            if (b.gameObject.name == "HostButton")
            {
                hostButton = b;
                break;
            }
        }

        if (gameNameText != null) gameNameText.text = gameInfo.gameName;
        else Debug.LogWarning("[UIManager_Streamlined] GameNameText not found in prefab instance.");

        if (gameDetailsText != null) gameDetailsText.text = gameInfo.GetDescription();
        else Debug.LogWarning("[UIManager_Streamlined] GameDetailsText not found in prefab instance.");

        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners(); // prevents duplicate listeners if rebuilt
            hostButton.onClick.AddListener(() => OnHostSavedGameClicked(gameInfo));
        }
        else Debug.LogWarning("[UIManager_Streamlined] HostButton not found in prefab instance.");
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
        Debug.Log("[UIManager_Streamlined] Rules configured, saving game...");

        // Analyze rules to determine game type
        string gameType = "Custom Game";
        string gameName = "Custom Game";
        int recommendedPlayerCount = 4;

        if (CustomGameAnalyzer.Instance != null)
        {
            var detectedType = CustomGameAnalyzer.Instance.AnalyzeGameRules(rules);
            recommendedPlayerCount = CustomGameAnalyzer.Instance.GetRecommendedPlayerCount(detectedType, rules);
            gameType = detectedType.ToString();
            gameName = $"{gameType} ({recommendedPlayerCount}P)";
        }

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
    #endregion
}

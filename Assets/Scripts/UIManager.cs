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
    [SerializeField] private GameObject gameSetupPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject gamePanel; // New panel for the actual game

    [Header("Main Menu")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button exitButton;

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

    [Header("Game Panel")] // New section for game UI
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private TextMeshProUGUI diceResultText;
    [SerializeField] private Button leaveGameButton;

    private Coroutine statusClearCoroutine;
    private float lastPlayerListUpdate = 0f;
    private const float PLAYER_LIST_UPDATE_INTERVAL = 2f;

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
        NetworkGameManager.OnGameStarted += OnGameStarted;
        NetworkGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
        NetworkGameManager.OnPlayerMoved += OnPlayerMoved;
        NetworkGameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        Debug.Log("Unsubscribing from network events...");
        NetworkGameManager.OnGameStarted -= OnGameStarted;
        NetworkGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
        NetworkGameManager.OnPlayerMoved -= OnPlayerMoved;
        NetworkGameManager.OnGameStateChanged -= OnGameStateChanged;
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
        UpdateRollDiceButton();
    }

    private void OnPlayerMoved(int playerId, int newPosition)
    {
        if (diceResultText != null)
        {
            diceResultText.text = $"Player {playerId + 1} moved to position {newPosition}";
        }
    }

    private void OnGameStateChanged(NetworkGameManager.GameState newState)
    {
        if (gameStatusText != null)
        {
            switch (newState)
            {
                case NetworkGameManager.GameState.WaitingToStart:
                    gameStatusText.text = "Waiting to start...";
                    break;
                case NetworkGameManager.GameState.InProgress:
                    gameStatusText.text = "Game in progress";
                    break;
                case NetworkGameManager.GameState.GameOver:
                    gameStatusText.text = "Game Over!";
                    if (rollDiceButton != null) rollDiceButton.interactable = false;
                    break;
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
        if (gameSetupPanel == null) Debug.LogError("GameSetupPanel is not assigned in UIManager!");
        if (lobbyPanel == null) Debug.LogError("LobbyPanel is not assigned in UIManager!");
        if (joinPanel == null) Debug.LogError("JoinPanel is not assigned in UIManager!");
        if (gamePanel == null) Debug.LogError("GamePanel is not assigned in UIManager!");
        if (hostButton == null) Debug.LogError("HostButton is not assigned in UIManager!");
        if (joinButton == null) Debug.LogError("JoinButton is not assigned in UIManager!");
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
        
        // Game panel validation
        if (currentPlayerText == null) Debug.LogError("CurrentPlayerText is not assigned in UIManager!");
        if (gameStatusText == null) Debug.LogError("GameStatusText is not assigned in UIManager!");
        if (rollDiceButton == null) Debug.LogError("RollDiceButton is not assigned in UIManager!");
        if (diceResultText == null) Debug.LogError("DiceResultText is not assigned in UIManager!");
        if (leaveGameButton == null) Debug.LogError("LeaveGameButton is not assigned in UIManager!");
    }

    private void SetupButtonListeners()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
        if (createLobbyButton != null) createLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
        if (backFromSetupButton != null) backFromSetupButton.onClick.AddListener(OnBackFromSetupClicked);
        if (submitJoinButton != null) submitJoinButton.onClick.AddListener(OnSubmitJoinClicked);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveLobbyButton != null) leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        
        // Game panel button listeners
        if (rollDiceButton != null) rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        if (leaveGameButton != null) leaveGameButton.onClick.AddListener(OnLeaveGameClicked);
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameSetupPanel != null) gameSetupPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);
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

    private void ShowGameSetup()
    {
        HideAllPanels();
        if (gameSetupPanel != null) 
        {
            gameSetupPanel.SetActive(true);
            Debug.Log("Showing Game Setup Panel");
            
            // player and tile space prompts
            if (tileCountInput != null) tileCountInput.text = "20";
            if (playerCountInput != null) playerCountInput.text = "2";
        }
        else
        {
            Debug.LogError("GameSetupPanel is NULL! Please assign it in the Inspector.");
        }
        ClearStatus();
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
        if (gamePanel != null) 
        {
            gamePanel.SetActive(true);
            Debug.Log("Showing Game Panel");
            
            // Initialize game UI
            UpdateCurrentPlayerDisplay(0);
            UpdateRollDiceButton();
            
            if (gameStatusText != null) gameStatusText.text = "Game in progress";
            if (diceResultText != null) diceResultText.text = "Game started! Roll the dice when it's your turn.";
        }
        ClearStatus();
    }

    #region Game Panel Methods

    private void UpdateCurrentPlayerDisplay(int currentPlayerId)
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"Current Turn: Player {currentPlayerId + 1}";
            
            // Highlight if it's the local player's turn
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsMyTurn())
            {
                currentPlayerText.color = Color.green;
                currentPlayerText.text += " (Your Turn!)";
            }
            else
            {
                currentPlayerText.color = Color.white;
            }
        }
    }

    private void UpdateRollDiceButton()
    {
        if (rollDiceButton != null && NetworkGameManager.Instance != null)
        {
            bool isMyTurn = NetworkGameManager.Instance.IsMyTurn();
            bool gameInProgress = NetworkGameManager.Instance.GetGameState() == NetworkGameManager.GameState.InProgress;
            
            rollDiceButton.interactable = isMyTurn && gameInProgress;
            
            if (isMyTurn && gameInProgress)
            {
                rollDiceButton.GetComponentInChildren<TextMeshProUGUI>().text = "Roll Dice!";
            }
            else if (!gameInProgress)
            {
                rollDiceButton.GetComponentInChildren<TextMeshProUGUI>().text = "Game Over";
            }
            else
            {
                rollDiceButton.GetComponentInChildren<TextMeshProUGUI>().text = "Wait Your Turn";
            }
        }
    }

    private void OnRollDiceClicked()
    {
        Debug.Log("Roll dice button clicked");
        
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RollDice();
        }
        else
        {
            Debug.LogError("NetworkGameManager instance not found!");
        }
    }

    private async void OnLeaveGameClicked()
    {
        Debug.Log("Leave game button clicked");
        
        // Return to main menu and disconnect
        await LobbyManager.Instance.LeaveLobby();
        ShowMainMenu();
    }

    #endregion

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
        Debug.Log("Host button clicked - showing game setup");
        ShowGameSetup();
        SetStatusInfo("Configure your game settings");
    }

    private async void OnCreateLobbyButtonClicked()
    {
        Debug.Log("Create lobby button clicked");
        
        if (int.TryParse(tileCountInput.text, out int tileCount))
        {
            configuredTileCount = Mathf.Clamp(tileCount, 10, 100);
        }
        else
        {
            configuredTileCount = 20;
        }

        if (int.TryParse(playerCountInput.text, out int playerCount))
        {
            configuredPlayerCount = Mathf.Clamp(playerCount, 2, 4);
        }
        else
        {
            configuredPlayerCount = 2;
        }

        if (createLobbyButton != null) createLobbyButton.interactable = false;
        
        SetStatusInfo($"Creating lobby for {configuredPlayerCount} players with {configuredTileCount} tiles...");
        
        try
        {
            string lobbyCode = await LobbyManager.Instance.CreateLobby($"Game - {configuredTileCount} tiles");
            if (!string.IsNullOrEmpty(lobbyCode))
            {
                GameSetupManager.Instance.ConfigureGame(configuredTileCount, configuredPlayerCount);
                
                if (lobbyCodeText != null)
                {
                    lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
                }
                SetStatusSuccess($"Lobby created! Code: {lobbyCode}");
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
            Debug.LogError($"Exception in OnCreateLobbyButtonClicked: {e}");
        }
        finally
        {
            if (createLobbyButton != null) createLobbyButton.interactable = true;
        }
    }

    private void OnBackFromSetupClicked()
    {
        Debug.Log("Back from setup button clicked");
        ShowMainMenu();
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

    // Exiting the game
    private void OnExitButtonClicked()
    {
        Debug.Log("Exit button clicked - quitting application");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stops play mode if in editor
        #else
            Application.Quit(); // Quits the built game
        #endif
    }


    // Updated: Start game button now initializes network game and spawns NetworkGameManager
    private void OnStartGameClicked()
    {
        Debug.Log("Start game button clicked");
        
        if (!LobbyManager.Instance.IsLobbyHost())
        {
            SetStatusError("Only the host can start the game!");
            return;
        }

        if (startGameButton != null) startGameButton.interactable = false;
        
        SetStatusInfo("Starting the game...");
        
        try
        {
            // Generate the board using the stored configuration
            GameSetupManager.Instance.GenerateBoard();
            
            // Spawn NetworkGameManager if not already spawned
            SpawnNetworkGameManager();
            
            // Initialize the multiplayer game with actual lobby player count
            if (NetworkGameManager.Instance != null)
            {
                // Get the actual number of players in the lobby (not configured count)
                int actualPlayerCount = LobbyManager.Instance.GetPlayersInfo().Count;
                Debug.Log($"Starting game with {actualPlayerCount} actual players in lobby");
                
                NetworkGameManager.Instance.InitializeGame(actualPlayerCount);
            }
            
            SetStatusSuccess("Game started!");
            Debug.Log("Board generated and network game initialized!");
        }
        catch (System.Exception e)
        {
            SetStatusError($"Error starting game: {e.Message}");
            Debug.LogError($"Exception in OnStartGameClicked: {e}");
        }
        finally
        {
            if (startGameButton != null) startGameButton.interactable = true;
        }
    }

    private void SpawnNetworkGameManager()
    {
        // Check if NetworkGameManager already exists
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsSpawned)
        {
            Debug.Log("NetworkGameManager already spawned");
            return;
        }

        // Only host should spawn the NetworkGameManager
        if (NetworkManager.Instance.IsHost())
        {
            Debug.Log("Spawning NetworkGameManager...");
            
            // Get Unity's NetworkManager component through our custom wrapper
            var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
            if (unityNetworkManager == null)
            {
                Debug.LogError("Unity's NetworkManager component not found! Please add Unity's NetworkManager component to the Managers GameObject.");
                return;
            }

            // Find the NetworkGameManager prefab in the Network Prefabs list
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
                // Instantiate and spawn the prefab from Network Prefabs list
                GameObject instance = Instantiate(networkGameManagerPrefab);
                Unity.Netcode.NetworkObject networkObject = instance.GetComponent<Unity.Netcode.NetworkObject>();
                networkObject.Spawn();
                Debug.Log("NetworkGameManager spawned from Network Prefabs list");
            }
            else
            {
                Debug.LogError("NetworkGameManager prefab not found in Network Prefabs list! Please add it to Unity's NetworkManager ? Network Prefabs.");
                
                // Fallback: Create dynamically (not recommended for production)
                GameObject networkGameManagerGO = new GameObject("NetworkGameManager");
                NetworkGameManager networkGameManager = networkGameManagerGO.AddComponent<NetworkGameManager>();
                Unity.Netcode.NetworkObject networkObject = networkGameManagerGO.AddComponent<Unity.Netcode.NetworkObject>();
                
                networkObject.Spawn();
                Debug.LogWarning("NetworkGameManager spawned dynamically as fallback");
            }
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
            Debug.LogWarning("PlayerListContent or PlayerListItemPrefab is null, cannot update player list");
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

        Debug.Log($"Updating player list with {players.Count} players");

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
                
                Debug.Log($"Created player item: {playerText.text}");
            }
            else
            {
                Debug.LogError("PlayerListItemPrefab must have a TextMeshProUGUI component!");
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
        
        // Update game panel UI when game is active
        if (gamePanel != null && gamePanel.activeSelf && NetworkGameManager.Instance != null)
        {
            UpdateRollDiceButton();
        }
        
        // Check if NetworkGameManager was spawned and we need to resubscribe
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsSpawned)
        {
            // Make sure we're subscribed to events (handles late spawning)
            EnsureNetworkEventSubscription();
        }
    }
    
    private bool isSubscribedToNetworkEvents = false;
    
    private void EnsureNetworkEventSubscription()
    {
        if (!isSubscribedToNetworkEvents && NetworkGameManager.Instance != null)
        {
            Debug.Log("Late subscribing to NetworkGameManager events...");
            SubscribeToNetworkEvents();
            isSubscribedToNetworkEvents = true;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI manager for Hybrid/Custom games spawned by HybridGameManager.
/// Reads GameRules at game start and shows only the panels relevant to the
/// active rule set — no manual configuration required.
/// Mirrors the DiceRaceUIManager architecture for consistency.
/// </summary>
public class HybridUIManager : MonoBehaviour
{
    private static HybridUIManager instance;
    public static HybridUIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<HybridUIManager>();
            return instance;
        }
    }

    #region UI References — Always Visible

    [Header("Core Game Panel")]
    [SerializeField] private GameObject hybridGamePanel;

    [Header("Always-On Elements")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private TextMeshProUGUI diceResultText;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Button leaveGameButton;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button returnToMenuButton;

    #endregion

    #region UI References — Conditional (shown based on active GameRules)

    [Header("Stats Panel — single VerticalLayoutGroup containing all stat rows")]
    [Tooltip("Parent panel with a VerticalLayoutGroup. Child rows are destroyed at startup if their rule is disabled.")]
    [SerializeField] private GameObject statsPanel;

    [Header("Currency Row (child of StatsPanel)")]
    [SerializeField] private GameObject currencyRow;
    [SerializeField] private TextMeshProUGUI playerMoneyText;

    [Header("Property Row (child of StatsPanel) — scrollable owned-properties list")]
    [SerializeField] private GameObject propertyRow;
    [SerializeField] private Transform propertyListContainer;   // VerticalLayoutGroup inside the scroll view
    [SerializeField] private GameObject propertyListItemPrefab; // one TextMeshProUGUI + Button per property

    [Header("Property Detail Popup (shown on property click)")]
    [SerializeField] private GameObject propertyDetailPopup;
    [SerializeField] private TextMeshProUGUI propertyDetailNameText;
    [SerializeField] private TextMeshProUGUI propertyDetailPriceText;
    [SerializeField] private TextMeshProUGUI propertyDetailRentText;
    [SerializeField] private TextMeshProUGUI propertyDetailDescText;
    [SerializeField] private Button propertyDetailCloseButton;

    [Header("Resource Row (child of StatsPanel)")]
    [SerializeField] private GameObject resourceRow;
    [SerializeField] private Transform resourceTrackerContainer;
    [SerializeField] private GameObject resourceTrackerPrefab;

    [Header("Purchase Button (in main panel — only created if canPurchaseProperties = true)")]
    [SerializeField] private Button purchasePropertyButton;

    [Header("Game Messages")]
    [SerializeField] private TextMeshProUGUI gameMessagesText;

    [Header("Board (scene-level, not a UI panel)")]
    [SerializeField] private Transform hybridBoardParent;

    private bool isSubscribedToEvents = false;
    private bool isGameStarted = false;
    private GameRules activeRules;
    private readonly List<GameObject> spawnedResourceTrackers = new List<GameObject>();

    // Player tokens — one per player, parented under hybridBoardParent
    private readonly List<GameObject> playerTokens = new List<GameObject>();
    private GameObject generatedBoardContainer = null;
    private SerializableBoardData cachedBoardData = null;

    private static readonly Color[] PlayerColours =
    {
        new Color(0.20f, 0.60f, 1.00f), // Blue
        new Color(1.00f, 0.25f, 0.25f), // Red
        new Color(0.20f, 0.85f, 0.35f), // Green
        new Color(1.00f, 0.85f, 0.10f), // Yellow
        new Color(0.80f, 0.20f, 0.90f), // Purple
        new Color(1.00f, 0.55f, 0.10f), // Orange
        new Color(0.10f, 0.85f, 0.85f), // Cyan
        new Color(1.00f, 0.45f, 0.75f), // Pink
    };

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

        SetupButtonListeners();
    }

    private void Start()
    {
        HideAllPanels();
        // Clients receive StartGameClientRpc which fires OnGameStarted.
        // Subscribing here ensures the client's UI transitions out of the lobby automatically.
        HybridGameManager.OnGameStarted += OnGameStartedHandler;
        Debug.Log("[HybridUIManager] Initialized");
    }

    private void OnGameStartedHandler()
    {
        // Only act if the game panel isn't already showing (avoids double-init on host)
        if (hybridGamePanel != null && !hybridGamePanel.activeSelf)
        {
            Debug.Log("[HybridUIManager] OnGameStarted received — starting UI (client path)");
            // Hide lobby and other navigation panels that the client still has open
            UIManager_Streamlined.Instance?.HideAllPanelsPublic();
            StartGame();
        }
    }

    private void OnDestroy()
    {
        HybridGameManager.OnGameStarted -= OnGameStartedHandler;
        UnsubscribeFromEvents();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by UIManager_Streamlined after CustomGameSpawner successfully spawns a Hybrid game.
    /// Reads the active rules and configures which UI panels are shown.
    /// </summary>
    public async void StartGame()
    {
        if (isGameStarted)
        {
            Debug.Log("[HybridUIManager] StartGame already initialised, skipping duplicate call");
            return;
        }
        isGameStarted = true;
        Debug.Log("[HybridUIManager] StartGame called");

        // Hide lobby/menu panels for all clients
        UIManager_Streamlined.Instance?.HideAllPanelsPublic();

        activeRules = RuleEditorManager.Instance?.GetCurrentRules()
                   ?? HybridGameManager.Instance?.GetActiveRules();

        if (activeRules == null)
        {
            Debug.LogError("[HybridUIManager] No active rules found — cannot configure UI!");
            return;
        }

        ConfigurePanelsFromRules(activeRules);
        SubscribeToEvents();

        hybridGamePanel?.SetActive(true);
        gameOverPanel?.SetActive(false);

        if (hybridBoardParent != null)
        {
            hybridBoardParent.gameObject.SetActive(true);
            LoadAndGenerateBoard();
        }
        else
        {
            Debug.LogWarning("[HybridUIManager] hybridBoardParent is not assigned — assign the scene-level board parent in the Inspector.");
        }

        UpdateCurrentPlayerDisplay(HybridGameManager.Instance != null ? HybridGameManager.Instance.GetCurrentPlayerId() : 0);

        if (gameStatusText != null)
            gameStatusText.text = "Game in progress — roll the dice!";

        // CRITICAL: Wait a frame so NetworkVariables finish syncing before updating button states
        await System.Threading.Tasks.Task.Delay(100);
        UpdateRollDiceButton();

        Debug.Log("[HybridUIManager] Game UI configured and visible");
    }

    /// <summary>
    /// Hide all panels and unsubscribe from events. Called when leaving a hybrid game.
    /// </summary>
    public void HideAndCleanup()
    {
        isGameStarted = false;
        UnsubscribeFromEvents();
        HideAllPanels();
        ClearResourceTrackers();

        // Destroy any generated board tiles, tokens, and deactivate the parent
        if (hybridBoardParent != null)
        {
            foreach (Transform child in hybridBoardParent)
                Destroy(child.gameObject);
            hybridBoardParent.gameObject.SetActive(false);
        }
        DestroyPlayerTokens();
        generatedBoardContainer = null;
        cachedBoardData = null;

        Debug.Log("[HybridUIManager] Cleaned up");
    }

    /// <summary>
    /// Loads the board JSON for the current game and generates UI tiles under hybridBoardParent.
    /// The game name stored in RuleEditorManager's current SavedGameInfo is used as the file name.
    /// </summary>
    private void LoadAndGenerateBoard()
    {
        if (hybridBoardParent == null) return;

        // Clear any previously generated tiles
        foreach (Transform child in hybridBoardParent)
            Destroy(child.gameObject);

        // Resolve the board file name from the currently loaded game
        SavedGameInfo gameInfo = RuleEditorManager.Instance?.GetCurrentGameInfo();
        string boardName = gameInfo?.gameName;

        if (string.IsNullOrEmpty(boardName))
        {
            Debug.LogWarning("[HybridUIManager] No game name available — cannot load board JSON.");
            return;
        }

        SerializableBoardData boardData = BoardJSONUtility.LoadBoardFromJSON(boardName);
        if (boardData == null)
        {
            Debug.LogWarning($"[HybridUIManager] Board JSON not found for '{boardName}'. Board will not be displayed.");
            return;
        }

        cachedBoardData = boardData;

        if (BoardUIGenerator.Instance == null)
        {
            Debug.LogError("[HybridUIManager] BoardUIGenerator instance not found — cannot generate board.");
            return;
        }

        GameObject generated = BoardUIGenerator.Instance.GenerateBoard(boardData, hybridBoardParent);
        if (generated != null)
        {
            generatedBoardContainer = generated;
            Debug.Log($"[HybridUIManager] Board '{boardName}' generated successfully ({boardData.tiles.Count} tiles).");
            SpawnPlayerTokens(boardData);
        }
        else
            Debug.LogError($"[HybridUIManager] BoardUIGenerator.GenerateBoard returned null for '{boardName}'.");
    }

    /// <summary>
    /// Spawns one circular token per player at tile 0 and stores them for later movement.
    /// </summary>
    private void SpawnPlayerTokens(SerializableBoardData boardData)
    {
        DestroyPlayerTokens();

        if (generatedBoardContainer == null || boardData.tiles.Count == 0) return;

        int numPlayers = HybridGameManager.Instance != null
            ? HybridGameManager.Instance.GetPlayerCount()
            : 2;

        Vector2 startPos = boardData.tiles.Count > 0
            ? new Vector2(boardData.tiles[0].positionX, boardData.tiles[0].positionY)
            : Vector2.zero;

        float tokenSize = 20f;
        float tokenSpread = 12f;

        for (int i = 0; i < numPlayers; i++)
        {
            GameObject token = new GameObject($"PlayerToken_{i}");
            token.transform.SetParent(generatedBoardContainer.transform, false);

            RectTransform rt = token.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tokenSize, tokenSize);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            // Offset tokens so they don't stack on the same tile
            float angle  = (360f / numPlayers) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tokenSpread;
            rt.anchoredPosition = startPos + offset;

            // Circular background
            Image img   = token.AddComponent<Image>();
            img.color   = i < PlayerColours.Length ? PlayerColours[i] : Color.white;
            img.raycastTarget = false;

            // Player number label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(token.transform, false);
            RectTransform labelRt  = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin      = Vector2.zero;
            labelRt.anchorMax      = Vector2.one;
            labelRt.sizeDelta      = Vector2.zero;
            labelRt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI label  = labelObj.AddComponent<TextMeshProUGUI>();
            label.text             = (i + 1).ToString();
            label.fontSize         = tokenSize * 0.55f;
            label.color            = Color.black;
            label.alignment        = TextAlignmentOptions.Center;
            label.raycastTarget    = false;

            // Render on top of tiles
            Canvas tokenCanvas         = token.AddComponent<Canvas>();
            tokenCanvas.overrideSorting = true;
            tokenCanvas.sortingOrder    = 10;

            playerTokens.Add(token);
            Debug.Log($"[HybridUIManager] Spawned token for player {i} at tile 0");
        }
    }

    /// <summary>
    /// Moves a player's token to the tile at the given board position index.
    /// </summary>
    private void MoveTokenToTile(int playerId, int tileIndex)
    {
        if (playerId < 0 || playerId >= playerTokens.Count) return;
        if (generatedBoardContainer == null || cachedBoardData == null) return;
        if (tileIndex < 0 || tileIndex >= cachedBoardData.tiles.Count) return;

        SerializableTileData tile = cachedBoardData.tiles[tileIndex];
        int numPlayers = playerTokens.Count;
        float tokenSpread = 12f;
        float angle  = (360f / Mathf.Max(numPlayers, 1)) * playerId * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tokenSpread;

        RectTransform rt = playerTokens[playerId].GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(tile.positionX, tile.positionY) + offset;
    }

    private void DestroyPlayerTokens()
    {
        foreach (var token in playerTokens)
            if (token != null) Destroy(token);
        playerTokens.Clear();
    }

    /// <summary>
    /// Configure the single StatsPanel by destroying rows that are not needed for this rule set.
    /// Rows that ARE needed remain and the VerticalLayoutGroup reflows automatically.
    /// Combat keeps its own separate panel so it can be positioned independently.
    /// </summary>
    private void ConfigurePanelsFromRules(GameRules rules)
    {
        Debug.Log("[HybridUIManager] Configuring stats panel from rules...");

        // --- Currency row ---
        bool showCurrency = rules.enableCurrency;
        if (!showCurrency && currencyRow != null)
        {
            Destroy(currencyRow);
            currencyRow = null;
            playerMoneyText = null;
            Debug.Log("[HybridUIManager]   Currency row: DESTROYED");
        }
        else
        {
            Debug.Log($"[HybridUIManager]   Currency row: {(showCurrency ? "KEPT" : "no row assigned")}");
        }

        // --- Property row — requires currency ---
        bool showProperty = rules.canPurchaseProperties && rules.enableCurrency;
        if (!showProperty && propertyRow != null)
        {
            // Null out the ScrollRect's Content reference before destroying so it does not
            // fire canvas callbacks against a missing RectTransform during the destroy frame.
            ScrollRect scrollRect = propertyRow.GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
                scrollRect.content = null;

            Destroy(propertyRow);
            propertyRow = null;
            propertyListContainer = null;
            Debug.Log("[HybridUIManager]   Property row: DESTROYED");
        }
        else if (showProperty)
        {
            RefreshPropertyList();
            Debug.Log("[HybridUIManager]   Property row: KEPT");
        }

        // Purchase button — shown only when canPurchaseProperties is enabled
        if (!showProperty && purchasePropertyButton != null)
        {
            Destroy(purchasePropertyButton.gameObject);
            purchasePropertyButton = null;
            Debug.Log("[HybridUIManager]   Purchase button: DESTROYED");
        }

        // Detail popup starts hidden
        propertyDetailPopup?.SetActive(false);

        // --- Resource row ---
        bool showResources = rules.enableResources && rules.numberOfResources > 0;
        if (!showResources && resourceRow != null)
        {
            Destroy(resourceRow);
            resourceRow = null;
            resourceTrackerContainer = null;
            Debug.Log("[HybridUIManager]   Resource row: DESTROYED");
        }
        else if (showResources)
        {
            SpawnResourceTrackers(rules);
            Debug.Log($"[HybridUIManager]   Resource row: KEPT ({rules.numberOfResources} resources)");
        }

        // --- Hide entire StatsPanel if every row was destroyed ---
        if (statsPanel != null)
        {
            bool anyRowActive = (currencyRow != null) || (propertyRow != null) || (resourceRow != null);
            statsPanel.SetActive(anyRowActive);
            Debug.Log($"[HybridUIManager]   StatsPanel: {(anyRowActive ? "SHOWN" : "HIDDEN (all rows destroyed)")} ");
        }
    }

    /// <summary>
    /// Dynamically creates one tracker label per resource type defined in the rules.
    /// </summary>
    private void SpawnResourceTrackers(GameRules rules)
    {
        ClearResourceTrackers();

        if (resourceTrackerContainer == null || resourceTrackerPrefab == null)
        {
            Debug.LogWarning("[HybridUIManager] Resource tracker container or prefab not assigned — skipping tracker spawn");
            return;
        }

        for (int i = 0; i < rules.numberOfResources; i++)
        {
            string resourceName = (rules.resourceNames != null && i < rules.resourceNames.Length && !string.IsNullOrEmpty(rules.resourceNames[i]))
                ? rules.resourceNames[i]
                : $"Resource {i + 1}";

            GameObject tracker = Instantiate(resourceTrackerPrefab, resourceTrackerContainer);
            TextMeshProUGUI label = tracker.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{resourceName}: 0";

            spawnedResourceTrackers.Add(tracker);
        }

        Debug.Log($"[HybridUIManager] Spawned {rules.numberOfResources} resource trackers");
    }

    private void ClearResourceTrackers()
    {
        foreach (var tracker in spawnedResourceTrackers)
        {
            if (tracker != null)
                Destroy(tracker);
        }
        spawnedResourceTrackers.Clear();
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeToEvents()
    {
        if (isSubscribedToEvents) return;

        HybridGameManager.OnGameStarted += OnGameStarted;
        HybridGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
        HybridGameManager.OnPlayerMoved += OnPlayerMoved;
        HybridGameManager.OnGameMessage += OnGameMessage;
        HybridGameManager.OnGameStateChanged += OnGameStateChanged;

        isSubscribedToEvents = true;
        Debug.Log("[HybridUIManager] Subscribed to HybridGameManager events");
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribedToEvents) return;

        HybridGameManager.OnGameStarted -= OnGameStarted;
        HybridGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
        HybridGameManager.OnPlayerMoved -= OnPlayerMoved;
        HybridGameManager.OnGameMessage -= OnGameMessage;
        HybridGameManager.OnGameStateChanged -= OnGameStateChanged;

        isSubscribedToEvents = false;
        Debug.Log("[HybridUIManager] Unsubscribed from HybridGameManager events");
    }

    #endregion

    #region Event Handlers

    private void OnGameStarted()
    {
        if (gameStatusText != null)
            gameStatusText.text = "Game started!";
        UpdateRollDiceButton();
    }

    private void OnPlayerTurnChanged(int currentPlayerId)
    {
        UpdateCurrentPlayerDisplay(currentPlayerId);
        UpdateRollDiceButton();
        UpdateMoneyDisplay();
        RefreshPropertyList();
    }

    private void OnPlayerMoved(int playerId, int newPosition)
    {
        if (diceResultText != null)
            diceResultText.text = $"Player {playerId + 1} moved to position {newPosition}";
        MoveTokenToTile(playerId, newPosition);
    }

    private void OnGameMessage(string message)
    {
        if (gameMessagesText != null)
            gameMessagesText.text = message;
    }

    private void OnGameStateChanged(HybridGameManager.GameState newState)
    {
        switch (newState)
        {
            case HybridGameManager.GameState.WaitingToStart:
                if (gameStatusText != null) gameStatusText.text = "Waiting to start...";
                break;
            case HybridGameManager.GameState.InProgress:
                if (gameStatusText != null) gameStatusText.text = "Game in progress";
                UpdateRollDiceButton();
                break;
            case HybridGameManager.GameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    #endregion

    #region UI Updates

    private void UpdateCurrentPlayerDisplay(int currentPlayerId)
    {
        if (currentPlayerText == null) return;

        bool isMyTurn = HybridGameManager.Instance != null && HybridGameManager.Instance.IsMyTurn();
        currentPlayerText.text = isMyTurn
            ? $"Current Turn: Player {currentPlayerId + 1} (Your Turn!)"
            : $"Current Turn: Player {currentPlayerId + 1}";
        currentPlayerText.color = isMyTurn ? Color.green : Color.white;
    }

    private void UpdateRollDiceButton()
    {
        if (rollDiceButton == null || HybridGameManager.Instance == null) return;

        bool isMyTurn = HybridGameManager.Instance.IsMyTurn();
        bool gameInProgress = HybridGameManager.Instance.GetGameState() == HybridGameManager.GameState.InProgress;
        rollDiceButton.interactable = isMyTurn && gameInProgress;

        TextMeshProUGUI buttonText = rollDiceButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isMyTurn && gameInProgress)
            {
                buttonText.text = "Roll Dice!";
                buttonText.color = Color.white;
            }
            else if (HybridGameManager.Instance.GetGameState() == HybridGameManager.GameState.GameOver)
            {
                buttonText.text = "Game Over";
                buttonText.color = Color.red;
            }
            else if (!gameInProgress)
            {
                // WaitingToStart or other non-progress state
                buttonText.text = "Waiting...";
                buttonText.color = Color.gray;
            }
            else
            {
                buttonText.text = "Wait Your Turn";
                buttonText.color = Color.gray;
            }
        }
    }

    /// <summary>
    /// Rebuilds the owned-properties list for the local player.
    /// Each entry is a button; clicking it opens the detail popup with data
    /// sourced from HybridPropertyModule (which is populated from SerializableTileData
    /// by the board editor via BoardUIGenerator).
    /// </summary>
    private void RefreshPropertyList()
    {
        if (propertyListContainer == null || propertyListItemPrefab == null) return;

        // Clear old entries
        foreach (Transform child in propertyListContainer)
            Destroy(child.gameObject);

        var propertyModule = HybridGameManager.Instance?.GetPropertyModule();
        if (propertyModule == null) return;

        int localPlayerId = HybridGameManager.Instance.GetLocalPlayerId();
        List<PropertyData> owned = propertyModule.GetPlayerProperties(localPlayerId);

        foreach (PropertyData prop in owned)
        {
            GameObject item = Instantiate(propertyListItemPrefab, propertyListContainer);

            // Set label text
            TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = prop.propertyName;

            // Capture for lambda closure
            PropertyData captured = prop;
            Button btn = item.GetComponent<Button>();
            btn?.onClick.AddListener(() => ShowPropertyDetail(captured));
        }

        Canvas.ForceUpdateCanvases();
        if (propertyListContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    /// <summary>
    /// Opens the property detail popup and fills it with data from PropertyData.
    /// PropertyData is populated from SerializableTileData (board editor) by
    /// HybridPropertyModule.LoadPropertiesFromBoardData().
    /// </summary>
    private void ShowPropertyDetail(PropertyData prop)
    {
        if (propertyDetailPopup == null) return;

        if (propertyDetailNameText != null)
            propertyDetailNameText.text = prop.propertyName;

        if (propertyDetailPriceText != null)
            propertyDetailPriceText.text = $"Price: ${prop.purchasePrice}";

        if (propertyDetailRentText != null)
            propertyDetailRentText.text = $"Rent: ${prop.rentPrice}";

        // Description comes from SerializableTileData.tileDescription via PropertyData
        if (propertyDetailDescText != null)
            propertyDetailDescText.text = !string.IsNullOrEmpty(prop.description)
                ? prop.description
                : "No description available.";

        propertyDetailPopup.SetActive(true);
        Debug.Log($"[HybridUIManager] Showing detail for: {prop.propertyName}");
    }

    private void UpdateMoneyDisplay()
    {
        if (playerMoneyText == null || HybridGameManager.Instance == null) return;
        if (activeRules == null || !activeRules.enableCurrency) return;

        int localPlayerId = HybridGameManager.Instance.GetLocalPlayerId();
        var playerData = HybridGameManager.Instance.GetPlayer(localPlayerId);
        if (playerData != null)
            playerMoneyText.text = $"Money: ${playerData.money}";
    }

    private void ShowGameOver()
    {
        hybridGamePanel?.SetActive(false);
        gameOverPanel?.SetActive(true);

        if (gameMessagesText != null && winnerText != null)
            winnerText.text = gameMessagesText.text;
        else if (winnerText != null)
            winnerText.text = "Game Over!";

        Debug.Log("[HybridUIManager] Showing game over panel");
    }

    #endregion

    #region Button Handlers

    private void SetupButtonListeners()
    {
        rollDiceButton?.onClick.AddListener(OnRollDiceClicked);
        leaveGameButton?.onClick.AddListener(OnLeaveGameClicked);
        purchasePropertyButton?.onClick.AddListener(OnPurchasePropertyClicked);
        returnToMenuButton?.onClick.AddListener(OnReturnToMenuClicked);
        propertyDetailCloseButton?.onClick.AddListener(() => propertyDetailPopup?.SetActive(false));
    }

    private void OnRollDiceClicked()
    {
        if (HybridGameManager.Instance != null)
            HybridGameManager.Instance.RollDice();
        else
            Debug.LogError("[HybridUIManager] HybridGameManager not found!");
    }

    private void OnPurchasePropertyClicked()
    {
        Debug.Log("[HybridUIManager] Purchase property clicked");
        var propertyModule = HybridGameManager.Instance?.GetPropertyModule();
        if (propertyModule != null)
        {
            int localPlayerId = HybridGameManager.Instance.GetLocalPlayerId();
            int position = HybridGameManager.Instance.GetPlayer(localPlayerId)?.position ?? -1;
            if (position >= 0)
                propertyModule.TryPurchaseProperty(localPlayerId, position, HybridGameManager.Instance.GetAllPlayers());
        }
        else
        {
            Debug.LogWarning("[HybridUIManager] HybridPropertyModule not active");
        }
    }

    private async void OnLeaveGameClicked()
    {
        HideAndCleanup();
        await LobbyManager.Instance.LeaveLobby();

        if (UIManager_Streamlined.Instance != null)
            UIManager_Streamlined.Instance.ShowMainMenuPublic();
        else if (UIManager.Instance != null)
            UIManager.Instance.ShowLobbyPublic(); // fallback
    }

    private void OnReturnToMenuClicked()
    {
        HideAndCleanup();
        if (UIManager_Streamlined.Instance != null)
            UIManager_Streamlined.Instance.ShowMainMenuPublic();
        else if (UIManager.Instance != null)
            UIManager.Instance.ShowLobbyPublic(); // fallback
    }

    #endregion

    #region Panel Management

    private void HideAllPanels()
    {
        hybridGamePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
    }

    #endregion
}

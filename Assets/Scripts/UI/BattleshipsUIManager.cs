using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ADD THIS

/// <summary>
/// Manages all UI elements and interactions for the Battleships game
/// Handles ship placement UI, combat UI, and game state displays
/// Enhanced with keyboard controls for better UX
/// </summary>
public class BattleshipsUIManager : MonoBehaviour
{
    private static BattleshipsUIManager instance;
    public static BattleshipsUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BattleshipsUIManager>();
            }
            return instance;
        }
    }

    #region UI References

    [Header("Main Panels")]
    [SerializeField] private GameObject battleShipPanels; // NEW: Parent container for all Battleships panels
    [SerializeField] private GameObject shipPlacementPanel;
    [SerializeField] private GameObject combatPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject boardPanel; // NEW: Parent panel containing both boards

    [Header("Ship Placement UI")]
    [SerializeField] private Button carrierButton;
    [SerializeField] private Button battleshipButton;
    [SerializeField] private Button cruiserButton;
    [SerializeField] private Button submarineButton;
    [SerializeField] private Button destroyerButton;
    [SerializeField] private TextMeshProUGUI placementStatusText;
    [SerializeField] private TextMeshProUGUI selectedShipText;
    [SerializeField] private TextMeshProUGUI instructionsText; // Shows keyboard instructions

    [Header("Combat UI")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private TextMeshProUGUI attackResultText;
    [SerializeField] private TMP_Dropdown enemyPlayerDropdown; // NEW: Dropdown to select which enemy to attack
    [SerializeField] private Button leaveGameButton; // NEW: Button to leave the game
    [SerializeField] private Button targetPlayerButton; // For multiplayer target selection
    [SerializeField] private GameObject targetPlayerPanel;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button playAgainButton;

    [Header("Ship Status Display")]
    [SerializeField] private TextMeshProUGUI shipStatusTitleText; // NEW: Title for ship status panel
    [SerializeField] private TextMeshProUGUI carrierStatusText;
    [SerializeField] private TextMeshProUGUI battleshipStatusText;
    [SerializeField] private TextMeshProUGUI cruiserStatusText;
    [SerializeField] private TextMeshProUGUI submarineStatusText;
    [SerializeField] private TextMeshProUGUI destroyerStatusText;

    [Header("Board Grid References")]
    [SerializeField] private Transform playerBoardParent;
    [SerializeField] private Transform enemyBoardParent;
    [SerializeField] private GameObject tilePrefab;

    #endregion

    #region Private State

    private BattleshipsGameManager.ShipType selectedShipType;
    private bool isHorizontalPlacement = true;
    private int localPlayerId = -1;
    private int selectedTargetPlayerId = -1;

    // Preview state
    private Vector2Int hoveredTilePosition = new Vector2Int(-1, -1);
    private List<GameObject> previewTiles = new List<GameObject>();

    private Dictionary<BattleshipsGameManager.ShipType, bool> shipsPlaced = new Dictionary<BattleshipsGameManager.ShipType, bool>()
    {
        { BattleshipsGameManager.ShipType.Carrier, false },
        { BattleshipsGameManager.ShipType.Battleship, false },
        { BattleshipsGameManager.ShipType.Cruiser, false },
        { BattleshipsGameManager.ShipType.Submarine, false },
        { BattleshipsGameManager.ShipType.Destroyer, false }
    };

    // Track local ship placements for UI display
    private Dictionary<BattleshipsGameManager.ShipType, (Vector2Int startPos, bool isHorizontal)> localShipPlacements =
        new Dictionary<BattleshipsGameManager.ShipType, (Vector2Int, bool)>();

    // Board tile references for visual updates
    private Dictionary<Vector2Int, GameObject> playerBoardTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> enemyBoardTiles = new Dictionary<Vector2Int, GameObject>();

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
        // Start with all panels hidden
        HideAllPanels();

        // Don't set localPlayerId here - it will be fetched when needed
        // This ensures we get the correct ID after network connection
        Debug.Log("? BattleshipsUIManager started");
    }

    private void Update()
    {
        // Handle keyboard input during ship placement
        if (BattleshipsGameManager.Instance != null &&
            BattleshipsGameManager.Instance.GetGameState() == BattleshipsGameManager.GameState.PlacingShips)
        {
            HandleShipPlacementInput();
        }
    }

    private void OnEnable()
    {
        // Subscribe to game state changes
        SubscribeToGameEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }

    #endregion

    #region Keyboard Input Handling

    /// <summary>
    /// Handle keyboard input during ship placement
    /// </summary>
    private void HandleShipPlacementInput()
    {
        // Get keyboard input using new Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return; // No keyboard connected

        // Press R to rotate ship
        if (keyboard.rKey.wasPressedThisFrame)
        {
            RotateShip();
        }

        // Press Enter to confirm all ships placed
        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (AreAllShipsPlaced())
            {
                ConfirmAllShipsPlaced();
            }
            else
            {
                ShowMessage("Place all 5 ships before confirming! (Press number keys 1-5 to select ships)");
            }
        }

        // Number keys 1-5 to select ships quickly
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SelectShip(BattleshipsGameManager.ShipType.Carrier);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SelectShip(BattleshipsGameManager.ShipType.Battleship);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            SelectShip(BattleshipsGameManager.ShipType.Cruiser);
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            SelectShip(BattleshipsGameManager.ShipType.Submarine);
        }
        else if (keyboard.digit5Key.wasPressedThisFrame)
        {
            SelectShip(BattleshipsGameManager.ShipType.Destroyer);
        }
    }

    #endregion

    #region Initialization

    private void InitializeButtons()
    {
        // Ship selection buttons
        if (carrierButton) carrierButton.onClick.AddListener(() => SelectShip(BattleshipsGameManager.ShipType.Carrier));
        if (battleshipButton) battleshipButton.onClick.AddListener(() => SelectShip(BattleshipsGameManager.ShipType.Battleship));
        if (cruiserButton) cruiserButton.onClick.AddListener(() => SelectShip(BattleshipsGameManager.ShipType.Cruiser));
        if (submarineButton) submarineButton.onClick.AddListener(() => SelectShip(BattleshipsGameManager.ShipType.Submarine));
        if (destroyerButton) destroyerButton.onClick.AddListener(() => SelectShip(BattleshipsGameManager.ShipType.Destroyer));

        // Enemy player dropdown
        if (enemyPlayerDropdown) enemyPlayerDropdown.onValueChanged.AddListener(OnEnemyPlayerDropdownChanged);

        // Combat buttons
        if (leaveGameButton) leaveGameButton.onClick.AddListener(OnLeaveGameClicked);

        // Game over buttons - FIXED: Use same leave game method
        if (returnToLobbyButton) returnToLobbyButton.onClick.AddListener(OnLeaveGameClicked);
        if (playAgainButton) playAgainButton.onClick.AddListener(PlayAgain);
    }

    private void SubscribeToGameEvents()
    {
        // Subscribe to network variable changes if needed
    }

    private void UnsubscribeFromGameEvents()
    {
        // Unsubscribe from events
    }

    #endregion

    #region Panel Management

    private void HideAllPanels()
    {
        if (battleShipPanels) battleShipPanels.SetActive(false); // Hide parent container
        if (shipPlacementPanel) shipPlacementPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (waitingPanel) waitingPanel.SetActive(false);
        if (targetPlayerPanel) targetPlayerPanel.SetActive(false);
        if (boardPanel) boardPanel.SetActive(false); // NEW: Hide board panel
    }

    public void ShowShipPlacementPanel()
    {
        HideAllPanels();

        // Activate parent container first
        if (battleShipPanels) battleShipPanels.SetActive(true);

        if (shipPlacementPanel) shipPlacementPanel.SetActive(true);

        // CRITICAL: Activate BoardPanel with ONLY player board visible
        if (boardPanel)
        {
            boardPanel.SetActive(true);
            Debug.Log("? BoardPanel activated for ship placement");
        }

        // Show ONLY player board for ship placement
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(true);
            Debug.Log("? Player board activated for ship placement");
        }

        // Ensure enemy board stays HIDDEN during ship placement
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(false);
            Debug.Log("?? Enemy board kept hidden during ship placement");
        }

        // CRITICAL FIX: Reset ship placement state when showing panel
        ResetShipPlacementState();

        // Auto-select first ship (Carrier) for convenience
        SelectShip(BattleshipsGameManager.ShipType.Carrier);

        UpdateShipPlacementUI();
        UpdateInstructionsText();

        Debug.Log("? Ship placement panel shown - Carrier auto-selected");
    }

    public void ShowCombatPanel()
    {
        HideAllPanels();

        // Activate parent container first
        if (battleShipPanels) battleShipPanels.SetActive(true);

        if (combatPanel) combatPanel.SetActive(true);

        // CRITICAL: Keep BoardPanel active and now show BOTH boards
        if (boardPanel)
        {
            boardPanel.SetActive(true);
            Debug.Log("?? BoardPanel active for combat");
        }

        // Ensure player board stays active
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(true);
            Debug.Log("?? Player board active for combat");
        }

        // CRITICAL FIX: Clear any lingering preview tiles from ship placement
        ClearShipPreview();

        // CRITICAL FIX: Refresh player board visuals to remove green selection tiles
        RefreshPlayerBoardForCombat();

        // NOW activate enemy board for combat phase
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(true);
            Debug.Log("?? Enemy board NOW activated for combat phase");
        }

        // Set ship status title
        if (shipStatusTitleText != null)
        {
            shipStatusTitleText.text = "<b>Fleet Status</b>";
            shipStatusTitleText.richText = true; // Enable bold
        }

        // CRITICAL FIX: Show target player panel (contains enemy dropdown)
        if (targetPlayerPanel != null)
        {
            targetPlayerPanel.SetActive(true);
            Debug.Log("?? Target player panel activated (dropdown should be visible)");
        }
        else
        {
            Debug.LogWarning("?? Target player panel is null - dropdown will not be visible!");
        }

        // Ensure leave game button is visible and interactable
        if (leaveGameButton != null)
        {
            leaveGameButton.gameObject.SetActive(true);
            leaveGameButton.interactable = true;
            Debug.Log("?? Leave game button activated");
        }

        // NEW: Populate enemy player dropdown
        PopulateEnemyPlayerDropdown();

        // CRITICAL: Log the initial selected target for debugging
        Debug.Log($"?? Combat panel shown - Initial target: Player {selectedTargetPlayerId}");

        UpdateCombatUI();
    }

    public void ShowGameOverPanel(int winnerId)
    {
        HideAllPanels();

        // Activate parent container first
        if (battleShipPanels) battleShipPanels.SetActive(true);

        if (gameOverPanel) gameOverPanel.SetActive(true);

        // Get local player ID to determine if we won or were eliminated
        int myPlayerId = GetLocalPlayerId();

        if (winnerText)
        {
            if (winnerId == myPlayerId)
            {
                // Local player won
                winnerText.text = "<color=green><b>Victory!</b></color>\nYou Win!";
            }
            else if (BattleshipsGameManager.Instance != null &&
                     BattleshipsGameManager.Instance.IsPlayerEliminated(myPlayerId))
            {
                // Local player was eliminated (but game just ended for everyone)
                winnerText.text = $"<color=yellow>Game Over</color>\nPlayer {winnerId + 1} Wins!";
            }
            else
            {
                // Local player is spectating or game ended otherwise
                winnerText.text = $"<color=yellow>Game Over</color>\nPlayer {winnerId + 1} Wins!";
            }
        }

        // CRITICAL FIX: Use returnToLobbyButton (on gameOverPanel) not leaveGameButton (on combatPanel)
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.gameObject.SetActive(true);
            returnToLobbyButton.interactable = true;
            Debug.Log("? Return to lobby button visible on game over panel");
        }
        else
        {
            Debug.LogWarning("?? Return to lobby button is null - player cannot leave!");
        }

        // Hide Play Again button (not implemented yet)
        if (playAgainButton != null)
        {
            playAgainButton.gameObject.SetActive(false);
        }

        Debug.Log($"? Game over panel shown - Winner: Player {winnerId}, Local Player: {myPlayerId}");
    }

    /// <summary>
    /// Show defeat panel for eliminated player (game continues for others)
    /// </summary>
    private void ShowDefeatPanel()
    {
        HideAllPanels();

        // Activate parent container first
        if (battleShipPanels) battleShipPanels.SetActive(true);

        if (gameOverPanel) gameOverPanel.SetActive(true);

        if (winnerText)
        {
            winnerText.text = "<color=red><b>Defeated!</b></color>\nYou've been eliminated!";
        }

        // CRITICAL FIX: Use returnToLobbyButton (on gameOverPanel) not leaveGameButton (on combatPanel)
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.gameObject.SetActive(true);
            returnToLobbyButton.interactable = true;
            Debug.Log("? Return to lobby button visible on defeat panel");
        }
        else
        {
            Debug.LogWarning("?? Return to lobby button is null - player cannot leave!");
        }

        // Hide other buttons
        if (playAgainButton != null)
        {
            playAgainButton.gameObject.SetActive(false);
        }

        Debug.Log("? Defeat panel shown for eliminated player");
    }

    public void ShowWaitingPanel(string message)
    {
        HideAllPanels();

        // Activate parent container first
        if (battleShipPanels) battleShipPanels.SetActive(true);

        // Waiting panel is optional - only show if assigned
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);

            TextMeshProUGUI waitingText = waitingPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (waitingText != null)
            {
                waitingText.text = message;
            }
            else
            {
                Debug.LogWarning("Waiting panel has no TextMeshProUGUI component");
            }
        }
        else
        {
            Debug.Log($"Waiting panel not assigned - message: {message}");
        }

        // CRITICAL: Hide the board panel when showing waiting screen
        if (boardPanel != null)
        {
            boardPanel.SetActive(false);
            Debug.Log("? Board panel hidden while waiting");
        }

        // Also explicitly hide individual board parents
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(false);
            Debug.Log("? Player board hidden while waiting");
        }

        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(false);
            Debug.Log("? Enemy board hidden while waiting");
        }
    }

    #endregion

    #region Enemy Player Selection

    /// <summary>
    /// Populate the enemy player dropdown based on the number of players in the game
    /// </summary>
    private void PopulateEnemyPlayerDropdown()
    {
        if (enemyPlayerDropdown == null)
        {
            Debug.LogWarning("?? Enemy player dropdown is not assigned!");
            return;
        }

        if (BattleshipsGameManager.Instance == null)
        {
            Debug.LogError("? BattleshipsGameManager.Instance is null!");
            return;
        }

        // Get local player ID
        localPlayerId = GetLocalPlayerId();

        // Get total player count
        int totalPlayers = BattleshipsGameManager.Instance.GetTotalPlayers();

        // Clear existing options
        enemyPlayerDropdown.ClearOptions();

        // Create list of enemy player options (only alive players)
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < totalPlayers; i++)
        {
            // Skip the local player and eliminated players
            if (i == localPlayerId) continue;
            if (BattleshipsGameManager.Instance.IsPlayerEliminated(i)) continue;

            // Add enemy player to dropdown
            string playerLabel = $"Enemy Player {i + 1}";
            options.Add(new TMP_Dropdown.OptionData(playerLabel));
        }

        // Add options to dropdown
        enemyPlayerDropdown.AddOptions(options);

        // Select the first enemy by default (index 0 in the filtered list)
        if (options.Count > 0)
        {
            enemyPlayerDropdown.value = 0;

            // Calculate actual enemy player ID (first enemy after local player)
            selectedTargetPlayerId = GetEnemyPlayerIdFromDropdownIndex(0);

            Debug.Log($"? Populated enemy dropdown with {options.Count} enemies. Selected: Player {selectedTargetPlayerId}");
        }
        else
        {
            Debug.LogWarning("?? No enemy players found!");
            selectedTargetPlayerId = -1;
        }
    }

    /// <summary>
    /// Called when a player is eliminated - update dropdown and switch targets if necessary
    /// </summary>
    public void OnPlayerEliminated(int eliminatedPlayerId)
    {
        Debug.Log($"?? OnPlayerEliminated called for Player {eliminatedPlayerId}");

        int myPlayerId = GetLocalPlayerId();

        // CRITICAL: If I'm the eliminated player, show defeat panel immediately
        if (eliminatedPlayerId == myPlayerId)
        {
            Debug.Log("? I was eliminated - showing defeat panel");
            ShowDefeatPanel();
            return; // Don't update dropdown for eliminated player
        }

        // Check if we were targeting the eliminated player
        bool wasTargetingEliminated = (selectedTargetPlayerId == eliminatedPlayerId);

        // Repopulate dropdown to remove the eliminated player
        PopulateEnemyPlayerDropdown();

        // If we were targeting the eliminated player, we've automatically switched to the first available target
        if (wasTargetingEliminated)
        {
            Debug.Log($"? Was targeting eliminated player {eliminatedPlayerId} - switched to Player {selectedTargetPlayerId}");

            // Regenerate the enemy board for the new target
            if (selectedTargetPlayerId >= 0)
            {
                SwitchEnemyBoard(selectedTargetPlayerId);
            }
        }
    }

    /// <summary>
    /// Called when enemy player dropdown selection changes
    /// </summary>
    private void OnEnemyPlayerDropdownChanged(int dropdownIndex)
    {
        // Get the actual enemy player ID from the dropdown index
        int newTargetPlayerId = GetEnemyPlayerIdFromDropdownIndex(dropdownIndex);

        Debug.Log($"?? Enemy target changed from Player {selectedTargetPlayerId} to Player {newTargetPlayerId} (dropdown index: {dropdownIndex})");

        // Update selected target BEFORE switching board
        selectedTargetPlayerId = newTargetPlayerId;

        // Switch to the selected enemy's board
        SwitchEnemyBoard(selectedTargetPlayerId);

        // Update UI to reflect new target
        UpdateCombatUI();
    }

    /// <summary>
    /// Convert dropdown index to actual enemy player ID
    /// (dropdown only shows enemies, so we need to map back to real player IDs)
    /// </summary>
    private int GetEnemyPlayerIdFromDropdownIndex(int dropdownIndex)
    {
        if (BattleshipsGameManager.Instance == null) return 0;

        int totalPlayers = BattleshipsGameManager.Instance.GetTotalPlayers();
        int enemyCount = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            // Skip local player
            if (i == localPlayerId) continue;

            // Skip eliminated players
            if (BattleshipsGameManager.Instance.IsPlayerEliminated(i)) continue;

            // This is an alive enemy - check if it matches our dropdown index
            if (enemyCount == dropdownIndex)
            {
                return i;
            }

            enemyCount++;
        }

        // Fallback: find first non-local, non-eliminated player
        for (int i = 0; i < totalPlayers; i++)
        {
            if (i != localPlayerId && !BattleshipsGameManager.Instance.IsPlayerEliminated(i))
            {
                return i;
            }
        }

        return -1; // No valid targets
    }

    /// <summary>
    /// Switch the enemy board to show the selected target player's board
    /// </summary>
    private void SwitchEnemyBoard(int targetPlayerId)
    {
        BattleshipsBoardGenerator boardGenerator = FindFirstObjectByType<BattleshipsBoardGenerator>();
        if (boardGenerator == null)
        {
            Debug.LogError("?? BattleshipsBoardGenerator not found!");
            return;
        }

        Debug.Log($"?? Switching enemy board to Player {targetPlayerId} (was viewing Player {selectedTargetPlayerId})");

        // Regenerate enemy board for the selected target player
        Dictionary<Vector2Int, GameObject> newEnemyTiles = boardGenerator.RegenerateEnemyBoard(targetPlayerId);

        if (newEnemyTiles != null && newEnemyTiles.Count > 0)
        {
            // Update local reference
            enemyBoardTiles = newEnemyTiles;
            Debug.Log($"? Enemy board switched successfully ({newEnemyTiles.Count} tiles) - Now viewing Player {targetPlayerId}");

            // CRITICAL FIX: Request board state from server before refreshing visuals
            if (BattleshipsGameManager.Instance != null)
            {
                int myPlayerId = GetLocalPlayerId();
                Debug.Log($"?? Requesting board state for Player {targetPlayerId} from server...");
                BattleshipsGameManager.Instance.RequestBoardStateServerRpc(myPlayerId, targetPlayerId);
            }

            // Note: Visual refresh will happen in OnBoardStateReceived callback
        }
        else
        {
            Debug.LogError($"?? Failed to regenerate enemy board for Player {targetPlayerId}");
        }
    }

    /// <summary>
    /// Update enemy board tiles dictionary (called by BattleshipsBoardGenerator after regeneration)
    /// </summary>
    public void UpdateEnemyBoardTiles(Dictionary<Vector2Int, GameObject> newEnemyTiles)
    {
        if (newEnemyTiles == null)
        {
            Debug.LogWarning("?? Attempted to update enemy board tiles with null dictionary");
            return;
        }

        enemyBoardTiles = newEnemyTiles;
        Debug.Log($"? Updated enemy board tiles dictionary ({newEnemyTiles.Count} tiles)");
    }

    /// <summary>
    /// Called when board state is received from server (after requesting it)
    /// </summary>
    public void OnBoardStateReceived(int targetPlayerId)
    {
        Debug.Log($"?? Board state received for Player {targetPlayerId}");

        // Only refresh if this is the currently selected target
        if (targetPlayerId == selectedTargetPlayerId)
        {
            Debug.Log($"?? This is the current target - refreshing visuals");
            RefreshEnemyBoardVisuals(targetPlayerId);
        }
        else
        {
            Debug.Log($"?? Board state received for Player {targetPlayerId} but current target is Player {selectedTargetPlayerId} - skipping refresh");
        }
    }

    /// <summary>
    /// Refresh the visual state of the enemy board based on attack history
    /// </summary>
    private void RefreshEnemyBoardVisuals(int targetPlayerId)
    {
        if (BattleshipsGameManager.Instance == null)
        {
            Debug.LogWarning("?? BattleshipsGameManager.Instance is null - cannot refresh visuals");
            return;
        }

        // Get the target player's board
        var targetBoard = BattleshipsGameManager.Instance.GetPlayerBoard(targetPlayerId);
        if (targetBoard == null)
        {
            Debug.LogWarning($"?? Cannot get board for player {targetPlayerId} - board data not available");

            // FALLBACK: Reset all tiles to default blue (water)
            Debug.Log($"?? Resetting all enemy board tiles to default (waiting for server data)");
            foreach (var kvp in enemyBoardTiles)
            {
                GameObject tile = kvp.Value;
                if (tile != null)
                {
                    Image tileImage = tile.GetComponent<Image>();
                    if (tileImage != null)
                    {
                        tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Blue water
                    }
                }
            }
            return;
        }

        Debug.Log($"?? Refreshing enemy board visuals for Player {targetPlayerId} ({enemyBoardTiles.Count} tiles)");
        Debug.Log($"   Target board has {targetBoard.hits.Count} hits and {targetBoard.misses.Count} misses");

        int hitsFound = 0;
        int missesFound = 0;
        int untouchedTiles = 0;

        // Update each tile based on hit/miss history
        foreach (var kvp in enemyBoardTiles)
        {
            Vector2Int pos = kvp.Key;
            GameObject tile = kvp.Value;

            if (tile == null)
            {
                Debug.LogWarning($"?? Tile at {pos} is null!");
                continue;
            }

            Image tileImage = tile.GetComponent<Image>();
            if (tileImage == null)
            {
                Debug.LogWarning($"?? Tile at {pos} has no Image component!");
                continue;
            }

            // Check if this tile has been attacked (hit or miss)
            if (targetBoard.hits.Contains(pos))
            {
                // This tile was hit - show black
                tileImage.color = Color.black;
                hitsFound++;
            }
            else if (targetBoard.misses.Contains(pos))
            {
                // This tile was a miss - show red
                tileImage.color = Color.red;
                missesFound++;
            }
            else
            {
                // Not attacked yet - show blue water
                tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Blue water
                untouchedTiles++;
            }
        }

        Debug.Log($"? Refreshed enemy board: {hitsFound} hits, {missesFound} misses, {untouchedTiles} untouched tiles");
    }

    #endregion

    #region Ship Placement UI

    /// <summary>
    /// Update instructions text with controls
    /// </summary>
    private void UpdateInstructionsText()
    {
        if (instructionsText != null)
        {
            instructionsText.text =
                "<b>Ship Placement Controls:</b>\n" +
                "• <b>Click</b> ship buttons to select (or press 1-5)\n" +
                "• <b>Double-click</b> a tile to place ship\n" +
                "• <b>Press R</b> to rotate ship orientation\n";
                
        }
    }

    private void SelectShip(BattleshipsGameManager.ShipType shipType)
    {
        // Don't allow selecting already placed ships
        if (shipsPlaced[shipType])
        {
            ShowMessage("This ship has already been placed!");
            return;
        }

        selectedShipType = shipType;
        UpdateSelectedShipDisplay();

        Debug.Log($"Selected ship: {shipType}");
    }

    private void RotateShip()
    {
        // Simple toggle between horizontal and vertical
        isHorizontalPlacement = !isHorizontalPlacement;

        UpdateSelectedShipDisplay();

        // Force preview update immediately with current hovered position
        if (hoveredTilePosition.x >= 0 && hoveredTilePosition.y >= 0)
        {
            // Force update even if position is the same (forceUpdate = true)
            ShowShipPreview(hoveredTilePosition, forceUpdate: true);
        }

        string orientation = isHorizontalPlacement ? "Horizontal ?" : "Vertical ?";
        Debug.Log($"Ship rotation: {orientation}");
        ShowMessage($"Ship rotated to {orientation}");
    }

    private void UpdateSelectedShipDisplay()
    {
        if (selectedShipText)
        {
            string orientation = isHorizontalPlacement ? "Horizontal ?" : "Vertical ?";
            int length = GetShipLength(selectedShipType);
            selectedShipText.text = $"Selected: <b>{selectedShipType}</b> ({length} tiles, {orientation})";
        }
    }

    private void UpdateShipPlacementUI()
    {
        if (placementStatusText)
        {
            int placedCount = 0;
            foreach (var placed in shipsPlaced.Values)
            {
                if (placed) placedCount++;
            }

            // Enhanced status with remaining ships
            if (placedCount == 5)
            {
                placementStatusText.text = "<color=green><b>? All Ships Placed!</b></color> Press Enter to confirm";
            }
            else
            {
                placementStatusText.text = $"Ships Placed: <b>{placedCount}/5</b> (Select a ship and double-click to place)";
            }
        }

        // Update ship button states
        UpdateShipButton(carrierButton, BattleshipsGameManager.ShipType.Carrier, "1");
        UpdateShipButton(battleshipButton, BattleshipsGameManager.ShipType.Battleship, "2");
        UpdateShipButton(cruiserButton, BattleshipsGameManager.ShipType.Cruiser, "3");
        UpdateShipButton(submarineButton, BattleshipsGameManager.ShipType.Submarine, "4");
        UpdateShipButton(destroyerButton, BattleshipsGameManager.ShipType.Destroyer, "5");
    }

    private void UpdateShipButton(Button button, BattleshipsGameManager.ShipType shipType, string keyNumber)
    {
        if (button == null) return;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

        if (shipsPlaced[shipType])
        {
            button.interactable = false;

            // Show checkmark when placed
            if (buttonText != null)
            {
                int length = GetShipLength(shipType);
                buttonText.text = $"<color=green>?</color> {shipType} ({length})";
            }

            var colors = button.colors;
            colors.disabledColor = new Color(0.3f, 0.8f, 0.3f, 0.7f); // Light green
            button.colors = colors;
        }
        else
        {
            // Show key number hint
            if (buttonText != null)
            {
                int length = GetShipLength(shipType);
                buttonText.text = $"[{keyNumber}] {shipType} ({length})";
            }
        }
    }

    private bool AreAllShipsPlaced()
    {
        foreach (var placed in shipsPlaced.Values)
        {
            if (!placed) return false;
        }
        return true;
    }

    private void ConfirmAllShipsPlaced()
    {
        if (!AreAllShipsPlaced())
        {
            ShowMessage("You must place all 5 ships before continuing!");
            return;
        }

        ShowWaitingPanel("Waiting for other players to place their ships...");
        Debug.Log("All ships placed - waiting for other players");
    }

    /// <summary>
    /// Called when player clicks a tile to place a ship
    /// </summary>
    public void OnTilePlacementClick(Vector2Int position)
    {
        Debug.Log($"? OnTilePlacementClick called at position {position}");

        if (BattleshipsGameManager.Instance == null)
        {
            Debug.LogError("? BattleshipsGameManager.Instance is NULL!");
            return;
        }

        if (BattleshipsGameManager.Instance.GetGameState() != BattleshipsGameManager.GameState.PlacingShips)
        {
            Debug.LogWarning($"? Cannot place ship - game state is {BattleshipsGameManager.Instance.GetGameState()}, expected PlacingShips");
            return;
        }

        // Get fresh local player ID (important for network synchronization)
        localPlayerId = GetLocalPlayerId();

        Debug.Log($"?? Attempting to place {selectedShipType} at {position}, horizontal: {isHorizontalPlacement}");
        Debug.Log($"?? Local player ID: {localPlayerId}");

        // Send placement request to server (simple - no adjustment needed)
        BattleshipsGameManager.Instance.PlaceShipServerRpc(
            localPlayerId,
            (int)selectedShipType,
            position,
            isHorizontalPlacement
        );

        // Clear preview after placement attempt
        ClearShipPreview();

        Debug.Log($"?? Sent PlaceShipServerRpc to server");
    }

    /// <summary>
    /// Called by game manager when ship placement succeeds
    /// </summary>
    public void OnShipPlaced(int playerId, BattleshipsGameManager.ShipType shipType, Vector2Int startPos, bool isHorizontal)
    {
        // CRITICAL FIX: Update UI for LOCAL player only (each client manages their own UI)
        int myPlayerId = GetLocalPlayerId();
        if (playerId != myPlayerId)
        {
            Debug.Log($"Ignoring ship placement for other player {playerId} (I am {myPlayerId})");
            return;
        }

        // Track the placement locally
        localShipPlacements[shipType] = (startPos, isHorizontal);
        shipsPlaced[shipType] = true;

        UpdateShipPlacementUI();

        // Visual feedback
        VisualizeShipOnBoard(startPos, GetShipLength(shipType), isHorizontal);
        ShowMessage($"<color=green>? {shipType} placed successfully!</color>");

        // Update ship status display with local data
        UpdateLocalShipStatus(shipType);

        // Auto-select next unplaced ship for convenience
        SelectNextUnplacedShip();
    }

    /// <summary>
    /// Auto-select the next unplaced ship after placing one
    /// </summary>
    private void SelectNextUnplacedShip()
    {
        // Try to select ships in order
        BattleshipsGameManager.ShipType[] shipOrder = {
            BattleshipsGameManager.ShipType.Carrier,
            BattleshipsGameManager.ShipType.Battleship,
            BattleshipsGameManager.ShipType.Cruiser,
            BattleshipsGameManager.ShipType.Submarine,
            BattleshipsGameManager.ShipType.Destroyer
        };

        foreach (var ship in shipOrder)
        {
            if (!shipsPlaced[ship])
            {
                SelectShip(ship);
                return;
            }
        }
    }

    /// <summary>
    /// Called by game manager when ship placement fails
    /// </summary>
    public void OnShipPlacementFailed(int playerId)
    {
        if (playerId != localPlayerId) return;

        ShowMessage("<color=red>Cannot place ship here!</color> Try a different position or rotate (Press R).");
    }

    #endregion

    #region Combat UI

    public void UpdateCombatUI()
    {
        if (BattleshipsGameManager.Instance == null) return;

        // Update turn indicator
        int currentTurn = BattleshipsGameManager.Instance.GetCurrentTurn();
        bool isMyTurn = (currentTurn == localPlayerId);

        if (currentTurnText)
        {
            currentTurnText.text = isMyTurn ? "<color=green><b>Your Turn!</b></color>" : $"Player {currentTurn}'s Turn";
        }

        if (gameStatusText)
        {
            if (isMyTurn)
            {
                gameStatusText.text = "Click an enemy tile to attack!";
            }
            else
            {
                gameStatusText.text = "Waiting for opponent...";
            }
        }

        UpdateShipStatusDisplay();
    }

    private void UpdateShipStatusDisplay()
    {
        // CRITICAL FIX: Use local tracking instead of server data (clients can't access other player boards)
        UpdateLocalShipStatus(BattleshipsGameManager.ShipType.Carrier);
        UpdateLocalShipStatus(BattleshipsGameManager.ShipType.Battleship);
        UpdateLocalShipStatus(BattleshipsGameManager.ShipType.Cruiser);
        UpdateLocalShipStatus(BattleshipsGameManager.ShipType.Submarine);
        UpdateLocalShipStatus(BattleshipsGameManager.ShipType.Destroyer);
    }

    /// <summary>
    /// Update status for a specific ship using local tracking (client-friendly)
    /// </summary>
    private void UpdateLocalShipStatus(BattleshipsGameManager.ShipType shipType)
    {
        TextMeshProUGUI statusText = shipType switch
        {
            BattleshipsGameManager.ShipType.Carrier => carrierStatusText,
            BattleshipsGameManager.ShipType.Battleship => battleshipStatusText,
            BattleshipsGameManager.ShipType.Cruiser => cruiserStatusText,
            BattleshipsGameManager.ShipType.Submarine => submarineStatusText,
            BattleshipsGameManager.ShipType.Destroyer => destroyerStatusText,
            _ => null
        };

        if (statusText == null) return;

        // Check local placement tracking first
        if (!shipsPlaced[shipType])
        {
            // Not placed yet
            statusText.text = $"{shipType}: Not Placed";
            statusText.color = Color.gray;
            return;
        }

        // Ship is placed - try to get hit data from server if available (for combat phase)
        if (BattleshipsGameManager.Instance != null &&
            BattleshipsGameManager.Instance.GetGameState() == BattleshipsGameManager.GameState.InProgress)
        {
            var playerBoard = BattleshipsGameManager.Instance.GetPlayerBoard(localPlayerId);
            if (playerBoard != null)
            {
                var ship = playerBoard.shipList.Find(s => s.shipType == shipType);
                if (ship != null)
                {
                    int hitCount = ship.GetHitCount(playerBoard.hits);
                    int length = ship.length;

                    if (ship.IsSunk(playerBoard.hits))
                    {
                        statusText.text = $"{shipType}: SUNK";
                        statusText.color = Color.red;
                        statusText.fontStyle = TMPro.FontStyles.Bold;
                        return;
                    }
                    else if (hitCount > 0)
                    {
                        statusText.text = $"{shipType}: {hitCount}/{length} hits";
                        statusText.color = new Color(1f, 0.5f, 0f); // Orange
                        statusText.fontStyle = TMPro.FontStyles.Normal;
                        return;
                    }
                }
            }
        }

        // Default: Ship is placed and intact
        statusText.text = $"{shipType}: Intact";
        statusText.color = Color.green;
        statusText.fontStyle = TMPro.FontStyles.Normal;
    }

    /// <summary>
    /// Called when player clicks an enemy tile to attack
    /// </summary>
    public void OnTileAttackClick(Vector2Int position, int targetPlayerId)
    {
        if (BattleshipsGameManager.Instance == null) return;
        if (BattleshipsGameManager.Instance.GetGameState() != BattleshipsGameManager.GameState.InProgress) return;

        int currentTurn = BattleshipsGameManager.Instance.GetCurrentTurn();
        if (currentTurn != localPlayerId)
        {
            ShowMessage("It's not your turn!");
            return;
        }

        // Use the selected target from dropdown (override parameter if dropdown is being used)
        int actualTarget = selectedTargetPlayerId >= 0 ? selectedTargetPlayerId : targetPlayerId;

        // Send attack request to server
        BattleshipsGameManager.Instance.AttackTileServerRpc(localPlayerId, actualTarget, position);
    }

    /// <summary>
    /// Called by game manager when attack result is received
    /// </summary>
    public void OnAttackResult(int attackerId, int targetId, Vector2Int position, BattleshipsGameManager.AttackResult result)
    {
        // CRITICAL FIX: Only visualize if this attack is relevant to what we're currently viewing
        int myPlayerId = GetLocalPlayerId();

        // Determine if we should visualize this attack
        bool shouldVisualize = false;
        bool isPlayerBoard = false;

        if (targetId == myPlayerId)
        {
            // Attack on MY board - always visualize on player board
            shouldVisualize = true;
            isPlayerBoard = true;
            Debug.Log($"?? Attack on MY board (Player {myPlayerId}) at {position} - Result: {result}");
        }
        else if (targetId == selectedTargetPlayerId && BattleshipsGameManager.Instance != null &&
                 BattleshipsGameManager.Instance.GetGameState() == BattleshipsGameManager.GameState.InProgress)
        {
            // Attack on the enemy I'm currently viewing - visualize on enemy board
            shouldVisualize = true;
            isPlayerBoard = false;
            Debug.Log($"?? Attack on CURRENTLY VIEWED enemy (Player {targetId}) at {position} - Result: {result}");
        }
        else
        {
            // Attack on a different player that I'm not viewing - don't visualize
            Debug.Log($"?? Attack on Player {targetId} (not currently viewing) - skipping visualization");
        }

        // Only visualize if this attack is relevant to current view
        if (shouldVisualize)
        {
            VisualizeAttackResult(position, result, isPlayerBoard);
        }

        // Show attack result message with color (only if I'm the attacker or target)
        if (attackerId == myPlayerId || targetId == myPlayerId)
        {
            string resultMessage = result switch
            {
                BattleshipsGameManager.AttackResult.Miss => "<color=blue><b>Miss!</b></color>",
                BattleshipsGameManager.AttackResult.Hit => "<color=orange><b>Hit!</b></color>",
                BattleshipsGameManager.AttackResult.Sunk => "<color=red><b>Ship Sunk!</b></color>",
                BattleshipsGameManager.AttackResult.Eliminated => "<color=purple><b>Player Eliminated!</b></color>",
                _ => ""
            };

            if (attackResultText)
            {
                attackResultText.text = resultMessage;
            }

            ShowMessage(resultMessage);
        }

        UpdateCombatUI();
    }

    #endregion

    #region Visual Board Updates

    private void VisualizeShipOnBoard(Vector2Int startPos, int length, bool isHorizontal)
    {
        for (int i = 0; i < length; i++)
        {
            Vector2Int pos = isHorizontal
                ? new Vector2Int(startPos.x + i, startPos.y)
                : new Vector2Int(startPos.x, startPos.y + i);

            if (playerBoardTiles.TryGetValue(pos, out GameObject tile))
            {
                // Change tile color to grey for ship placement
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage)
                {
                    tileImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Grey for ships
                }
            }
        }
    }

    /// <summary>
    /// Refresh player board visuals for combat phase (show placed ships, clear previews)
    /// </summary>
    private void RefreshPlayerBoardForCombat()
    {
        Debug.Log("?? Refreshing player board for combat phase...");

        // First, reset all tiles to water color (clears any lingering preview tiles)
        SetTilesToWaterColor(playerBoardTiles);

        // Then, visualize all placed ships on the player board
        foreach (var shipPlacement in localShipPlacements)
        {
            BattleshipsGameManager.ShipType shipType = shipPlacement.Key;
            Vector2Int startPos = shipPlacement.Value.Item1;
            bool isHorizontal = shipPlacement.Value.Item2;
            int length = GetShipLength(shipType);

            // Visualize the ship (grey tiles)
            VisualizeShipOnBoard(startPos, length, isHorizontal);
            Debug.Log($"  ? Re-visualized {shipType} at {startPos} (horizontal: {isHorizontal})");
        }

        Debug.Log($"? Player board refreshed - {localShipPlacements.Count} ships visualized");
    }

    /// <summary>
    /// Visualize attack result on the correct board (updated signature - uses bool instead of targetId)
    /// </summary>
    private void VisualizeAttackResult(Vector2Int position, BattleshipsGameManager.AttackResult result, bool isPlayerBoard)
    {
        // Select the correct board based on the flag
        Dictionary<Vector2Int, GameObject> targetBoard = isPlayerBoard ? playerBoardTiles : enemyBoardTiles;

        string boardName = isPlayerBoard ? "PLAYER" : "ENEMY";

        if (targetBoard.TryGetValue(position, out GameObject tile))
        {
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage)
            {
                switch (result)
                {
                    case BattleshipsGameManager.AttackResult.Miss:
                        tileImage.color = Color.red; // Red for miss
                        Debug.Log($"? Visualized MISS on {boardName} board at {position}");
                        break;
                    case BattleshipsGameManager.AttackResult.Hit:
                    case BattleshipsGameManager.AttackResult.Sunk:
                    case BattleshipsGameManager.AttackResult.Eliminated:
                        tileImage.color = Color.black; // Black for hit
                        Debug.Log($"? Visualized HIT on {boardName} board at {position} - Result: {result}");
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"?? Tile at {position} on {boardName} board has no Image component!");
            }
        }
        else
        {
            Debug.LogWarning($"?? Could not find tile at {position} on {boardName} board (board has {targetBoard.Count} tiles)");
        }
    }

    /// <summary>
    /// Initialize board tile references (call this after creating the grid)
    /// </summary>
    public void InitializeBoardTiles(Dictionary<Vector2Int, GameObject> playerTiles, Dictionary<Vector2Int, GameObject> enemyTiles)
    {
        playerBoardTiles = playerTiles;
        enemyBoardTiles = enemyTiles;

        // Set all tiles to blue water color initially
        SetTilesToWaterColor(playerBoardTiles);
        SetTilesToWaterColor(enemyBoardTiles);

        Debug.Log($"Board tiles initialized: {playerBoardTiles.Count} player tiles, {enemyBoardTiles.Count} enemy tiles (all set to blue water)");
    }

    /// <summary>
    /// Set all tiles in a board to blue water color
    /// </summary>
    private void SetTilesToWaterColor(Dictionary<Vector2Int, GameObject> tiles)
    {
        foreach (var kvp in tiles)
        {
            GameObject tile = kvp.Value;
            if (tile != null)
            {
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage != null)
                {
                    tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Blue water
                }
            }
        }
    }

    #endregion

    #region Game Control Methods

    /// <summary>
    /// Called when leave game button is clicked
    /// </summary>
    private async void OnLeaveGameClicked()
    {
        Debug.Log("Leave game button clicked");

        // Disable button to prevent multiple clicks
        if (leaveGameButton != null) leaveGameButton.interactable = false;

        try
        {
            // Hide all game panels immediately
            HideAllPanels();
            Debug.Log("? All game panels hidden");

            // Leave the lobby/disconnect from game
            if (LobbyManager.Instance != null)
            {
                await LobbyManager.Instance.LeaveLobby();
                Debug.Log("? Left game successfully");
            }

            // Return to main menu
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameModeSelectionPublic();
                Debug.Log("? Returned to main menu");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Error leaving game: {e.Message}");

            // Force hide panels and return to menu anyway
            HideAllPanels();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameModeSelectionPublic();
            }
        }
        finally
        {
            // Re-enable button
            if (leaveGameButton != null) leaveGameButton.interactable = true;
        }
    }

    #endregion

    #region Game Over

    public void OnGameOver(int winnerId)
    {
        ShowGameOverPanel(winnerId);
    }

    private void ReturnToLobby()
    {
        // DEPRECATED: Now using OnLeaveGameClicked for consistency
        OnLeaveGameClicked();
    }

    private void PlayAgain()
    {
        // Reset game and start new match
        Debug.Log("Starting new game...");

        // Reset local state
        ResetGameState();
    }

    private void ResetGameState()
    {
        // Reset ship placement tracking
        foreach (var key in new List<BattleshipsGameManager.ShipType>(shipsPlaced.Keys))
        {
            shipsPlaced[key] = false;
        }

        // Clear local ship placements tracking
        localShipPlacements.Clear();

        isHorizontalPlacement = true;
        // Note: selectedTargetPlayerId reserved for future multiplayer targeting features
        // This will be used when implementing target selection for 3-4 player games
        selectedTargetPlayerId = -1;

        HideAllPanels();
    }

    #endregion

    #region Ship Placement State Reset

    /// <summary>
    /// Reset ship placement state to allow fresh placement
    /// CRITICAL: Call this when showing ship placement panel to ensure clean state
    /// </summary>
    private void ResetShipPlacementState()
    {
        Debug.Log("?? Resetting ship placement state...");

        // Reset all ships to unplaced
        shipsPlaced[BattleshipsGameManager.ShipType.Carrier] = false;
        shipsPlaced[BattleshipsGameManager.ShipType.Battleship] = false;
        shipsPlaced[BattleshipsGameManager.ShipType.Cruiser] = false;
        shipsPlaced[BattleshipsGameManager.ShipType.Submarine] = false;
        shipsPlaced[BattleshipsGameManager.ShipType.Destroyer] = false;

        // Clear local placements tracking
        localShipPlacements.Clear();

        // Reset orientation
        isHorizontalPlacement = true;

        // Clear preview
        ClearShipPreview();
        hoveredTilePosition = new Vector2Int(-1, -1);

        // Re-enable all ship buttons
        if (carrierButton) carrierButton.interactable = true;
        if (battleshipButton) battleshipButton.interactable = true;
        if (cruiserButton) cruiserButton.interactable = true;
        if (submarineButton) submarineButton.interactable = true;
        if (destroyerButton) destroyerButton.interactable = true;

        // Clear ship visualizations from player board
        ClearShipVisualizations();

        Debug.Log("? Ship placement state reset complete");
    }

    /// <summary>
    /// Clear all ship visualizations from the player board
    /// </summary>
    private void ClearShipVisualizations()
    {
        foreach (var kvp in playerBoardTiles)
        {
            GameObject tile = kvp.Value;
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage != null)
            {
                // Reset to blue water color
                tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Blue water
            }
        }

        Debug.Log("?? Cleared ship visualizations from player board");
    }

    #endregion

    #region Public Cleanup API

    /// <summary>
    /// Public method to clean up all Battleships UI and boards when returning to main menu
    /// Called by UIManager when leaving a Battleships game
    /// </summary>
    public void CleanupForMainMenu()
    {
        Debug.Log("?? BattleshipsUIManager: Cleaning up for main menu...");

        // Hide all UI panels
        HideAllPanels();
        Debug.Log("? All Battleships UI panels hidden");

        // CRITICAL: Deactivate parent container that holds all Battleships panels
        if (battleShipPanels != null)
        {
            battleShipPanels.SetActive(false);
            Debug.Log("? Deactivated battleShipPanels parent container");
        }

        // CRITICAL: Deactivate BoardPanel (which contains both board parents)
        if (boardPanel != null)
        {
            boardPanel.SetActive(false);
            Debug.Log("? Deactivated BoardPanel (parent container)");
        }

        // CRITICAL FIX: Directly deactivate board parents using serialized references
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(false);
            Debug.Log("? Deactivated player board directly");
        }
        else
        {
            Debug.LogWarning("?? Player board parent reference is null");
        }

        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(false);
            Debug.Log("? Deactivated enemy board directly");
        }
        else
        {
            Debug.LogWarning("?? Enemy board parent reference is null");
        }

        Debug.Log("? BattleshipsUIManager cleanup complete");
    }

    #endregion

    #region Helper Methods

    private void ShowMessage(string message)
    {
        Debug.Log($"[BattleshipsUI] {message}");
        // TODO: Show popup message or toast notification
    }

    private int GetShipLength(BattleshipsGameManager.ShipType shipType)
    {
        return shipType switch
        {
            BattleshipsGameManager.ShipType.Carrier => 5,
            BattleshipsGameManager.ShipType.Battleship => 4,
            BattleshipsGameManager.ShipType.Cruiser => 3,
            BattleshipsGameManager.ShipType.Submarine => 3,
            BattleshipsGameManager.ShipType.Destroyer => 2,
            _ => 3
        };
    }

    private int GetLocalPlayerId()
    {
        // Use Unity.Netcode.NetworkManager
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClient != null)
        {
            return (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        }
        return 0;
    }

    #endregion

    #region Public API for Game Manager

    /// <summary>
    /// Called when game state changes
    /// </summary>
    public void OnGameStateChanged(BattleshipsGameManager.GameState newState)
    {
        switch (newState)
        {
            case BattleshipsGameManager.GameState.WaitingToStart:
                ShowWaitingPanel("Waiting for game to start...");
                break;
            case BattleshipsGameManager.GameState.PlacingShips:
                ShowShipPlacementPanel();
                break;
            case BattleshipsGameManager.GameState.InProgress:
                ShowCombatPanel();
                break;
            case BattleshipsGameManager.GameState.GameOver:
                // Game over handled by OnGameOver call
                break;
        }
    }

    #endregion

    #region Ship Preview

    /// <summary>
    /// Show preview of where ship will be placed (call on hover or single-click)
    /// </summary>
    public void ShowShipPreview(Vector2Int startPosition, bool forceUpdate = false)
    {
        // Don't show preview during combat or if no ship selected
        if (BattleshipsGameManager.Instance == null ||
            BattleshipsGameManager.Instance.GetGameState() != BattleshipsGameManager.GameState.PlacingShips)
        {
            return;
        }

        // If same position and not forcing update, don't regenerate
        if (startPosition == hoveredTilePosition && !forceUpdate)
        {
            return;
        }

        hoveredTilePosition = startPosition;

        // Clear previous preview
        ClearShipPreview();

        // Calculate ship positions based on orientation (simplified)
        List<Vector2Int> shipPositions = CalculateShipPositions(startPosition, GetShipLength(selectedShipType), isHorizontalPlacement);

        // Check if placement is valid
        bool isValidPlacement = IsValidPlacement(shipPositions);
        Color previewColor = isValidPlacement
            ? new Color(0.0f, 1.0f, 0.0f, 0.5f)  // Green semi-transparent for valid
            : new Color(1.0f, 0.0f, 0.0f, 0.5f); // Red semi-transparent for invalid

        // Create preview tiles
        foreach (var pos in shipPositions)
        {
            if (playerBoardTiles.TryGetValue(pos, out GameObject tile))
            {
                // Store original color and change to preview color
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage != null)
                {
                    // Create visual feedback
                    GameObject preview = new GameObject("Preview");
                    preview.transform.SetParent(tile.transform);

                    RectTransform previewRect = preview.AddComponent<RectTransform>();
                    previewRect.anchorMin = Vector2.zero;
                    previewRect.anchorMax = Vector2.one;
                    previewRect.sizeDelta = Vector2.zero;
                    previewRect.localPosition = Vector3.zero;
                    previewRect.localScale = Vector3.one;

                    Image previewImage = preview.AddComponent<Image>();
                    previewImage.color = previewColor;
                    previewImage.raycastTarget = false; // Don't block clicks

                    previewTiles.Add(preview);
                }
            }
        }
    }

    /// <summary>
    /// Clear ship placement preview
    /// </summary>
    public void ClearShipPreview()
    {
        foreach (var preview in previewTiles)
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
        previewTiles.Clear();
        // DON'T reset hoveredTilePosition here - we need it for rotation updates
    }

    /// <summary>
    /// Calculate ship tile positions based on start position and orientation
    /// </summary>
    private List<Vector2Int> CalculateShipPositions(Vector2Int startPos, int length, bool isHorizontal)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // Simple: Horizontal goes right, Vertical goes down
        Vector2Int direction = isHorizontal
            ? new Vector2Int(1, 0)   // Horizontal: Right
            : new Vector2Int(0, 1);  // Vertical: Down

        for (int i = 0; i < length; i++)
        {
            positions.Add(startPos + direction * i);
        }

        return positions;
    }

    /// <summary>
    /// Check if ship placement would be valid
    /// </summary>
    private bool IsValidPlacement(List<Vector2Int> positions)
    {
        if (BattleshipsGameManager.Instance == null) return false;

        HashSet<Vector2Int> activeTiles = BattleshipsGameManager.Instance.GetActiveTiles();

        foreach (var pos in positions)
        {
            // Check if tile is in active tiles (exists on board)
            if (!activeTiles.Contains(pos))
            {
                return false;
            }

            // Check if tile already has a ship (if we can access player board)
            var playerBoard = BattleshipsGameManager.Instance.GetPlayerBoard(localPlayerId);
            if (playerBoard != null && playerBoard.HasShip(pos))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
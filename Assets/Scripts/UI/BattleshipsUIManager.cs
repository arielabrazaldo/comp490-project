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
        if (combatPanel) combatPanel.SetActive(true);
        
        // CRITICAL: Keep BoardPanel active and now show BOTH boards
        if (boardPanel) 
        {
            boardPanel.SetActive(true);
            Debug.Log("? BoardPanel active for combat");
        }
        
        // Ensure player board stays active
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(true);
            Debug.Log("? Player board active for combat");
        }
        
        // NOW activate enemy board for combat phase
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(true);
            Debug.Log("? Enemy board NOW activated for combat phase");
        }
        
        // Set ship status title
        if (shipStatusTitleText != null)
        {
            shipStatusTitleText.text = "<b>Your Fleet Status</b>";
            shipStatusTitleText.richText = true; // Enable bold
        }
        
        // CRITICAL FIX: Show target player panel (contains enemy dropdown)
        if (targetPlayerPanel != null)
        {
            targetPlayerPanel.SetActive(true);
            Debug.Log("? Target player panel activated (dropdown should be visible)");
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
            Debug.Log("? Leave game button activated");
        }
        
        // NEW: Populate enemy player dropdown
        PopulateEnemyPlayerDropdown();
        
        UpdateCombatUI();
    }

    public void ShowGameOverPanel(int winnerId)
    {
        HideAllPanels();
        if (gameOverPanel) gameOverPanel.SetActive(true);
        
        if (winnerText)
        {
            if (winnerId == localPlayerId)
            {
                winnerText.text = "Victory! You Win!";
            }
            else
            {
                winnerText.text = $"Player {winnerId} Wins!";
            }
        }
    }

    public void ShowWaitingPanel(string message)
    {
        HideAllPanels();
        
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
        
        // Create list of enemy player options
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        for (int i = 0; i < totalPlayers; i++)
        {
            // Skip the local player
            if (i == localPlayerId) continue;
            
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
            
            Debug.Log($"? Populated enemy dropdown with {options.Count} enemies. Selected: Player {selectedTargetPlayerId + 1}");
        }
        else
        {
            Debug.LogWarning("?? No enemy players found!");
        }
    }

    /// <summary>
    /// Called when enemy player dropdown selection changes
    /// </summary>
    private void OnEnemyPlayerDropdownChanged(int dropdownIndex)
    {
        // Get the actual enemy player ID from the dropdown index
        selectedTargetPlayerId = GetEnemyPlayerIdFromDropdownIndex(dropdownIndex);
        
        Debug.Log($"?? Enemy target changed to Player {selectedTargetPlayerId + 1} (dropdown index: {dropdownIndex})");
        
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
            
            // This is an enemy - check if it matches our dropdown index
            if (enemyCount == dropdownIndex)
            {
                return i;
            }
            
            enemyCount++;
        }
        
        // Fallback to first non-local player
        return (localPlayerId == 0) ? 1 : 0;
    }

    /// <summary>
    /// Switch the enemy board to show the selected target player's board
    /// </summary>
    private void SwitchEnemyBoard(int targetPlayerId)
    {
        BattleshipsBoardGenerator boardGenerator = FindFirstObjectByType<BattleshipsBoardGenerator>();
        if (boardGenerator == null)
        {
            Debug.LogError("? BattleshipsBoardGenerator not found!");
            return;
        }

        // Get the enemy board for the selected player
        Transform enemyBoardParent = boardGenerator.GetEnemyBoardParent();
        if (enemyBoardParent == null)
        {
            Debug.LogError("? Enemy board parent is null!");
            return;
        }

        // Clear current enemy board tiles
        foreach (Transform child in enemyBoardParent)
        {
            Destroy(child.gameObject);
        }
        enemyBoardTiles.Clear();

        // Regenerate enemy board for the selected target
        // Note: This requires BattleshipsBoardGenerator to have a method to regenerate a specific player's board
        // For now, we'll just log this - you may need to implement board regeneration in BattleshipsBoardGenerator
        Debug.Log($"?? Switching enemy board to Player {targetPlayerId + 1}");
        
        // TODO: Implement board regeneration in BattleshipsBoardGenerator
        // boardGenerator.RegenerateEnemyBoard(targetPlayerId);
        
        // Update visual state of tiles based on attack history
        RefreshEnemyBoardVisuals(targetPlayerId);
    }

    /// <summary>
    /// Refresh the visual state of the enemy board based on attack history
    /// </summary>
    private void RefreshEnemyBoardVisuals(int targetPlayerId)
    {
        if (BattleshipsGameManager.Instance == null) return;

        // Get the target player's board
        var targetBoard = BattleshipsGameManager.Instance.GetPlayerBoard(targetPlayerId);
        if (targetBoard == null)
        {
            Debug.LogWarning($"?? Cannot get board for player {targetPlayerId}");
            return;
        }

        // Update each tile based on hit/miss history
        foreach (var kvp in enemyBoardTiles)
        {
            Vector2Int pos = kvp.Key;
            GameObject tile = kvp.Value;
            
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage == null) continue;

            // Check if this tile has been hit
            if (targetBoard.hits.Contains(pos))
            {
                // Check if it was a hit or miss
                if (targetBoard.HasShip(pos))
                {
                    // Hit - show red
                    tileImage.color = new Color(0.8f, 0.1f, 0.0f, 1.0f); // Dark red
                }
                else
                {
                    // Miss - show lighter blue
                    tileImage.color = new Color(0.0f, 0.6f, 0.9f, 1.0f); // Light blue
                }
            }
            else
            {
                // Not attacked yet - reset to default
                tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Default blue
            }
        }
        
        Debug.Log($"? Refreshed enemy board visuals for Player {targetPlayerId + 1}");
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
                "• <b>Press R</b> to rotate ship orientation\n" +
                "• <b>Press Enter</b> when all ships placed\n" +
                "• Ships rotate automatically if they don't fit";
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
        // Update visual board
        VisualizeAttackResult(position, result, targetId);

        // Show attack result message with color
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
                // Change tile color to light gray - very obvious contrast with blue water
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage) 
                {
                    tileImage.color = new Color(0.75f, 0.75f, 0.75f, 1.0f); // Light gray - much more visible!
                }
            }
        }
    }

    private void VisualizeAttackResult(Vector2Int position, BattleshipsGameManager.AttackResult result, int targetId)
    {
        // Determine which board to update
        Dictionary<Vector2Int, GameObject> targetBoard = (targetId == localPlayerId) ? playerBoardTiles : enemyBoardTiles;

        if (targetBoard.TryGetValue(position, out GameObject tile))
        {
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage)
            {
                switch (result)
                {
                    case BattleshipsGameManager.AttackResult.Miss:
                        tileImage.color = new Color(0.0f, 0.6f, 0.9f, 1.0f); // Lighter blue for miss (splash)
                        break;
                    case BattleshipsGameManager.AttackResult.Hit:
                    case BattleshipsGameManager.AttackResult.Sunk:
                    case BattleshipsGameManager.AttackResult.Eliminated:
                        tileImage.color = new Color(0.8f, 0.1f, 0.0f, 1.0f); // Dark red for hit (fire/damage)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Initialize board tile references (call this after creating the grid)
    /// </summary>
    public void InitializeBoardTiles(Dictionary<Vector2Int, GameObject> playerTiles, Dictionary<Vector2Int, GameObject> enemyTiles)
    {
        playerBoardTiles = playerTiles;
        enemyBoardTiles = enemyTiles;
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
                // Reset to default water color
                tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Default blue
            }
        }
        
        Debug.Log("? Cleared ship visualizations from player board");
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

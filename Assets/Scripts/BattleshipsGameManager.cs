using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// OBSOLETE: Use HybridGameManager with GameRules.CreateBattleshipsRules() instead.
/// This manager is deprecated and will be removed in a future update.
/// All Battleships games should now route through HybridGameManager + Modules.
/// See STANDARD_GAME_LIBRARY_IMPLEMENTATION.md for migration guide.
/// </summary>
[Obsolete("Use HybridGameManager with GameRules.CreateBattleshipsRules() and CustomGameSpawner instead.")]
public class BattleshipsGameManager : NetworkBehaviour
{
    private static BattleshipsGameManager instance;
    public static BattleshipsGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BattleshipsGameManager>();
            }
            return instance;
        }
    }

    #region Game State

    // Shared board layout (all players use same custom board)
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();
    
    // Per-player board states
    private Dictionary<int, PlayerBoardState> playerBoards = new Dictionary<int, PlayerBoardState>();
    
    // Game configuration
    private int boardRows;
    private int boardColumns;
    private int playerCount;
    
    // Current game state
    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<int> targetPlayerForAttack = new NetworkVariable<int>(-1);

    #endregion

    #region Data Structures

    /// <summary>
    /// Represents a player's board state
    /// </summary>
    public class PlayerBoardState
    {
        public int playerId;
        public HashSet<Vector2Int> ships = new HashSet<Vector2Int>();
        public HashSet<Vector2Int> hits = new HashSet<Vector2Int>();
        public HashSet<Vector2Int> misses = new HashSet<Vector2Int>();
        public List<Ship> shipList = new List<Ship>();
        public bool hasPlacedAllShips = false;
        public bool isEliminated = false;

        public PlayerBoardState(int id)
        {
            playerId = id;
        }

        /// <summary>
        /// Check if a tile is valid (exists in active tiles)
        /// </summary>
        public bool IsValidTile(Vector2Int pos, HashSet<Vector2Int> activeTiles)
        {
            return activeTiles.Contains(pos);
        }

        /// <summary>
        /// Check if a tile was already attacked
        /// </summary>
        public bool WasAttacked(Vector2Int pos)
        {
            return hits.Contains(pos) || misses.Contains(pos);
        }

        /// <summary>
        /// Check if player has a ship at this position
        /// </summary>
        public bool HasShip(Vector2Int pos)
        {
            return ships.Contains(pos);
        }

        /// <summary>
        /// Check if all tiles for a ship placement are valid and empty
        /// </summary>
        public bool CanPlaceShip(Vector2Int start, int length, bool isHorizontal, HashSet<Vector2Int> activeTiles)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            for (int i = 0; i < length; i++)
            {
                Vector2Int pos = isHorizontal 
                    ? new Vector2Int(start.x + i, start.y)
                    : new Vector2Int(start.x, start.y + i);
                
                // Check if tile exists in active tiles
                if (!activeTiles.Contains(pos))
                {
                    Debug.Log($"Cannot place ship: Position {pos} is not an active tile");
                    return false;
                }
                
                // Check if tile already has a ship
                if (ships.Contains(pos))
                {
                    Debug.Log($"Cannot place ship: Position {pos} already has a ship");
                    return false;
                }
                
                positions.Add(pos);
            }
            
            return true;
        }

        /// <summary>
        /// Place a ship on the board
        /// </summary>
        public void PlaceShip(Ship ship)
        {
            foreach (var pos in ship.positions)
            {
                ships.Add(pos);
            }
            shipList.Add(ship);
            Debug.Log($"Player {playerId} placed {ship.shipType} at {ship.positions.Count} positions");
        }

        /// <summary>
        /// Process an attack at the given position
        /// </summary>
        public AttackResult Attack(Vector2Int pos)
        {
            if (ships.Contains(pos))
            {
                hits.Add(pos);
                
                // Check if any ship was sunk
                Ship sunkShip = CheckIfShipSunk(pos);
                if (sunkShip != null)
                {
                    Debug.Log($"Player {playerId}'s {sunkShip.shipType} was sunk!");
                    
                    // Check if all ships are sunk
                    if (AreAllShipsSunk())
                    {
                        isEliminated = true;
                        Debug.Log($"Player {playerId} has been eliminated!");
                        return AttackResult.Eliminated;
                    }
                    
                    return AttackResult.Sunk;
                }
                
                return AttackResult.Hit;
            }
            else
            {
                misses.Add(pos);
                return AttackResult.Miss;
            }
        }

        /// <summary>
        /// Check if the hit at this position sunk a ship
        /// </summary>
        private Ship CheckIfShipSunk(Vector2Int hitPosition)
        {
            foreach (var ship in shipList)
            {
                if (ship.positions.Contains(hitPosition) && ship.IsSunk(hits))
                {
                    return ship;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if all ships are sunk
        /// </summary>
        private bool AreAllShipsSunk()
        {
            return shipList.All(ship => ship.IsSunk(hits));
        }

        /// <summary>
        /// Get the number of remaining (not sunk) ships
        /// </summary>
        public int GetRemainingShipCount()
        {
            return shipList.Count(ship => !ship.IsSunk(hits));
        }
    }

    /// <summary>
    /// Represents a ship on the board
    /// </summary>
    public class Ship
    {
        public List<Vector2Int> positions;
        public ShipType shipType;
        public int length;

        public Ship(ShipType type, List<Vector2Int> shipPositions)
        {
            shipType = type;
            positions = new List<Vector2Int>(shipPositions);
            length = positions.Count;
        }

        /// <summary>
        /// Check if this ship is completely sunk
        /// </summary>
        public bool IsSunk(HashSet<Vector2Int> hits)
        {
            return positions.All(pos => hits.Contains(pos));
        }

        /// <summary>
        /// Get the number of hits on this ship
        /// </summary>
        public int GetHitCount(HashSet<Vector2Int> hits)
        {
            return positions.Count(pos => hits.Contains(pos));
        }
    }

    #endregion

    #region Enums

    public enum GameState
    {
        WaitingToStart,
        PlacingShips,
        InProgress,
        GameOver
    }

    public enum AttackResult
    {
        Miss,
        Hit,
        Sunk,
        Eliminated
    }

    public enum ShipType
    {
        Carrier,      // 5 tiles
        Battleship,   // 4 tiles
        Cruiser,      // 3 tiles
        Submarine,    // 3 tiles
        Destroyer     // 2 tiles
    }

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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("BattleshipsGameManager spawned on network");
        
        // Subscribe to game state changes
        gameState.OnValueChanged += OnGameStateChanged;
        
        // Subscribe to turn changes
        currentPlayerTurn.OnValueChanged += OnCurrentTurnChanged;
        
        // Notify UI of initial state
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnGameStateChanged(gameState.Value);
            // Also update turn display immediately
            UpdateCombatUI();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Unsubscribe from changes
        gameState.OnValueChanged -= OnGameStateChanged;
        currentPlayerTurn.OnValueChanged -= OnCurrentTurnChanged;
    }
    
    /// <summary>
    /// Called when current turn changes on network
    /// </summary>
    private void OnCurrentTurnChanged(int previousTurn, int newTurn)
    {
        Debug.Log($"Turn changed from Player {previousTurn} to Player {newTurn}");
        
        // Update UI immediately
        UpdateCombatUI();
    }
    
    /// <summary>
    /// Update combat UI with current turn info
    /// </summary>
    private void UpdateCombatUI()
    {
        if (BattleshipsUIManager.Instance != null && gameState.Value == GameState.InProgress)
        {
            BattleshipsUIManager.Instance.UpdateCombatUI();
        }
    }

    /// <summary>
    /// Called when game state changes on network
    /// </summary>
    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        Debug.Log($"Game state changed from {previousState} to {newState}");
        
        // CRITICAL FIX: Show game panel on all clients when entering PlacingShips state
        if (newState == GameState.PlacingShips)
        {
            Debug.Log("Game entering PlacingShips state - showing game panel on all clients");
            
            // Add delay on clients to ensure board configuration is received
            if (!IsServer && UIManager.Instance != null)
            {
                Debug.Log("[Client] Waiting for board configuration before showing UI...");
                // Increased delay to 1.0 second to ensure RPC is received
                StartCoroutine(ShowGamePanelAfterDelay(1.0f));
            }
            else if (UIManager.Instance != null)
            {
                // Host can show immediately
                UIManager.Instance.OnNetworkGameStarted();
            }
        }
        
        // Notify UI
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnGameStateChanged(newState);
        }
    }

    /// <summary>
    /// Show game panel after a short delay (for clients to receive board config)
    /// </summary>
    private System.Collections.IEnumerator ShowGamePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Double-check that board configuration has been received
        if (activeTiles.Count == 0)
        {
            Debug.LogWarning("[Client] Board configuration not received yet - waiting longer...");
            yield return new WaitForSeconds(0.5f); // Wait another half second
        }
        
        if (UIManager.Instance != null)
        {
            Debug.Log($"[Client] Board configuration ready ({activeTiles.Count} tiles) - showing game panel now");
            UIManager.Instance.OnNetworkGameStarted();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the game with board configuration from BattleshipsSetupManager
    /// </summary>
    public void InitializeGame(int numPlayers, int rows, int cols, bool[,] tileStates)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can initialize the game!");
            return;
        }

        playerCount = numPlayers;
        boardRows = rows;
        boardColumns = cols;

        Debug.Log($"Initializing Battleships game: {numPlayers} players, {rows}x{cols} board");

        // Convert tileStates array to HashSet of active tiles
        activeTiles.Clear();
        int inactiveTileCount = 0; // FIXED: Declare at method scope
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (tileStates[row, col])
                {
                    activeTiles.Add(new Vector2Int(col, row));
                }
                else
                {
                    inactiveTileCount++;
                    // Log first few inactive tiles for verification
                    if (inactiveTileCount <= 5)
                    {
                        Debug.Log($"[Server] Tile ({col}, {row}) is INACTIVE");
                    }
                }
            }
        }

        Debug.Log($"[Server] Board initialized with {activeTiles.Count} active tiles, {inactiveTileCount} inactive tiles");

        // Initialize player board states
        playerBoards.Clear();
        for (int i = 0; i < numPlayers; i++)
        {
            playerBoards[i] = new PlayerBoardState(i);
            Debug.Log($"Initialized board for Player {i}");
        }

        // CRITICAL FIX: Convert 2D array to 1D array for network serialization
        bool[] tileStates1D = new bool[rows * cols];
        int active1DCount = 0;
        int inactive1DCount = 0;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                tileStates1D[index] = tileStates[row, col];
                
                if (tileStates[row, col])
                {
                    active1DCount++;
                }
                else
                {
                    inactive1DCount++;
                }
            }
        }

        Debug.Log($"[Server] 1D array created: {active1DCount} active, {inactive1DCount} inactive (total: {tileStates1D.Length})");
        Debug.Log($"[Server] Sending board config to clients: {numPlayers} players, {rows}x{cols}, {activeTiles.Count} tiles");
        
        // Send board configuration to all clients using 1D array
        SyncBoardConfigurationToClientsClientRpc(numPlayers, rows, cols, tileStates1D);

        // Set game state to ship placement phase
        gameState.Value = GameState.PlacingShips;
        currentPlayerTurn.Value = 0;

        Debug.Log("Game initialized - Players can now place their ships");
    }

    /// <summary>
    /// Sync board configuration to all clients (called by server)
    /// FIXED: Uses 1D array instead of 2D array for proper network serialization
    /// </summary>
    [ClientRpc]
    private void SyncBoardConfigurationToClientsClientRpc(int numPlayers, int rows, int cols, bool[] tileStates1D)
    {
        // Skip if we're the server (already initialized)
        if (IsServer)
        {
            Debug.Log("[Server] Skipping ClientRpc - already initialized");
            return;
        }

        Debug.Log($"[Client] ? Received board configuration: {numPlayers} players, {rows}x{cols} board");
        Debug.Log($"[Client] Received 1D array with {tileStates1D.Length} elements");

        // Set board configuration on client
        playerCount = numPlayers;
        boardRows = rows;
        boardColumns = cols;

        // Convert 1D array back to HashSet of active tiles
        activeTiles.Clear();
        int activeTileCount = 0;
        int inactiveTileCount = 0; // FIXED: Added declaration
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                if (index < tileStates1D.Length)
                {
                    if (tileStates1D[index])
                    {
                        activeTiles.Add(new Vector2Int(col, row));
                        activeTileCount++;
                    }
                    else
                    {
                        inactiveTileCount++;
                        // Log first few inactive tiles for verification
                        if (inactiveTileCount <= 5)
                        {
                            Debug.Log($"[Client] Tile ({col}, {row}) is INACTIVE (index {index})");
                        }
                    }
                }
            }
        }

        Debug.Log($"[Client] ? Board configuration synced: {activeTiles.Count} active tiles, {inactiveTileCount} inactive tiles");
        Debug.Log($"[Client] First few active tiles: {string.Join(", ", System.Linq.Enumerable.Take(activeTiles, 5))}");

        // Initialize player board states on client
        playerBoards.Clear();
        for (int i = 0; i < numPlayers; i++)
        {
            playerBoards[i] = new PlayerBoardState(i);
        }

        Debug.Log("[Client] ? Board configuration complete - ready for ship placement");
    }

    /// <summary>
    /// Initialize with default board (all tiles active)
    /// </summary>
    public void InitializeGameWithDefaultBoard(int numPlayers, int rows, int cols)
    {
        bool[,] defaultBoard = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                defaultBoard[r, c] = true;
            }
        }

        Debug.Log($"Initializing with default {rows}x{cols} board (all tiles active)");
        InitializeGame(numPlayers, rows, cols, defaultBoard);
    }

    #endregion

    #region Ship Placement

    /// <summary>
    /// Place a ship for a player
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlaceShipServerRpc(int playerId, int shipTypeInt, Vector2Int startPos, bool isHorizontal, ServerRpcParams rpcParams = default)
    {
        ShipType shipType = (ShipType)shipTypeInt;
        int length = GetShipLength(shipType);

        Debug.Log($"Player {playerId} attempting to place {shipType} ({length} tiles) at {startPos}, horizontal: {isHorizontal}");

        if (!playerBoards.ContainsKey(playerId))
        {
            Debug.LogError($"Player {playerId} board not found!");
            return;
        }

        PlayerBoardState playerBoard = playerBoards[playerId];

        // Generate ship positions
        List<Vector2Int> shipPositions = new List<Vector2Int>();
        for (int i = 0; i < length; i++)
        {
            Vector2Int pos = isHorizontal 
                ? new Vector2Int(startPos.x + i, startPos.y)
                : new Vector2Int(startPos.x, startPos.y + i);
            shipPositions.Add(pos);
        }

        // Validate ship placement
        if (!playerBoard.CanPlaceShip(startPos, length, isHorizontal, activeTiles))
        {
            Debug.LogWarning($"Cannot place {shipType} for Player {playerId} at {startPos}");
            NotifyShipPlacementFailedClientRpc(playerId);
            return;
        }

        // Place the ship
        Ship ship = new Ship(shipType, shipPositions);
        playerBoard.PlaceShip(ship);

        // Notify client of successful placement
        NotifyShipPlacedClientRpc(playerId, shipTypeInt, startPos, isHorizontal);

        // Check if player has placed all required ships
        CheckIfPlayerFinishedPlacingShips(playerId);
    }

    /// <summary>
    /// Get the length of a ship based on its type
    /// </summary>
    private int GetShipLength(ShipType type)
    {
        switch (type)
        {
            case ShipType.Carrier: return 5;
            case ShipType.Battleship: return 4;
            case ShipType.Cruiser: return 3;
            case ShipType.Submarine: return 3;
            case ShipType.Destroyer: return 2;
            default: return 3;
        }
    }

    /// <summary>
    /// Check if a player has placed all required ships
    /// </summary>
    private void CheckIfPlayerFinishedPlacingShips(int playerId)
    {
        if (!playerBoards.ContainsKey(playerId)) return;

        PlayerBoardState playerBoard = playerBoards[playerId];

        // Standard Battleship has 5 ships
        int requiredShips = 5;
        
        if (playerBoard.shipList.Count >= requiredShips)
        {
            playerBoard.hasPlacedAllShips = true;
            Debug.Log($"Player {playerId} has finished placing all ships ({playerBoard.shipList.Count}/{requiredShips})");

            // Notify the client to show waiting panel and hide their board
            NotifyPlayerFinishedPlacementClientRpc(playerId);

            // Check if all players have finished
            if (playerBoards.Values.All(board => board.hasPlacedAllShips))
            {
                StartGamePhase();
            }
        }
    }

    /// <summary>
    /// Start the main game phase after all players have placed ships
    /// </summary>
    private void StartGamePhase()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can start game phase!");
            return;
        }
        
        gameState.Value = GameState.InProgress;
        currentPlayerTurn.Value = 0;
        
        Debug.Log("[Server] All players have placed their ships - Starting combat phase!");
        
        // Notify all clients to show combat UI
        NotifyGameStartedClientRpc();
    }

    #endregion

    #region Combat

    /// <summary>
    /// Player attacks a tile on target player's board
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AttackTileServerRpc(int attackingPlayerId, int targetPlayerId, Vector2Int position, ServerRpcParams rpcParams = default)
    {
        if (gameState.Value != GameState.InProgress)
        {
            Debug.LogWarning("Cannot attack - game not in progress!");
            return;
        }

        if (currentPlayerTurn.Value != attackingPlayerId)
        {
            Debug.LogWarning($"Player {attackingPlayerId} tried to attack but it's not their turn!");
            return;
        }

        if (!playerBoards.ContainsKey(targetPlayerId))
        {
            Debug.LogError($"Target player {targetPlayerId} board not found!");
            return;
        }

        PlayerBoardState targetBoard = playerBoards[targetPlayerId];

        // Check if tile is valid
        if (!activeTiles.Contains(position))
        {
            Debug.LogWarning($"Invalid attack position: {position} is not an active tile");
            return;
        }

        // Check if tile was already attacked
        if (targetBoard.WasAttacked(position))
        {
            Debug.LogWarning($"Tile {position} was already attacked!");
            return;
        }

        // Process the attack
        AttackResult result = targetBoard.Attack(position);

        Debug.Log($"Player {attackingPlayerId} attacked Player {targetPlayerId} at {position} - Result: {result}");

        // Notify all clients of the attack result
        NotifyAttackResultClientRpc(attackingPlayerId, targetPlayerId, position, (int)result);

        // CRITICAL FIX: Send updated board state to the target player so they can update ship status
        if (result == AttackResult.Hit || result == AttackResult.Sunk || result == AttackResult.Eliminated)
        {
            // Convert hits HashSet to array for network serialization
            Vector2Int[] hitsArray = new Vector2Int[targetBoard.hits.Count];
            targetBoard.hits.CopyTo(hitsArray);
            
            Debug.Log($"[Server] Sending {hitsArray.Length} hits to Player {targetPlayerId} for ship status update");
            
            // Send only to the target player
            SyncPlayerBoardHitsClientRpc(targetPlayerId, hitsArray, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { (ulong)targetPlayerId }
                }
            });
        }

        // NEW: Notify all clients if a player was eliminated
        if (result == AttackResult.Eliminated)
        {
            Debug.Log($"[Server] Player {targetPlayerId} has been eliminated - notifying all clients");
            NotifyPlayerEliminatedClientRpc(targetPlayerId);
            CheckForGameOver();
        }

        // Next player's turn
        AdvanceTurn();
    }

    /// <summary>
    /// Advance to the next player's turn
    /// </summary>
    private void AdvanceTurn()
    {
        if (gameState.Value != GameState.InProgress) return;

        int nextPlayer = (currentPlayerTurn.Value + 1) % playerCount;
        
        // Skip eliminated players
        int attempts = 0;
        while (playerBoards[nextPlayer].isEliminated && attempts < playerCount)
        {
            nextPlayer = (nextPlayer + 1) % playerCount;
            attempts++;
        }

        currentPlayerTurn.Value = nextPlayer;
        Debug.Log($"Turn advanced to Player {nextPlayer}");
    }

    /// <summary>
    /// Check if the game is over (only one player remaining)
    /// </summary>
    private void CheckForGameOver()
    {
        int remainingPlayers = playerBoards.Values.Count(board => !board.isEliminated);

        if (remainingPlayers <= 1)
        {
            gameState.Value = GameState.GameOver;
            
            // Find the winner
            int winnerId = playerBoards.First(kvp => !kvp.Value.isEliminated).Key;
            
            Debug.Log($"Game Over! Player {winnerId} wins!");
            NotifyGameOverClientRpc(winnerId);
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event fired when a player is eliminated from the game
    /// </summary>
    public static event System.Action<int> OnPlayerEliminated;

    #endregion

    #region Client RPCs (Network Messages)

    [ClientRpc]
    private void NotifyShipPlacedClientRpc(int playerId, int shipTypeInt, Vector2Int startPos, bool isHorizontal)
    {
        Debug.Log($"[{(IsServer ? "Server" : "Client")}] NotifyShipPlacedClientRpc called for Player {playerId}, ship: {(ShipType)shipTypeInt}");
        
        // CRITICAL FIX: Skip GAME LOGIC on server (already done in ServerRpc)
        // But allow UI updates to proceed for host
        if (IsServer)
        {
            Debug.Log($"[Server] Skipping ship data storage (already done in ServerRpc) - proceeding to UI update only");
        }
        else
        {
            // CLIENTS ONLY: Store ship data locally for ship status tracking
            if (playerBoards.ContainsKey(playerId))
            {
                ShipType shipType = (ShipType)shipTypeInt;
                int length = GetShipLength(shipType);
                
                // Generate ship positions
                List<Vector2Int> shipPositions = new List<Vector2Int>();
                for (int i = 0; i < length; i++)
                {
                    Vector2Int pos = isHorizontal 
                        ? new Vector2Int(startPos.x + i, startPos.y)
                        : new Vector2Int(startPos.x, startPos.y + i);
                    shipPositions.Add(pos);
                }
                
                // Create ship object and add to local board
                Ship ship = new Ship(shipType, shipPositions);
                playerBoards[playerId].PlaceShip(ship);
                
                Debug.Log($"[Client] Stored ship data locally for Player {playerId}: {shipType} with {shipPositions.Count} positions");
            }
            else
            {
                Debug.LogWarning($"[Client] Cannot store ship data - Player {playerId} board not found locally");
            }
        }
        
        // IMPORTANT: Update UI for BOTH server (host) and clients
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnShipPlaced(playerId, (ShipType)shipTypeInt, startPos, isHorizontal);
            Debug.Log($"[{(IsServer ? "Server/Host" : "Client")}] UI updated for Player {playerId} ship placement");
        }
        else
        {
            Debug.LogWarning($"[{(IsServer ? "Server/Host" : "Client")}] BattleshipsUIManager.Instance is null - cannot update UI");
        }
    }

    [ClientRpc]
    private void NotifyShipPlacementFailedClientRpc(int playerId)
    {
        Debug.LogWarning($"[Client] Ship placement failed for Player {playerId}");
        
        // Update UI
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnShipPlacementFailed(playerId);
        }
    }

    [ClientRpc]
    private void NotifyPlayerFinishedPlacementClientRpc(int playerId)
    {
        Debug.Log($"[Client] Player {playerId} has finished placing all ships");
        
        // Only update UI for the player who finished
        int localPlayerId = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClient != null
            ? (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId
            : 0;
            
        if (playerId == localPlayerId && BattleshipsUIManager.Instance != null)
        {
            // Show waiting panel for this player
            BattleshipsUIManager.Instance.ShowWaitingPanel("All ships placed! Waiting for other players...");
            Debug.Log($"[Client] Showing waiting panel for Player {playerId}");
        }
    }

    [ClientRpc]
    private void NotifyGameStartedClientRpc()
    {
        Debug.Log($"[{(IsServer ? "Server/Host" : "Client")}] Combat phase starting - all players finished placing ships");
        
        // Update UI to combat phase for ALL clients (including host)
        if (BattleshipsUIManager.Instance != null)
        {
            Debug.Log($"[{(IsServer ? "Server/Host" : "Client")}] Showing combat panel via OnGameStateChanged");
            BattleshipsUIManager.Instance.OnGameStateChanged(GameState.InProgress);
        }
        else
        {
            Debug.LogError($"[{(IsServer ? "Server/Host" : "Client")}] BattleshipsUIManager.Instance is null - cannot show combat UI!");
        }
    }

    [ClientRpc]
    private void NotifyAttackResultClientRpc(int attackerId, int targetId, Vector2Int position, int resultInt)
    {
        AttackResult result = (AttackResult)resultInt;
        Debug.Log($"[Client] Player {attackerId} attacked Player {targetId} at {position} - {result}");
        
        // Update UI with attack result
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnAttackResult(attackerId, targetId, position, result);
        }
    }

    [ClientRpc]
    private void NotifyGameOverClientRpc(int winnerId)
    {
        Debug.Log($"[Client] Game Over! Player {winnerId} wins!");
        
        // Update UI to show game over screen
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnGameOver(winnerId);
        }
    }
    
    /// <summary>
    /// Notify all clients that a player has been eliminated
    /// </summary>
    [ClientRpc]
    private void NotifyPlayerEliminatedClientRpc(int eliminatedPlayerId)
    {
        Debug.Log($"[Client] Player {eliminatedPlayerId} has been eliminated from the game");
        
        // CRITICAL FIX: Update the eliminated player's board state on ALL clients
        if (playerBoards.ContainsKey(eliminatedPlayerId))
        {
            playerBoards[eliminatedPlayerId].isEliminated = true;
            Debug.Log($"[Client] Marked Player {eliminatedPlayerId} as eliminated locally");
        }
        else
        {
            Debug.LogError($"[Client] Cannot mark Player {eliminatedPlayerId} as eliminated - board not found locally!");
        }
        
        // Fire the static event so UI can respond
        OnPlayerEliminated?.Invoke(eliminatedPlayerId);
        
        // Update UI to remove this player from targeting options
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnPlayerEliminated(eliminatedPlayerId);
        }
    }
    
    /// <summary>
    /// Client requests the board state for a specific player (used when switching targets)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestBoardStateServerRpc(int requestingPlayerId, int targetPlayerId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can handle board state requests!");
            return;
        }

        Debug.Log($"[Server] Player {requestingPlayerId} requested board state for Player {targetPlayerId}");

        if (!playerBoards.ContainsKey(targetPlayerId))
        {
            Debug.LogError($"[Server] Target player {targetPlayerId} board not found!");
            return;
        }

        PlayerBoardState targetBoard = playerBoards[targetPlayerId];

        // Convert HashSets to arrays for network serialization
        Vector2Int[] hitsArray = new Vector2Int[targetBoard.hits.Count];
        targetBoard.hits.CopyTo(hitsArray);

        Vector2Int[] missesArray = new Vector2Int[targetBoard.misses.Count];
        targetBoard.misses.CopyTo(missesArray);

        Debug.Log($"[Server] Sending board state: {hitsArray.Length} hits, {missesArray.Length} misses");

        // Send board state back to the requesting client
        SendBoardStateClientRpc(targetPlayerId, hitsArray, missesArray, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { (ulong)requestingPlayerId }
            }
        });
    }

    /// <summary>
    /// Server sends board state to a specific client
    /// </summary>
    [ClientRpc]
    private void SendBoardStateClientRpc(int targetPlayerId, Vector2Int[] hits, Vector2Int[] misses, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Client] Received board state for Player {targetPlayerId}: {hits.Length} hits, {misses.Length} misses");

        if (!playerBoards.ContainsKey(targetPlayerId))
        {
            Debug.LogError($"[Client] Target player {targetPlayerId} board not found locally!");
            return;
        }

        PlayerBoardState targetBoard = playerBoards[targetPlayerId];

        // Update the board state with received data
        targetBoard.hits.Clear();
        foreach (var hit in hits)
        {
            targetBoard.hits.Add(hit);
        }

        targetBoard.misses.Clear();
        foreach (var miss in misses)
        {
            targetBoard.misses.Add(miss);
        }

        Debug.Log($"[Client] Updated board state for Player {targetPlayerId}");

        // Notify UI to refresh if this is the currently viewed enemy
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.OnBoardStateReceived(targetPlayerId);
        }
    }
    
    /// <summary>
    /// Sync player's own board hits to the client (called when they are attacked)
    /// This allows the client to update their ship status display
    /// </summary>
    [ClientRpc]
    private void SyncPlayerBoardHitsClientRpc(int playerId, Vector2Int[] hits, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Client] Received {hits.Length} hits for my board (Player {playerId})");
        
        if (!playerBoards.ContainsKey(playerId))
        {
            Debug.LogWarning($"[Client] Player {playerId} board not found locally - creating it");
            playerBoards[playerId] = new PlayerBoardState(playerId);
        }
        
        PlayerBoardState playerBoard = playerBoards[playerId];
        
        // Update the hits on the client's local board state
        playerBoard.hits.Clear();
        foreach (var hit in hits)
        {
            playerBoard.hits.Add(hit);
        }
        
        Debug.Log($"[Client] Updated my board with {playerBoard.hits.Count} hits");
        
        // Update the UI ship status display
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.UpdateCombatUI();
        }
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Get the current game state
    /// </summary>
    public GameState GetGameState() => gameState.Value;

    /// <summary>
    /// Get the current player's turn
    /// </summary>
    public int GetCurrentTurn() => currentPlayerTurn.Value;

    /// <summary>
    /// Check if it's the local player's turn
    /// </summary>
    public bool IsMyTurn()
    {
        // TODO: Get local player ID from network
        return true; // Placeholder
    }

    /// <summary>
    /// Get the set of active tiles
    /// </summary>
    public HashSet<Vector2Int> GetActiveTiles() => new HashSet<Vector2Int>(activeTiles);

    /// <summary>
    /// Get a player's board state (only if you're that player or game is over)
    /// </summary>
    public PlayerBoardState GetPlayerBoard(int playerId)
    {
        if (playerBoards.ContainsKey(playerId))
        {
            return playerBoards[playerId];
        }
        return null;
    }

    /// <summary>
    /// Check if a tile is active on the board
    /// </summary>
    public bool IsTileActive(Vector2Int position)
    {
        return activeTiles.Contains(position);
    }

    /// <summary>
    /// Get board dimensions
    /// </summary>
    public (int rows, int cols) GetBoardDimensions() => (boardRows, boardColumns);

    /// <summary>
    /// Get the total number of players in the game
    /// </summary>
    public int GetTotalPlayers() => playerCount;
    
    /// <summary>
    /// Check if a player is eliminated
    /// </summary>
    public bool IsPlayerEliminated(int playerId)
    {
        if (playerBoards.ContainsKey(playerId))
        {
            return playerBoards[playerId].isEliminated;
        }
        return false;
    }
    
    /// <summary>
    /// Get list of alive (non-eliminated) player IDs
    /// </summary>
    public List<int> GetAlivePlayers()
    {
        List<int> alivePlayers = new List<int>();
        for (int i = 0; i < playerCount; i++)
        {
            if (playerBoards.ContainsKey(i) && !playerBoards[i].isEliminated)
            {
                alivePlayers.Add(i);
            }
        }
        return alivePlayers;
    }

    #endregion

    #region Debug / Testing

    [ContextMenu("Print Game State")]
    private void PrintGameState()
    {
        Debug.Log("=== Battleships Game State ===");
        Debug.Log($"Game State: {gameState.Value}");
        Debug.Log($"Current Turn: {currentPlayerTurn.Value}");
        Debug.Log($"Board: {boardRows}x{boardColumns} with {activeTiles.Count} active tiles");
        Debug.Log($"Players: {playerCount}");

        foreach (var kvp in playerBoards)
        {
            var board = kvp.Value;
            Debug.Log($"Player {kvp.Key}: Ships={board.shipList.Count}, Remaining={board.GetRemainingShipCount()}, Eliminated={board.isEliminated}");
        }
    }
    
    [ContextMenu("Verify Player Boards Initialized")]
    private void VerifyPlayerBoards()
    {
        Debug.Log("=== Verifying Player Board Data ===");
        Debug.Log($"Total Players: {playerCount}");
        Debug.Log($"PlayerBoards Dictionary Count: {playerBoards.Count}");
        
        for (int i = 0; i < playerCount; i++)
        {
            if (playerBoards.ContainsKey(i))
            {
                var board = playerBoards[i];
                Debug.Log($"? Player {i}: EXISTS - Ships={board.ships.Count}, Hits={board.hits.Count}, Misses={board.misses.Count}");
            }
            else
            {
                Debug.LogError($"? Player {i}: MISSING FROM DICTIONARY!");
            }
        }
        
        // Check for extra entries
        foreach (var kvp in playerBoards)
        {
            if (kvp.Key >= playerCount)
            {
                Debug.LogWarning($"?? Extra player board found: Player {kvp.Key} (playerCount={playerCount})");
            }
        }
    }

    #endregion
}

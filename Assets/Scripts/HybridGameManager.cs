using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Hybrid Game Manager that dynamically composes functionality from existing game managers
/// based on configured custom rules. Supports mixing features from Monopoly, Battleships, and Dice Race.
/// </summary>
public class HybridGameManager : NetworkBehaviour
{
    private static HybridGameManager instance;
    public static HybridGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HybridGameManager>();
            }
            return instance;
        }
    }

    #region Network Variables

    private NetworkVariable<int> currentPlayerId = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(2);

    #endregion

    #region Game State

    public enum GameState
    {
        WaitingToStart,
        InProgress,
        GameOver
    }

    /// <summary>
    /// Active game rules configuration
    /// </summary>
    private GameRules activeRules;

    /// <summary>
    /// Active game modules
    /// </summary>
    private HybridCurrencyModule currencyModule;
    private HybridBoardModule boardModule;
    private HybridPropertyModule propertyModule;
    private HybridCombatModule combatModule;
    private HybridMovementModule movementModule;

    /// <summary>
    /// Player data for hybrid games
    /// </summary>
    private List<HybridPlayerData> players = new List<HybridPlayerData>();

    #endregion

    #region Events

    public static event System.Action OnGameStarted;
    public static event System.Action<int> OnPlayerTurnChanged;
    public static event System.Action<int, int> OnPlayerMoved;
    public static event System.Action<string> OnGameMessage;
    public static event System.Action<GameState> OnGameStateChanged;
    /// <summary>Fired after movement when combat is enabled. Carries IDs of enemies in range (may be empty).</summary>
    public static event System.Action<List<int>> OnCombatAvailable;

    #endregion

    #region Initialization

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
        
        if (IsServer)
        {
            gameState.Value = GameState.WaitingToStart;
        }
        
        // CRITICAL: Use OnValueChanged so turn notifications always fire AFTER the
        // NetworkVariable has been updated on this client. RPCs have no ordering
        // guarantee relative to NetworkVariable syncs, so reading currentPlayerId.Value
        // inside an RPC callback can return the stale previous value.
        currentPlayerId.OnValueChanged += (prev, next) => OnPlayerTurnChanged?.Invoke(next);
        
        Debug.Log("[HybridGameManager] Network spawned");
    }

    /// <summary>
    /// Initialize hybrid game with custom rules
    /// </summary>
    public void InitializeGame(int numPlayers, GameRules rules, string gameName = "")
    {
        if (!IsServer)
        {
            Debug.LogError("[HybridGameManager] Only server can initialize game!");
            return;
        }

        Debug.Log($"[HybridGameManager] Initializing hybrid game with {numPlayers} players");
        Debug.Log($"[HybridGameManager] Rules: {rules.GetRulesSummary()}");

        // Store configuration
        playerCount.Value = numPlayers;
        activeRules = rules;

        // Initialize modules based on rules
        InitializeModules();

        // Create player data
        InitializePlayers(numPlayers);

        // Start game
        string rulesJson = JsonUtility.ToJson(rules);
        
        // CRITICAL: Set state BEFORE sending RPC so clients read InProgress when UI initialises
        gameState.Value = GameState.InProgress;
        OnGameStarted?.Invoke();
        OnGameStateChanged?.Invoke(GameState.InProgress);
        
        StartGameClientRpc(rulesJson, gameName);

        Debug.Log("[HybridGameManager] Hybrid game initialized and started!");
    }

    /// <summary>
    /// Initialize game modules based on active rules
    /// </summary>
    private void InitializeModules()
    {
        Debug.Log("[HybridGameManager] Initializing modules based on rules...");

        // Currency Module (from Monopoly)
        if (activeRules.enableCurrency)
        {
            currencyModule = gameObject.AddComponent<HybridCurrencyModule>();
            currencyModule.Initialize(activeRules.startingMoney);
            Debug.Log($"[HybridGameManager] ? Currency Module enabled (starting: ${activeRules.startingMoney})");
        }

        // Board Module (from all games)
        boardModule = gameObject.AddComponent<HybridBoardModule>();
        boardModule.Initialize(activeRules);
        Debug.Log($"[HybridGameManager] ? Board Module enabled (separate boards: {activeRules.separatePlayerBoards})");

        // Property Module (from Monopoly)
        if (activeRules.canPurchaseProperties || activeRules.enablePropertyTrading)
        {
            propertyModule = gameObject.AddComponent<HybridPropertyModule>();
            propertyModule.Initialize(activeRules, currencyModule);
            Debug.Log("[HybridGameManager] ? Property Module enabled");
        }

        // Combat Module (from Battleships)
        if (activeRules.enableCombat)
        {
            combatModule = gameObject.AddComponent<HybridCombatModule>();
            combatModule.Initialize(activeRules);
            Debug.Log("[HybridGameManager] ? Combat Module enabled");
        }

        // Movement Module (from all games)
        movementModule = gameObject.AddComponent<HybridMovementModule>();
        movementModule.Initialize(activeRules);
        Debug.Log("[HybridGameManager] ? Movement Module enabled");
    }

    /// <summary>
    /// Initialize player data
    /// </summary>
    private void InitializePlayers(int numPlayers)
    {
        players.Clear();
        
        for (int i = 0; i < numPlayers; i++)
        {
            var playerData = new HybridPlayerData
            {
                playerId = i,
                position = 0,
                isActive = true
            };

            // Add currency if enabled
            if (currencyModule != null)
            {
                playerData.money = activeRules.startingMoney;
            }

            players.Add(playerData);
            Debug.Log($"[HybridGameManager] Initialized player {i}");
        }
    }

    [ClientRpc]
    private void StartGameClientRpc(string rulesJson, string gameName)
    {
        Debug.Log("[HybridGameManager] Game started on client");

        // Sync rules and game info into RuleEditorManager so HybridUIManager can read them
        if (!IsServer)
        {
            GameRules syncedRules = JsonUtility.FromJson<GameRules>(rulesJson);
            if (syncedRules != null)
            {
                activeRules = syncedRules;
                if (RuleEditorManager.Instance != null)
                {
                    var gameInfo = new SavedGameInfo(gameName, 3, playerCount.Value, syncedRules, isStandardGame: false);
                    RuleEditorManager.Instance.SetCurrentGameInfo(gameInfo);
                    RuleEditorManager.Instance.SetRules(syncedRules);
                    Debug.Log($"[HybridGameManager] Client synced rules for '{gameName}'");
                }
            }
        }

        OnGameStarted?.Invoke();

        // Ensure UI knows game is in progress (NetworkVariable may not have synced yet on clients)
        OnGameStateChanged?.Invoke(GameState.InProgress);
    }

    #endregion

    #region Turn Management

    /// <summary>
    /// Roll dice for current player (called by NetworkGameManager or MonopolyGameManager logic)
    /// </summary>
    public void RollDice()
    {
        if (!IsServer)
        {
            RequestRollDiceServerRpc();
            return;
        }

        if (gameState.Value != GameState.InProgress)
        {
            Debug.LogWarning("[HybridGameManager] Cannot roll dice - game not in progress");
            return;
        }

        int myPlayerId = GetLocalPlayerId();
        if (currentPlayerId.Value != myPlayerId)
        {
            Debug.LogWarning($"[HybridGameManager] Not your turn! Current: {currentPlayerId.Value}, You: {myPlayerId}");
            return;
        }

        ProcessTurn(myPlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRollDiceServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerId = (int)senderId;

        if (currentPlayerId.Value == playerId && gameState.Value == GameState.InProgress)
        {
            ProcessTurn(playerId);
        }
    }

    /// <summary>
    /// Process a player's turn
    /// </summary>
    private void ProcessTurn(int playerId)
    {
        Debug.Log($"[HybridGameManager] Processing turn for player {playerId}");

        // Roll dice (using movement module)
        int diceRoll = Random.Range(1, 7);
        Debug.Log($"[HybridGameManager] Player {playerId} rolled {diceRoll}");

        // Move player
        if (movementModule != null)
        {
            int newPosition = movementModule.MovePlayer(playerId, players[playerId].position, diceRoll, activeRules);
            players[playerId].position = newPosition;
            
            OnPlayerMoved?.Invoke(playerId, newPosition);
            UpdatePlayerPositionClientRpc(playerId, newPosition, diceRoll);
            
            Debug.Log($"[HybridGameManager] Player {playerId} moved to position {newPosition}");
        }

        // Process space landed on
        ProcessSpaceLanded(playerId);

        // Check win condition
        if (CheckWinCondition(playerId))
        {
            EndGame(playerId);
            return;
        }

        // When combat is enabled, handle based on range setting
        if (activeRules.enableCombat && combatModule != null)
        {
            List<int> enemies = combatModule.GetEnemiesInRange(
                playerId, players[playerId].position, players, activeRules.combatRange);

            if (activeRules.combatRange == 0)
            {
                // Range 0: auto-resolve combat immediately when landing on an occupied tile
                foreach (int enemyId in enemies)
                {
                    combatModule.Attack(playerId, enemyId, players);
                    if (CheckWinCondition(playerId)) { EndGame(playerId); return; }
                }
                // Notify clients that auto-combat resolved (UI shows result via OnGameMessage)
                NotifyAutoCombatClientRpc(playerId, enemies.ToArray());
                AdvanceTurn();
                return;
            }

            // Range > 0: pause for player decision (attack a highlighted token or end turn)
            OnCombatAvailable?.Invoke(enemies);
            NotifyCombatAvailableClientRpc(enemies.ToArray());
            return; // Turn advances via EndTurn() or AttackPlayer()
        }

        // Next turn (non-combat games)
        AdvanceTurn();
    }

    /// <summary>
    /// Process space that player landed on
    /// </summary>
    private void ProcessSpaceLanded(int playerId)
    {
        int position = players[playerId].position;
        
        // Property space (if property module active)
        if (propertyModule != null)
        {
            propertyModule.ProcessPropertySpace(playerId, position, players);
        }

        // Combat space (if combat module active)
        if (combatModule != null)
        {
            combatModule.ProcessCombatSpace(playerId, position, players);
        }

        // Currency events (if currency module active)
        if (currencyModule != null)
        {
            currencyModule.ProcessCurrencySpace(playerId, position, players);
        }
    }

    /// <summary>
    /// Manually end the current player's turn (used when combat is enabled).
    /// </summary>
    public void EndTurn()
    {
        if (!IsServer) { RequestEndTurnServerRpc(); return; }
        AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        int senderId = (int)rpcParams.Receive.SenderClientId;
        if (currentPlayerId.Value == senderId && gameState.Value == GameState.InProgress)
            AdvanceTurn();
    }

    /// <summary>
    /// Attack a specific opponent then end the turn.
    /// </summary>
    public void AttackPlayer(int targetId)
    {
        if (!IsServer) { RequestAttackServerRpc(targetId); return; }
        int attackerId = currentPlayerId.Value;
        if (combatModule != null)
            combatModule.Attack(attackerId, targetId, players);
        if (CheckWinCondition(attackerId)) { EndGame(attackerId); return; }
        AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAttackServerRpc(int targetId, ServerRpcParams rpcParams = default)
    {
        int attackerId = (int)rpcParams.Receive.SenderClientId;
        if (currentPlayerId.Value == attackerId && gameState.Value == GameState.InProgress)
        {
            combatModule?.Attack(attackerId, targetId, players);
            if (CheckWinCondition(attackerId)) { EndGame(attackerId); return; }
            AdvanceTurn();
        }
    }

    [ClientRpc]
    private void NotifyCombatAvailableClientRpc(int[] enemyIds)
    {
        if (!IsServer) // Avoid double-firing on host
            OnCombatAvailable?.Invoke(new List<int>(enemyIds));
    }

    [ClientRpc]
    private void NotifyAutoCombatClientRpc(int attackerId, int[] defeatedIds)
    {
        // Game messages are already broadcast by HybridCombatModule.Attack via BroadcastGameMessage.
        // This RPC exists so clients can react to auto-combat results if needed in the future.
        Debug.Log($"[HybridGameManager] Auto-combat resolved: player {attackerId} vs {defeatedIds.Length} opponent(s)");
    }


    /// <summary>
    /// Advance to next player's turn
    /// </summary>
    private void AdvanceTurn()
    {
        int nextPlayer = (currentPlayerId.Value + 1) % playerCount.Value;
        currentPlayerId.Value = nextPlayer;
        // OnPlayerTurnChanged is fired via currentPlayerId.OnValueChanged on all machines
        Debug.Log($"[HybridGameManager] Turn advanced to player {nextPlayer}");
    }

    [ClientRpc]
    private void UpdateCurrentPlayerClientRpc(int newPlayerId)
    {
        // Kept for backwards compatibility — turn notification now handled by OnValueChanged
    }

    [ClientRpc]
    private void UpdatePlayerPositionClientRpc(int playerId, int newPosition, int diceRoll)
    {
        if (!IsServer)
        {
            OnPlayerMoved?.Invoke(playerId, newPosition);
            OnGameMessage?.Invoke($"Player {playerId + 1} rolled {diceRoll} and moved to position {newPosition}");
        }
    }

    #endregion

    #region Win Conditions

    /// <summary>
    /// Check if player has won based on active win condition
    /// </summary>
    private bool CheckWinCondition(int playerId)
    {
        switch (activeRules.winCondition)
        {
            case WinCondition.ReachGoal:
                return CheckReachGoalWin(playerId);
                
            case WinCondition.ReachSpecificTile: // NEW: Support for ReachSpecificTile
                return CheckReachSpecificTileWin(playerId);
                
            case WinCondition.LastPlayerStanding:
                return CheckLastPlayerStandingWin(playerId);
                
            case WinCondition.HighestScore:
                // This is typically time-based or round-based
                return false;
                
            case WinCondition.EliminateAllEnemies:
                return CheckEliminateAllWin(playerId);
                
            case WinCondition.MoneyThreshold: // NEW: Support for MoneyThreshold
                return CheckMoneyThresholdWin(playerId);
                
            default:
                Debug.LogWarning($"[HybridGameManager] Unknown win condition: {activeRules.winCondition}");
                return false;
        }
    }

    private bool CheckReachGoalWin(int playerId)
    {
        // Check if player reached goal position (last tile)
        int goalPosition = boardModule != null ? boardModule.GetGoalPosition() : activeRules.tilesPerSide - 1;
        return players[playerId].position >= goalPosition;
    }

    /// <summary>
    /// NEW: Check if player reached specific target tile
    /// </summary>
    private bool CheckReachSpecificTileWin(int playerId)
    {
        // Check if player reached the target tile number
        int targetTile = activeRules.targetTileNumber;
        bool hasWon = players[playerId].position >= targetTile;
        
        if (hasWon)
        {
            Debug.Log($"[HybridGameManager] Player {playerId} reached target tile {targetTile} (position: {players[playerId].position})");
        }
        
        return hasWon;
    }

    private bool CheckLastPlayerStandingWin(int playerId)
    {
        // Check if all other players are bankrupt/eliminated
        int activePlayers = 0;
        foreach (var player in players)
        {
            if (player.isActive) activePlayers++;
        }
        return activePlayers <= 1;
    }

    private bool CheckEliminateAllWin(int playerId)
    {
        // Check if player has eliminated all enemies (combat-based)
        if (combatModule != null)
        {
            return combatModule.HasPlayerWon(playerId, players);
        }
        return false;
    }

    /// <summary>
    /// NEW: Check if player reached money threshold
    /// </summary>
    private bool CheckMoneyThresholdWin(int playerId)
    {
        // Check if player reached winning money threshold
        if (currencyModule == null || !activeRules.enableCurrency)
        {
            Debug.LogWarning("[HybridGameManager] MoneyThreshold win condition requires currency to be enabled!");
            return false;
        }
        
        int threshold = activeRules.winningMoneyThreshold;
        bool hasWon = players[playerId].money >= threshold;
        
        if (hasWon)
        {
            Debug.Log($"[HybridGameManager] Player {playerId} reached money threshold ${threshold} (current: ${players[playerId].money})");
        }
        
        return hasWon;
    }

    #endregion

    #region Game End

    /// <summary>
    /// End the game with a winner
    /// </summary>
    private void EndGame(int winnerId)
    {
        Debug.Log($"[HybridGameManager] Game over! Winner: Player {winnerId}");
        
        gameState.Value = GameState.GameOver;
        OnGameStateChanged?.Invoke(GameState.GameOver);
        
        string winMessage = $"Player {winnerId + 1} wins!";
        OnGameMessage?.Invoke(winMessage);
        
        AnnounceWinnerClientRpc(winnerId, winMessage);
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(int winnerId, string message)
    {
        if (!IsServer) OnGameMessage?.Invoke(message);
        Debug.Log($"[HybridGameManager] {message}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Broadcast a game message (can be called by modules)
    /// </summary>
    public void BroadcastGameMessage(string message)
    {
        OnGameMessage?.Invoke(message);
        
        if (IsServer)
        {
            BroadcastMessageClientRpc(message);
        }
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        if (!IsServer) OnGameMessage?.Invoke(message);
    }

    /// <summary>
    /// Check if it's the local player's turn
    /// </summary>
    public bool IsMyTurn()
    {
        return currentPlayerId.Value == GetLocalPlayerId();
    }

    /// <summary>
    /// Get current game state
    /// </summary>
    public GameState GetGameState()
    {
        return gameState.Value;
    }

    /// <summary>
    /// Get current player ID
    /// </summary>
    public int GetCurrentPlayerId()
    {
        return currentPlayerId.Value;
    }

    /// <summary>
    /// Get total player count
    /// </summary>
    public int GetPlayerCount()
    {
        return playerCount.Value;
    }

    /// <summary>
    /// Get local player ID
    /// </summary>
    public int GetLocalPlayerId()
    {
        // Use our custom NetworkManager singleton, not Unity's NetworkManager
        var customNetworkManager = global::NetworkManager.Instance;
        if (customNetworkManager != null)
        {
            var unityNetworkManager = customNetworkManager.GetUnityNetworkManager();
            if (unityNetworkManager != null && unityNetworkManager.LocalClient != null)
            {
                return (int)unityNetworkManager.LocalClientId;
            }
        }
        return 0;
    }

    /// <summary>
    /// Get player data
    /// </summary>
    public HybridPlayerData GetPlayer(int playerId)
    {
        if (playerId >= 0 && playerId < players.Count)
        {
            return players[playerId];
        }
        return null;
    }

    /// <summary>
    /// Get all players
    /// </summary>
    public List<HybridPlayerData> GetAllPlayers()
    {
        return players;
    }

    /// <summary>
    /// Get active rules
    /// </summary>
    public GameRules GetActiveRules()
    {
        return activeRules;
    }

    /// <summary>
    /// Get currency module (if active)
    /// </summary>
    public HybridCurrencyModule GetCurrencyModule()
    {
        return currencyModule;
    }

    /// <summary>
    /// Get property module (if active)
    /// </summary>
    public HybridPropertyModule GetPropertyModule()
    {
        return propertyModule;
    }

    /// <summary>
    /// Get combat module (if active)
    /// </summary>
    public HybridCombatModule GetCombatModule()
    {
        return combatModule;
    }

    #endregion

    #region Debug

    [ContextMenu("Print Hybrid Game State")]
    private void PrintGameState()
    {
        Debug.Log("=== Hybrid Game State ===");
        Debug.Log($"Game State: {gameState.Value}");
        Debug.Log($"Current Player: {currentPlayerId.Value}");
        Debug.Log($"Player Count: {playerCount.Value}");
        Debug.Log($"\nActive Modules:");
        Debug.Log($"- Currency: {currencyModule != null}");
        Debug.Log($"- Board: {boardModule != null}");
        Debug.Log($"- Property: {propertyModule != null}");
        Debug.Log($"- Combat: {combatModule != null}");
        Debug.Log($"- Movement: {movementModule != null}");
        
        Debug.Log($"\nPlayers:");
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            Debug.Log($"Player {i}: Pos={player.position}, Money=${player.money}, Active={player.isActive}");
        }
    }

    #endregion
}

/// <summary>
/// Hybrid player data that combines all possible player attributes
/// </summary>
[System.Serializable]
public class HybridPlayerData
{
    public int playerId;
    public int position;
    public int money;
    public bool isActive;
    public List<int> ownedProperties = new List<int>();
    public Dictionary<string, int> combatStats = new Dictionary<string, int>();
}

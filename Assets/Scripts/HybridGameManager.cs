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
        
        Debug.Log("[HybridGameManager] Network spawned");
    }

    /// <summary>
    /// Initialize hybrid game with custom rules
    /// </summary>
    public void InitializeGame(int numPlayers, GameRules rules)
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
        StartGameClientRpc();
        
        gameState.Value = GameState.InProgress;
        OnGameStarted?.Invoke();
        OnGameStateChanged?.Invoke(GameState.InProgress);

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
    private void StartGameClientRpc()
    {
        Debug.Log("[HybridGameManager] Game started on client");
        OnGameStarted?.Invoke();
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

        // Next turn
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
    /// Advance to next player's turn
    /// </summary>
    private void AdvanceTurn()
    {
        int nextPlayer = (currentPlayerId.Value + 1) % playerCount.Value;
        currentPlayerId.Value = nextPlayer;
        
        OnPlayerTurnChanged?.Invoke(nextPlayer);
        UpdateCurrentPlayerClientRpc(nextPlayer);
        
        Debug.Log($"[HybridGameManager] Turn advanced to player {nextPlayer}");
    }

    [ClientRpc]
    private void UpdateCurrentPlayerClientRpc(int newPlayerId)
    {
        OnPlayerTurnChanged?.Invoke(newPlayerId);
    }

    [ClientRpc]
    private void UpdatePlayerPositionClientRpc(int playerId, int newPosition, int diceRoll)
    {
        OnPlayerMoved?.Invoke(playerId, newPosition);
        OnGameMessage?.Invoke($"Player {playerId + 1} rolled {diceRoll} and moved to position {newPosition}");
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
                
            case WinCondition.LastPlayerStanding:
                return CheckLastPlayerStandingWin(playerId);
                
            case WinCondition.HighestScore:
                // This is typically time-based or round-based
                return false;
                
            case WinCondition.EliminateAllEnemies:
                return CheckEliminateAllWin(playerId);
                
            default:
                return false;
        }
    }

    private bool CheckReachGoalWin(int playerId)
    {
        // Check if player reached goal position
        int goalPosition = boardModule != null ? boardModule.GetGoalPosition() : activeRules.tilesPerSide - 1;
        return players[playerId].position >= goalPosition;
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
        OnGameMessage?.Invoke(message);
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
        OnGameMessage?.Invoke(message);
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

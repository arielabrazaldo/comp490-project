using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class NetworkGameManager : NetworkBehaviour
{
    private static NetworkGameManager instance;
    public static NetworkGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NetworkGameManager>();
            }
            return instance;
        }
    }

    [Header("Game State")]
    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0);
    
    // Player positions on the board (NetworkList for automatic sync)
    private NetworkList<int> playerPositions;
    
    // Events for UI updates
    public static event Action<int> OnPlayerTurnChanged;
    public static event Action<int, int> OnPlayerMoved; // playerId, newPosition
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnGameStarted;

    public enum GameState
    {
        WaitingToStart,
        InProgress,
        GameOver
    }

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

        // Initialize NetworkList
        playerPositions = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to network variable changes
        currentPlayerTurn.OnValueChanged += OnCurrentPlayerTurnChanged;
        gameState.OnValueChanged += OnGameStateValueChanged;
        
        // Subscribe to player position changes
        playerPositions.OnListChanged += OnPlayerPositionsChanged;

        Debug.Log($"NetworkGameManager spawned. IsHost: {IsHost}, IsClient: {IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        currentPlayerTurn.OnValueChanged -= OnCurrentPlayerTurnChanged;
        gameState.OnValueChanged -= OnGameStateValueChanged;
        playerPositions.OnListChanged -= OnPlayerPositionsChanged;
    }

    #region Network Variable Event Handlers

    private void OnCurrentPlayerTurnChanged(int previousValue, int newValue)
    {
        Debug.Log($"Player turn changed from {previousValue} to {newValue}");
        OnPlayerTurnChanged?.Invoke(newValue);
    }

    private void OnGameStateValueChanged(GameState previousValue, GameState newValue)
    {
        Debug.Log($"Game state changed from {previousValue} to {newValue}");
        OnGameStateChanged?.Invoke(newValue);
        
        if (newValue == GameState.InProgress)
        {
            OnGameStarted?.Invoke();
        }
    }

    private void OnPlayerPositionsChanged(NetworkListEvent<int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<int>.EventType.Value)
        {
            int playerId = changeEvent.Index;
            int newPosition = changeEvent.Value;
            Debug.Log($"Player {playerId} moved to position {newPosition}");
            OnPlayerMoved?.Invoke(playerId, newPosition);
        }
    }

    #endregion

    #region Public Methods (Called by UI/BoardGenerator)

    /// <summary>
    /// Initialize the game with player count (Host only)
    /// </summary>
    public void InitializeGame(int playerCount)
    {
        if (!IsHost) return;

        Debug.Log($"Initializing game with {playerCount} players");
        
        totalPlayers.Value = playerCount;
        
        // Initialize player positions (all start at position 1)
        playerPositions.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            playerPositions.Add(1); // All players start at tile 1
        }
        
        // Start the game
        gameState.Value = GameState.InProgress;
        currentPlayerTurn.Value = 0; // First player starts
        
        // Notify all clients that the game has started
        StartGameClientRpc();
    }

    /// <summary>
    /// Roll dice for current player (can be called by any client)
    /// </summary>
    public void RollDice()
    {
        if (gameState.Value != GameState.InProgress)
        {
            Debug.LogWarning("Cannot roll dice - game not in progress");
            return;
        }

        // Get the current player's network ID
        int currentPlayerId = GetCurrentPlayerId();
        
        // Check if it's this client's turn
        if (!IsMyTurn())
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        // Roll dice (1-6)
        int diceRoll = UnityEngine.Random.Range(1, 7);
        Debug.Log($"Player {currentPlayerId} rolled: {diceRoll}");

        // Send roll to server
        RollDiceServerRpc(diceRoll);
    }

    /// <summary>
    /// Check if it's the local player's turn
    /// </summary>
    public bool IsMyTurn()
    {
        if (!IsSpawned) return false;
        
        int currentPlayerId = GetCurrentPlayerId();
        int myPlayerId = GetMyPlayerId();
        
        bool isMyTurn = currentPlayerId == myPlayerId;
        Debug.Log($"IsMyTurn: CurrentPlayerId={currentPlayerId}, MyPlayerId={myPlayerId}, IsMyTurn={isMyTurn}");
        return isMyTurn;
    }

    /// <summary>
    /// Get current player's ID
    /// </summary>
    public int GetCurrentPlayerId()
    {
        return currentPlayerTurn.Value;
    }

    /// <summary>
    /// Get local player's ID based on network connection
    /// </summary>
    public int GetMyPlayerId()
    {
        if (!IsSpawned) return -1;
        
        // Use a deterministic approach: sort client IDs to ensure consistent ordering
        var connectedClients = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        var sortedClientIds = new List<ulong>(connectedClients);
        sortedClientIds.Sort(); // This ensures consistent ordering across all clients
        
        var myClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        
        int playerId = sortedClientIds.IndexOf(myClientId);
        Debug.Log($"GetMyPlayerId: MyClientId={myClientId}, PlayerId={playerId}, SortedClients=[{string.Join(",", sortedClientIds)}]");
        
        return playerId;
    }

    /// <summary>
    /// Get player position
    /// </summary>
    public int GetPlayerPosition(int playerId)
    {
        if (playerId >= 0 && playerId < playerPositions.Count)
        {
            return playerPositions[playerId];
        }
        return 1; // Default start position
    }

    /// <summary>
    /// Get all player positions
    /// </summary>
    public List<int> GetAllPlayerPositions()
    {
        var positions = new List<int>();
        for (int i = 0; i < playerPositions.Count; i++)
        {
            positions.Add(playerPositions[i]);
        }
        return positions;
    }

    /// <summary>
    /// Get current game state
    /// </summary>
    public GameState GetGameState()
    {
        return gameState.Value;
    }

    /// <summary>
    /// Get total number of players
    /// </summary>
    public int GetTotalPlayers()
    {
        return totalPlayers.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RollDiceServerRpc(int diceRoll, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdFromClientId(senderId);
        
        Debug.Log($"RollDiceServerRpc: SenderId={senderId}, PlayerId={playerId}, CurrentTurn={currentPlayerTurn.Value}, DiceRoll={diceRoll}");
        
        // Validate it's the correct player's turn
        if (playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Player {playerId} tried to roll out of turn! Current turn is {currentPlayerTurn.Value}");
            return;
        }

        Debug.Log($"Valid turn confirmed for Player {playerId}");

        // Move the player
        MovePlayer(playerId, diceRoll);
        
        // Notify all clients about the move
        PlayerMovedClientRpc(playerId, diceRoll, playerPositions[playerId]);
        
        // Check for win condition (assuming winning position is the last tile)
        var (tileCount, playerCount) = GameSetupManager.Instance.GetGameConfiguration();
        if (playerPositions[playerId] >= tileCount)
        {
            // Player won!
            Debug.Log($"Player {playerId} wins the game!");
            GameOverClientRpc(playerId);
            gameState.Value = GameState.GameOver;
            return;
        }
        
        // Move to next player's turn
        NextPlayerTurn();
    }

    #endregion

    #region Client RPCs (Sent to all clients)

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Game started! Received start game notification.");
        
        // Ensure UI Manager knows about game start
        if (UIManager.Instance != null)
        {
            // Force the UI to show the game panel
            UIManager.Instance.OnNetworkGameStarted();
        }
    }

    [ClientRpc]
    private void PlayerMovedClientRpc(int playerId, int diceRoll, int newPosition)
    {
        Debug.Log($"Player {playerId} rolled {diceRoll} and moved to position {newPosition}");
        
        // Update visual representation (GameSetupManager will handle this)
        if (GameSetupManager.Instance != null)
        {
            GameSetupManager.Instance.MovePlayerToken(playerId, newPosition);
        }
    }

    [ClientRpc]
    private void GameOverClientRpc(int winnerPlayerId)
    {
        Debug.Log($"Game Over! Player {winnerPlayerId} wins!");
        
        // Notify UI about game over
        // UIManager can listen to this event to show a win screen
    }

    #endregion

    #region Private Methods

    private void MovePlayer(int playerId, int diceRoll)
    {
        if (playerId >= 0 && playerId < playerPositions.Count)
        {
            int currentPosition = playerPositions[playerId];
            int newPosition = currentPosition + diceRoll;
            
            // Cap at max tile count
            var (tileCount, _) = GameSetupManager.Instance.GetGameConfiguration();
            newPosition = Mathf.Min(newPosition, tileCount);
            
            playerPositions[playerId] = newPosition;
            Debug.Log($"Player {playerId} moved from {currentPosition} to {newPosition}");
        }
    }

    private void NextPlayerTurn()
    {
        int nextPlayer = (currentPlayerTurn.Value + 1) % totalPlayers.Value;
        currentPlayerTurn.Value = nextPlayer;
        Debug.Log($"Next player turn: {nextPlayer}");
    }

    private int GetPlayerIdFromClientId(ulong clientId)
    {
        // Use the same deterministic approach as GetMyPlayerId
        var connectedClients = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        var sortedClientIds = new List<ulong>(connectedClients);
        sortedClientIds.Sort(); // This ensures consistent ordering across all clients
        
        int playerId = sortedClientIds.IndexOf(clientId);
        Debug.Log($"GetPlayerIdFromClientId: ClientId={clientId}, PlayerId={playerId}, SortedClients=[{string.Join(",", sortedClientIds)}]");
        
        return playerId;
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// Debug method to display current game state and player information
    /// </summary>
    [ContextMenu("Debug Game State")]
    public void DebugGameState()
    {
        if (!IsSpawned)
        {
            Debug.Log("NetworkGameManager not spawned yet");
            return;
        }

        Debug.Log("=== GAME STATE DEBUG ===");
        Debug.Log($"Game State: {gameState.Value}");
        Debug.Log($"Total Players: {totalPlayers.Value}");
        Debug.Log($"Current Turn: {currentPlayerTurn.Value}");
        Debug.Log($"My Player ID: {GetMyPlayerId()}");
        Debug.Log($"Is My Turn: {IsMyTurn()}");
        Debug.Log($"My Client ID: {Unity.Netcode.NetworkManager.Singleton.LocalClientId}");
        
        var connectedClients = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        var sortedClientIds = new List<ulong>(connectedClients);
        sortedClientIds.Sort();
        Debug.Log($"Connected Clients (sorted): [{string.Join(",", sortedClientIds)}]");
        
        for (int i = 0; i < playerPositions.Count; i++)
        {
            Debug.Log($"Player {i}: Position {playerPositions[i]}");
        }
        Debug.Log("========================");
    }

    #endregion
}
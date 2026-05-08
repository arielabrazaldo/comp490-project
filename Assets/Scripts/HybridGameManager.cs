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

    private NetworkVariable<int> currentPlayerId = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(2);

    public enum GameState
    {
        WaitingToStart,
        InProgress,
        GameOver
    }

    private GameRules activeRules;

    private HybridCurrencyModule currencyModule;
    private HybridBoardModule boardModule;
    private HybridPropertyModule propertyModule;
    private HybridCombatModule combatModule;
    private HybridMovementModule movementModule;

    private List<HybridPlayerData> players = new List<HybridPlayerData>();

    public static event System.Action OnGameStarted;
    public static event System.Action<int> OnPlayerTurnChanged;
    public static event System.Action<int, int> OnPlayerMoved;
    public static event System.Action<string> OnGameMessage;
    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action<List<int>> OnCombatAvailable;

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

        currentPlayerId.OnValueChanged += OnCurrentPlayerIdChanged;

        Debug.Log("[HybridGameManager] Network spawned");
    }

    public override void OnNetworkDespawn()
    {
        currentPlayerId.OnValueChanged -= OnCurrentPlayerIdChanged;
        base.OnNetworkDespawn();

        Debug.Log("[HybridGameManager] Network despawned");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnCurrentPlayerIdChanged(int prev, int next)
    {
        OnPlayerTurnChanged?.Invoke(next);
    }

    public void InitializeGame(int numPlayers, GameRules rules, string gameName = "")
    {
        if (!IsServer)
        {
            Debug.LogError("[HybridGameManager] Only server can initialize game!");
            return;
        }

        Debug.Log($"[HybridGameManager] Initializing hybrid game with {numPlayers} players");
        Debug.Log($"[HybridGameManager] Rules: {rules.GetRulesSummary()}");

        playerCount.Value = numPlayers;
        currentPlayerId.Value = 0;
        gameState.Value = GameState.WaitingToStart;
        activeRules = rules;

        InitializeModules();
        InitializePlayers(numPlayers);

        string rulesJson = JsonUtility.ToJson(rules);

        gameState.Value = GameState.InProgress;
        OnGameStarted?.Invoke();
        OnGameStateChanged?.Invoke(GameState.InProgress);

        StartGameClientRpc(rulesJson, gameName);

        Debug.Log("[HybridGameManager] Hybrid game initialized and started!");
    }

    private void InitializeModules()
    {
        Debug.Log("[HybridGameManager] Initializing modules based on rules...");

        if (activeRules.enableCurrency)
        {
            currencyModule = gameObject.AddComponent<HybridCurrencyModule>();
            currencyModule.Initialize(activeRules.startingMoney);
        }

        boardModule = gameObject.AddComponent<HybridBoardModule>();
        boardModule.Initialize(activeRules);

        if (activeRules.canPurchaseProperties || activeRules.enablePropertyTrading)
        {
            propertyModule = gameObject.AddComponent<HybridPropertyModule>();
            propertyModule.Initialize(activeRules, currencyModule);
        }

        if (activeRules.enableCombat)
        {
            combatModule = gameObject.AddComponent<HybridCombatModule>();
            combatModule.Initialize(activeRules);
        }

        movementModule = gameObject.AddComponent<HybridMovementModule>();
        movementModule.Initialize(activeRules);
    }

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
                }
            }
        }

        OnGameStarted?.Invoke();
        OnGameStateChanged?.Invoke(GameState.InProgress);
    }

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
        int playerId = GetPlayerIdFromClientId(senderId);

        if (playerId >= 0 && currentPlayerId.Value == playerId && gameState.Value == GameState.InProgress)
        {
            ProcessTurn(playerId);
        }
    }

    private void ProcessTurn(int playerId)
    {
        if (playerId < 0 || playerId >= players.Count)
        {
            Debug.LogWarning($"[HybridGameManager] Invalid playerId for turn: {playerId}");
            return;
        }

        Debug.Log($"[HybridGameManager] Processing turn for player {playerId}");

        int diceRoll = Random.Range(1, 7);

        if (movementModule != null)
        {
            int newPosition = movementModule.MovePlayer(playerId, players[playerId].position, diceRoll, activeRules);
            players[playerId].position = newPosition;

            OnPlayerMoved?.Invoke(playerId, newPosition);
            UpdatePlayerPositionClientRpc(playerId, newPosition, diceRoll);
        }

        ProcessSpaceLanded(playerId);

        if (CheckWinCondition(playerId))
        {
            EndGame(playerId);
            return;
        }

        if (activeRules.enableCombat && combatModule != null)
        {
            List<int> enemies = combatModule.GetEnemiesInRange(
                playerId,
                players[playerId].position,
                players,
                activeRules.combatRange
            );

            if (activeRules.combatRange == 0)
            {
                foreach (int enemyId in enemies)
                {
                    combatModule.Attack(playerId, enemyId, players);

                    if (CheckWinCondition(playerId))
                    {
                        EndGame(playerId);
                        return;
                    }
                }

                NotifyAutoCombatClientRpc(playerId, enemies.ToArray());
                AdvanceTurn();
                return;
            }

            OnCombatAvailable?.Invoke(enemies);
            NotifyCombatAvailableClientRpc(enemies.ToArray());
            return;
        }

        AdvanceTurn();
    }

    private void ProcessSpaceLanded(int playerId)
    {
        int position = players[playerId].position;

        if (propertyModule != null)
        {
            propertyModule.ProcessPropertySpace(playerId, position, players);
        }

        if (combatModule != null)
        {
            combatModule.ProcessCombatSpace(playerId, position, players);
        }

        if (currencyModule != null)
        {
            currencyModule.ProcessCurrencySpace(playerId, position, players);
        }
    }

    public void EndTurn()
    {
        if (!IsServer)
        {
            RequestEndTurnServerRpc();
            return;
        }

        AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdFromClientId(senderId);

        if (playerId >= 0 && currentPlayerId.Value == playerId && gameState.Value == GameState.InProgress)
        {
            AdvanceTurn();
        }
    }

    public void AttackPlayer(int targetId)
    {
        if (!IsServer)
        {
            RequestAttackServerRpc(targetId);
            return;
        }

        int attackerId = currentPlayerId.Value;

        if (combatModule != null)
        {
            combatModule.Attack(attackerId, targetId, players);
        }

        if (CheckWinCondition(attackerId))
        {
            EndGame(attackerId);
            return;
        }

        AdvanceTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAttackServerRpc(int targetId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int attackerId = GetPlayerIdFromClientId(senderId);

        if (attackerId >= 0 && currentPlayerId.Value == attackerId && gameState.Value == GameState.InProgress)
        {
            combatModule?.Attack(attackerId, targetId, players);

            if (CheckWinCondition(attackerId))
            {
                EndGame(attackerId);
                return;
            }

            AdvanceTurn();
        }
    }

    [ClientRpc]
    private void NotifyCombatAvailableClientRpc(int[] enemyIds)
    {
        if (!IsServer)
        {
            OnCombatAvailable?.Invoke(new List<int>(enemyIds));
        }
    }

    [ClientRpc]
    private void NotifyAutoCombatClientRpc(int attackerId, int[] defeatedIds)
    {
        Debug.Log($"[HybridGameManager] Auto-combat resolved: player {attackerId} vs {defeatedIds.Length} opponent(s)");
    }

    private void AdvanceTurn()
    {
        int nextPlayer = (currentPlayerId.Value + 1) % playerCount.Value;
        currentPlayerId.Value = nextPlayer;

        Debug.Log($"[HybridGameManager] Turn advanced to player {nextPlayer}");
    }

    [ClientRpc]
    private void UpdateCurrentPlayerClientRpc(int newPlayerId)
    {
        // Kept for backwards compatibility.
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

    private bool CheckWinCondition(int playerId)
    {
        switch (activeRules.winCondition)
        {
            case WinCondition.ReachGoal:
                return CheckReachGoalWin(playerId);

            case WinCondition.ReachSpecificTile:
                return CheckReachSpecificTileWin(playerId);

            case WinCondition.LastPlayerStanding:
                return CheckLastPlayerStandingWin(playerId);

            case WinCondition.HighestScore:
                return false;

            case WinCondition.EliminateAllEnemies:
                return CheckEliminateAllWin(playerId);

            case WinCondition.MoneyThreshold:
                return CheckMoneyThresholdWin(playerId);

            default:
                Debug.LogWarning($"[HybridGameManager] Unknown win condition: {activeRules.winCondition}");
                return false;
        }
    }

    private bool CheckReachGoalWin(int playerId)
    {
        int goalPosition = boardModule != null ? boardModule.GetGoalPosition() : activeRules.tilesPerSide - 1;
        return players[playerId].position >= goalPosition;
    }

    private bool CheckReachSpecificTileWin(int playerId)
    {
        int targetTile = activeRules.targetTileNumber;
        return players[playerId].position >= targetTile;
    }

    private bool CheckLastPlayerStandingWin(int playerId)
    {
        int activePlayers = 0;

        foreach (var player in players)
        {
            if (player.isActive)
            {
                activePlayers++;
            }
        }

        return activePlayers <= 1;
    }

    private bool CheckEliminateAllWin(int playerId)
    {
        if (combatModule != null)
        {
            return combatModule.HasPlayerWon(playerId, players);
        }

        return false;
    }

    private bool CheckMoneyThresholdWin(int playerId)
    {
        if (currencyModule == null || !activeRules.enableCurrency)
        {
            Debug.LogWarning("[HybridGameManager] MoneyThreshold win condition requires currency to be enabled!");
            return false;
        }

        int threshold = activeRules.winningMoneyThreshold;
        return players[playerId].money >= threshold;
    }

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
        if (!IsServer)
        {
            OnGameMessage?.Invoke(message);
            OnGameStateChanged?.Invoke(GameState.GameOver);
        }

        Debug.Log($"[HybridGameManager] {message}");
    }

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
        if (!IsServer)
        {
            OnGameMessage?.Invoke(message);
        }
    }

    public bool IsMyTurn()
    {
        return currentPlayerId.Value == GetLocalPlayerId();
    }

    public GameState GetGameState()
    {
        return gameState.Value;
    }

    public int GetCurrentPlayerId()
    {
        return currentPlayerId.Value;
    }

    public int GetPlayerCount()
    {
        return playerCount.Value;
    }

    public int GetLocalPlayerId()
    {
        var unityNetworkManager = GetUnityNetworkManager();

        if (unityNetworkManager == null || unityNetworkManager.LocalClient == null)
        {
            return -1;
        }

        return GetPlayerIdFromClientId(unityNetworkManager.LocalClientId);
    }

    private int GetPlayerIdFromClientId(ulong clientId)
    {
        var unityNetworkManager = GetUnityNetworkManager();

        if (unityNetworkManager == null)
        {
            return -1;
        }

        var sortedClientIds = new List<ulong>(unityNetworkManager.ConnectedClientsIds);
        sortedClientIds.Sort();

        int playerId = sortedClientIds.IndexOf(clientId);

        Debug.Log($"[HybridGameManager] ClientId={clientId}, PlayerId={playerId}, SortedClients=[{string.Join(",", sortedClientIds)}]");

        return playerId;
    }

    private Unity.Netcode.NetworkManager GetUnityNetworkManager()
    {
        var customNetworkManager = global::NetworkManager.Instance;

        if (customNetworkManager != null)
        {
            var unityNetworkManager = customNetworkManager.GetUnityNetworkManager();

            if (unityNetworkManager != null)
            {
                return unityNetworkManager;
            }
        }

        return Unity.Netcode.NetworkManager.Singleton;
    }

    public HybridPlayerData GetPlayer(int playerId)
    {
        if (playerId >= 0 && playerId < players.Count)
        {
            return players[playerId];
        }

        return null;
    }

    public List<HybridPlayerData> GetAllPlayers()
    {
        return players;
    }

    public GameRules GetActiveRules()
    {
        return activeRules;
    }

    public HybridCurrencyModule GetCurrencyModule()
    {
        return currencyModule;
    }

    public HybridPropertyModule GetPropertyModule()
    {
        return propertyModule;
    }

    public HybridCombatModule GetCombatModule()
    {
        return combatModule;
    }

    [ContextMenu("Print Hybrid Game State")]
    private void PrintGameState()
    {
        Debug.Log("=== Hybrid Game State ===");
        Debug.Log($"Game State: {gameState.Value}");
        Debug.Log($"Current Player: {currentPlayerId.Value}");
        Debug.Log($"Player Count: {playerCount.Value}");

        Debug.Log("Active Modules:");
        Debug.Log($"- Currency: {currencyModule != null}");
        Debug.Log($"- Board: {boardModule != null}");
        Debug.Log($"- Property: {propertyModule != null}");
        Debug.Log($"- Combat: {combatModule != null}");
        Debug.Log($"- Movement: {movementModule != null}");

        Debug.Log("Players:");

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            Debug.Log($"Player {i}: Pos={player.position}, Money=${player.money}, Active={player.isActive}");
        }
    }
}

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
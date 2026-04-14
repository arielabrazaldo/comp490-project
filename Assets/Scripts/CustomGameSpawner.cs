using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;

/// <summary>
/// Spawns game managers based on game type.
/// Standard games (Monopoly, Battleships, Dice Race) spawn their dedicated managers.
/// Custom/Hybrid games use HybridGameManager with modular rules.
/// </summary>
public class CustomGameSpawner : MonoBehaviour
{
    private static CustomGameSpawner instance;
    public static CustomGameSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CustomGameSpawner>();
                
                // Auto-create if not found in scene
                if (instance == null)
                {
                    GameObject go = new GameObject("CustomGameSpawner");
                    instance = go.AddComponent<CustomGameSpawner>();
                    Debug.Log("[CustomGameSpawner] Auto-created CustomGameSpawner GameObject");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CustomGameSpawner] Initialized and set to DontDestroyOnLoad");
        }
        else if (instance != this)
        {
            Debug.Log("[CustomGameSpawner] Duplicate instance found, destroying");
            Destroy(gameObject);
        }
    }

    #region Game Spawning

    /// <summary>
    /// Spawn game from SavedGameInfo (uses explicit game type - no analysis needed)
    /// This is the PRIMARY method for spawning games.
    /// </summary>
    public async Task<bool> SpawnGame(SavedGameInfo gameInfo, int playerCount)
    {
        if (gameInfo == null)
        {
            Debug.LogError("[CustomGameSpawner] GameInfo is null!");
            return false;
        }

        if (gameInfo.rules == null)
        {
            Debug.LogError("[CustomGameSpawner] GameInfo.rules is null!");
            return false;
        }

        if (!NetworkManager.Instance.IsHost())
        {
            Debug.LogError("[CustomGameSpawner] Only the host can spawn game managers!");
            return false;
        }

        Debug.Log($"[CustomGameSpawner] Spawning game: {gameInfo.gameName} (Type: {gameInfo.gameType})");

        // Spawn based on explicit game type - no analyzer needed
        bool success = gameInfo.gameType switch
        {
            1 => await SpawnMonopolyGame(gameInfo.rules, playerCount),
            2 => await SpawnBattleshipsGame(gameInfo.rules, playerCount),
            3 => await SpawnDiceRaceGame(gameInfo.rules, playerCount),
            _ => await SpawnHybridGame(gameInfo.rules, playerCount) // All custom/hybrid/unknown games
        };

        if (success)
        {
            Debug.Log($"[CustomGameSpawner] Successfully spawned {gameInfo.gameType} game");
            ApplyRulesToGame(gameInfo.rules);
        }
        else
        {
            Debug.LogError($"[CustomGameSpawner] Failed to spawn {gameInfo.gameType} game");
        }

        return success;
    }

    /// <summary>
    /// Spawn game from rules with explicit game type int.
    /// Use this when you have rules but not a full SavedGameInfo.
    /// </summary>
    public async Task<bool> SpawnGame(GameRules rules, int playerCount, int gameType)
    {
        if (rules == null)
        {
            Debug.LogError("[CustomGameSpawner] Rules are null!");
            return false;
        }

        // Create temporary SavedGameInfo to use primary spawn method
        var tempGameInfo = new SavedGameInfo(
            gameName: GetGameTypeName(gameType),
            gameType: gameType,
            playerCount: playerCount,
            rules: rules,
            isStandardGame: false
        );

        return await SpawnGame(tempGameInfo, playerCount);
    }

    /// <summary>
    /// Get display name for game type int
    /// </summary>
    private string GetGameTypeName(int gameType)
    {
        return gameType switch
        {
            1 => "Monopoly",
            2 => "Battleships",
            3 => "Dice Race",
            4 => "Hybrid",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// [DEPRECATED] Spawn game from rules only - defaults to Hybrid for unknown types.
    /// Prefer SpawnGame(SavedGameInfo, int) or SpawnGame(GameRules, int, int) instead.
    /// </summary>
    public async Task<bool> SpawnGameFromRules(GameRules rules, int playerCount)
    {
        Debug.LogWarning("[CustomGameSpawner] SpawnGameFromRules is deprecated. Use SpawnGame with explicit game type.");
        
        if (rules == null)
        {
            Debug.LogError("[CustomGameSpawner] Rules are null!");
            return false;
        }

        // Without explicit type, default to Hybrid (4) as safest option
        return await SpawnGame(rules, playerCount, 4); // 4 = Hybrid
    }

    #endregion

    #region Specific Game Spawning

    /// <summary>
    /// Spawn and configure a Monopoly game
    /// </summary>
    private async Task<bool> SpawnMonopolyGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Monopoly game...");

        // Verify we're the host before proceeding
        if (!NetworkManager.Instance.IsHost())
        {
            Debug.LogError("[CustomGameSpawner] Only the host can spawn Monopoly games!");
            return false;
        }

        // Configure board first
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.ConfigureGame(playerCount);
        }
        else
        {
            Debug.LogWarning("[CustomGameSpawner] MonopolyBoardManager.Instance is null - board won't be configured");
        }

        // Spawn MonopolyGameManager
        if (!TrySpawnNetworkPrefab<MonopolyGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn MonopolyGameManager");
            return false;
        }

        // Wait for network spawn to complete
        await Task.Delay(500);
        
        // Wait a bit more and verify the instance is ready
        int maxRetries = 10;
        MonopolyGameManager gameManager = null;
        
        for (int i = 0; i < maxRetries; i++)
        {
            gameManager = MonopolyGameManager.Instance;
            if (gameManager != null && gameManager.IsSpawned)
            {
                Debug.Log($"[CustomGameSpawner] MonopolyGameManager ready after {i + 1} attempts");
                break;
            }
            await Task.Delay(100);
        }

        // Initialize game
        if (gameManager != null && gameManager.IsSpawned)
        {
            Debug.Log($"[CustomGameSpawner] Initializing Monopoly game with {playerCount} players");
            gameManager.InitializeGame(playerCount);
            
            // Apply custom starting money if different from default
            if (rules.startingMoney != 1500)
            {
                Debug.Log($"[CustomGameSpawner] Custom starting money: ${rules.startingMoney}");
            }
            
            return true;
        }
        else
        {
            Debug.LogError("[CustomGameSpawner] MonopolyGameManager not ready after waiting!");
            return false;
        }
    }

    /// <summary>
    /// Spawn and configure a Battleships game
    /// </summary>
    private async Task<bool> SpawnBattleshipsGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Battleships game...");

        // Get board dimensions from BattleshipsSetupManager if available
        int boardRows = 10;
        int boardCols = 10;
        bool[,] customTileStates = null;
        
        if (BattleshipsSetupManager.Instance != null)
        {
            boardRows = BattleshipsSetupManager.Instance.GetMaxRows();
            boardCols = BattleshipsSetupManager.Instance.GetMaxColumns();
            
            // Get custom tile states if the grid was customized
            if (BattleshipsSetupManager.Instance.IsCustomized())
            {
                customTileStates = BattleshipsSetupManager.Instance.GetTileStates();
                Debug.Log($"[CustomGameSpawner] Using custom board layout: {boardRows}x{boardCols} with {CountActiveTiles(customTileStates)} active tiles");
            }
            else
            {
                Debug.Log($"[CustomGameSpawner] Using default board: {boardRows}x{boardCols}");
            }
        }
        else
        {
            // Fallback to rules if setup manager not available
            boardRows = Mathf.Max(rules.tilesPerSide, 10);
            boardCols = boardRows;
            Debug.Log($"[CustomGameSpawner] BattleshipsSetupManager not found, using rules: {boardRows}x{boardCols}");
        }

        // Spawn BattleshipsGameManager
        if (!TrySpawnNetworkPrefab<BattleshipsGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn BattleshipsGameManager");
            return false;
        }

        // Wait for spawn
        await Task.Delay(500);

        // Initialize game with proper configuration
        if (BattleshipsGameManager.Instance != null)
        {
            if (customTileStates != null)
            {
                // Use custom board with disabled tiles
                BattleshipsGameManager.Instance.InitializeGame(playerCount, boardRows, boardCols, customTileStates);
                Debug.Log($"[CustomGameSpawner] Initialized Battleships with custom board: {boardRows}x{boardCols}");
            }
            else
            {
                // Use default board (all tiles active)
                BattleshipsGameManager.Instance.InitializeGameWithDefaultBoard(playerCount, boardRows, boardCols);
                Debug.Log($"[CustomGameSpawner] Initialized Battleships with default board: {boardRows}x{boardCols}");
            }
            
            // Clear the grid state after initialization
            if (BattleshipsSetupManager.Instance != null)
            {
                BattleshipsSetupManager.Instance.ClearGridState();
            }
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Spawn and configure a Dice Race game
    /// </summary>
    private async Task<bool> SpawnDiceRaceGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Dice Race game...");

        // Get tile count from rules (clamp to reasonable range)
        int tileCount = Mathf.Clamp(rules.tilesPerSide, 10, 100);

        // Configure and generate board
        if (GameSetupManager.Instance != null)
        {
            GameSetupManager.Instance.ConfigureGame(tileCount, playerCount);
            GameSetupManager.Instance.GenerateBoard();
        }

        // Spawn NetworkGameManager
        if (!TrySpawnNetworkPrefab<NetworkGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn NetworkGameManager");
            return false;
        }

        // Wait for spawn
        await Task.Delay(500);

        // Initialize game
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.InitializeGame(playerCount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Spawn and configure a hybrid game (custom mix of game types)
    /// </summary>
    private async Task<bool> SpawnHybridGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Hybrid game...");
        Debug.Log("[CustomGameSpawner] Using modular HybridGameManager for custom rules");

        // Spawn HybridGameManager
        if (!TrySpawnNetworkPrefab<HybridGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn HybridGameManager");
            return false;
        }

        // Wait for spawn
        await Task.Delay(500);

        // Initialize hybrid game with custom rules
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.InitializeGame(playerCount, rules);
            
            Debug.Log("[CustomGameSpawner] Hybrid game initialized with modular systems:");
            Debug.Log($"  - Currency: {rules.enableCurrency}");
            Debug.Log($"  - Properties: {rules.canPurchaseProperties}");
            Debug.Log($"  - Combat: {rules.enableCombat}");
            Debug.Log($"  - Separate Boards: {rules.separatePlayerBoards}");
            
            return true;
        }

        return false;
    }

    #endregion

    #region Network Spawning Utilities

    /// <summary>
    /// Try to spawn a network prefab of type T
    /// </summary>
    private bool TrySpawnNetworkPrefab<T>() where T : NetworkBehaviour
    {
        var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
        if (unityNetworkManager == null)
        {
            Debug.LogError("[CustomGameSpawner] Unity NetworkManager not found!");
            return false;
        }

        // Search for prefab in network prefabs list
        GameObject prefab = FindNetworkPrefab<T>(unityNetworkManager);
        
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            networkObject.Spawn();
            Debug.Log($"[CustomGameSpawner] Spawned {typeof(T).Name} from prefab");
            return true;
        }
        else
        {
            // Fallback: Dynamic spawn
            Debug.LogWarning($"[CustomGameSpawner] {typeof(T).Name} prefab not found, using dynamic spawn");
            GameObject go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            // Only add NetworkObject if the component doesn't already inherit one
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = go.AddComponent<NetworkObject>();
            }
            networkObject.Spawn();
            return true;
        }
    }

    /// <summary>
    /// Find a network prefab of type T
    /// </summary>
    private GameObject FindNetworkPrefab<T>(Unity.Netcode.NetworkManager networkManager) where T : Component
    {
        if (networkManager.NetworkConfig?.Prefabs?.Prefabs != null)
        {
            foreach (var networkPrefab in networkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab != null && networkPrefab.Prefab.GetComponent<T>() != null)
                {
                    return networkPrefab.Prefab;
                }
            }
        }
        return null;
    }

    #endregion

    #region Rule Application

    /// <summary>
    /// Apply rules to the active game manager
    /// </summary>
    private void ApplyRulesToGame(GameRules rules)
    {
        Debug.Log("[CustomGameSpawner] Applying custom rules to game...");

        // Apply rules through RuleEditorManager
        if (RuleEditorManager.Instance != null)
        {
            RuleEditorManager.Instance.SetRules(rules);
        }
        else
        {
            Debug.LogWarning("[CustomGameSpawner] RuleEditorManager not found - rules may not be applied");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Spawn game using current rules from RuleEditorManager
    /// </summary>
    public async Task<bool> SpawnGameFromCurrentRules(int playerCount)
    {
        if (RuleEditorManager.Instance == null)
        {
            Debug.LogError("[CustomGameSpawner] RuleEditorManager not found!");
            return false;
        }

        GameRules rules = RuleEditorManager.Instance.GetCurrentRules();
        
        // Get current game info if available, otherwise use Hybrid
        SavedGameInfo currentGame = RuleEditorManager.Instance.GetCurrentGameInfo();
        if (currentGame != null)
        {
            return await SpawnGame(currentGame, playerCount);
        }
        
        // Fallback to Hybrid (4) if no game info
        return await SpawnGame(rules, playerCount, 4); // 4 = Hybrid
    }

    /// <summary>
    /// Check if a SavedGameInfo can spawn a valid game
    /// </summary>
    public bool CanSpawnGame(SavedGameInfo gameInfo)
    {
        if (gameInfo == null || gameInfo.rules == null) return false;

        // Validate rules
        string errorMessage;
        return gameInfo.rules.ValidateRules(out errorMessage);
    }

    /// <summary>
    /// Check if rules can spawn a valid game
    /// </summary>
    public bool CanSpawnGame(GameRules rules)
    {
        if (rules == null) return false;

        string errorMessage;
        return rules.ValidateRules(out errorMessage);
    }

    #endregion

    #region Debug

    [ContextMenu("Test Spawn from Current Rules")]
    private async void DebugSpawnFromCurrentRules()
    {
        if (!NetworkManager.Instance.IsHost())
        {
            Debug.LogError("Must be host to spawn game!");
            return;
        }

        bool success = await SpawnGameFromCurrentRules(2);
        Debug.Log($"Spawn result: {success}");
    }

    #endregion

    /// <summary>
    /// Count active tiles in a tile states array
    /// </summary>
    private int CountActiveTiles(bool[,] tileStates)
    {
        if (tileStates == null) return 0;
        
        int count = 0;
        for (int r = 0; r < tileStates.GetLength(0); r++)
        {
            for (int c = 0; c < tileStates.GetLength(1); c++)
            {
                if (tileStates[r, c]) count++;
            }
        }
        return count;
    }
}

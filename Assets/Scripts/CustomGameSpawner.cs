using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;

/// <summary>
/// Spawns appropriate game managers based on analyzed custom rules.
/// This is the bridge between rule configuration and actual gameplay.
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
    /// Analyze rules and spawn the appropriate game manager
    /// </summary>
    public async Task<bool> SpawnGameFromRules(GameRules rules, int playerCount)
    {
        if (rules == null)
        {
            Debug.LogError("[CustomGameSpawner] Rules are null!");
            return false;
        }

        if (!NetworkManager.Instance.IsHost())
        {
            Debug.LogError("[CustomGameSpawner] Only the host can spawn game managers!");
            return false;
        }

        // Analyze rules to detect game type
        CustomGameAnalyzer.DetectedGameType gameType = CustomGameAnalyzer.Instance.AnalyzeGameRules(rules);
        
        Debug.Log($"[CustomGameSpawner] Spawning game for detected type: {gameType}");

        // Validate rules compatibility
        if (!CustomGameAnalyzer.Instance.AreRulesCompatible(rules, gameType))
        {
            Debug.LogError($"[CustomGameSpawner] Rules are not compatible with {gameType}!");
            return false;
        }

        // Spawn appropriate game manager
        bool success = false;
        switch (gameType)
        {
            case CustomGameAnalyzer.DetectedGameType.Monopoly:
                success = await SpawnMonopolyGame(rules, playerCount);
                break;
            case CustomGameAnalyzer.DetectedGameType.Battleships:
                success = await SpawnBattleshipsGame(rules, playerCount);
                break;
            case CustomGameAnalyzer.DetectedGameType.DiceRace:
                success = await SpawnDiceRaceGame(rules, playerCount);
                break;
            case CustomGameAnalyzer.DetectedGameType.Hybrid:
                success = await SpawnHybridGame(rules, playerCount);
                break;
            default:
                Debug.LogError($"[CustomGameSpawner] Unknown game type: {gameType}");
                break;
        }

        if (success)
        {
            Debug.Log($"[CustomGameSpawner] Successfully spawned {gameType} game");
            
            // Apply rules to the spawned game manager
            ApplyRulesToGame(rules);
        }
        else
        {
            Debug.LogError($"[CustomGameSpawner] Failed to spawn {gameType} game");
        }

        return success;
    }

    #endregion

    #region Specific Game Spawning

    /// <summary>
    /// Spawn and configure a Monopoly game
    /// </summary>
    private async Task<bool> SpawnMonopolyGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Monopoly game...");

        // Configure board first
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.ConfigureGame(playerCount);
        }

        // Spawn MonopolyGameManager
        if (!TrySpawnNetworkPrefab<MonopolyGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn MonopolyGameManager");
            return false;
        }

        // Wait for spawn
        await Task.Delay(500);

        // Initialize game
        if (MonopolyGameManager.Instance != null)
        {
            MonopolyGameManager.Instance.InitializeGame(playerCount);
            
            // Apply custom starting money if different from default
            if (rules.startingMoney != 1500)
            {
                // You would need to add a method to set starting money in MonopolyGameManager
                Debug.Log($"[CustomGameSpawner] Custom starting money: ${rules.startingMoney}");
            }
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Spawn and configure a Battleships game
    /// </summary>
    private async Task<bool> SpawnBattleshipsGame(GameRules rules, int playerCount)
    {
        Debug.Log("[CustomGameSpawner] Spawning Battleships game...");

        // Get board configuration
        int boardSize = CustomGameAnalyzer.Instance.GetRecommendedBoardSize(
            CustomGameAnalyzer.DetectedGameType.Battleships, rules);

        // Spawn BattleshipsGameManager
        if (!TrySpawnNetworkPrefab<BattleshipsGameManager>())
        {
            Debug.LogError("[CustomGameSpawner] Failed to spawn BattleshipsGameManager");
            return false;
        }

        // Wait for spawn
        await Task.Delay(500);

        // Initialize game with custom board size
        if (BattleshipsGameManager.Instance != null)
        {
            BattleshipsGameManager.Instance.InitializeGameWithDefaultBoard(playerCount, boardSize, boardSize);
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

        // Get board configuration
        int tileCount = CustomGameAnalyzer.Instance.GetRecommendedBoardSize(
            CustomGameAnalyzer.DetectedGameType.DiceRace, rules);

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
        Debug.Log("[CustomGameSpawner] Using TRUE hybrid system with modular game manager");

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
            GameObject gameObject = new GameObject(typeof(T).Name);
            gameObject.AddComponent<T>();
            NetworkObject networkObject = gameObject.AddComponent<NetworkObject>();
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
        return await SpawnGameFromRules(rules, playerCount);
    }

    /// <summary>
    /// Check if rules can spawn a valid game
    /// </summary>
    public bool CanSpawnGame(GameRules rules)
    {
        if (rules == null) return false;

        CustomGameAnalyzer.DetectedGameType gameType = CustomGameAnalyzer.Instance.AnalyzeGameRules(rules);
        return CustomGameAnalyzer.Instance.AreRulesCompatible(rules, gameType);
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
}

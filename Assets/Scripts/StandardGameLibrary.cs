using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides standard game configurations (Monopoly, Battleships, Dice Race).
/// These are built-in games that users can host directly without custom rule configuration.
/// All standard games route through HybridGameManager via CustomGameSpawner.
/// </summary>
public class StandardGameLibrary : MonoBehaviour
{
    private static StandardGameLibrary instance;
    public static StandardGameLibrary Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<StandardGameLibrary>();
                
                if (instance == null)
                {
                    Debug.LogWarning("[StandardGameLibrary] No instance found in scene!");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        Debug.Log("[StandardGameLibrary] Awake called");
        
        if (instance == null)
        {
            instance = this;
            
            // CRITICAL FIX: Must be a root GameObject for DontDestroyOnLoad to work
            if (transform.parent != null)
            {
                Debug.Log("[StandardGameLibrary] Detaching from parent to become root GameObject");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            Debug.Log("[StandardGameLibrary] ? Instance set and moved to DontDestroyOnLoad");
        }
        else if (instance != this)
        {
            Debug.Log("[StandardGameLibrary] ?? Duplicate instance detected, destroying");
            Destroy(gameObject);
            return;
        }
        
        // Initialize standard games
        InitializeStandardGames();
    }
    
    private void OnEnable()
    {
        Debug.Log("[StandardGameLibrary] OnEnable called");
    }
    
    private void Start()
    {
        Debug.Log("[StandardGameLibrary] Start called - Standard games ready");
    }
    
    /// <summary>
    /// Initialize and validate all standard games on startup
    /// </summary>
    private void InitializeStandardGames()
    {
        Debug.Log("[StandardGameLibrary] Initializing standard games...");
        
        List<SavedGameInfo> games = GetAllStandardGames();
        Debug.Log($"[StandardGameLibrary] ? {games.Count} standard games initialized");
        
        // Validate each game
        foreach (var game in games)
        {
            bool valid = ValidateStandardGame(game);
            if (!valid)
            {
                Debug.LogError($"[StandardGameLibrary] ? {game.gameName} failed validation!");
            }
        }
    }

    #region Standard Game Definitions

    /// <summary>
    /// Get Classic Monopoly game configuration
    /// </summary>
    public SavedGameInfo GetMonopolyRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        
        return new SavedGameInfo(
            gameName: "Classic Monopoly",
            gameType: "Monopoly",
            playerCount: 4,
            rules: rules,
            isStandardGame: true
        );
    }

    /// <summary>
    /// Get Classic Battleships game configuration
    /// </summary>
    public SavedGameInfo GetBattleshipsRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        
        return new SavedGameInfo(
            gameName: "Classic Battleships",
            gameType: "Battleships",
            playerCount: 2,
            rules: rules,
            isStandardGame: true
        );
    }

    /// <summary>
    /// Get Dice Race game configuration
    /// </summary>
    public SavedGameInfo GetDiceRaceRules()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        
        return new SavedGameInfo(
            gameName: "Dice Race",
            gameType: "Dice Race",
            playerCount: 4,
            rules: rules,
            isStandardGame: true
        );
    }

    #endregion

    #region Library Access

    /// <summary>
    /// Get all standard games available in the library
    /// </summary>
    public List<SavedGameInfo> GetAllStandardGames()
    {
        List<SavedGameInfo> standardGames = new List<SavedGameInfo>
        {
            GetMonopolyRules(),
            GetBattleshipsRules(),
            GetDiceRaceRules()
        };

        Debug.Log($"[StandardGameLibrary] Loaded {standardGames.Count} standard games");
        return standardGames;
    }

    /// <summary>
    /// Get a standard game by name
    /// </summary>
    public SavedGameInfo GetStandardGameByName(string gameName)
    {
        switch (gameName.ToLower())
        {
            case "monopoly":
            case "classic monopoly":
                return GetMonopolyRules();
            
            case "battleships":
            case "classic battleships":
                return GetBattleshipsRules();
            
            case "dice race":
            case "dicerace":
                return GetDiceRaceRules();
            
            default:
                Debug.LogWarning($"[StandardGameLibrary] Unknown standard game: {gameName}");
                return null;
        }
    }

    /// <summary>
    /// Check if a game name is a standard game
    /// </summary>
    public bool IsStandardGame(string gameName)
    {
        return GetStandardGameByName(gameName) != null;
    }

    #endregion

    #region Variants (Future Expansion)

    /// <summary>
    /// Get a variant of Monopoly with modified rules
    /// Example: Speed Monopoly (faster gameplay)
    /// </summary>
    public SavedGameInfo GetMonopolySpeedVariant()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        
        // Modify for faster gameplay
        rules.startingMoney = 2000; // More starting money
        rules.passGoBonus = 300; // Higher pass GO bonus
        rules.winningMoneyThreshold = 3000; // Lower winning threshold
        rules.moneyThresholdWins = true; // Enable money victory
        
        return new SavedGameInfo(
            gameName: "Speed Monopoly",
            gameType: "Monopoly",
            playerCount: 4,
            rules: rules,
            isStandardGame: true
        );
    }

    /// <summary>
    /// Get a larger Battleships variant
    /// Example: Naval Warfare (larger board)
    /// </summary>
    public SavedGameInfo GetBattleshipsLargeVariant()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        
        // Modify for larger board
        rules.tilesPerSide = 15; // 15x15 board instead of 10x10
        
        return new SavedGameInfo(
            gameName: "Naval Warfare",
            gameType: "Battleships",
            playerCount: 2,
            rules: rules,
            isStandardGame: true
        );
    }

    /// <summary>
    /// Get all standard games including variants
    /// </summary>
    public List<SavedGameInfo> GetAllStandardGamesWithVariants()
    {
        List<SavedGameInfo> allGames = GetAllStandardGames();
        
        // Add variants (commented out for now, can be enabled later)
        // allGames.Add(GetMonopolySpeedVariant());
        // allGames.Add(GetBattleshipsLargeVariant());
        
        return allGames;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate that a standard game's rules are properly configured
    /// </summary>
    public bool ValidateStandardGame(SavedGameInfo gameInfo)
    {
        if (gameInfo == null)
        {
            Debug.LogError("[StandardGameLibrary] Game info is null!");
            return false;
        }

        if (gameInfo.rules == null)
        {
            Debug.LogError($"[StandardGameLibrary] {gameInfo.gameName} has no rules!");
            return false;
        }

        string errorMessage;
        if (!gameInfo.rules.ValidateRules(out errorMessage))
        {
            Debug.LogError($"[StandardGameLibrary] {gameInfo.gameName} validation failed: {errorMessage}");
            return false;
        }

        Debug.Log($"[StandardGameLibrary] {gameInfo.gameName} validated successfully");
        return true;
    }

    #endregion

    #region Debug

    [ContextMenu("List All Standard Games")]
    private void DebugListAllStandardGames()
    {
        List<SavedGameInfo> games = GetAllStandardGames();
        
        Debug.Log("=== Standard Games Library ===");
        foreach (var game in games)
        {
            Debug.Log($"- {game.gameName} ({game.gameType}) - {game.playerCount} players");
            Debug.Log($"  Rules Summary:\n{game.rules.GetRulesSummary()}");
        }
    }

    [ContextMenu("Validate All Standard Games")]
    private void DebugValidateAllStandardGames()
    {
        List<SavedGameInfo> games = GetAllStandardGames();
        
        Debug.Log("=== Validating Standard Games ===");
        foreach (var game in games)
        {
            bool valid = ValidateStandardGame(game);
            Debug.Log($"- {game.gameName}: {(valid ? "VALID" : "INVALID")}");
        }
    }

    #endregion
}

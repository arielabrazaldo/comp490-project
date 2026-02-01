using UnityEngine;

/// <summary>
/// Analyzes GameRules to determine what type of game has been configured.
/// This bridges custom rule creation with actual gameplay by identifying
/// the closest matching game type or creating a hybrid game mode.
/// </summary>
public class CustomGameAnalyzer : MonoBehaviour
{
    private static CustomGameAnalyzer instance;
    public static CustomGameAnalyzer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CustomGameAnalyzer>();
                
                // Auto-create if not found in scene
                if (instance == null)
                {
                    GameObject go = new GameObject("CustomGameAnalyzer");
                    instance = go.AddComponent<CustomGameAnalyzer>();
                    Debug.Log("[CustomGameAnalyzer] Auto-created CustomGameAnalyzer GameObject");
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Game types that can be spawned from custom rules
    /// </summary>
    public enum DetectedGameType
    {
        DiceRace,           // Simple race game
        Monopoly,           // Property trading game
        Battleships,        // Naval combat game
        Hybrid,             // Custom hybrid of multiple game types
        Unknown             // Couldn't determine game type
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CustomGameAnalyzer] Initialized and set to DontDestroyOnLoad");
        }
        else if (instance != this)
        {
            Debug.Log("[CustomGameAnalyzer] Duplicate instance found, destroying");
            Destroy(gameObject);
        }
    }

    #region Game Type Detection

    /// <summary>
    /// Analyzes rules and returns the detected game type
    /// </summary>
    public DetectedGameType AnalyzeGameRules(GameRules rules)
    {
        if (rules == null)
        {
            Debug.LogError("[CustomGameAnalyzer] Rules are null!");
            return DetectedGameType.Unknown;
        }

        Debug.Log($"[CustomGameAnalyzer] Analyzing rules: {rules.GetRulesSummary()}");

        // Calculate confidence scores for each game type
        float monopolyScore = CalculateMonopolyScore(rules);
        float battleshipsScore = CalculateBattleshipsScore(rules);
        float diceRaceScore = CalculateDiceRaceScore(rules);

        Debug.Log($"[CustomGameAnalyzer] Scores - Monopoly: {monopolyScore:F2}, Battleships: {battleshipsScore:F2}, DiceRace: {diceRaceScore:F2}");

        // Determine game type based on highest score
        float maxScore = Mathf.Max(monopolyScore, battleshipsScore, diceRaceScore);
        
        // If no clear winner (scores too close), it's a hybrid
        if (maxScore < 0.6f)
        {
            Debug.Log("[CustomGameAnalyzer] No clear game type match - detected as Hybrid");
            return DetectedGameType.Hybrid;
        }

        if (monopolyScore == maxScore)
        {
            Debug.Log("[CustomGameAnalyzer] Detected game type: Monopoly");
            return DetectedGameType.Monopoly;
        }
        else if (battleshipsScore == maxScore)
        {
            Debug.Log("[CustomGameAnalyzer] Detected game type: Battleships");
            return DetectedGameType.Battleships;
        }
        else if (diceRaceScore == maxScore)
        {
            Debug.Log("[CustomGameAnalyzer] Detected game type: DiceRace");
            return DetectedGameType.DiceRace;
        }

        return DetectedGameType.Unknown;
    }

    /// <summary>
    /// Calculate confidence score (0-1) that rules match Monopoly
    /// </summary>
    private float CalculateMonopolyScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Monopoly characteristics
        if (rules.enableCurrency) { score += 1f; checks++; }           // Currency is essential
        if (!rules.separatePlayerBoards) { score += 1f; checks++; }    // Shared board
        if (rules.canSeeEnemyTokens) { score += 1f; checks++; }        // All players visible
        if (rules.canPurchaseProperties) { score += 1f; checks++; }    // Property buying
        if (rules.enablePropertyTrading) { score += 1f; checks++; }    // Property trading
        if (rules.startingMoney > 0) { score += 0.5f; checks++; }      // Has starting money
        if (rules.enableBankruptcy) { score += 0.5f; checks++; }       // Bankruptcy rules
        if (rules.enableRentCollection) { score += 1f; checks++; }     // Rent collection

        return checks > 0 ? score / checks : 0f;
    }

    /// <summary>
    /// Calculate confidence score (0-1) that rules match Battleships
    /// </summary>
    private float CalculateBattleshipsScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Battleships characteristics
        if (rules.separatePlayerBoards) { score += 1f; checks++; }     // Separate boards
        if (!rules.canSeeEnemyTokens) { score += 1f; checks++; }       // Hidden enemy pieces
        if (!rules.enableCurrency) { score += 0.5f; checks++; }        // Usually no currency
        if (rules.enableCombat) { score += 1f; checks++; }             // Combat mechanics
        if (rules.enableShipPlacement) { score += 1f; checks++; }      // Ship placement
        if (rules.tilesPerSide >= 8) { score += 0.5f; checks++; }      // Larger board (8x8 or more)

        return checks > 0 ? score / checks : 0f;
    }

    /// <summary>
    /// Calculate confidence score (0-1) that rules match Dice Race
    /// </summary>
    private float CalculateDiceRaceScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Dice Race characteristics
        if (!rules.enableCurrency) { score += 0.5f; checks++; }        // Simple, no currency
        if (!rules.separatePlayerBoards) { score += 1f; checks++; }    // Shared board
        if (rules.canSeeEnemyTokens) { score += 1f; checks++; }        // All players visible
        if (!rules.canPurchaseProperties) { score += 1f; checks++; }   // No properties
        if (!rules.enablePropertyTrading) { score += 1f; checks++; }   // No trading
        if (!rules.enableCombat) { score += 1f; checks++; }            // No combat
        if (rules.winCondition == WinCondition.ReachGoal) { score += 1f; checks++; }  // Race to finish

        return checks > 0 ? score / checks : 0f;
    }

    #endregion

    #region Rule Compatibility Checks

    /// <summary>
    /// Check if rules are compatible with a specific game type
    /// </summary>
    public bool AreRulesCompatible(GameRules rules, DetectedGameType gameType)
    {
        switch (gameType)
        {
            case DetectedGameType.Monopoly:
                return CheckMonopolyCompatibility(rules);
            case DetectedGameType.Battleships:
                return CheckBattleshipsCompatibility(rules);
            case DetectedGameType.DiceRace:
                return CheckDiceRaceCompatibility(rules);
            case DetectedGameType.Hybrid:
                return true; // Hybrid accepts any rules
            default:
                return false;
        }
    }

    private bool CheckMonopolyCompatibility(GameRules rules)
    {
        // Monopoly requires currency
        if (!rules.enableCurrency)
        {
            Debug.LogWarning("[CustomGameAnalyzer] Monopoly requires currency to be enabled");
            return false;
        }

        // Monopoly requires shared board
        if (rules.separatePlayerBoards)
        {
            Debug.LogWarning("[CustomGameAnalyzer] Monopoly requires a shared board");
            return false;
        }

        return true;
    }

    private bool CheckBattleshipsCompatibility(GameRules rules)
    {
        // Battleships requires separate boards
        if (!rules.separatePlayerBoards)
        {
            Debug.LogWarning("[CustomGameAnalyzer] Battleships requires separate player boards");
            return false;
        }

        // Battleships needs adequate board size
        if (rules.tilesPerSide < 8)
        {
            Debug.LogWarning("[CustomGameAnalyzer] Battleships requires at least an 8x8 board");
            return false;
        }

        return true;
    }

    private bool CheckDiceRaceCompatibility(GameRules rules)
    {
        // Dice Race is very flexible, minimal requirements
        if (rules.separatePlayerBoards)
        {
            Debug.LogWarning("[CustomGameAnalyzer] Dice Race typically uses a shared board");
        }

        return true; // Dice Race is compatible with most rule sets
    }

    #endregion

    #region Game Configuration

    /// <summary>
    /// Get recommended board size for detected game type
    /// </summary>
    public int GetRecommendedBoardSize(DetectedGameType gameType, GameRules rules)
    {
        switch (gameType)
        {
            case DetectedGameType.Monopoly:
                return 40; // Monopoly standard board (40 spaces in loop)
            case DetectedGameType.Battleships:
                return Mathf.Max(rules.tilesPerSide, 10); // Minimum 10x10
            case DetectedGameType.DiceRace:
                return Mathf.Clamp(rules.tilesPerSide, 10, 100);
            case DetectedGameType.Hybrid:
                return rules.tilesPerSide;
            default:
                return 20; // Default fallback
        }
    }

    /// <summary>
    /// Get recommended player count for detected game type
    /// </summary>
    public int GetRecommendedPlayerCount(DetectedGameType gameType, GameRules rules)
    {
        switch (gameType)
        {
            case DetectedGameType.Monopoly:
                return Mathf.Clamp(rules.maxPlayers, 2, 4); // 2-4 players
            case DetectedGameType.Battleships:
                return 2; // Battleships is typically 1v1
            case DetectedGameType.DiceRace:
                return Mathf.Clamp(rules.maxPlayers, 2, 4); // 2-4 players
            case DetectedGameType.Hybrid:
                return rules.maxPlayers;
            default:
                return 2; // Default fallback
        }
    }

    #endregion

    #region Validation and Reporting

    /// <summary>
    /// Generate a detailed analysis report of the rules
    /// </summary>
    public string GenerateAnalysisReport(GameRules rules)
    {
        DetectedGameType gameType = AnalyzeGameRules(rules);
        
        string report = "=== Custom Game Analysis Report ===\n\n";
        report += $"Detected Game Type: {gameType}\n";
        report += $"Compatible: {AreRulesCompatible(rules, gameType)}\n\n";

        report += "Rule Configuration:\n";
        report += $"- Currency Enabled: {rules.enableCurrency}\n";
        report += $"- Starting Money: ${rules.startingMoney}\n";
        report += $"- Separate Boards: {rules.separatePlayerBoards}\n";
        report += $"- Enemy Visibility: {rules.canSeeEnemyTokens}\n";
        report += $"- Property Purchase: {rules.canPurchaseProperties}\n";
        report += $"- Property Trading: {rules.enablePropertyTrading}\n";
        report += $"- Combat: {rules.enableCombat}\n";
        report += $"- Board Size: {rules.tilesPerSide}\n";
        report += $"- Max Players: {rules.maxPlayers}\n";
        report += $"- Win Condition: {rules.winCondition}\n\n";

        report += "Recommendations:\n";
        report += $"- Board Size: {GetRecommendedBoardSize(gameType, rules)}\n";
        report += $"- Player Count: {GetRecommendedPlayerCount(gameType, rules)}\n";

        return report;
    }

    /// <summary>
    /// Validate rules and return list of warnings/errors
    /// </summary>
    public string[] ValidateRules(GameRules rules)
    {
        var warnings = new System.Collections.Generic.List<string>();

        if (rules.enableCurrency && rules.startingMoney <= 0)
        {
            warnings.Add("Currency enabled but starting money is 0 or negative");
        }

        if (!rules.canSeeEnemyTokens && rules.enemyTokenVisibilityRange == 0)
        {
            warnings.Add("Enemy tokens completely hidden - this may cause gameplay issues");
        }

        if (rules.separatePlayerBoards && rules.enablePropertyTrading)
        {
            warnings.Add("Property trading with separate boards may not work as expected");
        }

        if (rules.tilesPerSide < 4)
        {
            warnings.Add("Board size is very small - may cause gameplay issues");
        }

        if (rules.maxPlayers < 2)
        {
            warnings.Add("At least 2 players required for a game");
        }

        DetectedGameType gameType = AnalyzeGameRules(rules);
        if (!AreRulesCompatible(rules, gameType))
        {
            warnings.Add($"Rules are not compatible with detected game type: {gameType}");
        }

        return warnings.ToArray();
    }

    #endregion

    #region Debug

    [ContextMenu("Analyze Current Rules")]
    private void DebugAnalyzeCurrentRules()
    {
        if (RuleEditorManager.Instance != null)
        {
            GameRules rules = RuleEditorManager.Instance.GetCurrentRules();
            string report = GenerateAnalysisReport(rules);
            Debug.Log(report);

            string[] warnings = ValidateRules(rules);
            if (warnings.Length > 0)
            {
                Debug.LogWarning("=== Rule Validation Warnings ===");
                foreach (string warning in warnings)
                {
                    Debug.LogWarning($"- {warning}");
                }
            }
        }
        else
        {
            Debug.LogError("RuleEditorManager not found!");
        }
    }

    #endregion
}

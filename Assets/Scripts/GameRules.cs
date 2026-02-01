using System;
using UnityEngine;

/// <summary>
/// Win condition types for board games
/// </summary>
public enum WinCondition
{
    ReachGoal,              // Reach a specific goal position (Dice Race)
    LastPlayerStanding,     // Be the last player remaining (Monopoly/Battleships)
    HighestScore,           // Have the highest score at end
    EliminateAllEnemies,    // Eliminate all other players (Battleships)
    MoneyThreshold          // Reach a money threshold (Monopoly)
}

/// <summary>
/// Represents a set of game rules that can be applied to board games.
/// Supports features from Monopoly, Battleships, and custom hybrid games.
/// </summary>
[Serializable]
public class GameRules
{
    [Header("Currency System")]
    public bool enableCurrency = true;
    public int startingMoney = 1500;
    public int passGoBonus = 200;
    
    [Header("Board Configuration")]
    public bool separatePlayerBoards = false; // false = shared board (Monopoly), true = separate boards (Battleships)
    public int tilesPerSide = 20; // Board size (for dice race or grid-based games)
    
    [Header("Property System")]
    public bool canPurchaseProperties = false; // Enable property ownership (Monopoly)
    public bool enablePropertyTrading = false; // Allow trading properties between players
    public bool enableRentCollection = false; // Collect rent when landing on owned properties
    public bool enableBankruptcy = false; // Players can go bankrupt
    
    [Header("Combat System")]
    public bool enableCombat = false; // Enable combat mechanics (Battleships)
    public bool enableShipPlacement = false; // Enable ship placement phase (Battleships)
    
    [Header("Visibility Settings")]
    public bool canSeeEnemyTokens = true; // false = hidden like Battleships, true = visible like Monopoly
    public int enemyTokenVisibilityRange = -1; // -1 = unlimited, >0 = range in tiles
    
    [Header("Player Settings")]
    public int minPlayers = 2;
    public int maxPlayers = 4;
    
    [Header("Win Conditions")]
    public WinCondition winCondition = WinCondition.LastPlayerStanding;
    public bool lastPlayerStandingWins = true;
    public bool moneyThresholdWins = false;
    public int winningMoneyThreshold = 5000;
    
    [Header("Dice Mechanics")]
    public bool enableCustomDice = false;
    public int numberOfDice = 1; // How many dice to roll
    public int diceSides = 6; // Number of sides per die
    public bool duplicatesGrantExtraTurn = false; // Rolling duplicates grants extra turn
    public int duplicatesRequired = 2; // How many matching dice needed for extra turn
    
    [Header("Resource System")]
    public bool enableResources = false;
    public int numberOfResources = 0; // How many resource types
    public string[] resourceNames = new string[0]; // Names of each resource type
    public bool enableResourceCap = false; // Whether resources have a maximum
    public int maxResourcesPerType = 10; // Maximum amount per resource type
    
    [Header("Advanced Gameplay")]
    public bool allowBankruptcy = true;
    public bool allowTrading = true;
    
    /// <summary>
    /// Creates default Monopoly rules
    /// </summary>
    public static GameRules CreateMonopolyRules()
    {
        return new GameRules
        {
            enableCurrency = true,
            startingMoney = 1500,
            passGoBonus = 200,
            separatePlayerBoards = false, // Shared board
            tilesPerSide = 40, // Monopoly standard board
            canPurchaseProperties = true,
            enablePropertyTrading = true,
            enableRentCollection = true,
            enableBankruptcy = true,
            enableCombat = false,
            enableShipPlacement = false,
            canSeeEnemyTokens = true, // All players visible
            enemyTokenVisibilityRange = -1, // Unlimited visibility
            minPlayers = 2,
            maxPlayers = 4,
            winCondition = WinCondition.LastPlayerStanding,
            lastPlayerStandingWins = true,
            moneyThresholdWins = false,
            enableCustomDice = false, // Use default dice
            numberOfDice = 2,
            diceSides = 6,
            duplicatesGrantExtraTurn = true, // Doubles in Monopoly
            duplicatesRequired = 2,
            enableResources = false,
            numberOfResources = 0,
            resourceNames = new string[0],
            enableResourceCap = false,
            maxResourcesPerType = 10,
            allowBankruptcy = true,
            allowTrading = true
        };
    }
    
    /// <summary>
    /// Creates default Battleships rules
    /// </summary>
    public static GameRules CreateBattleshipsRules()
    {
        return new GameRules
        {
            enableCurrency = false,
            startingMoney = 0,
            passGoBonus = 0,
            separatePlayerBoards = true, // Separate boards
            tilesPerSide = 10, // 10x10 grid
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableRentCollection = false,
            enableBankruptcy = false,
            enableCombat = true,
            enableShipPlacement = true,
            canSeeEnemyTokens = false, // Enemy ships hidden
            enemyTokenVisibilityRange = 0, // No visibility
            minPlayers = 2,
            maxPlayers = 2,
            winCondition = WinCondition.EliminateAllEnemies,
            lastPlayerStandingWins = true,
            moneyThresholdWins = false,
            enableCustomDice = false,
            numberOfDice = 1,
            diceSides = 6,
            duplicatesGrantExtraTurn = false,
            duplicatesRequired = 2,
            enableResources = false,
            numberOfResources = 0,
            resourceNames = new string[0],
            enableResourceCap = false,
            maxResourcesPerType = 10,
            allowBankruptcy = false,
            allowTrading = false
        };
    }
    
    /// <summary>
    /// Creates default Dice Race rules
    /// </summary>
    public static GameRules CreateDiceRaceRules()
    {
        return new GameRules
        {
            enableCurrency = false,
            startingMoney = 0,
            passGoBonus = 0,
            separatePlayerBoards = false, // Shared board
            tilesPerSide = 20,
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableRentCollection = false,
            enableBankruptcy = false,
            enableCombat = false,
            enableShipPlacement = false,
            canSeeEnemyTokens = true, // All players visible
            enemyTokenVisibilityRange = -1, // Unlimited visibility
            minPlayers = 2,
            maxPlayers = 4,
            winCondition = WinCondition.ReachGoal,
            lastPlayerStandingWins = true, // CRITICAL FIX: Enable this to pass validation (first to finish wins)
            moneyThresholdWins = false,
            enableCustomDice = false,
            numberOfDice = 1,
            diceSides = 6,
            duplicatesGrantExtraTurn = false,
            duplicatesRequired = 2,
            enableResources = false,
            numberOfResources = 0,
            resourceNames = new string[0],
            enableResourceCap = false,
            maxResourcesPerType = 10,
            allowBankruptcy = false,
            allowTrading = false
        };
    }
    
    /// <summary>
    /// Creates default custom/hybrid rules with ALL features enabled
    /// This preset enables everything, allowing users to customize from there
    /// </summary>
    public static GameRules CreateCustomRules()
    {
        return new GameRules
        {
            // Enable ALL features for maximum customization
            enableCurrency = true,
            startingMoney = 1000,
            passGoBonus = 100,
            separatePlayerBoards = true, // Enable separate boards
            tilesPerSide = 20,
            canPurchaseProperties = true,
            enablePropertyTrading = true,
            enableRentCollection = true,
            enableBankruptcy = true,
            enableCombat = true,
            enableShipPlacement = false,
            canSeeEnemyTokens = true, // Enable visibility
            enemyTokenVisibilityRange = 5, // Set a limited range (not unlimited)
            minPlayers = 2,
            maxPlayers = 4,
            winCondition = WinCondition.LastPlayerStanding,
            lastPlayerStandingWins = true, // Enable both victory conditions
            moneyThresholdWins = true, // Enable money threshold
            winningMoneyThreshold = 3000,
            enableCustomDice = true, // Enable custom dice
            numberOfDice = 2,
            diceSides = 6,
            duplicatesGrantExtraTurn = true,
            duplicatesRequired = 2,
            enableResources = true, // Enable resources
            numberOfResources = 3,
            resourceNames = new string[] { "Wood", "Stone", "Wheat" },
            enableResourceCap = true,
            maxResourcesPerType = 10,
            allowBankruptcy = true, // Enable bankruptcy
            allowTrading = true // Enable trading
        };
    }
    
    /// <summary>
    /// Validates the current rules configuration
    /// </summary>
    public bool ValidateRules(out string errorMessage)
    {
        if (minPlayers < 1)
        {
            errorMessage = "Minimum players must be at least 1";
            return false;
        }
        
        if (maxPlayers < minPlayers)
        {
            errorMessage = "Maximum players must be greater than or equal to minimum players";
            return false;
        }
        
        if (enableCurrency && startingMoney < 0)
        {
            errorMessage = "Starting money cannot be negative";
            return false;
        }
        
        if (enableCurrency && passGoBonus < 0)
        {
            errorMessage = "Pass GO bonus cannot be negative";
            return false;
        }
        
        if (moneyThresholdWins && !enableCurrency)
        {
            errorMessage = "Money threshold victory requires currency to be enabled";
            return false;
        }
        
        if (moneyThresholdWins && winningMoneyThreshold <= 0)
        {
            errorMessage = "Winning money threshold must be greater than 0";
            return false;
        }
        
        if (!lastPlayerStandingWins && !moneyThresholdWins)
        {
            errorMessage = "At least one victory condition must be enabled";
            return false;
        }
        
        if (tilesPerSide < 4)
        {
            errorMessage = "Board size must be at least 4";
            return false;
        }
        
        // Dice validation
        if (enableCustomDice && numberOfDice < 1)
        {
            errorMessage = "Number of dice must be at least 1";
            return false;
        }
        
        if (enableCustomDice && diceSides < 2)
        {
            errorMessage = "Dice must have at least 2 sides";
            return false;
        }
        
        if (enableCustomDice && duplicatesGrantExtraTurn && duplicatesRequired < 2)
        {
            errorMessage = "Duplicates required must be at least 2";
            return false;
        }
        
        if (enableCustomDice && duplicatesGrantExtraTurn && duplicatesRequired > numberOfDice)
        {
            errorMessage = "Duplicates required cannot exceed number of dice";
            return false;
        }
        
        // Resource validation
        if (enableResources && numberOfResources < 1)
        {
            errorMessage = "Number of resources must be at least 1 when resources are enabled";
            return false;
        }
        
        if (enableResources && (resourceNames == null || resourceNames.Length != numberOfResources))
        {
            errorMessage = "Resource names array must match number of resources";
            return false;
        }
        
        if (enableResources)
        {
            for (int i = 0; i < resourceNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(resourceNames[i]))
                {
                    errorMessage = $"Resource {i + 1} name cannot be empty";
                    return false;
                }
            }
        }
        
        if (enableResources && enableResourceCap && maxResourcesPerType < 1)
        {
            errorMessage = "Maximum resources per type must be at least 1";
            return false;
        }
        
        errorMessage = string.Empty;
        return true;
    }
    
    /// <summary>
    /// Creates a deep copy of these rules
    /// </summary>
    public GameRules Clone()
    {
        return new GameRules
        {
            enableCurrency = this.enableCurrency,
            startingMoney = this.startingMoney,
            passGoBonus = this.passGoBonus,
            separatePlayerBoards = this.separatePlayerBoards,
            tilesPerSide = this.tilesPerSide,
            canPurchaseProperties = this.canPurchaseProperties,
            enablePropertyTrading = this.enablePropertyTrading,
            enableRentCollection = this.enableRentCollection,
            enableBankruptcy = this.enableBankruptcy,
            enableCombat = this.enableCombat,
            enableShipPlacement = this.enableShipPlacement,
            canSeeEnemyTokens = this.canSeeEnemyTokens,
            enemyTokenVisibilityRange = this.enemyTokenVisibilityRange,
            minPlayers = this.minPlayers,
            maxPlayers = this.maxPlayers,
            winCondition = this.winCondition,
            lastPlayerStandingWins = this.lastPlayerStandingWins,
            moneyThresholdWins = this.moneyThresholdWins,
            winningMoneyThreshold = this.winningMoneyThreshold,
            enableCustomDice = this.enableCustomDice,
            numberOfDice = this.numberOfDice,
            diceSides = this.diceSides,
            duplicatesGrantExtraTurn = this.duplicatesGrantExtraTurn,
            duplicatesRequired = this.duplicatesRequired,
            enableResources = this.enableResources,
            numberOfResources = this.numberOfResources,
            resourceNames = (string[])this.resourceNames?.Clone(),
            enableResourceCap = this.enableResourceCap,
            maxResourcesPerType = this.maxResourcesPerType,
            allowBankruptcy = this.allowBankruptcy,
            allowTrading = this.allowTrading
        };
    }
    
    /// <summary>
    /// Gets a summary string of these rules
    /// </summary>
    public string GetRulesSummary()
    {
        string summary = "Game Rules Summary:\n";
        summary += $"- Currency: {(enableCurrency ? $"Enabled (Start: ${startingMoney}, Pass GO: ${passGoBonus})" : "Disabled")}\n";
        summary += $"- Board: {(separatePlayerBoards ? "Separate (Battleships-style)" : "Shared (Monopoly-style)")} [{tilesPerSide} tiles]\n";
        summary += $"- Properties: {(canPurchaseProperties ? "Enabled" : "Disabled")}\n";
        summary += $"- Combat: {(enableCombat ? "Enabled" : "Disabled")}\n";
        
        if (canSeeEnemyTokens)
        {
            string range = enemyTokenVisibilityRange == -1 ? "unlimited" : $"{enemyTokenVisibilityRange} tiles";
            summary += $"- Visibility: Visible ({range})\n";
        }
        else
        {
            summary += "- Visibility: Hidden (Fog of War)\n";
        }
        
        summary += $"- Players: {minPlayers}-{maxPlayers}\n";
        summary += $"- Win Condition: {winCondition}\n";
        
        // Dice summary
        if (enableCustomDice)
        {
            summary += $"- Dice: {numberOfDice}d{diceSides}";
            if (duplicatesGrantExtraTurn)
            {
                summary += $" (Extra turn on {duplicatesRequired}+ duplicates)";
            }
            summary += "\n";
        }
        
        // Resource summary
        if (enableResources)
        {
            summary += $"- Resources: {numberOfResources} types (";
            summary += string.Join(", ", resourceNames);
            summary += ")";
            if (enableResourceCap)
            {
                summary += $" [Max: {maxResourcesPerType} each]";
            }
            summary += "\n";
        }
        
        summary += $"- Bankruptcy: {(allowBankruptcy ? "Allowed" : "Disabled")}\n";
        summary += $"- Trading: {(allowTrading ? "Allowed" : "Disabled")}\n";
        
        return summary;
    }
}

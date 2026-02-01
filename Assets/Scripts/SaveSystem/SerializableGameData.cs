using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable wrapper for SavedGameInfo that can be saved to JSON.
/// Includes support for custom board layouts for future board editor.
/// </summary>
[Serializable]
public class SerializableGameData
{
    // Metadata
    public string gameName;
    public string gameType;
    public int playerCount;
    public bool isStandardGame;
    public string createdDate;
    public string lastModifiedDate;

    // Game Rules (all settings)
    public SerializableGameRules rules;

    // Board Layout (for custom boards)
    public SerializableBoardLayout boardLayout;

    /// <summary>
    /// Create from SavedGameInfo
    /// </summary>
    public SerializableGameData(SavedGameInfo gameInfo)
    {
        gameName = gameInfo.gameName;
        gameType = gameInfo.gameType;
        playerCount = gameInfo.playerCount;
        isStandardGame = gameInfo.isStandardGame;
        createdDate = gameInfo.createdDate.ToString("o"); // ISO 8601 format
        lastModifiedDate = gameInfo.lastModifiedDate.ToString("o");

        // Convert GameRules
        rules = new SerializableGameRules(gameInfo.rules);

        // Board layout (if exists)
        boardLayout = new SerializableBoardLayout();
        // TODO: Populate from board editor when implemented
    }

    /// <summary>
    /// Convert back to SavedGameInfo
    /// </summary>
    public SavedGameInfo ToSavedGameInfo()
    {
        SavedGameInfo gameInfo = new SavedGameInfo(
            gameName,
            gameType,
            playerCount,
            rules.ToGameRules(),
            isStandardGame
        );

        // Restore dates
        gameInfo.createdDate = DateTime.Parse(createdDate);
        gameInfo.lastModifiedDate = DateTime.Parse(lastModifiedDate);

        return gameInfo;
    }
}

/// <summary>
/// Serializable version of GameRules
/// </summary>
[Serializable]
public class SerializableGameRules
{
    // Currency System
    public bool enableCurrency;
    public int startingMoney;
    public int passGoBonus;

    // Board Configuration
    public bool separatePlayerBoards;
    public int tilesPerSide;

    // Property System
    public bool canPurchaseProperties;
    public bool enablePropertyTrading;
    public bool enableRentCollection;
    public bool enableBankruptcy;

    // Combat System
    public bool enableCombat;
    public bool enableShipPlacement;

    // Visibility Settings
    public bool canSeeEnemyTokens;
    public int enemyTokenVisibilityRange;

    // Player Settings
    public int minPlayers;
    public int maxPlayers;

    // Win Conditions
    public string winCondition; // Stored as string for JSON
    public bool lastPlayerStandingWins;
    public bool moneyThresholdWins;
    public int winningMoneyThreshold;

    // Dice Mechanics
    public bool enableCustomDice;
    public int numberOfDice;
    public int diceSides;
    public bool duplicatesGrantExtraTurn;
    public int duplicatesRequired;

    // Resource System
    public bool enableResources;
    public int numberOfResources;
    public string[] resourceNames;
    public bool enableResourceCap;
    public int maxResourcesPerType;

    // Advanced Gameplay
    public bool allowBankruptcy;
    public bool allowTrading;

    /// <summary>
    /// Create from GameRules
    /// </summary>
    public SerializableGameRules(GameRules rules)
    {
        enableCurrency = rules.enableCurrency;
        startingMoney = rules.startingMoney;
        passGoBonus = rules.passGoBonus;

        separatePlayerBoards = rules.separatePlayerBoards;
        tilesPerSide = rules.tilesPerSide;

        canPurchaseProperties = rules.canPurchaseProperties;
        enablePropertyTrading = rules.enablePropertyTrading;
        enableRentCollection = rules.enableRentCollection;
        enableBankruptcy = rules.enableBankruptcy;

        enableCombat = rules.enableCombat;
        enableShipPlacement = rules.enableShipPlacement;

        canSeeEnemyTokens = rules.canSeeEnemyTokens;
        enemyTokenVisibilityRange = rules.enemyTokenVisibilityRange;

        minPlayers = rules.minPlayers;
        maxPlayers = rules.maxPlayers;

        winCondition = rules.winCondition.ToString();
        lastPlayerStandingWins = rules.lastPlayerStandingWins;
        moneyThresholdWins = rules.moneyThresholdWins;
        winningMoneyThreshold = rules.winningMoneyThreshold;

        enableCustomDice = rules.enableCustomDice;
        numberOfDice = rules.numberOfDice;
        diceSides = rules.diceSides;
        duplicatesGrantExtraTurn = rules.duplicatesGrantExtraTurn;
        duplicatesRequired = rules.duplicatesRequired;

        enableResources = rules.enableResources;
        numberOfResources = rules.numberOfResources;
        resourceNames = rules.resourceNames != null ? (string[])rules.resourceNames.Clone() : new string[0];
        enableResourceCap = rules.enableResourceCap;
        maxResourcesPerType = rules.maxResourcesPerType;

        allowBankruptcy = rules.allowBankruptcy;
        allowTrading = rules.allowTrading;
    }

    /// <summary>
    /// Convert back to GameRules
    /// </summary>
    public GameRules ToGameRules()
    {
        GameRules rules = new GameRules
        {
            enableCurrency = enableCurrency,
            startingMoney = startingMoney,
            passGoBonus = passGoBonus,

            separatePlayerBoards = separatePlayerBoards,
            tilesPerSide = tilesPerSide,

            canPurchaseProperties = canPurchaseProperties,
            enablePropertyTrading = enablePropertyTrading,
            enableRentCollection = enableRentCollection,
            enableBankruptcy = enableBankruptcy,

            enableCombat = enableCombat,
            enableShipPlacement = enableShipPlacement,

            canSeeEnemyTokens = canSeeEnemyTokens,
            enemyTokenVisibilityRange = enemyTokenVisibilityRange,

            minPlayers = minPlayers,
            maxPlayers = maxPlayers,

            winCondition = (WinCondition)Enum.Parse(typeof(WinCondition), winCondition),
            lastPlayerStandingWins = lastPlayerStandingWins,
            moneyThresholdWins = moneyThresholdWins,
            winningMoneyThreshold = winningMoneyThreshold,

            enableCustomDice = enableCustomDice,
            numberOfDice = numberOfDice,
            diceSides = diceSides,
            duplicatesGrantExtraTurn = duplicatesGrantExtraTurn,
            duplicatesRequired = duplicatesRequired,

            enableResources = enableResources,
            numberOfResources = numberOfResources,
            resourceNames = resourceNames != null ? (string[])resourceNames.Clone() : new string[0],
            enableResourceCap = enableResourceCap,
            maxResourcesPerType = maxResourcesPerType,

            allowBankruptcy = allowBankruptcy,
            allowTrading = allowTrading
        };

        return rules;
    }
}

/// <summary>
/// Serializable board layout data for custom boards (future board editor)
/// </summary>
[Serializable]
public class SerializableBoardLayout
{
    // Board type
    public string boardType; // "Linear", "Grid", "Circular", "Custom"

    // Board dimensions
    public int rows;
    public int columns;

    // Tile data (for custom boards)
    public List<SerializableTile> tiles;

    // Board shape (for irregular boards)
    public bool[] activeTiles; // Flattened 2D array

    // Special spaces
    public List<SerializableSpecialSpace> specialSpaces;

    public SerializableBoardLayout()
    {
        boardType = "Linear";
        rows = 1;
        columns = 20;
        tiles = new List<SerializableTile>();
        activeTiles = new bool[0];
        specialSpaces = new List<SerializableSpecialSpace>();
    }
}

/// <summary>
/// Serializable tile data for custom board editor
/// </summary>
[Serializable]
public class SerializableTile
{
    public int x;
    public int y;
    public bool isActive;
    public string tileType; // "Normal", "Property", "Start", "Goal", "Special"
    public string tileName;
    public string tileColor;

    public SerializableTile(int x, int y, bool isActive = true)
    {
        this.x = x;
        this.y = y;
        this.isActive = isActive;
        this.tileType = "Normal";
        this.tileName = "";
        this.tileColor = "#FFFFFF";
    }
}

/// <summary>
/// Serializable special space data (Go, Jail, Free Parking, etc.)
/// </summary>
[Serializable]
public class SerializableSpecialSpace
{
    public int position;
    public string spaceType; // "Go", "Jail", "FreeParking", "GoToJail", "Chance", "CommunityChest", "Tax"
    public string spaceName;
    public int spaceValue; // For taxes, bonuses, etc.

    public SerializableSpecialSpace(int position, string spaceType, string spaceName = "")
    {
        this.position = position;
        this.spaceType = spaceType;
        this.spaceName = spaceName;
        this.spaceValue = 0;
    }
}

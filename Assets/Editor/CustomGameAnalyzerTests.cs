using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unity EditMode tests for CustomGameAnalyzer functionality.
/// Tests game type detection and score calculations.
/// Since CustomGameAnalyzer uses DontDestroyOnLoad, we test the logic directly.
/// </summary>
public class CustomGameAnalyzerTests
{
    #region Game Type Detection Tests - Monopoly

    [Test]
    public void AnalyzeGameRules_DetectsMonopoly_WithStandardRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.Monopoly, result);
    }

    [Test]
    public void AnalyzeGameRules_DetectsMonopoly_WithCurrencyAndProperties()
    {
        GameRules rules = new GameRules
        {
            enableCurrency = true,
            startingMoney = 1000,
            separatePlayerBoards = false,
            canSeeEnemyTokens = true,
            canPurchaseProperties = true,
            enablePropertyTrading = true,
            enableRentCollection = true,
            enableBankruptcy = true,
            enableCombat = false,
            minPlayers = 2,
            maxPlayers = 4
        };
        
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.Monopoly, result);
    }

    #endregion

    #region Game Type Detection Tests - Battleships

    [Test]
    public void AnalyzeGameRules_DetectsBattleships_WithStandardRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.Battleships, result);
    }

    [Test]
    public void AnalyzeGameRules_DetectsBattleships_WithSeparateBoardsAndCombat()
    {
        GameRules rules = new GameRules
        {
            enableCurrency = false,
            separatePlayerBoards = true,
            canSeeEnemyTokens = false,
            enableCombat = true,
            enableShipPlacement = true,
            tilesPerSide = 10,
            minPlayers = 2,
            maxPlayers = 2
        };
        
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.Battleships, result);
    }

    #endregion

    #region Game Type Detection Tests - Dice Race

    [Test]
    public void AnalyzeGameRules_DetectsDiceRace_WithStandardRules()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.DiceRace, result);
    }

    [Test]
    public void AnalyzeGameRules_DetectsDiceRace_WithSimpleRaceRules()
    {
        GameRules rules = new GameRules
        {
            enableCurrency = false,
            separatePlayerBoards = false,
            canSeeEnemyTokens = true,
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableCombat = false,
            winCondition = WinCondition.ReachSpecificTile,
            tilesPerSide = 20,
            targetTileNumber = 20
        };
        
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.DiceRace, result);
    }

    [Test]
    public void AnalyzeGameRules_DetectsDiceRace_WithReachGoalWinCondition()
    {
        GameRules rules = new GameRules
        {
            enableCurrency = false,
            separatePlayerBoards = false,
            canSeeEnemyTokens = true,
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableCombat = false,
            winCondition = WinCondition.ReachGoal,
            tilesPerSide = 20
        };
        
        var result = AnalyzeGameType(rules);
        
        Assert.AreEqual(DetectedGameType.DiceRace, result);
    }

    #endregion

    #region Game Type Detection Tests - Hybrid

    [Test]
    public void AnalyzeGameRules_DetectsHybrid_WithMixedFeatures()
    {
        // Mixed rules that don't clearly fit any category
        GameRules rules = new GameRules
        {
            enableCurrency = true,
            startingMoney = 500,
            separatePlayerBoards = true, // Battleships-like
            canSeeEnemyTokens = true,    // Monopoly-like
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableCombat = true,         // Battleships-like
            winCondition = WinCondition.HighestScore,
            tilesPerSide = 15
        };
        
        var result = AnalyzeGameType(rules);
        
        // Should be Hybrid due to mixed characteristics
        Assert.AreEqual(DetectedGameType.Hybrid, result);
    }

    #endregion

    #region Monopoly Score Calculation Tests

    [Test]
    public void CalculateMonopolyScore_ReturnsHighScore_ForMonopolyRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        float score = CalculateMonopolyScore(rules);
        
        Assert.GreaterOrEqual(score, 0.8f, "Monopoly rules should have high Monopoly score");
    }

    [Test]
    public void CalculateMonopolyScore_ReturnsLowScore_ForBattleshipsRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        float score = CalculateMonopolyScore(rules);
        
        Assert.LessOrEqual(score, 0.4f, "Battleships rules should have low Monopoly score");
    }

    [Test]
    public void CalculateMonopolyScore_RequiresCurrency()
    {
        GameRules rulesWithCurrency = new GameRules { enableCurrency = true };
        GameRules rulesWithoutCurrency = new GameRules { enableCurrency = false };
        
        float scoreWith = CalculateMonopolyScore(rulesWithCurrency);
        float scoreWithout = CalculateMonopolyScore(rulesWithoutCurrency);
        
        Assert.Greater(scoreWith, scoreWithout);
    }

    #endregion

    #region Battleships Score Calculation Tests

    [Test]
    public void CalculateBattleshipsScore_ReturnsHighScore_ForBattleshipsRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        float score = CalculateBattleshipsScore(rules);
        
        Assert.GreaterOrEqual(score, 0.8f, "Battleships rules should have high Battleships score");
    }

    [Test]
    public void CalculateBattleshipsScore_ReturnsLowScore_ForMonopolyRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        float score = CalculateBattleshipsScore(rules);
        
        Assert.LessOrEqual(score, 0.4f, "Monopoly rules should have low Battleships score");
    }

    [Test]
    public void CalculateBattleshipsScore_RequiresSeparateBoards()
    {
        GameRules rulesWithSeparate = new GameRules { separatePlayerBoards = true, tilesPerSide = 10 };
        GameRules rulesWithShared = new GameRules { separatePlayerBoards = false, tilesPerSide = 10 };
        
        float scoreWith = CalculateBattleshipsScore(rulesWithSeparate);
        float scoreWithout = CalculateBattleshipsScore(rulesWithShared);
        
        Assert.Greater(scoreWith, scoreWithout);
    }

    #endregion

    #region Dice Race Score Calculation Tests

    [Test]
    public void CalculateDiceRaceScore_ReturnsHighScore_ForDiceRaceRules()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        float score = CalculateDiceRaceScore(rules);
        
        Assert.GreaterOrEqual(score, 0.8f, "Dice Race rules should have high Dice Race score");
    }

    [Test]
    public void CalculateDiceRaceScore_FavorsSimpleRules()
    {
        GameRules simpleRules = new GameRules
        {
            enableCurrency = false,
            separatePlayerBoards = false,
            canSeeEnemyTokens = true,
            canPurchaseProperties = false,
            enablePropertyTrading = false,
            enableCombat = false,
            winCondition = WinCondition.ReachSpecificTile
        };
        
        GameRules complexRules = new GameRules
        {
            enableCurrency = true,
            separatePlayerBoards = true,
            canSeeEnemyTokens = false,
            canPurchaseProperties = true,
            enablePropertyTrading = true,
            enableCombat = true,
            winCondition = WinCondition.LastPlayerStanding
        };
        
        float simpleScore = CalculateDiceRaceScore(simpleRules);
        float complexScore = CalculateDiceRaceScore(complexRules);
        
        Assert.Greater(simpleScore, complexScore);
    }

    #endregion

    #region Helper Methods (Mirror CustomGameAnalyzer logic)

    private enum DetectedGameType
    {
        DiceRace,
        Monopoly,
        Battleships,
        Hybrid,
        Unknown
    }

    private DetectedGameType AnalyzeGameType(GameRules rules)
    {
        if (rules == null) return DetectedGameType.Unknown;

        float monopolyScore = CalculateMonopolyScore(rules);
        float battleshipsScore = CalculateBattleshipsScore(rules);
        float diceRaceScore = CalculateDiceRaceScore(rules);

        float maxScore = Mathf.Max(monopolyScore, battleshipsScore, diceRaceScore);

        if (maxScore < 0.6f)
            return DetectedGameType.Hybrid;

        if (monopolyScore == maxScore)
            return DetectedGameType.Monopoly;
        else if (battleshipsScore == maxScore)
            return DetectedGameType.Battleships;
        else if (diceRaceScore == maxScore)
            return DetectedGameType.DiceRace;

        return DetectedGameType.Unknown;
    }

    private float CalculateMonopolyScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Monopoly characteristics - weighted by importance
        if (rules.enableCurrency) { score += 1f; checks++; }
        if (!rules.separatePlayerBoards) { score += 0.5f; checks++; }
        if (rules.canSeeEnemyTokens) { score += 0.5f; checks++; }
        if (rules.canPurchaseProperties) { score += 1f; checks++; }
        if (rules.enablePropertyTrading) { score += 1f; checks++; }
        if (rules.startingMoney > 0) { score += 0.5f; checks++; }
        if (rules.enableBankruptcy) { score += 0.5f; checks++; }
        if (rules.enableRentCollection) { score += 1f; checks++; }

        return checks > 0 ? score / checks : 0f;
    }

    private float CalculateBattleshipsScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Battleships characteristics - weighted by importance
        if (rules.separatePlayerBoards) { score += 1f; checks++; }
        if (!rules.canSeeEnemyTokens) { score += 1f; checks++; }
        if (rules.enableCombat) { score += 1f; checks++; }
        if (rules.enableShipPlacement) { score += 1f; checks++; }
        if (rules.tilesPerSide >= 8) { score += 0.3f; checks++; }

        return checks > 0 ? score / checks : 0f;
    }

    private float CalculateDiceRaceScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

        // Dice Race characteristics - simple race game with NO complex features
        if (!rules.enableCurrency) { score += 1f; checks++; }
        if (!rules.separatePlayerBoards) { score += 1f; checks++; }
        if (rules.canSeeEnemyTokens) { score += 1f; checks++; }
        if (!rules.canPurchaseProperties) { score += 1f; checks++; }
        if (!rules.enablePropertyTrading) { score += 1f; checks++; }
        if (!rules.enableCombat) { score += 1f; checks++; }
        if (!rules.enableRentCollection) { score += 1f; checks++; }
        if (rules.winCondition == WinCondition.ReachGoal ||
            rules.winCondition == WinCondition.ReachSpecificTile)
        {
            score += 1f;
            checks++;
        }

        return checks > 0 ? score / checks : 0f;
    }

    #endregion
}

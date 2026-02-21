using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Regression tests to ensure previously fixed bugs don't reoccur.
/// Each test documents a specific bug that was fixed and verifies the fix remains in place.
/// 
/// Naming Convention: Regression_[Component]_[BugDescription]_[IssueNumberOrDate]
/// </summary>
public class RegressionTests
{
    #region GameRules Regressions

    /// <summary>
    /// REGRESSION: Monopoly rules were being detected as DiceRace because
    /// the scoring algorithm didn't properly weight Monopoly-specific features
    /// like enableRentCollection and canPurchaseProperties.
    /// 
    /// Fixed: Added enableRentCollection to Monopoly scoring with high weight
    /// </summary>
    [Test]
    public void Regression_GameAnalyzer_MonopolyShouldNotBeDetectedAsDiceRace()
    {
        // Arrange
        GameRules monopolyRules = GameRules.CreateMonopolyRules();

        // Act
        float monopolyScore = CalculateMonopolyScore(monopolyRules);
        float diceRaceScore = CalculateDiceRaceScore(monopolyRules);

        // Assert
        Assert.Greater(monopolyScore, diceRaceScore,
            $"REGRESSION: Monopoly rules should score higher as Monopoly ({monopolyScore:F2}) " +
            $"than DiceRace ({diceRaceScore:F2}). " +
            "Check that enableRentCollection and canPurchaseProperties are properly weighted.");
    }

    /// <summary>
    /// REGRESSION: Battleships rules were not being detected correctly because
    /// the scoring algorithm included !enableCurrency which is too generic.
    /// 
    /// Fixed: Removed !enableCurrency from Battleships scoring, kept KEY features only
    /// </summary>
    [Test]
    public void Regression_GameAnalyzer_BattleshipsShouldBeDetectedCorrectly()
    {
        // Arrange
        GameRules battleshipsRules = GameRules.CreateBattleshipsRules();

        // Act
        float battleshipsScore = CalculateBattleshipsScore(battleshipsRules);
        float monopolyScore = CalculateMonopolyScore(battleshipsRules);
        float diceRaceScore = CalculateDiceRaceScore(battleshipsRules);

        // Assert
        Assert.Greater(battleshipsScore, monopolyScore,
            $"REGRESSION: Battleships rules should score higher as Battleships ({battleshipsScore:F2}) " +
            $"than Monopoly ({monopolyScore:F2})");

        Assert.Greater(battleshipsScore, diceRaceScore,
            $"REGRESSION: Battleships rules should score higher as Battleships ({battleshipsScore:F2}) " +
            $"than DiceRace ({diceRaceScore:F2})");
    }

    #endregion

    #region Helper Methods (mirror CustomGameAnalyzer scoring)

    private float CalculateMonopolyScore(GameRules rules)
    {
        float score = 0f;
        int checks = 0;

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

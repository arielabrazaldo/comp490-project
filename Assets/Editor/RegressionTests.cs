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
    #region StandardGameLibrary Regressions

    /// <summary>
    /// REGRESSION: Standard games must have explicit game types set correctly.
    /// This ensures spawning uses the correct game manager without any analyzer.
    /// </summary>
    [Test]
    public void Regression_StandardGameLibrary_MonopolyHasCorrectGameType()
    {
        // Arrange & Act
        SavedGameInfo monopolyGame = StandardGameLibrary_GetMonopolyRules();

        // Assert
        Assert.AreEqual("Monopoly", monopolyGame.gameType,
            "REGRESSION: Monopoly game must have gameType='Monopoly' for direct spawning.");
        Assert.IsTrue(monopolyGame.isStandardGame,
            "REGRESSION: Monopoly should be marked as a standard game.");
        Assert.IsNotNull(monopolyGame.rules,
            "REGRESSION: Monopoly must have rules configured.");
    }

    [Test]
    public void Regression_StandardGameLibrary_BattleshipsHasCorrectGameType()
    {
        // Arrange & Act
        SavedGameInfo battleshipsGame = StandardGameLibrary_GetBattleshipsRules();

        // Assert
        Assert.AreEqual("Battleships", battleshipsGame.gameType,
            "REGRESSION: Battleships game must have gameType='Battleships' for direct spawning.");
        Assert.IsTrue(battleshipsGame.isStandardGame,
            "REGRESSION: Battleships should be marked as a standard game.");
        Assert.IsNotNull(battleshipsGame.rules,
            "REGRESSION: Battleships must have rules configured.");
    }

    [Test]
    public void Regression_StandardGameLibrary_DiceRaceHasCorrectGameType()
    {
        // Arrange & Act
        SavedGameInfo diceRaceGame = StandardGameLibrary_GetDiceRaceRules();

        // Assert
        Assert.AreEqual("Dice Race", diceRaceGame.gameType,
            "REGRESSION: Dice Race game must have gameType='Dice Race' for direct spawning.");
        Assert.IsTrue(diceRaceGame.isStandardGame,
            "REGRESSION: Dice Race should be marked as a standard game.");
        Assert.IsNotNull(diceRaceGame.rules,
            "REGRESSION: Dice Race must have rules configured.");
    }

    #endregion

    #region GameRules Validation Regressions

    /// <summary>
    /// REGRESSION: Monopoly rules must have key features enabled.
    /// </summary>
    [Test]
    public void Regression_GameRules_MonopolyHasRequiredFeatures()
    {
        // Arrange
        GameRules monopolyRules = GameRules.CreateMonopolyRules();

        // Assert - Key Monopoly features
        Assert.IsTrue(monopolyRules.enableCurrency,
            "REGRESSION: Monopoly requires currency system.");
        Assert.IsTrue(monopolyRules.canPurchaseProperties,
            "REGRESSION: Monopoly requires property purchasing.");
        Assert.IsTrue(monopolyRules.enableRentCollection,
            "REGRESSION: Monopoly requires rent collection.");
        Assert.IsFalse(monopolyRules.separatePlayerBoards,
            "REGRESSION: Monopoly uses shared board (not separate boards).");
    }

    /// <summary>
    /// REGRESSION: Battleships rules must have key features enabled.
    /// </summary>
    [Test]
    public void Regression_GameRules_BattleshipsHasRequiredFeatures()
    {
        // Arrange
        GameRules battleshipsRules = GameRules.CreateBattleshipsRules();

        // Assert - Key Battleships features
        Assert.IsTrue(battleshipsRules.separatePlayerBoards,
            "REGRESSION: Battleships requires separate player boards.");
        Assert.IsFalse(battleshipsRules.canSeeEnemyTokens,
            "REGRESSION: Battleships requires hidden enemy tokens.");
        Assert.IsTrue(battleshipsRules.enableCombat,
            "REGRESSION: Battleships requires combat system.");
        Assert.IsTrue(battleshipsRules.enableShipPlacement,
            "REGRESSION: Battleships requires ship placement.");
    }

    /// <summary>
    /// REGRESSION: Dice Race rules must have simple race configuration.
    /// </summary>
    [Test]
    public void Regression_GameRules_DiceRaceHasRequiredFeatures()
    {
        // Arrange
        GameRules diceRaceRules = GameRules.CreateDiceRaceRules();

        // Assert - Dice Race is simple (no complex features)
        Assert.IsFalse(diceRaceRules.enableCurrency,
            "REGRESSION: Dice Race should not have currency.");
        Assert.IsFalse(diceRaceRules.canPurchaseProperties,
            "REGRESSION: Dice Race should not have properties.");
        Assert.IsFalse(diceRaceRules.enableCombat,
            "REGRESSION: Dice Race should not have combat.");
        Assert.IsFalse(diceRaceRules.separatePlayerBoards,
            "REGRESSION: Dice Race uses shared board.");
        Assert.IsTrue(
            diceRaceRules.winCondition == WinCondition.ReachGoal || 
            diceRaceRules.winCondition == WinCondition.ReachSpecificTile,
            "REGRESSION: Dice Race should use ReachGoal or ReachSpecificTile win condition.");
    }

    #endregion

    #region Helper Methods (simulate StandardGameLibrary without MonoBehaviour)

    private SavedGameInfo StandardGameLibrary_GetMonopolyRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        return new SavedGameInfo(
            gameName: "Classic Monopoly",
            gameType: 1, // Monopoly
            playerCount: 4,
            rules: rules,
            isStandardGame: true
        );
    }

    private SavedGameInfo StandardGameLibrary_GetBattleshipsRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        return new SavedGameInfo(
            gameName: "Classic Battleships",
            gameType: 2, // Battleships
            playerCount: 2,
            rules: rules,
            isStandardGame: true
        );
    }

    private SavedGameInfo StandardGameLibrary_GetDiceRaceRules()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        return new SavedGameInfo(
            gameName: "Dice Race",
            gameType: 3, // Dice Race
            playerCount: 4,
            rules: rules,
            isStandardGame: true
        );
    }

    #endregion
}

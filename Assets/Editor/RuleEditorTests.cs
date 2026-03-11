using NUnit.Framework;
using UnityEngine;
using System.Linq;

/// <summary>
/// Unity EditMode tests for GameRules and rule validation.
/// Tests rule creation, validation, cloning, and preset loading.
/// </summary>
public class RuleEditorTests
{
    #region GameRules Creation Tests

    [Test]
    public void CreateMonopolyRules_ReturnsValidRules()
    {
        GameRules rules = GameRules.CreateMonopolyRules();

        Assert.IsNotNull(rules);
        Assert.IsTrue(rules.ValidateRules(out string error), $"Validation failed: {error}");
    }

    [Test]
    public void CreateMonopolyRules_HasCorrectDefaults()
    {
        GameRules rules = GameRules.CreateMonopolyRules();

        Assert.IsTrue(rules.enableCurrency);
        Assert.AreEqual(1500, rules.startingMoney);
        Assert.AreEqual(200, rules.passGoBonus);
        Assert.IsFalse(rules.separatePlayerBoards);
        Assert.IsTrue(rules.canPurchaseProperties);
        Assert.IsTrue(rules.enablePropertyTrading);
        Assert.IsTrue(rules.enableRentCollection);
        Assert.IsFalse(rules.enableCombat);
        Assert.IsTrue(rules.canSeeEnemyTokens);
        Assert.AreEqual(2, rules.numberOfDice);
        Assert.IsTrue(rules.duplicatesGrantExtraTurn);
    }

    [Test]
    public void CreateBattleshipsRules_ReturnsValidRules()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();

        Assert.IsNotNull(rules);
        Assert.IsTrue(rules.ValidateRules(out string error), $"Validation failed: {error}");
    }

    [Test]
    public void CreateBattleshipsRules_HasCorrectDefaults()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();

        Assert.IsFalse(rules.enableCurrency);
        Assert.AreEqual(0, rules.startingMoney);
        Assert.IsTrue(rules.separatePlayerBoards);
        Assert.IsFalse(rules.canPurchaseProperties);
        Assert.IsTrue(rules.enableCombat);
        Assert.IsTrue(rules.enableShipPlacement);
        Assert.IsFalse(rules.canSeeEnemyTokens);
        Assert.AreEqual(2, rules.maxPlayers);
        Assert.AreEqual(WinCondition.EliminateAllEnemies, rules.winCondition);
    }

    [Test]
    public void CreateDiceRaceRules_ReturnsValidRules()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();

        Assert.IsNotNull(rules);
        Assert.IsTrue(rules.ValidateRules(out string error), $"Validation failed: {error}");
    }

    [Test]
    public void CreateDiceRaceRules_HasCorrectDefaults()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();

        Assert.IsFalse(rules.enableCurrency);
        Assert.IsFalse(rules.separatePlayerBoards);
        Assert.IsFalse(rules.canPurchaseProperties);
        Assert.IsFalse(rules.enableCombat);
        Assert.IsTrue(rules.canSeeEnemyTokens);
        Assert.AreEqual(1, rules.numberOfDice);
        Assert.AreEqual(WinCondition.ReachSpecificTile, rules.winCondition);
        Assert.AreEqual(20, rules.targetTileNumber);
    }

    [Test]
    public void CreateCustomRules_ReturnsValidRules()
    {
        GameRules rules = GameRules.CreateCustomRules();

        Assert.IsNotNull(rules);
        Assert.IsTrue(rules.ValidateRules(out string error), $"Validation failed: {error}");
    }

    [Test]
    public void CreateCustomRules_EnablesAllFeatures()
    {
        GameRules rules = GameRules.CreateCustomRules();

        Assert.IsTrue(rules.enableCurrency);
        Assert.IsTrue(rules.canPurchaseProperties);
        Assert.IsTrue(rules.enablePropertyTrading);
        Assert.IsTrue(rules.enableCombat);
        Assert.IsTrue(rules.enableCustomDice);
        Assert.IsTrue(rules.enableResources);
        Assert.IsTrue(rules.allowBankruptcy);
        Assert.IsTrue(rules.allowTrading);
    }

    #endregion

    #region Validation Tests - Player Settings

    [Test]
    public void ValidateRules_FailsWhenMinPlayersLessThanOne()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.minPlayers = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Minimum players"));
    }

    [Test]
    public void ValidateRules_FailsWhenMaxPlayersLessThanMinPlayers()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.minPlayers = 4;
        rules.maxPlayers = 2;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Maximum players"));
    }

    [Test]
    public void ValidateRules_PassesWhenMinEqualsMaxPlayers()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.minPlayers = 2;
        rules.maxPlayers = 2;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    #endregion

    #region Validation Tests - Currency System

    [Test]
    public void ValidateRules_FailsWhenStartingMoneyNegative()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCurrency = true;
        rules.startingMoney = -100;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Starting money"));
    }

    [Test]
    public void ValidateRules_FailsWhenPassGoBonusNegative()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCurrency = true;
        rules.passGoBonus = -50;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Pass GO bonus"));
    }

    [Test]
    public void ValidateRules_PassesWhenStartingMoneyZero()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCurrency = true;
        rules.startingMoney = 0;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    [Test]
    public void ValidateRules_FailsWhenMoneyThresholdWinsWithoutCurrency()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCurrency = false;
        rules.moneyThresholdWins = true;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Money threshold victory requires currency"));
    }

    [Test]
    public void ValidateRules_FailsWhenMoneyThresholdIsZeroOrNegative()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCurrency = true;
        rules.moneyThresholdWins = true;
        rules.winningMoneyThreshold = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Winning money threshold"));
    }

    #endregion

    #region Validation Tests - Board Settings

    [Test]
    public void ValidateRules_FailsWhenBoardSizeTooSmall()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.tilesPerSide = 3;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Board size"));
    }

    [Test]
    public void ValidateRules_PassesWithMinimumBoardSize()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.tilesPerSide = 4;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    #endregion

    #region Validation Tests - Win Conditions

    [Test]
    public void ValidateRules_FailsWhenTargetTileLessThanOne()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        rules.winCondition = WinCondition.ReachSpecificTile;
        rules.targetTileNumber = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Target tile number must be at least 1"));
    }

    [Test]
    public void ValidateRules_FailsWhenTargetTileExceedsBoardSize()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        rules.winCondition = WinCondition.ReachSpecificTile;
        rules.tilesPerSide = 20;
        rules.targetTileNumber = 25;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("cannot exceed board size"));
    }

    [Test]
    public void ValidateRules_PassesWhenTargetTileEqualsBoardSize()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        rules.winCondition = WinCondition.ReachSpecificTile;
        rules.tilesPerSide = 20;
        rules.targetTileNumber = 20;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    #endregion

    #region Validation Tests - Dice System

    [Test]
    public void ValidateRules_FailsWhenNumberOfDiceLessThanOne()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCustomDice = true;
        rules.numberOfDice = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Number of dice"));
    }

    [Test]
    public void ValidateRules_FailsWhenDiceSidesLessThanTwo()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCustomDice = true;
        rules.diceSides = 1;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Dice must have at least 2 sides"));
    }

    [Test]
    public void ValidateRules_FailsWhenDuplicatesRequiredLessThanTwo()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCustomDice = true;
        rules.duplicatesGrantExtraTurn = true;
        rules.duplicatesRequired = 1;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Duplicates required must be at least 2"));
    }

    [Test]
    public void ValidateRules_FailsWhenDuplicatesRequiredExceedsNumberOfDice()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.enableCustomDice = true;
        rules.numberOfDice = 2;
        rules.duplicatesGrantExtraTurn = true;
        rules.duplicatesRequired = 3;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Duplicates required cannot exceed"));
    }

    #endregion

    #region Validation Tests - Resource System

    [Test]
    public void ValidateRules_FailsWhenResourcesEnabledButCountIsZero()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.numberOfResources = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Number of resources must be at least 1"));
    }

    [Test]
    public void ValidateRules_FailsWhenResourceNamesArrayMismatch()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.numberOfResources = 3;
        rules.resourceNames = new string[] { "Wood", "Stone" }; // Only 2 names

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Resource names array must match"));
    }

    [Test]
    public void ValidateRules_FailsWhenResourceNameIsEmpty()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.numberOfResources = 3;
        rules.resourceNames = new string[] { "Wood", "", "Wheat" };

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("name cannot be empty"));
    }

    [Test]
    public void ValidateRules_FailsWhenMaxResourcesPerTypeIsZero()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.enableResourceCap = true;
        rules.maxResourcesPerType = 0;

        Assert.IsFalse(rules.ValidateRules(out string error));
        Assert.IsTrue(error.Contains("Maximum resources per type"));
    }

    #endregion

    #region Clone Tests

    [Test]
    public void Clone_CreatesDeepCopy()
    {
        GameRules original = GameRules.CreateMonopolyRules();
        GameRules clone = original.Clone();

        Assert.AreNotSame(original, clone);
    }

    [Test]
    public void Clone_PreservesAllValues()
    {
        GameRules original = GameRules.CreateCustomRules();
        original.startingMoney = 9999;
        original.tilesPerSide = 50;
        original.targetTileNumber = 25;

        GameRules clone = original.Clone();

        Assert.AreEqual(original.enableCurrency, clone.enableCurrency);
        Assert.AreEqual(original.startingMoney, clone.startingMoney);
        Assert.AreEqual(original.passGoBonus, clone.passGoBonus);
        Assert.AreEqual(original.separatePlayerBoards, clone.separatePlayerBoards);
        Assert.AreEqual(original.tilesPerSide, clone.tilesPerSide);
        Assert.AreEqual(original.canPurchaseProperties, clone.canPurchaseProperties);
        Assert.AreEqual(original.enableCombat, clone.enableCombat);
        Assert.AreEqual(original.winCondition, clone.winCondition);
        Assert.AreEqual(original.targetTileNumber, clone.targetTileNumber);
        Assert.AreEqual(original.numberOfDice, clone.numberOfDice);
        Assert.AreEqual(original.diceSides, clone.diceSides);
    }

    [Test]
    public void Clone_ResourceArraysAreIndependent()
    {
        GameRules original = GameRules.CreateCustomRules();
        original.enableResources = true;
        original.numberOfResources = 2;
        original.resourceNames = new string[] { "Gold", "Silver" };

        GameRules clone = original.Clone();
        clone.resourceNames[0] = "Modified";

        Assert.AreEqual("Gold", original.resourceNames[0]);
        Assert.AreEqual("Modified", clone.resourceNames[0]);
    }

    [Test]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        GameRules original = GameRules.CreateMonopolyRules();
        GameRules clone = original.Clone();

        clone.startingMoney = 9999;
        clone.enableCombat = true;

        Assert.AreEqual(1500, original.startingMoney);
        Assert.IsFalse(original.enableCombat);
    }

    #endregion

    #region GetRulesSummary Tests

    [Test]
    public void GetRulesSummary_ReturnsNonEmptyString()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        string summary = rules.GetRulesSummary();

        Assert.IsFalse(string.IsNullOrEmpty(summary));
    }

    [Test]
    public void GetRulesSummary_ContainsCurrencyInfo()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        string summary = rules.GetRulesSummary();

        Assert.IsTrue(summary.Contains("Currency"));
        Assert.IsTrue(summary.Contains("1500"));
    }

    [Test]
    public void GetRulesSummary_ContainsBoardInfo()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        string summary = rules.GetRulesSummary();

        Assert.IsTrue(summary.Contains("Board"));
        Assert.IsTrue(summary.Contains("Shared") || summary.Contains("Separate"));
    }

    [Test]
    public void GetRulesSummary_ContainsWinConditionInfo()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        string summary = rules.GetRulesSummary();

        Assert.IsTrue(summary.Contains("Win Condition"));
        Assert.IsTrue(summary.Contains("ReachSpecificTile") || summary.Contains("Tile #"));
    }

    [Test]
    public void GetRulesSummary_ShowsTargetTileForReachSpecificTile()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        rules.winCondition = WinCondition.ReachSpecificTile;
        rules.targetTileNumber = 15;

        string summary = rules.GetRulesSummary();

        Assert.IsTrue(summary.Contains("Tile #15"));
    }

    [Test]
    public void GetRulesSummary_ContainsResourceInfo_WhenEnabled()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.numberOfResources = 3;
        rules.resourceNames = new string[] { "Wood", "Stone", "Wheat" };

        string summary = rules.GetRulesSummary();

        Assert.IsTrue(summary.Contains("Resources"));
        Assert.IsTrue(summary.Contains("Wood"));
    }

    #endregion

    #region WinCondition Enum Tests

    [Test]
    public void WinCondition_AllValuesAreDefined()
    {
        var values = System.Enum.GetValues(typeof(WinCondition));

        Assert.IsTrue(values.Length >= 5); // At least 5 win conditions
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.ReachGoal));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.LastPlayerStanding));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.HighestScore));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.EliminateAllEnemies));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.MoneyThreshold));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WinCondition), WinCondition.ReachSpecificTile));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ValidateRules_PassesWithExtremelyLargeValues()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        rules.startingMoney = int.MaxValue;
        rules.passGoBonus = int.MaxValue;
        rules.tilesPerSide = 1000;
        rules.maxPlayers = 100;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    [Test]
    public void ValidateRules_CurrencyCanBeDisabledWithZeroMoney()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        rules.enableCurrency = false;
        rules.startingMoney = 0;
        rules.passGoBonus = 0;

        Assert.IsTrue(rules.ValidateRules(out _));
    }

    [Test]
    public void Clone_HandlesNullResourceNames()
    {
        GameRules original = GameRules.CreateMonopolyRules();
        original.enableResources = false;
        original.resourceNames = null;

        // Should not throw
        Assert.DoesNotThrow(() => original.Clone());
    }

    #endregion
}

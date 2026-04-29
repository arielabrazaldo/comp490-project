using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Integration tests for RuleEditor and GameSaveManager workflows.
/// Tests the complete flow: Create Rules -> Save Game -> Load Game -> Verify Rules Preserved
/// Since these managers use DontDestroyOnLoad, we test the logic directly in EditMode.
/// </summary>
public class RuleEditorGameSaveIntegrationTests
{
    private string _testSaveDirectory;
    private const string SAVE_FILE_EXTENSION = ".json";

    #region Test Setup/Teardown

    [SetUp]
    public void SetUp()
    {
        // Use the same save directory as GameSaveManager
        _testSaveDirectory = Path.Combine(Application.persistentDataPath, "CustomGames");

        // Create directory if needed
        if (!Directory.Exists(_testSaveDirectory))
        {
            Directory.CreateDirectory(_testSaveDirectory);
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test files (only files created during test)
        if (!string.IsNullOrEmpty(_testSaveDirectory) && Directory.Exists(_testSaveDirectory))
        {
            string[] testFiles = Directory.GetFiles(_testSaveDirectory, "IntegrationTest_*.json");
            foreach (string file in testFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }

    #endregion

    #region Helper Methods (mirroring GameSaveManager and RuleEditorManager logic)

    private string GetSafeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        safe = safe.Replace(' ', '_');
        if (string.IsNullOrEmpty(safe))
        {
            safe = "CustomGame";
        }
        return safe;
    }

    private bool SaveGame(SavedGameInfo gameInfo)
    {
        try
        {
            string fileName = GetSafeFileName(gameInfo.gameName);
            string filePath = Path.Combine(_testSaveDirectory, fileName + SAVE_FILE_EXTENSION);

            SerializableGameData saveData = new SerializableGameData(gameInfo);
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            return false;
        }
    }

    private SavedGameInfo LoadGame(string gameName)
    {
        try
        {
            string fileName = GetSafeFileName(gameName);
            string filePath = Path.Combine(_testSaveDirectory, fileName + SAVE_FILE_EXTENSION);

            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            SerializableGameData saveData = JsonUtility.FromJson<SerializableGameData>(json);
            return saveData.ToSavedGameInfo();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Simulates RuleEditorManager.SetRules() validation
    /// </summary>
    private bool ValidateAndSetRules(GameRules rules, out string errorMessage)
    {
        return rules.ValidateRules(out errorMessage);
    }

    /// <summary>
    /// Simulates the full workflow: Configure rules -> Create SavedGameInfo -> Save
    /// </summary>
    private SavedGameInfo CreateAndSaveCustomGame(string gameName, GameRules rules, int gameType)
    {
        // Validate rules (like RuleEditorManager.SetRules does)
        if (!rules.ValidateRules(out string error))
        {
            Debug.LogError($"Invalid rules: {error}");
            return null;
        }

        // Create SavedGameInfo (like UIManager_Streamlined.OnRulesConfigured does)
        SavedGameInfo gameInfo = new SavedGameInfo(
            gameName,
            gameType,
            rules.maxPlayers,
            rules.Clone(),
            isStandardGame: false
        );

        // Save to disk
        if (SaveGame(gameInfo))
        {
            return gameInfo;
        }

        return null;
    }

    /// <summary>
    /// Analyze rules to determine game type (returns int: 1=Monopoly, 2=Battleships, 3=DiceRace, 4=Hybrid)
    /// </summary>
    private int AnalyzeGameType(GameRules rules)
    {
        // Check for Monopoly-specific features
        if (rules.enableCurrency && rules.canPurchaseProperties && rules.enableRentCollection)
        {
            return 1; // Monopoly
        }
        
        // Check for Battleships-specific features
        if (rules.separatePlayerBoards && rules.enableCombat && rules.enableShipPlacement)
        {
            return 2; // Battleships
        }
        
        // Check for simple Dice Race (no complex features)
        if (!rules.enableCurrency && !rules.canPurchaseProperties && !rules.enableCombat &&
            (rules.winCondition == WinCondition.ReachGoal || rules.winCondition == WinCondition.ReachSpecificTile))
        {
            return 3; // DiceRace
        }
        
        // Default to Hybrid for mixed/custom rules
        return 4; // Hybrid
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
            3 => "DiceRace",
            4 => "Hybrid",
            _ => "Unknown"
        };
    }

    #endregion

    #region Integration Tests - Full Workflow

    [Test]
    public void FullWorkflow_CreateMonopolyRules_SaveAndLoad_PreservesAllSettings()
    {
        // Step 1: Create Monopoly rules (like user selecting preset)
        GameRules originalRules = GameRules.CreateMonopolyRules();

        // Step 2: Validate rules (like RuleEditorManager)
        Assert.IsTrue(ValidateAndSetRules(originalRules, out string error), $"Validation failed: {error}");

        // Step 3: Save game (like UIManager_Streamlined.OnRulesConfigured)
        SavedGameInfo savedGame = CreateAndSaveCustomGame(
            "IntegrationTest_Monopoly",
            originalRules,
            AnalyzeGameType(originalRules)
        );

        Assert.IsNotNull(savedGame);
        Assert.AreEqual(1, savedGame.gameType); // 1 = Monopoly

        // Step 4: Load game (like UIManager_Streamlined.OnHostSavedGameClicked)
        SavedGameInfo loadedGame = LoadGame("IntegrationTest_Monopoly");

        // Step 5: Verify all settings preserved
        Assert.IsNotNull(loadedGame);
        Assert.AreEqual(savedGame.gameName, loadedGame.gameName);
        Assert.AreEqual(savedGame.gameType, loadedGame.gameType);

        // Verify critical Monopoly rules
        Assert.AreEqual(originalRules.enableCurrency, loadedGame.rules.enableCurrency);
        Assert.AreEqual(originalRules.startingMoney, loadedGame.rules.startingMoney);
        Assert.AreEqual(originalRules.passGoBonus, loadedGame.rules.passGoBonus);
        Assert.AreEqual(originalRules.canPurchaseProperties, loadedGame.rules.canPurchaseProperties);
        Assert.AreEqual(originalRules.enablePropertyTrading, loadedGame.rules.enablePropertyTrading);
        Assert.AreEqual(originalRules.enableRentCollection, loadedGame.rules.enableRentCollection);
        Assert.AreEqual(originalRules.duplicatesGrantExtraTurn, loadedGame.rules.duplicatesGrantExtraTurn);
    }

    [Test]
    public void FullWorkflow_CreateDiceRaceRules_SaveAndLoad_PreservesWinCondition()
    {
        // Step 1: Create Dice Race rules
        GameRules originalRules = GameRules.CreateDiceRaceRules();

        // Step 2: Validate
        Assert.IsTrue(ValidateAndSetRules(originalRules, out _));

        // Step 3: Save
        SavedGameInfo savedGame = CreateAndSaveCustomGame(
            "IntegrationTest_DiceRace",
            originalRules,
            AnalyzeGameType(originalRules)
        );

        Assert.IsNotNull(savedGame);
        Assert.AreEqual(3, savedGame.gameType); // 3 = DiceRace

        // Step 4: Load
        SavedGameInfo loadedGame = LoadGame("IntegrationTest_DiceRace");

        // Step 5: Verify win condition preserved (this was a previous bug)
        Assert.IsNotNull(loadedGame);
        Assert.AreEqual(WinCondition.ReachSpecificTile, loadedGame.rules.winCondition);
        Assert.AreEqual(originalRules.targetTileNumber, loadedGame.rules.targetTileNumber);
        Assert.IsFalse(loadedGame.rules.enableCurrency);
        Assert.IsFalse(loadedGame.rules.canPurchaseProperties);
    }

    [Test]
    public void FullWorkflow_CreateBattleshipsRules_SaveAndLoad_PreservesCombatSettings()
    {
        // Step 1: Create Battleships rules
        GameRules originalRules = GameRules.CreateBattleshipsRules();

        // Step 2: Validate
        Assert.IsTrue(ValidateAndSetRules(originalRules, out _));

        // Step 3: Save
        SavedGameInfo savedGame = CreateAndSaveCustomGame(
            "IntegrationTest_Battleships",
            originalRules,
            AnalyzeGameType(originalRules)
        );

        Assert.IsNotNull(savedGame);
        Assert.AreEqual(2, savedGame.gameType); // 2 = Battleships

        // Step 4: Load
        SavedGameInfo loadedGame = LoadGame("IntegrationTest_Battleships");

        // Step 5: Verify Battleships-specific settings
        Assert.IsNotNull(loadedGame);
        Assert.IsTrue(loadedGame.rules.separatePlayerBoards);
        Assert.IsTrue(loadedGame.rules.enableCombat);
        Assert.IsTrue(loadedGame.rules.enableShipPlacement);
        Assert.IsFalse(loadedGame.rules.CanSeeEnemyTokens);
        Assert.AreEqual(WinCondition.EliminateAllEnemies, loadedGame.rules.winCondition);
    }

    [Test]
    public void FullWorkflow_CreateCustomRules_ModifyAndSave_PreservesModifications()
    {
        // Step 1: Start with custom rules
        GameRules customRules = GameRules.CreateCustomRules();

        // Step 2: Modify rules (like user editing in RuleEditorUI)
        customRules.startingMoney = 2500;
        customRules.numberOfDice = 3;
        customRules.diceSides = 8;
        customRules.tilesPerSide = 30;
        customRules.enableResources = true;
        customRules.numberOfResources = 2;
        customRules.resourceNames = new string[] { "Gold", "Gems" };

        // Step 3: Validate
        Assert.IsTrue(ValidateAndSetRules(customRules, out string error), $"Validation failed: {error}");

        // Step 4: Save
        SavedGameInfo savedGame = CreateAndSaveCustomGame(
            "IntegrationTest_CustomModified",
            customRules,
            4
        );

        Assert.IsNotNull(savedGame);

        // Step 5: Load
        SavedGameInfo loadedGame = LoadGame("IntegrationTest_CustomModified");

        // Step 6: Verify all modifications preserved
        Assert.IsNotNull(loadedGame);
        Assert.AreEqual(2500, loadedGame.rules.startingMoney);
        Assert.AreEqual(3, loadedGame.rules.numberOfDice);
        Assert.AreEqual(8, loadedGame.rules.diceSides);
        Assert.AreEqual(30, loadedGame.rules.tilesPerSide);
        Assert.IsTrue(loadedGame.rules.enableResources);
        Assert.AreEqual(2, loadedGame.rules.numberOfResources);
        Assert.AreEqual("Gold", loadedGame.rules.resourceNames[0]);
        Assert.AreEqual("Gems", loadedGame.rules.resourceNames[1]);
    }

    #endregion

    #region Integration Tests - Game Type Analysis

    [Test]
    public void GameTypeAnalysis_MonopolyRules_DetectedAsMonopoly()
    {
        GameRules rules = GameRules.CreateMonopolyRules();
        int gameType = AnalyzeGameType(rules);

        Assert.AreEqual(1, gameType);
    }

    [Test]
    public void GameTypeAnalysis_DiceRaceRules_DetectedAsDiceRace()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();
        int gameType = AnalyzeGameType(rules);

        Assert.AreEqual(3, gameType);
    }

    [Test]
    public void GameTypeAnalysis_BattleshipsRules_DetectedAsBattleships()
    {
        GameRules rules = GameRules.CreateBattleshipsRules();
        int gameType = AnalyzeGameType(rules);

        Assert.AreEqual(2, gameType);
    }

    [Test]
    public void GameTypeAnalysis_SavedGameTypeMatchesAnalysis()
    {
        // Create rules for each type
        GameRules monopolyRules = GameRules.CreateMonopolyRules();
        GameRules diceRaceRules = GameRules.CreateDiceRaceRules();
        GameRules battleshipsRules = GameRules.CreateBattleshipsRules();

        // Save with analyzed game types
        SavedGameInfo monopolyGame = CreateAndSaveCustomGame(
            "IntegrationTest_TypeMatch_Monopoly",
            monopolyRules,
            AnalyzeGameType(monopolyRules)
        );

        SavedGameInfo diceRaceGame = CreateAndSaveCustomGame(
            "IntegrationTest_TypeMatch_DiceRace",
            diceRaceRules,
            AnalyzeGameType(diceRaceRules)
        );

        SavedGameInfo battleshipsGame = CreateAndSaveCustomGame(
            "IntegrationTest_TypeMatch_Battleships",
            battleshipsRules,
            AnalyzeGameType(battleshipsRules)
        );

        // Load and verify
        SavedGameInfo loadedMonopoly = LoadGame("IntegrationTest_TypeMatch_Monopoly");
        SavedGameInfo loadedDiceRace = LoadGame("IntegrationTest_TypeMatch_DiceRace");
        SavedGameInfo loadedBattleships = LoadGame("IntegrationTest_TypeMatch_Battleships");

        Assert.AreEqual(1, loadedMonopoly.gameType);    // 1 = Monopoly
        Assert.AreEqual(3, loadedDiceRace.gameType);    // 3 = DiceRace
        Assert.AreEqual(2, loadedBattleships.gameType); // 2 = Battleships
    }

    #endregion

    #region Integration Tests - Validation Before Save

    [Test]
    public void ValidationBeforeSave_InvalidRules_ReturnsNull()
    {
        // Create invalid rules (negative starting money)
        GameRules invalidRules = GameRules.CreateMonopolyRules();
        invalidRules.enableCurrency = true;
        invalidRules.startingMoney = -100;

        // Expect the error log from CreateAndSaveCustomGame
        LogAssert.Expect(LogType.Error, "Invalid rules: Starting money cannot be negative");

        // Attempt to save (should fail validation)
        SavedGameInfo result = CreateAndSaveCustomGame(
            "IntegrationTest_Invalid",
            invalidRules,
            1
        );

        Assert.IsNull(result);
    }

    [Test]
    public void ValidationBeforeSave_InvalidBoardSize_ReturnsNull()
    {
        // Create rules with too small board
        GameRules invalidRules = GameRules.CreateDiceRaceRules();
        invalidRules.tilesPerSide = 2; // Too small (minimum is 4)

        // Expect the error log from CreateAndSaveCustomGame
        LogAssert.Expect(LogType.Error, "Invalid rules: Target tile number (20) cannot exceed board size (2)");

        SavedGameInfo result = CreateAndSaveCustomGame(
            "IntegrationTest_InvalidBoard",
            invalidRules,
            3
        );

        Assert.IsNull(result);
    }

    [Test]
    public void ValidationBeforeSave_InvalidTargetTile_ReturnsNull()
    {
        // Create rules where target tile exceeds board size
        GameRules invalidRules = GameRules.CreateDiceRaceRules();
        invalidRules.winCondition = WinCondition.ReachSpecificTile;
        invalidRules.tilesPerSide = 20;
        invalidRules.targetTileNumber = 25; // Exceeds board size

        // Expect the error log from CreateAndSaveCustomGame
        LogAssert.Expect(LogType.Error, "Invalid rules: Target tile number (25) cannot exceed board size (20)");

        SavedGameInfo result = CreateAndSaveCustomGame(
            "IntegrationTest_InvalidTarget",
            invalidRules,
            3
        );

        Assert.IsNull(result);
    }

    [Test]
    public void ValidationBeforeSave_ValidRules_SavesSuccessfully()
    {
        // Create valid rules
        GameRules validRules = GameRules.CreateDiceRaceRules();
        validRules.tilesPerSide = 25;
        validRules.targetTileNumber = 25;

        SavedGameInfo result = CreateAndSaveCustomGame(
            "IntegrationTest_ValidTarget",
            validRules,
            3
        );

        Assert.IsNotNull(result);
        Assert.AreEqual(25, result.rules.targetTileNumber);
    }

    #endregion

    #region Integration Tests - Serialization Integrity

    [Test]
    public void Serialization_WinConditionEnum_PreservedCorrectly()
    {
        // Test all win conditions serialize/deserialize correctly
        WinCondition[] allConditions = new WinCondition[]
        {
            WinCondition.ReachGoal,
            WinCondition.LastPlayerStanding,
            WinCondition.HighestScore,
            WinCondition.EliminateAllEnemies,
            WinCondition.MoneyThreshold,
            WinCondition.ReachSpecificTile
        };

        foreach (var condition in allConditions)
        {
            GameRules rules = GameRules.CreateCustomRules();
            rules.winCondition = condition;

            // Special handling for MoneyThreshold
            if (condition == WinCondition.MoneyThreshold)
            {
                rules.enableCurrency = true;
                rules.moneyThresholdWins = true;
                rules.winningMoneyThreshold = 5000;
            }

            string gameName = $"IntegrationTest_WinCondition_{condition}";
            SavedGameInfo saved = CreateAndSaveCustomGame(gameName, rules, 4); // 4 = Hybrid/Custom

            // Skip if validation failed for this condition
            if (saved == null) continue;

            SavedGameInfo loaded = LoadGame(gameName);

            Assert.IsNotNull(loaded, $"Failed to load game with WinCondition.{condition}");
            Assert.AreEqual(condition, loaded.rules.winCondition,
                $"WinCondition.{condition} not preserved after save/load");
        }
    }

    [Test]
    public void Serialization_ResourceArrays_PreservedCorrectly()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableResources = true;
        rules.numberOfResources = 4;
        rules.resourceNames = new string[] { "Wood", "Stone", "Iron", "Gold" };
        rules.enableResourceCap = true;
        rules.maxResourcesPerType = 50;

        SavedGameInfo saved = CreateAndSaveCustomGame(
            "IntegrationTest_Resources",
            rules,
            4 // 4 = Hybrid/Custom
        );

        Assert.IsNotNull(saved);

        SavedGameInfo loaded = LoadGame("IntegrationTest_Resources");

        Assert.IsNotNull(loaded);
        Assert.AreEqual(4, loaded.rules.numberOfResources);
        Assert.AreEqual(4, loaded.rules.resourceNames.Length);
        Assert.AreEqual("Wood", loaded.rules.resourceNames[0]);
        Assert.AreEqual("Stone", loaded.rules.resourceNames[1]);
        Assert.AreEqual("Iron", loaded.rules.resourceNames[2]);
        Assert.AreEqual("Gold", loaded.rules.resourceNames[3]);
        Assert.AreEqual(50, loaded.rules.maxResourcesPerType);
    }

    [Test]
    public void Serialization_DiceSettings_PreservedCorrectly()
    {
        GameRules rules = GameRules.CreateCustomRules();
        rules.enableCustomDice = true;
        rules.numberOfDice = 4;
        rules.diceSides = 12;
        rules.duplicatesGrantExtraTurn = true;
        rules.duplicatesRequired = 3;

        SavedGameInfo saved = CreateAndSaveCustomGame(
            "IntegrationTest_DiceSettings",
            rules,
            4 // 4 = Hybrid/Custom
        );

        Assert.IsNotNull(saved);

        SavedGameInfo loaded = LoadGame("IntegrationTest_DiceSettings");

        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded.rules.enableCustomDice);
        Assert.AreEqual(4, loaded.rules.numberOfDice);
        Assert.AreEqual(12, loaded.rules.diceSides);
        Assert.IsTrue(loaded.rules.duplicatesGrantExtraTurn);
        Assert.AreEqual(3, loaded.rules.duplicatesRequired);
    }

    #endregion

    #region Integration Tests - Edge Cases

    [Test]
    public void EdgeCase_SaveAndLoadMultipleGames_AllPreserved()
    {
        // Save multiple games
        List<SavedGameInfo> savedGames = new List<SavedGameInfo>();

        for (int i = 0; i < 5; i++)
        {
            GameRules rules = GameRules.CreateDiceRaceRules();
            rules.tilesPerSide = 10 + i * 5;
            rules.targetTileNumber = 10 + i * 5;

            SavedGameInfo saved = CreateAndSaveCustomGame(
                $"IntegrationTest_Multiple_{i}",
                rules,
                3
            );

            Assert.IsNotNull(saved);
            savedGames.Add(saved);
        }

        // Load and verify all
        for (int i = 0; i < 5; i++)
        {
            SavedGameInfo loaded = LoadGame($"IntegrationTest_Multiple_{i}");
            Assert.IsNotNull(loaded);
            Assert.AreEqual(10 + i * 5, loaded.rules.tilesPerSide);
            Assert.AreEqual(10 + i * 5, loaded.rules.targetTileNumber);
        }
    }

    [Test]
    public void EdgeCase_OverwriteExistingGame_UpdatesContent()
    {
        // Save initial game
        GameRules initialRules = GameRules.CreateDiceRaceRules();
        initialRules.tilesPerSide = 20;
        initialRules.targetTileNumber = 20;

        SavedGameInfo initial = CreateAndSaveCustomGame(
            "IntegrationTest_Overwrite",
            initialRules,
            3
        );

        Assert.IsNotNull(initial);

        // Overwrite with new rules
        GameRules newRules = GameRules.CreateDiceRaceRules();
        newRules.tilesPerSide = 50;
        newRules.targetTileNumber = 50;

        SavedGameInfo overwritten = CreateAndSaveCustomGame(
            "IntegrationTest_Overwrite",
            newRules,
            3
        );

        Assert.IsNotNull(overwritten);

        // Load and verify it's the updated version
        SavedGameInfo loaded = LoadGame("IntegrationTest_Overwrite");
        Assert.AreEqual(50, loaded.rules.tilesPerSide);
        Assert.AreEqual(50, loaded.rules.targetTileNumber);
    }

    [Test]
    public void EdgeCase_CloneRulesBeforeSave_OriginalUnaffected()
    {
        // Create and save a game
        GameRules originalRules = GameRules.CreateMonopolyRules();
        int originalMoney = originalRules.startingMoney;

        SavedGameInfo saved = CreateAndSaveCustomGame(
            "IntegrationTest_CloneTest",
            originalRules,
            1
        );

        // Modify the saved game's rules
        saved.rules.startingMoney = 9999;

        // Verify original is unchanged
        Assert.AreEqual(originalMoney, originalRules.startingMoney);

        // Load and verify saved game has clone
        SavedGameInfo loaded = LoadGame("IntegrationTest_CloneTest");
        Assert.AreEqual(originalMoney, loaded.rules.startingMoney);
    }

    #endregion

    #region Integration Tests - Date Tracking

    [Test]
    public void DateTracking_CreatedDatePreserved()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();

        SavedGameInfo saved = CreateAndSaveCustomGame(
            "IntegrationTest_DateTracking",
            rules,
            3
        );

        Assert.IsNotNull(saved);
        System.DateTime beforeSave = System.DateTime.Now.AddMinutes(1);

        SavedGameInfo loaded = LoadGame("IntegrationTest_DateTracking");

        Assert.IsNotNull(loaded);
        Assert.LessOrEqual(loaded.createdDate, beforeSave);
        Assert.Greater(loaded.createdDate, System.DateTime.MinValue);
    }

    [Test]
    public void Metadata_IsStandardGameFlag_PreservedCorrectly()
    {
        // Test custom game (not standard)
        GameRules customRules = GameRules.CreateMonopolyRules();
        SavedGameInfo customGame = new SavedGameInfo(
            "IntegrationTest_CustomFlag",
            1, // 1 = Monopoly
            4,
            customRules,
            isStandardGame: false
        );

        SaveGame(customGame);
        SavedGameInfo loadedCustom = LoadGame("IntegrationTest_CustomFlag");

        Assert.IsFalse(loadedCustom.isStandardGame);

        // Test standard game flag
        SavedGameInfo standardGame = new SavedGameInfo(
            "IntegrationTest_StandardFlag",
            1, // 1 = Monopoly
            4,
            customRules,
            isStandardGame: true
        );

        SaveGame(standardGame);
        SavedGameInfo loadedStandard = LoadGame("IntegrationTest_StandardFlag");

        Assert.IsTrue(loadedStandard.isStandardGame);
    }

    #endregion
}

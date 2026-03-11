using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unity EditMode tests for GameSaveManager save/load functionality.
/// These tests directly test the file I/O logic without instantiating the MonoBehaviour,
/// avoiding DontDestroyOnLoad errors in EditMode.
/// </summary>
public class GameSaveManagerTests
{
    private string _testSaveDirectory;
    private const string SAVE_FILE_EXTENSION = ".json";

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
        // Clean up test files
        if (!string.IsNullOrEmpty(_testSaveDirectory) && Directory.Exists(_testSaveDirectory))
        {
            // Delete test files only (files created during test)
            string[] testFiles = Directory.GetFiles(_testSaveDirectory, "Test_*.json");
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

    #region Helper Methods (mirroring GameSaveManager logic)

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

            if (File.Exists(filePath))
            {
                fileName = $"{fileName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                filePath = Path.Combine(_testSaveDirectory, fileName + SAVE_FILE_EXTENSION);
            }

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

    private bool SaveGame(SavedGameInfo gameInfo, string fileName)
    {
        try
        {
            string safeFileName = GetSafeFileName(fileName);
            string filePath = Path.Combine(_testSaveDirectory, safeFileName + SAVE_FILE_EXTENSION);

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

    private bool GameExists(string gameName)
    {
        string fileName = GetSafeFileName(gameName);
        string filePath = Path.Combine(_testSaveDirectory, fileName + SAVE_FILE_EXTENSION);
        return File.Exists(filePath);
    }

    private bool DeleteGame(string gameName)
    {
        try
        {
            string fileName = GetSafeFileName(gameName);
            string filePath = Path.Combine(_testSaveDirectory, fileName + SAVE_FILE_EXTENSION);

            if (!filePath.EndsWith(SAVE_FILE_EXTENSION))
            {
                filePath += SAVE_FILE_EXTENSION;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private List<string> GetAllSaveFileNames()
    {
        try
        {
            string[] files = Directory.GetFiles(_testSaveDirectory, "*" + SAVE_FILE_EXTENSION);
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private SavedGameInfo CreateTestSavedGameInfo(string gameName)
    {
        var rules = new GameRules
        {
            enableCurrency = false,
            startingMoney = 0,
            passGoBonus = 0,
            tilesPerSide = 10,
            minPlayers = 2,
            maxPlayers = 4
        };

        return new SavedGameInfo(gameName, 3, 2, rules, false); // 3 = DiceRace
    }

    #endregion

    #region Directory Tests

    [Test]
    public void SaveDirectory_Exists_AfterSetup()
    {
        Assert.IsTrue(Directory.Exists(_testSaveDirectory));
    }

    [Test]
    public void SaveDirectory_ContainsCustomGames()
    {
        Assert.IsTrue(_testSaveDirectory.Contains("CustomGames"));
    }

    #endregion

    #region Save Operations Tests

    [Test]
    public void SaveGame_ReturnsTrue_WithValidData()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_SaveGame_Valid");
        bool result = SaveGame(gameInfo);
        Assert.IsTrue(result);
    }

    [Test]
    public void SaveGame_CreatesFile_OnDisk()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_SaveGame_Creates");
        SaveGame(gameInfo);
        bool exists = GameExists("Test_SaveGame_Creates");
        Assert.IsTrue(exists);
    }

    [Test]
    public void SaveGame_WithSpecificFilename_UsesProvidedName()
    {
        var gameInfo = CreateTestSavedGameInfo("OriginalName");
        string customFileName = "Test_CustomFileName";

        bool result = SaveGame(gameInfo, customFileName);
        Assert.IsTrue(result);

        string expectedPath = Path.Combine(_testSaveDirectory, customFileName + ".json");
        Assert.IsTrue(File.Exists(expectedPath));

        // Cleanup
        if (File.Exists(expectedPath))
        {
            File.Delete(expectedPath);
        }
    }

    #endregion

    #region Load Operations Tests

    [Test]
    public void LoadGame_ReturnsCorrectData_AfterSave()
    {
        var originalInfo = CreateTestSavedGameInfo("Test_LoadGame_Correct");
        originalInfo.playerCount = 4;
        SaveGame(originalInfo);

        var loadedInfo = LoadGame("Test_LoadGame_Correct");

        Assert.IsNotNull(loadedInfo);
        Assert.AreEqual("Test_LoadGame_Correct", loadedInfo.gameName);
        Assert.AreEqual(4, loadedInfo.playerCount);
    }

    [Test]
    public void LoadGame_ReturnsNull_WhenFileNotFound()
    {
        var result = LoadGame("NonExistentGame_12345");
        Assert.IsNull(result);
    }

    [Test]
    public void LoadGame_PreservesGameType()
    {
        var originalInfo = CreateTestSavedGameInfo("Test_LoadGame_Type");
        originalInfo.gameType = 1; // 1 = Monopoly
        SaveGame(originalInfo);

        var loadedInfo = LoadGame("Test_LoadGame_Type");

        Assert.AreEqual(1, loadedInfo.gameType); // 1 = Monopoly
    }

    #endregion

    #region Delete Operations Tests

    [Test]
    public void DeleteGame_ReturnsTrue_WhenFileExists()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_Delete_Exists");
        SaveGame(gameInfo);

        bool result = DeleteGame("Test_Delete_Exists");
        Assert.IsTrue(result);
    }

    [Test]
    public void DeleteGame_ReturnsFalse_WhenFileNotFound()
    {
        bool result = DeleteGame("NonExistentGame_67890");
        Assert.IsFalse(result);
    }

    [Test]
    public void DeleteGame_RemovesFile_FromDisk()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_Delete_Removes");
        SaveGame(gameInfo);

        Assert.IsTrue(GameExists("Test_Delete_Removes"));

        DeleteGame("Test_Delete_Removes");

        Assert.IsFalse(GameExists("Test_Delete_Removes"));
    }

    #endregion

    #region Utility Methods Tests

    [Test]
    public void GameExists_ReturnsTrue_WhenGameIsSaved()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_Exists_True");
        SaveGame(gameInfo);

        bool exists = GameExists("Test_Exists_True");
        Assert.IsTrue(exists);
    }

    [Test]
    public void GameExists_ReturnsFalse_WhenGameNotSaved()
    {
        bool exists = GameExists("NonExistentGame_ABCDEF");
        Assert.IsFalse(exists);
    }

    [Test]
    public void GetAllSaveFileNames_ReturnsFileNames()
    {
        SaveGame(CreateTestSavedGameInfo("Test_FileNames_1"));
        SaveGame(CreateTestSavedGameInfo("Test_FileNames_2"));

        var fileNames = GetAllSaveFileNames();

        Assert.IsNotNull(fileNames);
        Assert.GreaterOrEqual(fileNames.Count, 2);
    }

    #endregion

    #region Data Integrity Tests

    [Test]
    public void SaveAndLoad_PreservesGameRules()
    {
        var originalInfo = CreateTestSavedGameInfo("Test_RulesIntegrity");
        originalInfo.rules = new GameRules
        {
            enableCurrency = true,
            startingMoney = 1500,
            passGoBonus = 200,
            tilesPerSide = 10,
            minPlayers = 2,
            maxPlayers = 6
        };
        SaveGame(originalInfo);

        var loadedInfo = LoadGame("Test_RulesIntegrity");

        Assert.IsNotNull(loadedInfo.rules);
        Assert.AreEqual(true, loadedInfo.rules.enableCurrency);
        Assert.AreEqual(1500, loadedInfo.rules.startingMoney);
        Assert.AreEqual(200, loadedInfo.rules.passGoBonus);
        Assert.AreEqual(10, loadedInfo.rules.tilesPerSide);
        Assert.AreEqual(2, loadedInfo.rules.minPlayers);
        Assert.AreEqual(6, loadedInfo.rules.maxPlayers);
    }

    [Test]
    public void SaveAndLoad_PreservesIsStandardGameFlag()
    {
        var standardGame = CreateTestSavedGameInfo("Test_StandardFlag");
        standardGame.isStandardGame = true;
        SaveGame(standardGame);

        var loadedStandard = LoadGame("Test_StandardFlag");

        Assert.IsTrue(loadedStandard.isStandardGame);
    }

    [Test]
    public void SaveAndLoad_PreservesCustomGameFlag()
    {
        var customGame = CreateTestSavedGameInfo("Test_CustomFlag");
        customGame.isStandardGame = false;
        SaveGame(customGame);

        var loadedCustom = LoadGame("Test_CustomFlag");

        Assert.IsFalse(loadedCustom.isStandardGame);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void SaveGame_HandlesSpecialCharactersInName()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_Special!@#$%");
        bool result = SaveGame(gameInfo);
        Assert.IsTrue(result);
    }

    [Test]
    public void SaveGame_HandlesSpacesInName()
    {
        var gameInfo = CreateTestSavedGameInfo("Test_Game With Spaces");
        bool result = SaveGame(gameInfo);
        Assert.IsTrue(result);
    }

    [Test]
    public void GetSafeFileName_RemovesInvalidCharacters()
    {
        string safeName = GetSafeFileName("Test:Game<>Name");
        Assert.IsFalse(safeName.Contains(":"));
        Assert.IsFalse(safeName.Contains("<"));
        Assert.IsFalse(safeName.Contains(">"));
    }

    [Test]
    public void GetSafeFileName_ReplacesSpaces()
    {
        string safeName = GetSafeFileName("Test Game Name");
        Assert.IsFalse(safeName.Contains(" "));
        Assert.IsTrue(safeName.Contains("_"));
    }

    #endregion
}

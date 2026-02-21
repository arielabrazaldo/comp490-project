using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Manages saving and loading custom games to/from disk as JSON files.
/// Supports saving game rules, board layouts, and metadata.
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    private static GameSaveManager instance;
    public static GameSaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSaveManager>();
                
                if (instance == null)
                {
                    Debug.LogWarning("[GameSaveManager] No instance found in scene!");
                }
            }
            return instance;
        }
    }

    // Save directory
    private string saveDirectory;
    
    // File extension
    private const string SAVE_FILE_EXTENSION = ".json";

    private void Awake()
    {
        Debug.Log("[GameSaveManager] Awake called");
        
        if (instance == null)
        {
            instance = this;
            
            // CRITICAL FIX: Must be a root GameObject for DontDestroyOnLoad to work
            if (transform.parent != null)
            {
                Debug.Log("[GameSaveManager] Detaching from parent to become root GameObject");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameSaveManager] ? Instance set and moved to DontDestroyOnLoad");
        }
        else if (instance != this)
        {
            Debug.Log("[GameSaveManager] ?? Duplicate instance detected, destroying");
            Destroy(gameObject);
            return;
        }

        // Initialize save directory
        InitializeSaveDirectory();
    }
    
    private void OnEnable()
    {
        Debug.Log("[GameSaveManager] OnEnable called");
    }
    
    private void Start()
    {
        Debug.Log("[GameSaveManager] Start called - Save system ready");
        Debug.Log($"[GameSaveManager] Save directory: {saveDirectory}");
    }
    
    /// <summary>
    /// Initialize the save directory and ensure it exists
    /// </summary>
    private void InitializeSaveDirectory()
    {
        saveDirectory = Path.Combine(Application.persistentDataPath, "CustomGames");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
            Debug.Log($"[GameSaveManager] ? Created save directory: {saveDirectory}");
        }
        else
        {
            Debug.Log($"[GameSaveManager] ? Save directory exists: {saveDirectory}");
            
            // Count existing files
            string[] files = Directory.GetFiles(saveDirectory, "*" + SAVE_FILE_EXTENSION);
            Debug.Log($"[GameSaveManager] Found {files.Length} existing saved games");
        }
    }

    #region Save Operations

    /// <summary>
    /// Save a custom game to disk as JSON
    /// </summary>
    public bool SaveGame(SavedGameInfo gameInfo)
    {
        try
        {
            // Generate unique filename
            string fileName = GetSafeFileName(gameInfo.gameName);
            string filePath = Path.Combine(saveDirectory, fileName + SAVE_FILE_EXTENSION);

            // Check if file already exists
            if (File.Exists(filePath))
            {
                // Add timestamp to make it unique
                fileName = $"{fileName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                filePath = Path.Combine(saveDirectory, fileName + SAVE_FILE_EXTENSION);
            }

            // Convert to JSON-serializable format
            SerializableGameData saveData = new SerializableGameData(gameInfo);

            // Serialize to JSON (pretty print for readability)
            string json = JsonUtility.ToJson(saveData, true);

            // Write to file
            File.WriteAllText(filePath, json);

            Debug.Log($"[GameSaveManager] Saved game to: {filePath}");
            Debug.Log($"[GameSaveManager] File size: {new FileInfo(filePath).Length} bytes");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to save game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save a custom game with a specific filename
    /// </summary>
    public bool SaveGame(SavedGameInfo gameInfo, string fileName)
    {
        try
        {
            string safeFileName = GetSafeFileName(fileName);
            string filePath = Path.Combine(saveDirectory, safeFileName + SAVE_FILE_EXTENSION);

            SerializableGameData saveData = new SerializableGameData(gameInfo);
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(filePath, json);

            Debug.Log($"[GameSaveManager] Saved game as: {fileName}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to save game: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Load Operations

    /// <summary>
    /// Load all saved games from disk
    /// </summary>
    public List<SavedGameInfo> LoadAllGames()
    {
        List<SavedGameInfo> games = new List<SavedGameInfo>();

        try
        {
            // Get all JSON files in save directory
            string[] files = Directory.GetFiles(saveDirectory, "*" + SAVE_FILE_EXTENSION);

            Debug.Log($"[GameSaveManager] Found {files.Length} saved game files");

            foreach (string filePath in files)
            {
                try
                {
                    SavedGameInfo gameInfo = LoadGameFromFile(filePath);
                    if (gameInfo != null)
                    {
                        games.Add(gameInfo);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[GameSaveManager] Failed to load {Path.GetFileName(filePath)}: {e.Message}");
                }
            }

            // Sort by last modified date (most recent first)
            games = games.OrderByDescending(g => g.lastModifiedDate).ToList();

            Debug.Log($"[GameSaveManager] Successfully loaded {games.Count} games");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to load games: {e.Message}");
        }

        return games;
    }

    /// <summary>
    /// Load a specific game by filename
    /// </summary>
    public SavedGameInfo LoadGame(string fileName)
    {
        try
        {
            string filePath = Path.Combine(saveDirectory, fileName);
            if (!filePath.EndsWith(SAVE_FILE_EXTENSION))
            {
                filePath += SAVE_FILE_EXTENSION;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[GameSaveManager] File not found: {fileName}");
                return null;
            }

            return LoadGameFromFile(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to load {fileName}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load game from a specific file path
    /// </summary>
    private SavedGameInfo LoadGameFromFile(string filePath)
    {
        // Read JSON from file
        string json = File.ReadAllText(filePath);

        // Deserialize
        SerializableGameData saveData = JsonUtility.FromJson<SerializableGameData>(json);

        // Convert back to SavedGameInfo
        SavedGameInfo gameInfo = saveData.ToSavedGameInfo();

        Debug.Log($"[GameSaveManager] Loaded: {gameInfo.gameName} (Type: {gameInfo.gameType}, Players: {gameInfo.playerCount})");

        return gameInfo;
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete a saved game
    /// </summary>
    public bool DeleteGame(string fileName)
    {
        try
        {
            string filePath = Path.Combine(saveDirectory, fileName);
            if (!filePath.EndsWith(SAVE_FILE_EXTENSION))
            {
                filePath += SAVE_FILE_EXTENSION;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[GameSaveManager] Deleted: {fileName}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[GameSaveManager] File not found: {fileName}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to delete {fileName}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a saved game by SavedGameInfo
    /// </summary>
    public bool DeleteGame(SavedGameInfo gameInfo)
    {
        string fileName = GetSafeFileName(gameInfo.gameName);
        return DeleteGame(fileName);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get a safe filename (remove invalid characters)
    /// </summary>
    private string GetSafeFileName(string fileName)
    {
        // Remove invalid characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        // Replace spaces with underscores
        safe = safe.Replace(' ', '_');

        // Ensure it's not empty
        if (string.IsNullOrEmpty(safe))
        {
            safe = "CustomGame";
        }

        return safe;
    }

    /// <summary>
    /// Check if a game with this name already exists
    /// </summary>
    public bool GameExists(string gameName)
    {
        string fileName = GetSafeFileName(gameName);
        string filePath = Path.Combine(saveDirectory, fileName + SAVE_FILE_EXTENSION);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Get the save directory path
    /// </summary>
    public string GetSaveDirectory()
    {
        return saveDirectory;
    }

    /// <summary>
    /// Get all saved game filenames
    /// </summary>
    public List<string> GetAllSaveFileNames()
    {
        try
        {
            string[] files = Directory.GetFiles(saveDirectory, "*" + SAVE_FILE_EXTENSION);
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Failed to get file names: {e.Message}");
            return new List<string>();
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Print Save Directory")]
    private void PrintSaveDirectory()
    {
        Debug.Log($"Save Directory: {saveDirectory}");
        Debug.Log($"Directory Exists: {Directory.Exists(saveDirectory)}");

        if (Directory.Exists(saveDirectory))
        {
            string[] files = Directory.GetFiles(saveDirectory, "*" + SAVE_FILE_EXTENSION);
            Debug.Log($"Saved Games: {files.Length}");
            foreach (string file in files)
            {
                Debug.Log($"  - {Path.GetFileName(file)}");
            }
        }
    }

    [ContextMenu("Open Save Directory in Explorer")]
    private void OpenSaveDirectory()
    {
        if (Directory.Exists(saveDirectory))
        {
            System.Diagnostics.Process.Start(saveDirectory);
        }
        else
        {
            Debug.LogWarning("Save directory does not exist yet!");
        }
    }

    #endregion
}

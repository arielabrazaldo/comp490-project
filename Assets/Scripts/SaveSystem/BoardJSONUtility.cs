using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Utility for JSON serialization and deserialization of board and UI data
/// Handles converting between Unity objects and JSON files
/// </summary>
public static class BoardJSONUtility
{
    /// <summary>
    /// Save board data to JSON file
    /// </summary>
    public static bool SaveBoardToJSON(SerializableBoardData boardData, string fileName)
    {
        try
        {
            string json = JsonUtility.ToJson(boardData, true); // Pretty print
            string filePath = GetBoardFilePath(fileName);
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            Debug.Log($"Board data saved to: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save board data: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load board data from JSON file
    /// </summary>
    public static SerializableBoardData LoadBoardFromJSON(string fileName)
    {
        try
        {
            string filePath = GetBoardFilePath(fileName);
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Board file not found: {filePath}");
                return null;
            }
            
            string json = File.ReadAllText(filePath);
            SerializableBoardData boardData = JsonUtility.FromJson<SerializableBoardData>(json);
            
            Debug.Log($"Board data loaded from: {filePath}");
            return boardData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load board data: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Save UI layout to JSON file
    /// </summary>
    public static bool SaveUILayoutToJSON(SerializableUILayout uiLayout, string fileName)
    {
        try
        {
            string json = JsonUtility.ToJson(uiLayout, true);
            string filePath = GetUILayoutFilePath(fileName);
            
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            Debug.Log($"UI layout saved to: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save UI layout: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load UI layout from JSON file
    /// </summary>
    public static SerializableUILayout LoadUILayoutFromJSON(string fileName)
    {
        try
        {
            string filePath = GetUILayoutFilePath(fileName);
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"UI layout file not found: {filePath}");
                return null;
            }
            
            string json = File.ReadAllText(filePath);
            SerializableUILayout uiLayout = JsonUtility.FromJson<SerializableUILayout>(json);
            
            Debug.Log($"UI layout loaded from: {filePath}");
            return uiLayout;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load UI layout: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Export current Monopoly board to JSON
    /// </summary>
    public static bool ExportMonopolyBoardToJSON(string fileName)
    {
        if (MonopolyBoardManager.Instance == null)
        {
            Debug.LogError("MonopolyBoardManager not found!");
            return false;
        }
        
        // Get board data from MonopolyBoardManager
        var boardData = MonopolyBoard.CreateStandardBoard();
        
        // TODO: Get tile GameObjects and positions from board manager
        // This requires MonopolyBoardManager to expose its generated tiles
        
        Debug.Log("Export Monopoly board to JSON (implementation pending)");
        return false;
    }
    
    /// <summary>
    /// Import board from JSON and generate in Unity
    /// </summary>
    public static GameObject ImportBoardFromJSON(string fileName, Transform parent)
    {
        SerializableBoardData boardData = LoadBoardFromJSON(fileName);
        
        if (boardData == null)
        {
            Debug.LogError("Failed to load board data!");
            return null;
        }
        
        if (BoardUIGenerator.Instance == null)
        {
            Debug.LogError("BoardUIGenerator not found!");
            return null;
        }
        
        GameObject board = BoardUIGenerator.Instance.GenerateBoard(boardData, parent);
        return board;
    }
    
    /// <summary>
    /// Get list of available board files
    /// </summary>
    public static List<string> GetAvailableBoardFiles()
    {
        List<string> boardFiles = new List<string>();
        
        try
        {
            string boardsDirectory = GetBoardsDirectory();
            
            if (!Directory.Exists(boardsDirectory))
            {
                return boardFiles;
            }
            
            string[] files = Directory.GetFiles(boardsDirectory, "*.json");
            
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                boardFiles.Add(fileName);
            }
            
            Debug.Log($"Found {boardFiles.Count} board files");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get board files: {e.Message}");
        }
        
        return boardFiles;
    }
    
    /// <summary>
    /// Get list of available UI layout files
    /// </summary>
    public static List<string> GetAvailableUILayoutFiles()
    {
        List<string> layoutFiles = new List<string>();
        
        try
        {
            string layoutsDirectory = GetUILayoutsDirectory();
            
            if (!Directory.Exists(layoutsDirectory))
            {
                return layoutFiles;
            }
            
            string[] files = Directory.GetFiles(layoutsDirectory, "*.json");
            
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                layoutFiles.Add(fileName);
            }
            
            Debug.Log($"Found {layoutFiles.Count} UI layout files");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get UI layout files: {e.Message}");
        }
        
        return layoutFiles;
    }
    
    /// <summary>
    /// Create example board JSON for reference
    /// </summary>
    public static void CreateExampleBoardJSON()
    {
        SerializableBoardData exampleBoard = new SerializableBoardData
        {
            boardName = "Example Custom Board",
            boardType = "Square",
            layoutPattern = "Perimeter",
            totalTiles = 4,
            boardWidth = 800f,
            boardHeight = 800f
        };
        
        // Add example tiles
        exampleBoard.tiles.Add(new SerializableTileData
        {
            tileId = 0,
            tileName = "Start",
            tileType = "Special",
            displayText = "START",
            backgroundColor = "#00FF00",
            positionX = 350f,
            positionY = -350f,
            width = 70f,
            height = 70f,
            fontSize = 16f,
            hasGlowEffect = true
        });
        
        exampleBoard.tiles.Add(new SerializableTileData
        {
            tileId = 1,
            tileName = "Park Place",
            tileType = "Property",
            displayText = "Park Place",
            backgroundColor = "#0000FF",
            propertyPrice = 350,
            propertyRent = 35,
            positionX = 280f,
            positionY = -350f,
            canHaveBuildings = true
        });
        
        SaveBoardToJSON(exampleBoard, "ExampleBoard");
        Debug.Log("Created example board JSON");
    }
    
    #region Helper Methods
    
    private static string GetBoardsDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "Boards");
    }
    
    private static string GetUILayoutsDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "UILayouts");
    }
    
    private static string GetBoardFilePath(string fileName)
    {
        if (!fileName.EndsWith(".json"))
        {
            fileName += ".json";
        }
        
        return Path.Combine(GetBoardsDirectory(), fileName);
    }
    
    private static string GetUILayoutFilePath(string fileName)
    {
        if (!fileName.EndsWith(".json"))
        {
            fileName += ".json";
        }
        
        return Path.Combine(GetUILayoutsDirectory(), fileName);
    }
    
    #endregion
}

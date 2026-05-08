using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager for custom board creation and loading
/// Coordinates between JSON data, board generation, and game setup
/// </summary>
public class CustomBoardManager : MonoBehaviour
{
    private static CustomBoardManager instance;
    public static CustomBoardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CustomBoardManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CustomBoardManager");
                    instance = go.AddComponent<CustomBoardManager>();
                }
            }
            return instance;
        }
    }

    [Header("Board Generation")]
    [SerializeField] private Transform boardParent;
    
    [Header("Current Board")]
    private SerializableBoardData currentBoardData;
    private GameObject currentBoardObject;
    private bool isCustomBoardActive = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // FIXED: Make root GameObject before DontDestroyOnLoad
            if (transform.parent != null)
            {
                Debug.Log("[CustomBoardManager] Detaching from parent to become root GameObject");
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Load and generate a board from JSON file
    /// </summary>
    public bool LoadBoardFromFile(string fileName)
    {
        Debug.Log($"Loading board from file: {fileName}");
        
        // Clear existing board
        ClearCurrentBoard();
        
        // Load board data
        currentBoardData = BoardJSONUtility.LoadBoardFromJSON(fileName);
        
        if (currentBoardData == null)
        {
            Debug.LogError($"Failed to load board: {fileName}");
            return false;
        }
        
        // Generate board
        return GenerateBoardFromData();
    }
    
    /// <summary>
    /// Generate board from loaded data
    /// </summary>
    private bool GenerateBoardFromData()
    {
        if (currentBoardData == null)
        {
            Debug.LogError("No board data loaded!");
            return false;
        }
        
        if (BoardUIGenerator.Instance == null)
        {
            Debug.LogError("BoardUIGenerator not found!");
            return false;
        }
        
        // Get or create board parent
        if (boardParent == null)
        {
            boardParent = FindBoardParent();
        }
        
        if (boardParent == null)
        {
            Debug.LogError("Board parent not found!");
            return false;
        }
        
        // Generate board
        currentBoardObject = BoardUIGenerator.Instance.GenerateBoard(currentBoardData, boardParent);
        
        if (currentBoardObject == null)
        {
            Debug.LogError("Failed to generate board!");
            return false;
        }
        
        isCustomBoardActive = true;
        Debug.Log($"Board generated successfully: {currentBoardData.boardName}");
        return true;
    }
    
    /// <summary>
    /// Save current board to JSON file
    /// </summary>
    public bool SaveCurrentBoard(string fileName)
    {
        if (currentBoardData == null)
        {
            Debug.LogError("No board data to save!");
            return false;
        }
        
        // Update last modified date
        currentBoardData.lastModifiedDate = System.DateTime.Now.ToString("o");
        
        return BoardJSONUtility.SaveBoardToJSON(currentBoardData, fileName);
    }
    
    /// <summary>
    /// Export standard Monopoly board to JSON (for editing)
    /// </summary>
    public bool ExportStandardMonopolyBoard(string fileName = "StandardMonopolyBoard")
    {
        if (MonopolyBoardManager.Instance == null)
        {
            Debug.LogError("MonopolyBoardManager not found!");
            return false;
        }
        
        // Create standard board data
        var spaces = MonopolyBoard.CreateStandardBoard();
        
        var boardData = new SerializableBoardData
        {
            boardName = "Standard Monopoly Board",
            boardType = "Square",
            layoutPattern = "Perimeter",
            totalTiles = 40,
            boardWidth = 800f,
            boardHeight = 800f
        };
        
        // Convert spaces to tile data
        float tileSize = 70f;
        float boardSize = tileSize * 10;
        
        for (int i = 0; i < spaces.Count; i++)
        {
            var space = spaces[i];
            Vector2 position = CalculateMonopolyTilePosition(i, boardSize, tileSize);
            
            var tileData = SerializableTileData.FromMonopolySpace(space, i, position, new Vector2(tileSize, tileSize));
            boardData.tiles.Add(tileData);
        }
        
        return BoardJSONUtility.SaveBoardToJSON(boardData, fileName);
    }
    
    /// <summary>
    /// Calculate tile position for Monopoly board layout
    /// </summary>
    private Vector2 CalculateMonopolyTilePosition(int index, float boardSize, float tileSize)
    {
        float halfBoard = boardSize / 2f;
        
        if (index <= 10) // Bottom side
        {
            float x = halfBoard - (index * tileSize);
            return new Vector2(x, -halfBoard);
        }
        else if (index <= 20) // Left side
        {
            int sidePosition = index - 10;
            float y = -halfBoard + (sidePosition * tileSize);
            return new Vector2(-halfBoard, y);
        }
        else if (index <= 30) // Top side
        {
            int sidePosition = index - 20;
            float x = -halfBoard + (sidePosition * tileSize);
            return new Vector2(x, halfBoard);
        }
        else // Right side
        {
            int sidePosition = index - 30;
            float y = halfBoard - (sidePosition * tileSize);
            return new Vector2(halfBoard, y);
        }
    }
    
    /// <summary>
    /// Create a new custom board from scratch
    /// </summary>
    public SerializableBoardData CreateNewBoard(string boardName, string boardType, int tileCount)
    {
        currentBoardData = new SerializableBoardData
        {
            boardName = boardName,
            boardType = boardType,
            totalTiles = tileCount,
            createdDate = System.DateTime.Now.ToString("o"),
            lastModifiedDate = System.DateTime.Now.ToString("o")
        };
        
        // Create default tiles
        for (int i = 0; i < tileCount; i++)
        {
            var tile = new SerializableTileData
            {
                tileId = i,
                tileName = $"Tile {i}",
                tileType = "Normal"
            };
            
            currentBoardData.tiles.Add(tile);
        }
        
        Debug.Log($"Created new board: {boardName} with {tileCount} tiles");
        return currentBoardData;
    }
    
    /// <summary>
    /// Clear current board
    /// </summary>
    public void ClearCurrentBoard()
    {
        if (currentBoardObject != null)
        {
            Destroy(currentBoardObject);
            currentBoardObject = null;
        }
        
        if (BoardUIGenerator.Instance != null)
        {
            BoardUIGenerator.Instance.ClearGeneratedTiles();
        }
        
        currentBoardData = null;
        isCustomBoardActive = false;
        
        Debug.Log("Current board cleared");
    }
    
    /// <summary>
    /// Get list of available board files
    /// </summary>
    public List<string> GetAvailableBoards()
    {
        return BoardJSONUtility.GetAvailableBoardFiles();
    }
    
    /// <summary>
    /// Get current board data (for editing)
    /// </summary>
    public SerializableBoardData GetCurrentBoardData()
    {
        return currentBoardData;
    }
    
    /// <summary>
    /// Update a specific tile in the current board
    /// </summary>
    public bool UpdateTile(int tileId, SerializableTileData updatedTileData)
    {
        if (currentBoardData == null)
        {
            Debug.LogError("No board loaded!");
            return false;
        }
        
        var tile = currentBoardData.tiles.Find(t => t.tileId == tileId);
        if (tile == null)
        {
            Debug.LogError($"Tile {tileId} not found!");
            return false;
        }
        
        // Update tile
        int index = currentBoardData.tiles.IndexOf(tile);
        currentBoardData.tiles[index] = updatedTileData;
        
        Debug.Log($"Updated tile {tileId}");
        return true;
    }
    
    /// <summary>
    /// Check if custom board is active
    /// </summary>
    public bool IsCustomBoardActive()
    {
        return isCustomBoardActive;
    }
    
    /// <summary>
    /// Find board parent transform in scene
    /// </summary>
    private Transform FindBoardParent()
    {
        // Try to find MonopolyBoardManager board parent
        if (MonopolyBoardManager.Instance != null && MonopolyBoardManager.Instance.boardParent != null)
        {
            return MonopolyBoardManager.Instance.boardParent;
        }
        
        // Try to find canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            return canvas.transform;
        }
        
        return null;
    }
    
    #region Debug Methods
    
    /// <summary>
    /// Create example board for testing
    /// </summary>
    [ContextMenu("Create Example Board")]
    public void CreateExampleBoard()
    {
        BoardJSONUtility.CreateExampleBoardJSON();
    }
    
    /// <summary>
    /// Export standard Monopoly board
    /// </summary>
    [ContextMenu("Export Standard Monopoly Board")]
    public void ExportStandardBoard()
    {
        ExportStandardMonopolyBoard();
    }
    
    /// <summary>
    /// List available boards
    /// </summary>
    [ContextMenu("List Available Boards")]
    public void ListAvailableBoards()
    {
        var boards = GetAvailableBoards();
        Debug.Log($"Available boards: {boards.Count}");
        foreach (var board in boards)
        {
            Debug.Log($"  - {board}");
        }
    }
    
    #endregion
}

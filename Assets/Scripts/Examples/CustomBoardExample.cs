using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the Custom Board System
/// Shows common use cases and workflows
/// </summary>
public class CustomBoardExample : MonoBehaviour
{
    [Header("Example Controls")]
    [Tooltip("Export standard Monopoly board as JSON template")]
    public bool exportStandardBoard = false;
    
    [Tooltip("Create example board from scratch")]
    public bool createExampleBoard = false;
    
    [Tooltip("Load custom board from file")]
    public bool loadCustomBoard = false;
    
    [Tooltip("Board file name to load (without .json)")]
    public string boardFileName = "MyCustomBoard";
    
    private void Update()
    {
        // Export standard board
        if (exportStandardBoard)
        {
            exportStandardBoard = false;
            ExportStandardBoardExample();
        }
        
        // Create example board
        if (createExampleBoard)
        {
            createExampleBoard = false;
            CreateExampleBoardFromScratch();
        }
        
        // Load custom board
        if (loadCustomBoard)
        {
            loadCustomBoard = false;
            LoadCustomBoardExample();
        }
    }
    
    /// <summary>
    /// Example 1: Export standard Monopoly board as JSON template
    /// This creates a fully editable JSON file of the standard board
    /// </summary>
    public void ExportStandardBoardExample()
    {
        Debug.Log("=== Example 1: Exporting Standard Board ===");
        
        bool success = CustomBoardManager.Instance.ExportStandardMonopolyBoard("StandardMonopolyTemplate");
        
        if (success)
        {
            Debug.Log("? Standard board exported!");
            Debug.Log($"File location: {Application.persistentDataPath}/Boards/StandardMonopolyTemplate.json");
            Debug.Log("You can now edit this JSON file to customize the board.");
        }
        else
        {
            Debug.LogError("? Failed to export standard board!");
        }
    }
    
    /// <summary>
    /// Example 2: Create a custom board from scratch
    /// </summary>
    public void CreateExampleBoardFromScratch()
    {
        Debug.Log("=== Example 2: Creating Custom Board ===");
        
        // Create new board with 12 tiles
        var boardData = CustomBoardManager.Instance.CreateNewBoard(
            "My Simple Board",
            "Linear",
            12
        );
        
        if (boardData == null)
        {
            Debug.LogError("? Failed to create board!");
            return;
        }
        
        Debug.Log($"? Created board: {boardData.boardName}");
        
        // Customize board properties
        boardData.boardWidth = 1000f;
        boardData.boardHeight = 200f;
        boardData.backgroundColor = "#F0F0F0";
        
        // Customize first tile (Start)
        var startTile = boardData.tiles[0];
        startTile.tileName = "Start";
        startTile.tileType = "Special";
        startTile.displayText = "START";
        startTile.backgroundColor = "#00FF00";
        startTile.fontSize = 18f;
        startTile.fontStyle = "Bold";
        startTile.hasGlowEffect = true;
        startTile.glowColor = "#FFFF00";
        startTile.positionX = -450f;
        
        // Customize middle tiles (Properties)
        for (int i = 1; i < 11; i++)
        {
            var tile = boardData.tiles[i];
            tile.tileName = $"Property {i}";
            tile.tileType = "Property";
            tile.displayText = $"Property {i}";
            tile.backgroundColor = GetColorForIndex(i);
            tile.propertyPrice = 100 + (i * 50);
            tile.propertyRent = 10 + (i * 5);
            tile.canHaveBuildings = true;
            tile.positionX = -450f + (i * 90f);
            tile.width = 80f;
            tile.height = 80f;
        }
        
        // Customize last tile (Goal)
        var goalTile = boardData.tiles[11];
        goalTile.tileName = "Goal";
        goalTile.tileType = "Special";
        goalTile.displayText = "GOAL";
        goalTile.backgroundColor = "#FFD700";
        goalTile.fontSize = 18f;
        goalTile.fontStyle = "Bold";
        goalTile.positionX = 450f;
        
        // Save board
        bool success = CustomBoardManager.Instance.SaveCurrentBoard("MySimpleBoard");
        
        if (success)
        {
            Debug.Log("? Board saved successfully!");
            Debug.Log($"File: {Application.persistentDataPath}/Boards/MySimpleBoard.json");
        }
        else
        {
            Debug.LogError("? Failed to save board!");
        }
    }
    
    /// <summary>
    /// Example 3: Load and display a custom board
    /// </summary>
    public void LoadCustomBoardExample()
    {
        Debug.Log($"=== Example 3: Loading Custom Board '{boardFileName}' ===");
        
        bool success = CustomBoardManager.Instance.LoadBoardFromFile(boardFileName);
        
        if (success)
        {
            Debug.Log("? Board loaded and displayed!");
            
            // Get board data to inspect
            var boardData = CustomBoardManager.Instance.GetCurrentBoardData();
            Debug.Log($"Board Name: {boardData.boardName}");
            Debug.Log($"Board Type: {boardData.boardType}");
            Debug.Log($"Total Tiles: {boardData.tiles.Count}");
            Debug.Log($"Board Size: {boardData.boardWidth}x{boardData.boardHeight}");
        }
        else
        {
            Debug.LogError($"? Failed to load board '{boardFileName}'!");
            Debug.LogError($"Make sure the file exists at: {Application.persistentDataPath}/Boards/{boardFileName}.json");
        }
    }
    
    /// <summary>
    /// Example 4: Modify existing board
    /// </summary>
    public void ModifyBoardExample()
    {
        Debug.Log("=== Example 4: Modifying Board ===");
        
        // First, load the board
        if (!CustomBoardManager.Instance.LoadBoardFromFile(boardFileName))
        {
            Debug.LogError("Failed to load board!");
            return;
        }
        
        // Get board data
        var boardData = CustomBoardManager.Instance.GetCurrentBoardData();
        
        // Modify a specific tile
        if (boardData.tiles.Count > 0)
        {
            var tile = boardData.tiles[0];
            Debug.Log($"Modifying tile: {tile.tileName}");
            
            // Change properties
            tile.backgroundColor = "#FF0000"; // Red
            tile.fontSize = 20f;
            tile.hasGlowEffect = true;
            
            // Update the tile
            CustomBoardManager.Instance.UpdateTile(tile.tileId, tile);
            
            Debug.Log("? Tile modified!");
        }
        
        // Save modified board
        CustomBoardManager.Instance.SaveCurrentBoard($"{boardFileName}_Modified");
        Debug.Log($"? Modified board saved as '{boardFileName}_Modified'");
    }
    
    /// <summary>
    /// Example 5: List all available boards
    /// </summary>
    public void ListAvailableBoardsExample()
    {
        Debug.Log("=== Example 5: Available Boards ===");
        
        var boards = CustomBoardManager.Instance.GetAvailableBoards();
        
        if (boards.Count == 0)
        {
            Debug.Log("No custom boards found.");
            Debug.Log($"Create boards and save them to: {Application.persistentDataPath}/Boards/");
        }
        else
        {
            Debug.Log($"Found {boards.Count} custom boards:");
            foreach (var board in boards)
            {
                Debug.Log($"  - {board}");
            }
        }
    }
    
    /// <summary>
    /// Example 6: Create animated board
    /// </summary>
    public void CreateAnimatedBoardExample()
    {
        Debug.Log("=== Example 6: Creating Animated Board ===");
        
        var boardData = CustomBoardManager.Instance.CreateNewBoard(
            "Animated Board",
            "Linear",
            6
        );
        
        // Make tiles animated
        for (int i = 0; i < boardData.tiles.Count; i++)
        {
            var tile = boardData.tiles[i];
            tile.isAnimated = true;
            tile.positionX = -250f + (i * 100f);
            
            // Different animation for each tile
            switch (i % 4)
            {
                case 0:
                    tile.animationType = "Pulse";
                    tile.backgroundColor = "#FF0000";
                    break;
                case 1:
                    tile.animationType = "Bounce";
                    tile.backgroundColor = "#00FF00";
                    break;
                case 2:
                    tile.animationType = "Rotate";
                    tile.backgroundColor = "#0000FF";
                    break;
                case 3:
                    tile.animationType = "Shake";
                    tile.backgroundColor = "#FFFF00";
                    break;
            }
            
            tile.animationSpeed = 1.5f;
        }
        
        CustomBoardManager.Instance.SaveCurrentBoard("AnimatedBoard");
        Debug.Log("? Animated board created and saved!");
    }
    
    /// <summary>
    /// Helper: Get color for tile index
    /// </summary>
    private string GetColorForIndex(int index)
    {
        string[] colors = {
            "#8B4513", // Brown
            "#87CEEB", // Light Blue
            "#FF69B4", // Pink
            "#FFA500", // Orange
            "#FF0000", // Red
            "#FFFF00", // Yellow
            "#00FF00", // Green
            "#00008B"  // Dark Blue
        };
        
        return colors[index % colors.Length];
    }
    
    #region Context Menu Commands
    
    [ContextMenu("1. Export Standard Board")]
    private void Menu_ExportStandardBoard()
    {
        ExportStandardBoardExample();
    }
    
    [ContextMenu("2. Create Example Board")]
    private void Menu_CreateExampleBoard()
    {
        CreateExampleBoardFromScratch();
    }
    
    [ContextMenu("3. Load Custom Board")]
    private void Menu_LoadCustomBoard()
    {
        LoadCustomBoardExample();
    }
    
    [ContextMenu("4. Modify Board")]
    private void Menu_ModifyBoard()
    {
        ModifyBoardExample();
    }
    
    [ContextMenu("5. List Available Boards")]
    private void Menu_ListBoards()
    {
        ListAvailableBoardsExample();
    }
    
    [ContextMenu("6. Create Animated Board")]
    private void Menu_CreateAnimatedBoard()
    {
        CreateAnimatedBoardExample();
    }
    
    #endregion
}

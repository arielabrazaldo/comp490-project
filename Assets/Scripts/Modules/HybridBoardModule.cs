using UnityEngine;

/// <summary>
/// Board module for hybrid games - manages board configuration
/// </summary>
public class HybridBoardModule : MonoBehaviour
{
    private GameRules rules;
    private bool isSeparateBoards;
    private int boardSize;

    public void Initialize(GameRules gameRules)
    {
        rules = gameRules;
        isSeparateBoards = rules.separatePlayerBoards;
        
        // Determine board size
        if (isSeparateBoards)
        {
            // Grid-based board (like Battleships)
            boardSize = rules.tilesPerSide * rules.tilesPerSide;
        }
        else
        {
            // Linear or loop board (like Monopoly/Dice Race)
            if (rules.enableCurrency && rules.canPurchaseProperties)
            {
                boardSize = 40; // Monopoly standard
            }
            else
            {
                boardSize = rules.tilesPerSide;
            }
        }
        
        Debug.Log($"[HybridBoardModule] Initialized - Separate boards: {isSeparateBoards}, Size: {boardSize}");
    }

    /// <summary>
    /// Get board size
    /// </summary>
    public int GetBoardSize()
    {
        return boardSize;
    }

    /// <summary>
    /// Check if using separate boards
    /// </summary>
    public bool IsSeparateBoards()
    {
        return isSeparateBoards;
    }

    /// <summary>
    /// Get goal position (win condition position)
    /// </summary>
    public int GetGoalPosition()
    {
        if (isSeparateBoards)
        {
            // For grid boards, goal is typically far corner
            return boardSize - 1;
        }
        else
        {
            // For loop boards, goal might be a specific landmark
            return boardSize - 1;
        }
    }

    /// <summary>
    /// Convert 1D position to 2D grid coordinates (for separate boards)
    /// </summary>
    public Vector2Int PositionToGrid(int position)
    {
        if (!isSeparateBoards)
        {
            Debug.LogWarning("[HybridBoardModule] Cannot convert to grid for shared board");
            return Vector2Int.zero;
        }
        
        int cols = rules.tilesPerSide;
        int x = position % cols;
        int y = position / cols;
        
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Convert 2D grid coordinates to 1D position (for separate boards)
    /// </summary>
    public int GridToPosition(Vector2Int grid)
    {
        if (!isSeparateBoards)
        {
            Debug.LogWarning("[HybridBoardModule] Cannot convert from grid for shared board");
            return 0;
        }
        
        int cols = rules.tilesPerSide;
        return grid.y * cols + grid.x;
    }

    /// <summary>
    /// Check if position is a special space (corner, property, etc.)
    /// </summary>
    public bool IsSpecialSpace(int position)
    {
        if (!isSeparateBoards)
        {
            // Monopoly-style special spaces
            return position == 0 || // GO
                   position == boardSize / 4 || // First corner
                   position == boardSize / 2 || // Second corner
                   position == (boardSize * 3) / 4; // Third corner
        }
        else
        {
            // Grid-based special spaces (corners, center, etc.)
            Vector2Int grid = PositionToGrid(position);
            int size = rules.tilesPerSide;
            
            // Corners
            return (grid.x == 0 && grid.y == 0) ||
                   (grid.x == size - 1 && grid.y == 0) ||
                   (grid.x == 0 && grid.y == size - 1) ||
                   (grid.x == size - 1 && grid.y == size - 1);
        }
    }

    /// <summary>
    /// Get space type name for position
    /// </summary>
    public string GetSpaceTypeName(int position)
    {
        if (position == 0)
        {
            return "START";
        }
        else if (position == GetGoalPosition())
        {
            return "GOAL";
        }
        else if (IsSpecialSpace(position))
        {
            return "SPECIAL";
        }
        else
        {
            return "NORMAL";
        }
    }
}

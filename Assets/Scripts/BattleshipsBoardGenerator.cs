using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Generates visual board grids for Battleships game
/// Creates player and enemy boards with clickable tiles
/// </summary>
public class BattleshipsBoardGenerator : MonoBehaviour
{
    [Header("Board References")]
    [SerializeField] private Transform playerBoardParent;
    [SerializeField] private Transform enemyBoardParent;
    
    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab;
    
    [Header("Board Configuration")]
    [SerializeField] private float cellSize = 40f;
    [SerializeField] private float spacing = 2f;
    
    private Dictionary<Vector2Int, GameObject> playerBoardTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> enemyBoardTiles = new Dictionary<Vector2Int, GameObject>();

    /// <summary>
    /// Generate both player and enemy boards
    /// </summary>
    public void GenerateBoards()
    {
        if (BattleshipsGameManager.Instance == null)
        {
            Debug.LogError("BattleshipsGameManager not found! Cannot generate boards.");
            return;
        }

        // Get board configuration from game manager
        var (rows, cols) = BattleshipsGameManager.Instance.GetBoardDimensions();
        HashSet<Vector2Int> activeTiles = BattleshipsGameManager.Instance.GetActiveTiles();

        if (activeTiles == null || activeTiles.Count == 0)
        {
            Debug.LogError("No active tiles found! Make sure game is initialized.");
            return;
        }

        Debug.Log($"Generating boards: {rows}x{cols} with {activeTiles.Count} active tiles");

        // CRITICAL FIX: Start with both boards INACTIVE
        // They will be activated when needed (player board for ship placement, enemy board for combat)
        if (playerBoardParent != null)
        {
            playerBoardParent.gameObject.SetActive(false);
            Debug.Log("?? Player board parent set to INACTIVE (will activate for ship placement)");
        }
        
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(false);
            Debug.Log("?? Enemy board parent set to INACTIVE (will activate for combat)");
        }

        // Get local player ID
        int localPlayerId = GetLocalPlayerId();
        Debug.Log($"?? Local Player ID: {localPlayerId}");

        // Generate player board tiles (but keep parent inactive)
        Debug.Log($"?? Generating PLAYER board tiles for local player {localPlayerId}");
        playerBoardParent.gameObject.SetActive(true); // Temporarily activate to generate tiles
        playerBoardTiles = GenerateBoard(playerBoardParent, rows, cols, activeTiles, true, localPlayerId);
        playerBoardParent.gameObject.SetActive(false); // Deactivate again - UI Manager will show it
        Debug.Log($"? Player board tiles generated ({playerBoardTiles.Count} tiles) - parent kept INACTIVE");
        
        // For enemy board, get opponent player ID
        int opponentId = GetOpponentPlayerId();
        Debug.Log($"?? Generating ENEMY board tiles for opponent {opponentId}");
        
        // Generate enemy board tiles (but keep parent inactive)
        enemyBoardParent.gameObject.SetActive(true); // Temporarily activate to generate tiles
        enemyBoardTiles = GenerateBoard(enemyBoardParent, rows, cols, activeTiles, false, opponentId);
        enemyBoardParent.gameObject.SetActive(false); // Deactivate - will show in combat
        Debug.Log($"? Enemy board tiles generated ({enemyBoardTiles.Count} tiles) - parent kept INACTIVE");

        // Initialize UI manager with tile references
        if (BattleshipsUIManager.Instance != null)
        {
            BattleshipsUIManager.Instance.InitializeBoardTiles(playerBoardTiles, enemyBoardTiles);
            Debug.Log("Board tiles registered with UI Manager");
        }
        else
        {
            Debug.LogWarning("BattleshipsUIManager not found! Tiles won't be registered.");
        }
        
        Debug.Log($"? Board generation complete! Both boards INACTIVE and ready for use");
    }

    /// <summary>
    /// Generate a single board (player or enemy)
    /// </summary>
    private Dictionary<Vector2Int, GameObject> GenerateBoard(
        Transform parent, 
        int rows, 
        int cols, 
        HashSet<Vector2Int> activeTiles, 
        bool isPlayerBoard,
        int targetPlayerId)
    {
        if (parent == null)
        {
            Debug.LogError($"Parent transform is null for {(isPlayerBoard ? "Player" : "Enemy")} board!");
            return new Dictionary<Vector2Int, GameObject>();
        }

        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned!");
            return new Dictionary<Vector2Int, GameObject>();
        }

        var tiles = new Dictionary<Vector2Int, GameObject>();

        // Clear any existing tiles
        ClearBoard(parent);

        // Configure GridLayoutGroup
        GridLayoutGroup grid = parent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = parent.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(spacing, spacing);
        grid.childAlignment = TextAnchor.MiddleCenter;

        Debug.Log($"Creating {(isPlayerBoard ? "Player" : "Enemy")} board with {activeTiles.Count} tiles");

        // Create tiles
        int tilesCreated = 0;
        int placeholdersCreated = 0;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Vector2Int pos = new Vector2Int(col, row);

                // Only create active tiles
                if (!activeTiles.Contains(pos))
                {
                    // Create visible placeholder with brown color to show disabled area
                    GameObject placeholder = new GameObject($"Placeholder_{row}_{col}");
                    placeholder.transform.SetParent(parent);
                    
                    RectTransform rectTransform = placeholder.AddComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                    
                    // CRITICAL FIX: Reset local position and scale
                    rectTransform.localPosition = Vector3.zero;
                    rectTransform.localScale = Vector3.one;
                    rectTransform.localRotation = Quaternion.identity;
                    
                    // Make it visible with brown color (like sand/earth)
                    Image placeholderImage = placeholder.AddComponent<Image>();
                    placeholderImage.color = new Color(0.55f, 0.35f, 0.2f, 0.8f); // Brown with slight transparency
                    placeholderImage.raycastTarget = false; // Don't catch clicks
                    
                    // Make it non-interactive
                    CanvasGroup canvasGroup = placeholder.AddComponent<CanvasGroup>();
                    canvasGroup.interactable = false; // Can't interact
                    canvasGroup.blocksRaycasts = false; // Doesn't block clicks
                    
                    placeholdersCreated++;
                    
                    // Log first few placeholders for verification
                    if (placeholdersCreated <= 3)
                    {
                        Debug.Log($"Created PLACEHOLDER at ({col}, {row}) for {(isPlayerBoard ? "Player" : "Enemy")} board");
                    }
                    
                    continue;
                }

                // Create active tile
                GameObject tile = Instantiate(tilePrefab, parent);
                tile.name = $"Tile_{row}_{col}_{(isPlayerBoard ? "Player" : "Enemy")}";

                // CRITICAL FIX: Immediately reset transform to avoid positioning issues
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                if (tileRect != null)
                {
                    // Reset to local space coordinates
                    tileRect.localPosition = Vector3.zero;
                    tileRect.localScale = Vector3.one;
                    tileRect.localRotation = Quaternion.identity;
                    
                    // Set anchors to center to avoid parent size issues
                    tileRect.anchorMin = new Vector2(0.5f, 0.5f);
                    tileRect.anchorMax = new Vector2(0.5f, 0.5f);
                    tileRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // Set size
                    tileRect.sizeDelta = new Vector2(cellSize, cellSize);
                }

                // Get or add Image component
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage == null)
                {
                    tileImage = tile.AddComponent<Image>();
                }
                // Set active tiles to sea water blue
                tileImage.color = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Deep sea blue
                tileImage.raycastTarget = true;

                // Get or add Button component
                Button tileButton = tile.GetComponent<Button>();
                if (tileButton == null)
                {
                    tileButton = tile.AddComponent<Button>();
                }

                // Configure button colors with sea water theme
                ColorBlock colors = tileButton.colors;
                colors.normalColor = new Color(0.0f, 0.4f, 0.7f, 1.0f); // Deep sea blue
                colors.highlightedColor = new Color(0.0f, 0.6f, 0.9f, 1.0f); // Lighter blue on hover
                colors.pressedColor = new Color(0.0f, 0.8f, 1.0f, 1.0f); // Bright cyan when pressed
                colors.selectedColor = new Color(0.0f, 0.5f, 0.8f, 1.0f); // Medium sea blue
                colors.disabledColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); // Gray when disabled
                tileButton.colors = colors;

                // Configure click handler
                BattleshipsTileClickHandler handler = tile.GetComponent<BattleshipsTileClickHandler>();
                if (handler == null)
                {
                    handler = tile.AddComponent<BattleshipsTileClickHandler>();
                }
                
                handler.SetTilePosition(pos);
                handler.SetBoardType(isPlayerBoard);
                
                if (!isPlayerBoard)
                {
                    handler.SetTargetPlayerId(targetPlayerId);
                }

                tiles[pos] = tile;
                tilesCreated++;
            }
        }

        Debug.Log($"Created {tilesCreated} active tiles and {placeholdersCreated} placeholders for {(isPlayerBoard ? "Player" : "Enemy")} board");
        
        // Force layout rebuild to position tiles correctly
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
        
        return tiles;
    }

    /// <summary>
    /// Clear all tiles from a board
    /// </summary>
    private void ClearBoard(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Clear both boards
    /// </summary>
    public void ClearAllBoards()
    {
        ClearBoard(playerBoardParent);
        ClearBoard(enemyBoardParent);
        
        playerBoardTiles.Clear();
        enemyBoardTiles.Clear();
        
        Debug.Log("All boards cleared");
    }

    /// <summary>
    /// Get local player ID from network
    /// </summary>
    private int GetLocalPlayerId()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && 
            Unity.Netcode.NetworkManager.Singleton.LocalClient != null)
        {
            return (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        }
        return 0;
    }

    /// <summary>
    /// Get opponent player ID (simplified for 2-player game)
    /// </summary>
    private int GetOpponentPlayerId()
    {
        int localId = GetLocalPlayerId();
        // For 2-player game, opponent is simply the other player
        return localId == 0 ? 1 : 0;
        
        // TODO: For multiplayer with more than 2 players, implement target selection
    }

    /// <summary>
    /// Update board cell size
    /// </summary>
    public void SetCellSize(float size)
    {
        cellSize = size;
        
        if (playerBoardParent != null)
        {
            GridLayoutGroup grid = playerBoardParent.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.cellSize = new Vector2(cellSize, cellSize);
        }
        
        if (enemyBoardParent != null)
        {
            GridLayoutGroup grid = enemyBoardParent.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    /// <summary>
    /// Update board spacing
    /// </summary>
    public void SetSpacing(float gap)
    {
        spacing = gap;
        
        if (playerBoardParent != null)
        {
            GridLayoutGroup grid = playerBoardParent.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.spacing = new Vector2(spacing, spacing);
        }
        
        if (enemyBoardParent != null)
        {
            GridLayoutGroup grid = enemyBoardParent.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.spacing = new Vector2(spacing, spacing);
        }
    }

    /// <summary>
    /// Show enemy board (call this when combat phase starts)
    /// </summary>
    public void ShowEnemyBoard()
    {
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(true);
            Debug.Log("? Enemy board now visible for combat phase");
        }
    }

    /// <summary>
    /// Hide enemy board (call this during ship placement)
    /// </summary>
    public void HideEnemyBoard()
    {
        if (enemyBoardParent != null)
        {
            enemyBoardParent.gameObject.SetActive(false);
            Debug.Log("? Enemy board hidden for ship placement phase");
        }
    }
    
    #region Context Menu Helpers

    [ContextMenu("Generate Boards (Test)")]
    private void GenerateBoardsTest()
    {
        GenerateBoards();
    }

    [ContextMenu("Clear Boards")]
    private void ClearBoardsTest()
    {
        ClearAllBoards();
    }

    [ContextMenu("Validate References")]
    private void ValidateReferences()
    {
        Debug.Log("=== BattleshipsBoardGenerator Reference Validation ===");
        
        bool allGood = true;
        
        if (playerBoardParent == null)
        {
            Debug.LogError("? Player Board Parent is not assigned!");
            allGood = false;
        }
        else
        {
            Debug.Log("? Player Board Parent assigned");
        }
        
        if (enemyBoardParent == null)
        {
            Debug.LogError("? Enemy Board Parent is not assigned!");
            allGood = false;
        }
        else
        {
            Debug.Log("? Enemy Board Parent assigned");
        }
        
        if (tilePrefab == null)
        {
            Debug.LogError("? Tile Prefab is not assigned!");
            allGood = false;
        }
        else
        {
            Debug.Log("? Tile Prefab assigned");
            
            // Check prefab components
            if (tilePrefab.GetComponent<Image>() == null)
            {
                Debug.LogWarning("?? Tile Prefab missing Image component!");
            }
            if (tilePrefab.GetComponent<Button>() == null)
            {
                Debug.LogWarning("?? Tile Prefab missing Button component!");
            }
            if (tilePrefab.GetComponent<BattleshipsTileClickHandler>() == null)
            {
                Debug.LogWarning("?? Tile Prefab missing BattleshipsTileClickHandler component!");
            }
        }
        
        if (allGood)
        {
            Debug.Log("? All references are properly assigned!");
        }
        else
        {
            Debug.LogError("? Some references are missing! Please assign them in the Inspector.");
        }
    }

    #endregion

    /// <summary>
    /// Get current cell size
    /// </summary>
    public float GetCellSize() => cellSize;
    
    /// <summary>
    /// Get player board parent transform
    /// </summary>
    public Transform GetPlayerBoardParent() => playerBoardParent;
    
    /// <summary>
    /// Get enemy board parent transform
    /// </summary>
    public Transform GetEnemyBoardParent() => enemyBoardParent;
    
    /// <summary>
    /// Regenerate boards with new cell size (clears and rebuilds)
    /// </summary>
    public void RegenerateBoardsWithNewSize(float newCellSize)
    {
        cellSize = Mathf.Clamp(newCellSize, 10f, 100f);
        Debug.Log($"?? Regenerating boards with new cell size: {cellSize}px");
        GenerateBoards();
    }
}

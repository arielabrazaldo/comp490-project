using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameSetupManager : MonoBehaviour
{
    private static GameSetupManager instance;
    public static GameSetupManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSetupManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameSetupManager");
                    instance = go.AddComponent<GameSetupManager>();
                }
            }
            return instance;
        }
    }

    [Header("Board Generation")]
    public GameObject tilePrefab;
    public Transform tileParent; // Parent container for tiles
    public Color[] tileColors; // Tile color pattern

    [Header("Player Tokens")]
    public GameObject[] playerTokenPrefabs; // Different colored tokens
    
    // Game configuration (set by UIManager)
    private int configuredTileCount = 20;
    private int configuredPlayerCount = 2;
    
    // Generated game objects
    private List<GameObject> generatedTiles = new List<GameObject>();
    private List<GameObject> playerTokens = new List<GameObject>();
    private int[] playerPositions;
    
    // Game state
    private bool isBoardGenerated = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ValidateReferences();
        
        // Hide the board initially
        if (tileParent != null)
        {
            tileParent.gameObject.SetActive(false);
        }
    }

    private void ValidateReferences()
    {
        if (tilePrefab == null) Debug.LogError("TilePrefab is not assigned in GameSetupManager!");
        if (tileParent == null) Debug.LogError("TileParent is not assigned in GameSetupManager!");
        if (playerTokenPrefabs == null || playerTokenPrefabs.Length == 0) 
            Debug.LogError("PlayerTokenPrefabs are not assigned in GameSetupManager!");
        if (tileColors == null || tileColors.Length == 0) 
            Debug.LogError("TileColors are not assigned in GameSetupManager!");
    }

    /// <summary>
    /// Configure the game settings (called from UIManager)
    /// </summary>
    /// <param name="tileCount">Number of tiles for the board</param>
    /// <param name="playerCount">Number of players</param>
    public void ConfigureGame(int tileCount, int playerCount)
    {
        configuredTileCount = Mathf.Clamp(tileCount, 10, 100);
        configuredPlayerCount = Mathf.Clamp(playerCount, 2, Mathf.Min(4, playerTokenPrefabs.Length));
        
        Debug.Log($"Game configured: {configuredTileCount} tiles, {configuredPlayerCount} players");
        
        // Clear any existing board
        ClearBoard();
    }

    /// <summary>
    /// Generate the board based on configured settings (called when game starts)
    /// </summary>
    public void GenerateBoard()
    {
        if (tilePrefab == null || tileParent == null)
        {
            Debug.LogError("Cannot generate board: Missing tile prefab or parent!");
            return;
        }

        Debug.Log($"Generating board with {configuredTileCount} tiles for {configuredPlayerCount} players");

        // Clear existing board first
        ClearBoard();

        // Show the board container
        tileParent.gameObject.SetActive(true);

        GameObject firstTile = null;

        // Generate tiles
        for (int i = 1; i <= configuredTileCount; i++)
        {
            GameObject tile = Instantiate(tilePrefab, tileParent);
            generatedTiles.Add(tile);

            // Save reference to first tile for player spawn
            if (i == 1)
            {
                firstTile = tile;
            }

            // Set the tile number
            Text numberText = tile.GetComponentInChildren<Text>();
            if (numberText != null)
            {
                numberText.text = i.ToString();
            }
            else
            {
                // Try TextMeshPro as fallback
                TextMeshProUGUI tmpText = tile.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = i.ToString();
                }
            }

            // Set background color
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage != null && tileColors.Length > 0)
            {
                tileImage.color = tileColors[(i - 1) % tileColors.Length];
            }

            Debug.Log($"Created tile #{i}");
        }

        // Spawn player tokens on the first tile
        if (firstTile != null)
        {
            SpawnPlayerTokens(firstTile);
        }

        isBoardGenerated = true;
        Debug.Log("Board generation completed!");
    }

    /// <summary>
    /// Spawn player tokens on the starting tile
    /// </summary>
    /// <param name="startTile">The tile where players start</param>
    private void SpawnPlayerTokens(GameObject startTile)
    {
        if (playerTokenPrefabs.Length < configuredPlayerCount)
        {
            Debug.LogError($"Not enough player token prefabs! Need {configuredPlayerCount}, have {playerTokenPrefabs.Length}");
            return;
        }

        playerPositions = new int[configuredPlayerCount];

        for (int p = 0; p < configuredPlayerCount; p++)
        {
            GameObject token = Instantiate(playerTokenPrefabs[p], startTile.transform);
            playerTokens.Add(token);

            RectTransform rt = token.GetComponent<RectTransform>();
            if (rt != null)
            {
                float spacing = 40f; // Spacing between tokens

                // Calculate grid layout (2x2 for up to 4 players)
                int cols = Mathf.Min(2, configuredPlayerCount);
                int rows = Mathf.CeilToInt(configuredPlayerCount / 2f);

                // Position tokens in a grid inside the tile
                float xOffset = (p % cols - (cols - 1) / 2f) * spacing;
                float yOffset = ((rows - 1) / 2f - p / cols) * spacing;

                rt.anchoredPosition = new Vector2(xOffset, yOffset);
                rt.localScale = Vector3.one * 0.1f; // Scale down for visibility
            }

            playerPositions[p] = 0; // All players start at position 0
            Debug.Log($"Player token #{p + 1} spawned at start position");
        }
    }

    /// <summary>
    /// Clear the generated board and tokens
    /// </summary>
    public void ClearBoard()
    {
        // Destroy existing tiles
        foreach (GameObject tile in generatedTiles)
        {
            if (tile != null) Destroy(tile);
        }
        generatedTiles.Clear();

        // Destroy existing tokens
        foreach (GameObject token in playerTokens)
        {
            if (token != null) Destroy(token);
        }
        playerTokens.Clear();

        // Reset positions array
        playerPositions = null;
        isBoardGenerated = false;

        // Hide the board container
        if (tileParent != null)
        {
            tileParent.gameObject.SetActive(false);
        }

        Debug.Log("Board cleared");
    }

    /// <summary>
    /// Move a player token to a specific tile position (called by NetworkGameManager)
    /// </summary>
    /// <param name="playerId">Player index (0-based)</param>
    /// <param name="tilePosition">Target tile position (1-based)</param>
    public void MovePlayerToken(int playerId, int tilePosition)
    {
        if (!isBoardGenerated)
        {
            Debug.LogError("Cannot move player: Board not generated!");
            return;
        }

        if (playerId < 0 || playerId >= playerTokens.Count)
        {
            Debug.LogError($"Invalid player ID: {playerId}");
            return;
        }

        if (tilePosition < 1 || tilePosition > generatedTiles.Count)
        {
            Debug.LogError($"Invalid tile position: {tilePosition}");
            return;
        }

        GameObject playerToken = playerTokens[playerId];
        GameObject targetTile = generatedTiles[tilePosition - 1]; // Convert to 0-based index

        if (playerToken != null && targetTile != null)
        {
            // Animate the move (simple version - you can enhance this with smoother animation)
            StartCoroutine(AnimatePlayerMove(playerToken, targetTile, playerId, tilePosition));
        }
    }

    /// <summary>
    /// Animate player token movement
    /// </summary>
    private System.Collections.IEnumerator AnimatePlayerMove(GameObject playerToken, GameObject targetTile, int playerId, int tilePosition)
    {
        Vector3 startPosition = playerToken.transform.position;
        
        // Move to target tile
        playerToken.transform.SetParent(targetTile.transform);
        
        Vector3 targetPosition = targetTile.transform.position;
        float animationTime = 0.5f;
        float elapsedTime = 0f;

        // Animate the movement
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationTime;
            
            // Add a slight arc to the movement for visual appeal
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 50f; // Arc height
            
            playerToken.transform.position = currentPos;
            yield return null;
        }

        // Ensure final position is exact
        playerToken.transform.position = targetPosition;
        
        // Reposition tokens on the tile to avoid overlap
        RepositionTokensOnTile(targetTile);
        
        Debug.Log($"Player {playerId + 1} animation complete - now at tile {tilePosition}");
    }

    /// <summary>
    /// Reposition all player tokens on a specific tile to avoid overlap
    /// </summary>
    /// <param name="tile">The tile to reposition tokens on</param>
    private void RepositionTokensOnTile(GameObject tile)
    {
        List<GameObject> tokensOnTile = new List<GameObject>();
        
        // Find all player tokens on this tile
        foreach (Transform child in tile.transform)
        {
            if (playerTokens.Contains(child.gameObject))
            {
                tokensOnTile.Add(child.gameObject);
            }
        }

        // Reposition tokens to avoid overlap
        for (int i = 0; i < tokensOnTile.Count; i++)
        {
            RectTransform rt = tokensOnTile[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                float spacing = 40f;
                int cols = Mathf.Min(2, tokensOnTile.Count);
                
                float xOffset = (i % cols - (cols - 1) / 2f) * spacing;
                float yOffset = ((Mathf.CeilToInt(tokensOnTile.Count / 2f) - 1) / 2f - i / cols) * spacing;

                rt.anchoredPosition = new Vector2(xOffset, yOffset);
            }
        }
    }

    /// <summary>
    /// Get the current game configuration
    /// </summary>
    /// <returns>Tuple containing (tileCount, playerCount)</returns>
    public (int tileCount, int playerCount) GetGameConfiguration()
    {
        return (configuredTileCount, configuredPlayerCount);
    }

    /// <summary>
    /// Get player positions array
    /// </summary>
    /// <returns>Array of player positions (1-based tile numbers)</returns>
    public int[] GetPlayerPositions()
    {
        return playerPositions?.Clone() as int[];
    }

    /// <summary>
    /// Check if the board is currently generated
    /// </summary>
    /// <returns>True if board is generated and ready</returns>
    public bool IsBoardGenerated()
    {
        return isBoardGenerated;
    }

    private void OnDestroy()
    {
        ClearBoard();
    }
}
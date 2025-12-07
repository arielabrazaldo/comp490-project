using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class MonopolyBoardManager : MonoBehaviour
{
    private static MonopolyBoardManager instance;
    public static MonopolyBoardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MonopolyBoardManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MonopolyBoardManager");
                    instance = go.AddComponent<MonopolyBoardManager>();
                }
            }
            return instance;
        }
    }

    [Header("Board Generation")]
    public GameObject propertyTilePrefab;
    public GameObject specialTilePrefab;
    public Transform boardParent;
    public Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta, Color.white, Color.black };

    [Header("Player Tokens")]
    public GameObject[] playerTokenPrefabs;
    
    [Header("Building Indicators")]
    [SerializeField] private Color houseColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green for houses
    [SerializeField] private Color hotelColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red for hotels
    [SerializeField] private float buildingIndicatorSize = 10f; // Size of house/hotel indicators
    
    [Header("Configuration")]
    [SerializeField] private int maxSupportedPlayers = 8; // Maximum players the board can support
    
    // Generated game objects
    private List<GameObject> generatedTiles = new List<GameObject>();
    private List<GameObject> playerTokens = new List<GameObject>();
    private List<MonopolySpace> boardData;
    
    // Building indicators - Dictionary maps property ID to list of building GameObjects
    private Dictionary<int, List<GameObject>> buildingIndicators = new Dictionary<int, List<GameObject>>();
    
    // Game state
    private bool isBoardGenerated = false;
    private int configuredPlayerCount = 2;

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
        if (boardParent != null)
        {
            boardParent.gameObject.SetActive(false);
        }
        
        // Subscribe to building events
        MonopolyGameManager.OnHousePurchased += OnHousePurchased;
        MonopolyGameManager.OnHotelPurchased += OnHotelPurchased;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from building events
        MonopolyGameManager.OnHousePurchased -= OnHousePurchased;
        MonopolyGameManager.OnHotelPurchased -= OnHotelPurchased;
        
        ClearBoard();
    }

    private void ValidateReferences()
    {
        if (propertyTilePrefab == null) Debug.LogError("PropertyTilePrefab is not assigned in MonopolyBoardManager!");
        if (specialTilePrefab == null) Debug.LogError("SpecialTilePrefab is not assigned in MonopolyBoardManager!");
        if (boardParent == null) Debug.LogError("BoardParent is not assigned in MonopolyBoardManager!");
        if (playerTokenPrefabs == null || playerTokenPrefabs.Length == 0) 
            Debug.LogError("PlayerTokenPrefabs are not assigned in MonopolyBoardManager!");
    }

    /// <summary>
    /// Configure the Monopoly game settings
    /// </summary>
    public void ConfigureGame(int playerCount)
    {
        // Clamp to available tokens or max supported
        int maxPossible = Mathf.Min(maxSupportedPlayers, playerTokenPrefabs != null ? playerTokenPrefabs.Length : 2);
        configuredPlayerCount = Mathf.Clamp(playerCount, 2, maxPossible);
        
        if (configuredPlayerCount != playerCount)
        {
            Debug.LogWarning($"Player count adjusted from {playerCount} to {configuredPlayerCount} (available tokens: {playerTokenPrefabs?.Length ?? 0}, max: {maxSupportedPlayers})");
        }
        
        Debug.Log($"Monopoly game configured: {configuredPlayerCount} players");
        
        // Clear any existing board
        ClearBoard();
    }

    /// <summary>
    /// Generate the Monopoly board
    /// </summary>
    public void GenerateBoard()
    {
        if (propertyTilePrefab == null || specialTilePrefab == null || boardParent == null)
        {
            Debug.LogError("Cannot generate Monopoly board: Missing prefabs or parent!");
            return;
        }

        Debug.Log($"Generating Monopoly board for {configuredPlayerCount} players");

        // Clear existing board first
        ClearBoard();

        // Get board data
        boardData = MonopolyBoard.CreateStandardBoard();

        // Show the board container
        boardParent.gameObject.SetActive(true);

        // Generate tiles in a square layout
        GenerateBoardTiles();

        // Spawn player tokens on GO
        if (generatedTiles.Count > 0)
        {
            SpawnPlayerTokens(generatedTiles[0]); // GO is at index 0
        }

        isBoardGenerated = true;
        Debug.Log("Monopoly board generation completed!");
    }

    private void GenerateBoardTiles()
    {
        // Monopoly board is 40 tiles arranged in a square perimeter
        // 10 tiles per side (including corners)
        
        float tileSize = 70f; // Slightly smaller tiles for better fit
        float boardSize = tileSize * 10; // Perfect square with 10 segments per side
        
        // Ensure the board parent is properly sized
        RectTransform boardRect = boardParent.GetComponent<RectTransform>();
        if (boardRect != null)
        {
            boardRect.sizeDelta = new Vector2(boardSize + tileSize * 2, boardSize + tileSize * 2); // Add padding
            
            // Disable layout components that might cause rendering issues
            var layoutGroup = boardParent.GetComponent<LayoutGroup>();
            if (layoutGroup != null) layoutGroup.enabled = false;
            
            var contentSizeFitter = boardParent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null) contentSizeFitter.enabled = false;
        }
        
        for (int i = 0; i < boardData.Count; i++)
        {
            var spaceData = boardData[i];
            Vector2 position = CalculateTilePosition(i, boardSize, tileSize);
            
            // Choose prefab based on tile type
            GameObject tilePrefab = IsSpecialTile(spaceData.type) ? specialTilePrefab : propertyTilePrefab;
            
            GameObject tile = Instantiate(tilePrefab, boardParent);
            generatedTiles.Add(tile);
            
            // Position the tile
            RectTransform rt = tile.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = position;
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                
                // Disable any layout that might interfere
                var tileLayout = tile.GetComponent<LayoutGroup>();
                if (tileLayout != null) tileLayout.enabled = false;
            }
            
            // Set tile data
            SetupTile(tile, spaceData, i);
        }
        
        Debug.Log($"Generated {generatedTiles.Count} Monopoly tiles in a perfect square layout");
    }

    private Vector2 CalculateTilePosition(int index, float boardSize, float tileSize)
    {
        // Monopoly board has 40 spaces arranged around the perimeter
        // Layout: 11 tiles per side (corners are shared between adjacent sides)
        // Bottom: indices 0-10 (GO=0, Jail=10)
        // Left: indices 10-20 (Jail=10, Free Parking=20)  
        // Top: indices 20-30 (Free Parking=20, Go To Jail=30)
        // Right: indices 30-39 (Go To Jail=30, wraps back to GO=0)
        
        float halfBoard = boardSize / 2f;
        
        // Calculate which side and position on that side
        if (index <= 10) // Bottom side (GO to Jail) - Right to Left
        {
            float x = halfBoard - (index * tileSize);
            return new Vector2(x, -halfBoard);
        }
        else if (index <= 20) // Left side (Jail to Free Parking) - Bottom to Top
        {
            int sidePosition = index - 10;
            float y = -halfBoard + (sidePosition * tileSize);
            return new Vector2(-halfBoard, y);
        }
        else if (index <= 30) // Top side (Free Parking to Go To Jail) - Left to Right
        {
            int sidePosition = index - 20;
            float x = -halfBoard + (sidePosition * tileSize);
            return new Vector2(x, halfBoard);
        }
        else // Right side (Go To Jail back toward GO) - Top to Bottom
        {
            int sidePosition = index - 30;
            float y = halfBoard - (sidePosition * tileSize);
            return new Vector2(halfBoard, y);
        }
    }

    private bool IsSpecialTile(PropertyType type)
    {
        return type == PropertyType.Go || 
               type == PropertyType.Jail || 
               type == PropertyType.FreeParking || 
               type == PropertyType.GoToJail ||
               type == PropertyType.Tax ||
               type == PropertyType.Chance ||
               type == PropertyType.CommunityChest;
    }

    private void SetupTile(GameObject tile, MonopolySpace spaceData, int index)
    {
        // Set the tile name text
        TextMeshProUGUI nameText = tile.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = spaceData.spaceName;
            nameText.fontSize = 8f; // Small font for tile names
            nameText.color = Color.black; // Ensure text is visible
        }

        // Set background color based on property group
        Image tileImage = tile.GetComponent<Image>();
        if (tileImage != null)
        {
            tileImage.color = spaceData.spaceColor;
            tileImage.raycastTarget = true; // Enable clicks
            
            // If the tile color is white/default, make it transparent or use a default color
            if (spaceData.spaceColor == Color.white || spaceData.spaceColor.a < 0.1f)
            {
                // Use a light color for non-property tiles instead of white
                tileImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray
            }
        }
        
        // Find all child images and disable ones that might be causing white boxes
        Image[] childImages = tile.GetComponentsInChildren<Image>();
        foreach (var img in childImages)
        {
            // Skip the main tile image
            if (img.gameObject == tile) continue;
            
            // Disable raycast on child images (only main tile should be clickable)
            img.raycastTarget = false;
            
            // If it's a white placeholder image, make it transparent
            if (img.color == Color.white && img.sprite == null)
            {
                img.color = new Color(1f, 1f, 1f, 0f); // Transparent
            }
        }

        // Add property price for purchasable properties
        if (spaceData.type == PropertyType.Property || 
            spaceData.type == PropertyType.Railroad || 
            spaceData.type == PropertyType.Utility)
        {
            // Try to find a price text component
            var textComponents = tile.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length > 1)
            {
                textComponents[1].text = $"${spaceData.price}";
                textComponents[1].fontSize = 6f;
                textComponents[1].color = Color.black;
            }
        }

        // Store space data reference
        var tileData = tile.GetComponent<MonopolyTileData>();
        if (tileData == null)
        {
            tileData = tile.AddComponent<MonopolyTileData>();
        }
        tileData.spaceData = spaceData;
        tileData.spaceIndex = index;

        // Add click handler for interactive tiles
        var clickHandler = tile.GetComponent<MonopolyTileClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = tile.AddComponent<MonopolyTileClickHandler>();
        }

        Debug.Log($"Created Monopoly tile #{index}: {spaceData.spaceName} (Color: {spaceData.spaceColor})");
    }

    /// <summary>
    /// Spawn player tokens on the starting tile (GO)
    /// </summary>
    private void SpawnPlayerTokens(GameObject startTile)
    {
        if (playerTokenPrefabs == null || playerTokenPrefabs.Length == 0)
        {
            Debug.LogError("No player token prefabs assigned!");
            return;
        }
        
        int availableTokens = playerTokenPrefabs.Length;
        int tokensToSpawn = Mathf.Min(configuredPlayerCount, availableTokens);
        
        if (tokensToSpawn < configuredPlayerCount)
        {
            Debug.LogWarning($"Not enough player token prefabs! Need {configuredPlayerCount}, have {availableTokens}. Will spawn {tokensToSpawn} tokens.");
        }

        for (int p = 0; p < tokensToSpawn; p++)
        {
            GameObject tokenPrefab = playerTokenPrefabs[p % availableTokens]; // Reuse tokens if needed
            GameObject token = Instantiate(tokenPrefab, startTile.transform);
            playerTokens.Add(token);

            RectTransform rt = token.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Dynamic grid calculation based on player count
                float spacing = configuredPlayerCount <= 4 ? 15f : 12f; // Tighter spacing for more players

                // Calculate grid layout for tokens on tile
                int cols = configuredPlayerCount <= 4 ? 2 : Mathf.Min(3, configuredPlayerCount);
                int rows = Mathf.CeilToInt((float)configuredPlayerCount / cols);

                // Position tokens in a grid inside the tile
                float xOffset = (p % cols - (cols - 1) / 2f) * spacing;
                float yOffset = ((rows - 1) / 2f - p / cols) * spacing;

                rt.anchoredPosition = new Vector2(xOffset, yOffset);
                rt.localScale = Vector3.one * (configuredPlayerCount <= 4 ? 0.3f : 0.25f); // Smaller for more players

                // Set player color
                Image tokenImage = token.GetComponent<Image>();
                if (tokenImage != null && p < playerColors.Length)
                {
                    tokenImage.color = playerColors[p];
                }
            }

            Debug.Log($"Monopoly player token #{p + 1} spawned at GO");
        }
        
        Debug.Log($"Spawned {playerTokens.Count} player tokens for {configuredPlayerCount} players");
    }

    /// <summary>
    /// Move a player token to a specific space
    /// </summary>
    public void MovePlayerToken(int playerId, int spacePosition)
    {
        if (!isBoardGenerated)
        {
            Debug.LogError("Cannot move player: Monopoly board not generated!");
            return;
        }

        if (playerId < 0 || playerId >= playerTokens.Count)
        {
            Debug.LogError($"Invalid player ID: {playerId}");
            return;
        }

        if (spacePosition < 0 || spacePosition >= generatedTiles.Count)
        {
            Debug.LogError($"Invalid space position: {spacePosition}");
            return;
        }

        GameObject playerToken = playerTokens[playerId];
        GameObject targetTile = generatedTiles[spacePosition];

        if (playerToken != null && targetTile != null)
        {
            // Animate the move
            StartCoroutine(AnimatePlayerMove(playerToken, targetTile, playerId, spacePosition));
        }
    }

    /// <summary>
    /// Animate player token movement
    /// </summary>
    private IEnumerator AnimatePlayerMove(GameObject playerToken, GameObject targetTile, int playerId, int spacePosition)
    {
        if (playerToken == null || targetTile == null)
        {
            Debug.LogError($"Cannot animate move: playerToken or targetTile is null!");
            yield break;
        }
        
        Vector3 startPosition = playerToken.transform.position;
        Vector3 targetPosition = targetTile.transform.position;
        
        // Check if this token has a NetworkObject component (regardless of spawn state)
        var networkObject = playerToken.GetComponent<Unity.Netcode.NetworkObject>();
        
        // ?? CRITICAL FIX: Never reparent tokens with NetworkObject components
        // NetworkObjects can only be reparented after being spawned on the network
        // Since these are UI-only tokens, we just animate position without reparenting
        if (networkObject != null)
        {
            Debug.Log($"?? Animating NetworkObject token {playerId} without reparenting (IsSpawned={networkObject.IsSpawned})");
        }
        else
        {
            // ? Regular GameObject without NetworkObject - safe to reparent
            Debug.Log($"?? Animating regular token {playerId} with reparenting");
            playerToken.transform.SetParent(targetTile.transform, false);
        }
        
        // Animate movement for both cases
        float animationTime = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < animationTime)
        {
            if (playerToken == null) yield break;
            
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationTime;
            
            // Add a slight arc to the movement
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 30f; // Arc height
            
            playerToken.transform.position = currentPos;
            yield return null;
        }

        // Set final position
        if (playerToken != null)
        {
            playerToken.transform.position = targetPosition;
            
            // Only reposition tokens on tile if we actually reparented (non-NetworkObject tokens)
            if (networkObject == null)
            {
                RepositionTokensOnTile(targetTile);
            }
        }
        
        var spaceName = boardData != null && spacePosition < boardData.Count ? boardData[spacePosition].spaceName : "Unknown";
        Debug.Log($"? Player {playerId + 1} moved to {spaceName}");
    }

    /// <summary>
    /// Reposition all player tokens on a specific tile to avoid overlap
    /// </summary>
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
                float spacing = 15f;
                int cols = Mathf.Min(2, tokensOnTile.Count);
                
                float xOffset = (i % cols - (cols - 1) / 2f) * spacing;
                float yOffset = ((Mathf.CeilToInt(tokensOnTile.Count / 2f) - 1) / 2f - i / cols) * spacing;

                rt.anchoredPosition = new Vector2(xOffset, yOffset);
            }
        }
    }

    #region Building Indicators (NEW)

    /// <summary>
    /// Event handler for house purchases
    /// </summary>
    private void OnHousePurchased(int playerId, int propertyId, int houseCount)
    {
        Debug.Log($"OnHousePurchased: Player {playerId}, Property {propertyId}, Houses {houseCount}");
        UpdateBuildingIndicators(propertyId, houseCount, false);
    }

    /// <summary>
    /// Event handler for hotel purchases
    /// </summary>
    private void OnHotelPurchased(int playerId, int propertyId)
    {
        Debug.Log($"OnHotelPurchased: Player {playerId}, Property {propertyId}");
        UpdateBuildingIndicators(propertyId, 0, true);
    }
    
    /// <summary>
    /// PUBLIC: Update property buildings from external callers (e.g., MonopolyUI)
    /// </summary>
    public void UpdatePropertyBuildings(int propertyId, int houseCount, bool hasHotel)
    {
        Debug.Log($"?? UpdatePropertyBuildings called: Property {propertyId}, Houses {houseCount}, Hotel {hasHotel}");
        UpdateBuildingIndicators(propertyId, houseCount, hasHotel);
    }

    /// <summary>
    /// Update building indicators (houses/hotel) on a property tile
    /// </summary>
    private void UpdateBuildingIndicators(int propertyId, int houseCount, bool hasHotel)
    {
        if (propertyId < 0 || propertyId >= generatedTiles.Count)
        {
            Debug.LogError($"Invalid property ID: {propertyId}");
            return;
        }

        GameObject propertyTile = generatedTiles[propertyId];
        if (propertyTile == null)
        {
            Debug.LogError($"Property tile {propertyId} is null!");
            return;
        }

        // Clear existing building indicators for this property
        if (buildingIndicators.ContainsKey(propertyId))
        {
            foreach (var indicator in buildingIndicators[propertyId])
            {
                if (indicator != null) Destroy(indicator);
            }
            buildingIndicators[propertyId].Clear();
        }
        else
        {
            buildingIndicators[propertyId] = new List<GameObject>();
        }

        // Create new indicators
        if (hasHotel)
        {
            // Create a single hotel indicator (larger, red)
            GameObject hotelIndicator = CreateBuildingIndicator(propertyTile, hotelColor, buildingIndicatorSize * 1.5f);
            buildingIndicators[propertyId].Add(hotelIndicator);
            
            // Position hotel in the center-top of the tile
            RectTransform rt = hotelIndicator.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0, 25); // Center-top
            }
            
            Debug.Log($"?? Created hotel indicator on property {propertyId}");
        }
        else if (houseCount > 0)
        {
            // Create house indicators (small, green)
            for (int i = 0; i < houseCount; i++)
            {
                GameObject houseIndicator = CreateBuildingIndicator(propertyTile, houseColor, buildingIndicatorSize);
                buildingIndicators[propertyId].Add(houseIndicator);
                
                // Position houses in a row at the top of the tile
                RectTransform rt = houseIndicator.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float spacing = buildingIndicatorSize + 2f;
                    float startX = -(spacing * (houseCount - 1)) / 2f; // Center the row
                    rt.anchoredPosition = new Vector2(startX + (i * spacing), 25);
                }
            }
            
            Debug.Log($"?? Created {houseCount} house indicators on property {propertyId}");
        }
    }

    /// <summary>
    /// Create a single building indicator (house or hotel)
    /// </summary>
    private GameObject CreateBuildingIndicator(GameObject parentTile, Color color, float size)
    {
        GameObject indicator = new GameObject("BuildingIndicator");
        indicator.transform.SetParent(parentTile.transform, false);
        
        // Add Image component
        Image image = indicator.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false; // Don't block clicks
        
        // Set size
        RectTransform rt = indicator.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        
        return indicator;
    }

    #endregion

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
        
        // Clear building indicators
        foreach (var kvp in buildingIndicators)
        {
            foreach (var indicator in kvp.Value)
            {
                if (indicator != null) Destroy(indicator);
            }
        }
        buildingIndicators.Clear();

        isBoardGenerated = false;

        // Hide the board container
        if (boardParent != null)
        {
            boardParent.gameObject.SetActive(false);
        }

        Debug.Log("Monopoly board cleared");
    }

    /// <summary>
    /// Get the current game configuration
    /// </summary>
    /// <returns>Tuple containing (playerCount, tileCount)</returns>
    public (int playerCount, int tileCount) GetGameConfiguration()
    {
        return (configuredPlayerCount, 40); // Monopoly always has 40 spaces
    }

    /// <summary>
    /// Check if the board is currently generated
    /// </summary>
    public bool IsBoardGenerated()
    {
        return isBoardGenerated;
    }

    /// <summary>
    /// Set the number of players
    /// </summary>
    public void SetNumberOfPlayers(int count)
    {
        configuredPlayerCount = Mathf.Clamp(count, 2, playerTokenPrefabs.Length);
    }

    /// <summary>
    /// Get player token GameObjects
    /// </summary>
    public List<GameObject> GetPlayerTokens()
    {
        return new List<GameObject>(playerTokens);
    }
}

/// <summary>
/// Component to store Monopoly tile data
/// </summary>
public class MonopolyTileData : MonoBehaviour
{
    public MonopolySpace spaceData;
    public int spaceIndex;
}
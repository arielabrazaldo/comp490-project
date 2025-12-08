using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the Battleships game setup panel
/// </summary>
public class BattleshipsSetupManager : MonoBehaviour
{
    private static BattleshipsSetupManager instance;
    public static BattleshipsSetupManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BattleshipsSetupManager>();
            }
            return instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject battleshipsGameSetupPanel;
    [SerializeField] private TMP_InputField battleshipsPlayerCountInput;
    [SerializeField] private TMP_InputField battleshipsMaxRowsInput;
    [SerializeField] private TMP_InputField battleshipsMaxColumnsInput;
    [SerializeField] private Button createBattleshipsLobbyButton;
    [SerializeField] private Button backFromBattleshipsSetupButton;
    [SerializeField] private Button customizeButton;
    
    [Header("Grid Customization")]
    [SerializeField] private GameObject gridContainer;
    [SerializeField] private GameObject gridTilePrefab;
    [SerializeField] private GameObject inputFieldsContainer;
    [SerializeField] private Button backFromCustomizeButton;

    [Header("Configuration")]
    private int battleshipsPlayerCount = 2;
    private int battleshipsMaxRows = 10;
    private int battleshipsMaxColumns = 10;
    
    // Grid state
    private List<GameObject> gridTiles = new List<GameObject>();
    private bool[,] tileStates; // true = enabled, false = disabled
    private bool isCustomizing = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetupButtonListeners();
        ValidateReferences();
        
        // Hide grid container initially
        if (gridContainer != null)
        {
            gridContainer.SetActive(false);
        }
    }

    private void SetupButtonListeners()
    {
        if (createBattleshipsLobbyButton != null)
        {
            createBattleshipsLobbyButton.onClick.AddListener(OnCreateBattleshipsLobbyButtonClicked);
        }

        if (backFromBattleshipsSetupButton != null)
        {
            backFromBattleshipsSetupButton.onClick.AddListener(OnBackFromBattleshipsSetupClicked);
        }
        
        if (customizeButton != null)
        {
            customizeButton.onClick.AddListener(OnCustomizeButtonClicked);
        }
        
        if (backFromCustomizeButton != null)
        {
            backFromCustomizeButton.onClick.AddListener(OnBackFromCustomizeClicked);
        }
    }

    private void ValidateReferences()
    {
        if (battleshipsGameSetupPanel == null) Debug.LogError("BattleshipsGameSetupPanel is not assigned!");
        if (battleshipsPlayerCountInput == null) Debug.LogError("BattleshipsPlayerCountInput is not assigned!");
        if (battleshipsMaxRowsInput == null) Debug.LogError("BattleshipsMaxRowsInput is not assigned!");
        if (battleshipsMaxColumnsInput == null) Debug.LogError("BattleshipsMaxColumnsInput is not assigned!");
        if (createBattleshipsLobbyButton == null) Debug.LogError("CreateBattleshipsLobbyButton is not assigned!");
        if (backFromBattleshipsSetupButton == null) Debug.LogError("BackFromBattleshipsSetupButton is not assigned!");
        if (customizeButton == null) Debug.LogError("CustomizeButton is not assigned!");
        if (gridContainer == null) Debug.LogError("GridContainer is not assigned!");
        if (gridTilePrefab == null) Debug.LogError("GridTilePrefab is not assigned!");
        if (inputFieldsContainer == null) Debug.LogError("InputFieldsContainer is not assigned!");
        if (backFromCustomizeButton == null) Debug.LogError("BackFromCustomizeButton is not assigned!");
    }

    /// <summary>
    /// Shows the Battleships game setup panel
    /// </summary>
    public void ShowBattleshipsGameSetup()
    {
        if (battleshipsGameSetupPanel != null)
        {
            battleshipsGameSetupPanel.SetActive(true);
            Debug.Log("Showing Battleships Game Setup Panel");

            // Set default values for Battleships configuration
            if (battleshipsPlayerCountInput != null) battleshipsPlayerCountInput.text = "2";
            if (battleshipsMaxRowsInput != null) battleshipsMaxRowsInput.text = "10";
            if (battleshipsMaxColumnsInput != null) battleshipsMaxColumnsInput.text = "10";
            
            // Show input fields, hide grid
            ShowInputFields();
        }
        else
        {
            Debug.LogError("BattleshipsGameSetupPanel is NULL! Please assign it in the Inspector.");
        }
    }

    /// <summary>
    /// Hides the Battleships game setup panel
    /// </summary>
    public void HideBattleshipsGameSetup()
    {
        if (battleshipsGameSetupPanel != null)
        {
            battleshipsGameSetupPanel.SetActive(false);
        }
    }
    
    private void OnCustomizeButtonClicked()
    {
        Debug.Log("Customize button clicked");
        
        // Parse grid dimensions from input fields
        if (int.TryParse(battleshipsMaxRowsInput.text, out int rows))
        {
            battleshipsMaxRows = Mathf.Clamp(rows, 5, 20);
        }
        else
        {
            battleshipsMaxRows = 10;
        }

        if (int.TryParse(battleshipsMaxColumnsInput.text, out int cols))
        {
            battleshipsMaxColumns = Mathf.Clamp(cols, 5, 20);
        }
        else
        {
            battleshipsMaxColumns = 10;
        }
        
        // Generate the grid
        GenerateCustomizableGrid(battleshipsMaxRows, battleshipsMaxColumns);
        
        // Hide input fields, show grid
        ShowGrid();
        
        isCustomizing = true;
    }
    
    private void OnBackFromCustomizeClicked()
    {
        Debug.Log("Back from customize clicked");
        
        // Clear the grid
        ClearGrid();
        
        // Show input fields, hide grid
        ShowInputFields();
        
        isCustomizing = false;
    }
    
    private void GenerateCustomizableGrid(int rows, int cols)
    {
        Debug.Log($"=== GenerateCustomizableGrid called: {rows}x{cols} ===");
        
        if (gridContainer == null)
        {
            Debug.LogError("? GridContainer is NULL!");
            return;
        }
        
        if (gridTilePrefab == null)
        {
            Debug.LogError("? GridTilePrefab is NULL!");
            return;
        }
        
        Debug.Log($"? GridContainer assigned: {gridContainer.name}");
        Debug.Log($"? GridTilePrefab assigned: {gridTilePrefab.name}");
        
        // CHECK: Is GridContainer active?
        if (!gridContainer.activeInHierarchy)
        {
            Debug.LogWarning("?? GridContainer is NOT ACTIVE in hierarchy! Making it active...");
            gridContainer.SetActive(true);
        }
        else
        {
            Debug.Log("? GridContainer is active");
        }
        
        // Clear any existing grid
        ClearGrid();
        
        // Initialize tile states array (all enabled by default)
        tileStates = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                tileStates[r, c] = true;
            }
        }
        
        // Setup GridLayoutGroup
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
            Debug.Log("? Added GridLayoutGroup component");
        }
        else
        {
            Debug.Log("? GridLayoutGroup already exists");
        }
        
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = cols;
        
        // Calculate cell size based on container size
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("? GridContainer missing RectTransform!");
            return;
        }
        
        // LOG: Container size
        Debug.Log($"?? GridContainer Size: {containerRect.rect.width} x {containerRect.rect.height}");
        
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;
        
        if (containerWidth <= 0 || containerHeight <= 0)
        {
            Debug.LogError($"? GridContainer has ZERO or negative size! Width={containerWidth}, Height={containerHeight}");
            Debug.LogError("This is why tiles aren't visible! Fix GridContainer RectTransform size.");
            return;
        }
        
        float cellWidth = (containerRect.rect.width - (gridLayout.spacing.x * (cols - 1))) / cols;
        float cellHeight = (containerRect.rect.height - (gridLayout.spacing.y * (rows - 1))) / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight, 50f); // Cap at 50px
        
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(2, 2);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        
        Debug.Log($"? Cell size calculated: {cellSize}px (from container {containerWidth}x{containerHeight})");
        Debug.Log($"? Grid layout configured: {cols} columns, {cellSize}x{cellSize} cells");
        
        // Create grid tiles
        int tilesCreated = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject tile = Instantiate(gridTilePrefab, gridContainer.transform);
                
                if (tile == null)
                {
                    Debug.LogError($"? Failed to instantiate tile at [{row},{col}]!");
                    continue;
                }
                
                // CRITICAL FIX: Reset local position to zero immediately
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                if (tileRect != null)
                {
                    tileRect.localPosition = Vector3.zero;
                    tileRect.localRotation = Quaternion.identity;
                    tileRect.localScale = Vector3.one;
                }
                
                // Force tile to be active
                if (!tile.activeSelf)
                {
                    tile.SetActive(true);
                }
                
                // Store row and col in the tile's name for reference
                tile.name = $"Tile_{row}_{col}";
                
                // LOG: First tile details for debugging
                if (tilesCreated == 0)
                {
                    Debug.Log($"?? First Tile Info (AFTER FIX):");
                    Debug.Log($"  - Name: {tile.name}");
                    Debug.Log($"  - Active: {tile.activeSelf}");
                    Debug.Log($"  - Parent: {tile.transform.parent.name}");
                    if (tileRect != null)
                    {
                        Debug.Log($"  - Size: {tileRect.rect.width} x {tileRect.rect.height}");
                        Debug.Log($"  - Local Position: {tileRect.localPosition}");
                        Debug.Log($"  - World Position: {tileRect.position}");
                    }
                    
                    // Check components
                    Image img = tile.GetComponent<Image>();
                    Button btn = tile.GetComponent<Button>();
                    Debug.Log($"  - Has Image: {img != null}");
                    Debug.Log($"  - Has Button: {btn != null}");
                    if (img != null)
                    {
                        Debug.Log($"  - Image Color: {img.color}");
                        Debug.Log($"  - Raycast Target: {img.raycastTarget}");
                    }
                }
                
                // Get or add button component
                Button tileButton = tile.GetComponent<Button>();
                if (tileButton == null)
                {
                    Debug.LogWarning($"?? Tile prefab missing Button component! Adding one at [{row},{col}]");
                    tileButton = tile.AddComponent<Button>();
                }
                
                // Verify Image component and raycast target
                Image tileImage = tile.GetComponent<Image>();
                if (tileImage == null)
                {
                    Debug.LogError($"? Tile prefab missing Image component at [{row},{col}]!");
                }
                else if (!tileImage.raycastTarget)
                {
                    Debug.LogWarning($"?? Tile Image raycastTarget is FALSE at [{row},{col}] - enabling it");
                    tileImage.raycastTarget = true;
                }
                
                // Setup button colors
                ColorBlock colors = tileButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.green;
                colors.pressedColor = Color.yellow;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                tileButton.colors = colors;
                
                // Capture row and col in closure for click handler
                int capturedRow = row;
                int capturedCol = col;
                
                // Add click listener
                tileButton.onClick.AddListener(() => OnTileClicked(capturedRow, capturedCol));
                
                // Set initial visual state
                UpdateTileVisual(tile, true);
                
                gridTiles.Add(tile);
                tilesCreated++;
            }
        }
        
        Debug.Log($"??? Grid generation complete! Created {tilesCreated} tiles (expected {rows * cols}) ???");
        
        if (tilesCreated != rows * cols)
        {
            Debug.LogError($"? Tile count mismatch! Created {tilesCreated} but expected {rows * cols}");
        }
        
        // Final check: Are tiles actually in the hierarchy?
        Debug.Log($"?? GridContainer child count: {gridContainer.transform.childCount}");
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        Debug.Log("? Forced layout rebuild");
    }
    
    private void OnTileClicked(int row, int col)
    {
        // Toggle tile state
        tileStates[row, col] = !tileStates[row, col];
        
        // Find the tile GameObject
        GameObject tile = gridTiles.Find(t => t.name == $"Tile_{row}_{col}");
        
        if (tile != null)
        {
            UpdateTileVisual(tile, tileStates[row, col]);
            Debug.Log($"Tile [{row},{col}] toggled to {(tileStates[row, col] ? "ENABLED" : "DISABLED")}</color>");
        }
    }
    
    private void UpdateTileVisual(GameObject tile, bool isEnabled)
    {
        // Update the tile's image color
        Image tileImage = tile.GetComponent<Image>();
        if (tileImage != null)
        {
            // Active tiles: Sea water blue (ocean color)
            // Inactive tiles: Brown (like sand/earth)
            tileImage.color = isEnabled 
                ? new Color(0.0f, 0.4f, 0.7f, 1.0f)  // Deep sea blue
                : new Color(0.55f, 0.35f, 0.2f, 1.0f); // Brown
        }
        
        // Optionally add a text indicator
        TextMeshProUGUI tileText = tile.GetComponentInChildren<TextMeshProUGUI>();
        if (tileText != null)
        {
            tileText.text = isEnabled ? "??" : "?";
            tileText.color = isEnabled ? Color.white : Color.red;
        }
    }
    
    private void ClearGrid()
    {
        foreach (GameObject tile in gridTiles)
        {
            Destroy(tile);
        }
        
        gridTiles.Clear();
        tileStates = null;
    }
    
    private void ShowInputFields()
    {
        if (inputFieldsContainer != null)
        {
            inputFieldsContainer.SetActive(true);
        }
        
        if (gridContainer != null)
        {
            gridContainer.SetActive(false);
        }
        
        if (customizeButton != null)
        {
            customizeButton.gameObject.SetActive(true);
        }
        
        // Back button should be outside GridContainer and controlled separately
        if (backFromCustomizeButton != null)
        {
            backFromCustomizeButton.gameObject.SetActive(false);
        }
        
        if (createBattleshipsLobbyButton != null)
        {
            createBattleshipsLobbyButton.gameObject.SetActive(true);
        }
    }
    
    private void ShowGrid()
    {
        if (inputFieldsContainer != null)
        {
            inputFieldsContainer.SetActive(false);
        }
        
        if (gridContainer != null)
        {
            gridContainer.SetActive(true);
        }
        
        if (customizeButton != null)
        {
            customizeButton.gameObject.SetActive(false);
        }
        
        // Back button should be outside GridContainer and controlled separately
        if (backFromCustomizeButton != null)
        {
            // Make sure it's active and positioned correctly
            backFromCustomizeButton.gameObject.SetActive(true);
            
            // Ensure it's not affected by GridLayoutGroup
            LayoutElement layoutElement = backFromCustomizeButton.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = backFromCustomizeButton.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true; // This will prevent layout groups from affecting it
        }
        
        if (createBattleshipsLobbyButton != null)
        {
            createBattleshipsLobbyButton.gameObject.SetActive(true);
        }
    }

    private async void OnCreateBattleshipsLobbyButtonClicked()
    {
        Debug.Log("Create Battleships lobby button clicked");

        // Parse player count
        if (int.TryParse(battleshipsPlayerCountInput.text, out int playerCount))
        {
            battleshipsPlayerCount = Mathf.Clamp(playerCount, 2, 4);
        }
        else
        {
            battleshipsPlayerCount = 2;
        }

        // If not customizing, parse dimensions from inputs
        if (!isCustomizing)
        {
            if (int.TryParse(battleshipsMaxRowsInput.text, out int maxRows))
            {
                battleshipsMaxRows = Mathf.Clamp(maxRows, 5, 20);
            }
            else
            {
                battleshipsMaxRows = 10;
            }

            if (int.TryParse(battleshipsMaxColumnsInput.text, out int maxColumns))
            {
                battleshipsMaxColumns = Mathf.Clamp(maxColumns, 5, 20);
            }
            else
            {
                battleshipsMaxColumns = 10;
            }
        }

        if (createBattleshipsLobbyButton != null) createBattleshipsLobbyButton.interactable = false;

        // Notify UIManager to show status
        if (UIManager.Instance != null)
        {
            string customInfo = isCustomizing ? " (Custom Layout)" : "";
            UIManager.Instance.SetStatusInfoPublic($"Creating Battleships lobby for {battleshipsPlayerCount} players with {battleshipsMaxRows}x{battleshipsMaxColumns} grid{customInfo}...");
        }

        try
        {
            string lobbyCode = await LobbyManager.Instance.CreateLobby($"Battleships - {battleshipsMaxRows}x{battleshipsMaxColumns}");
            if (!string.IsNullOrEmpty(lobbyCode))
            {
                // DON'T initialize game here - that's done in UIManager.OnStartGameClicked()
                // Just create the lobby and keep the board configuration intact

                // Hide the Battleships setup panel before showing lobby
                HideBattleshipsGameSetup();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetLobbyCode(lobbyCode);
                    UIManager.Instance.SetStatusSuccessPublic($"Battleships lobby created! Code: {lobbyCode}");
                    UIManager.Instance.ShowLobbyPublic();
                    UIManager.Instance.UpdatePlayerListPublic();
                }
                
                // CRITICAL FIX: DON'T clear grid state here!
                // Keep isCustomizing and tileStates intact so UIManager can read them when starting game
                // They will be cleared after game initialization
                Debug.Log($"Lobby created - keeping board config: {(isCustomizing ? "Custom" : "Default")}, tileStates={(tileStates != null ? "Set" : "NULL")}");
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetStatusErrorPublic("Failed to create Battleships lobby. Check console for details.");
                }
            }
        }
        catch (System.Exception e)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetStatusErrorPublic($"Error creating Battleships lobby: {e.Message}");
            }
            Debug.LogError($"Exception in OnCreateBattleshipsLobbyButtonClicked: {e}");
        }
        finally
        {
            if (createBattleshipsLobbyButton != null) createBattleshipsLobbyButton.interactable = true;
        }
    }

    private void OnBackFromBattleshipsSetupClicked()
    {
        Debug.Log("Back from Battleships setup button clicked");
        
        // Clear grid if it exists
        if (isCustomizing)
        {
            ClearGrid();
            isCustomizing = false;
        }
        
        // Hide the Battleships setup panel first
        HideBattleshipsGameSetup();
        
        // Notify UIManager to show game mode selection
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameModeSelectionPublic();
        }
    }

    /// <summary>
    /// Gets the configured player count
    /// </summary>
    public int GetPlayerCount() => battleshipsPlayerCount;

    /// <summary>
    /// Gets the configured max rows
    /// </summary>
    public int GetMaxRows() => battleshipsMaxRows;

    /// <summary>
    /// Gets the configured max columns
    /// </summary>
    public int GetMaxColumns() => battleshipsMaxColumns;
    
    /// <summary>
    /// Gets the custom tile states (null if not customized)
    /// </summary>
    public bool[,] GetTileStates() => tileStates;
    
    /// <summary>
    /// Gets whether the grid was customized
    /// </summary>
    public bool IsCustomized() => isCustomizing && tileStates != null;
    
    /// <summary>
    /// Clear the grid state after game initialization (called by UIManager)
    /// </summary>
    public void ClearGridState()
    {
        if (isCustomizing)
        {
            Debug.Log("Clearing grid state after game initialization");
            ClearGrid();
            isCustomizing = false;
        }
    }

    [ContextMenu("Validate Battleships Setup References")]
    public void ValidateBattleshipsSetupReferences()
    {
        Debug.Log("=== Battleships Setup Manager Reference Validation ===");

        int missingCount = 0;

        if (battleshipsGameSetupPanel == null) { Debug.LogError("BattleshipsGameSetupPanel is missing!"); missingCount++; }
        else Debug.Log("? BattleshipsGameSetupPanel assigned");

        if (battleshipsPlayerCountInput == null) { Debug.LogError("BattleshipsPlayerCountInput is missing!"); missingCount++; }
        else Debug.Log("? BattleshipsPlayerCountInput assigned");

        if (battleshipsMaxRowsInput == null) { Debug.LogError("BattleshipsMaxRowsInput is missing!"); missingCount++; }
        else Debug.Log("? BattleshipsMaxRowsInput assigned");

        if (battleshipsMaxColumnsInput == null) { Debug.LogError("BattleshipsMaxColumnsInput is missing!"); missingCount++; }
        else Debug.Log("? BattleshipsMaxColumnsInput assigned");

        if (createBattleshipsLobbyButton == null) { Debug.LogError("CreateBattleshipsLobbyButton is missing!"); missingCount++; }
        else Debug.Log("? CreateBattleshipsLobbyButton assigned");

        if (backFromBattleshipsSetupButton == null) { Debug.LogError("BackFromBattleshipsSetupButton is missing!"); missingCount++; }
        else Debug.Log("? BackFromBattleshipsSetupButton assigned");
        
        if (customizeButton == null) { Debug.LogError("CustomizeButton is missing!"); missingCount++; }
        else Debug.Log("? CustomizeButton assigned");
        
        if (gridContainer == null) { Debug.LogError("GridContainer is missing!"); missingCount++; }
        else Debug.Log("? GridContainer assigned");
        
        if (gridTilePrefab == null) { Debug.LogError("GridTilePrefab is missing!"); missingCount++; }
        else Debug.Log("? GridTilePrefab assigned");
        
        if (inputFieldsContainer == null) { Debug.LogError("InputFieldsContainer is missing!"); missingCount++; }
        else Debug.Log("? InputFieldsContainer assigned");
        
        if (backFromCustomizeButton == null) { Debug.LogError("BackFromCustomizeButton is missing!"); missingCount++; }
        else Debug.Log("? BackFromCustomizeButton assigned");

        Debug.Log($"=== Validation Complete: {missingCount} missing references ===");

        if (missingCount > 0)
        {
            Debug.LogWarning("You need to assign the missing UI elements in the Inspector!");
        }
        else
        {
            Debug.Log("All Battleships setup references are properly assigned!");
        }
    }
}

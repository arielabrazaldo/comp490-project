using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manager for preset game UI (works alongside UIManager)
/// Handles preset game selection and customization panels
/// </summary>
public class PresetUIManager : MonoBehaviour
{
    private static PresetUIManager instance;
    public static PresetUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PresetUIManager>();
            }
            return instance;
        }
    }

    [Header("Panels")]
    [SerializeField] private GameObject customizationPanel;

    [Header("Customization Panel - Input Fields")]
    [SerializeField] private TMP_InputField tileCountInput;
    [SerializeField] private TMP_InputField playerCountInput;

    [Header("Customization Panel - Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Customization Panel - Labels")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI tileCountLabel;
    [SerializeField] private TextMeshProUGUI playerCountLabel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private int minTileCount = 10;
    [SerializeField] private int maxTileCount = 50;
    [SerializeField] private int minPlayerCount = 2;
    [SerializeField] private int maxPlayerCount = 8;

    // Current preset being customized
    private string currentPresetType = "";
    private SerializableGameData currentPresetData = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ValidateReferences();
        SetupButtonListeners();
        HideCustomizationPanel();
    }

    private void ValidateReferences()
    {
        if (customizationPanel == null) Debug.LogError("CustomizationPanel is not assigned in PresetUIManager!");
        if (tileCountInput == null) Debug.LogError("TileCountInput is not assigned in PresetUIManager!");
        if (playerCountInput == null) Debug.LogError("PlayerCountInput is not assigned in PresetUIManager!");
        if (confirmButton == null) Debug.LogError("ConfirmButton is not assigned in PresetUIManager!");
        if (cancelButton == null) Debug.LogError("CancelButton is not assigned in PresetUIManager!");
    }

    private void SetupButtonListeners()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

        // Add input validation listeners
        if (tileCountInput != null)
        {
            tileCountInput.onEndEdit.AddListener(OnTileCountChanged);
            tileCountInput.characterValidation = TMP_InputField.CharacterValidation.Integer;
        }

        if (playerCountInput != null)
        {
            playerCountInput.onEndEdit.AddListener(OnPlayerCountChanged);
            playerCountInput.characterValidation = TMP_InputField.CharacterValidation.Integer;
        }
    }

    #region Public API

    /// <summary>
    /// Show customization panel for Dice Race
    /// Called when user selects Dice Race from game selection
    /// </summary>
    public void ShowDiceRaceCustomization()
    {
        Debug.Log("[PresetUIManager] Showing Dice Race customization panel");

        currentPresetType = "DiceRace";

        // Load Dice Race preset JSON
        currentPresetData = LoadDiceRacePreset();

        if (currentPresetData == null)
        {
            Debug.LogError("Failed to load Dice Race preset!");
            SetStatus("Error: Could not load Dice Race preset", Color.red);
            return;
        }

        // Show panel with current values
        ShowCustomizationPanel(
            "Customize Dice Race",
            currentPresetData.boardLayout.totalTiles,
            currentPresetData.playerCount
        );
    }

    /// <summary>
    /// Show customization panel for any preset game
    /// </summary>
    public void ShowPresetCustomization(string presetType)
    {
        Debug.Log($"[PresetUIManager] Showing {presetType} customization panel");

        currentPresetType = presetType;

        // Load appropriate preset
        switch (presetType)
        {
            case "DiceRace":
                ShowDiceRaceCustomization();
                break;
            // Add more preset types here in future
            default:
                Debug.LogWarning($"Unknown preset type: {presetType}");
                break;
        }
    }

    /// <summary>
    /// Hide customization panel
    /// </summary>
    public void HideCustomizationPanel()
    {
        if (customizationPanel != null)
        {
            customizationPanel.SetActive(false);
        }

        currentPresetType = "";
        currentPresetData = null;

        Debug.Log("[PresetUIManager] Customization panel hidden");
    }

    #endregion

    #region Private Methods

    private void ShowCustomizationPanel(string title, int defaultTileCount, int defaultPlayerCount)
    {
        if (customizationPanel == null)
        {
            Debug.LogError("Customization panel is null!");
            return;
        }

        // Show panel
        customizationPanel.SetActive(true);

        // Set title
        if (titleText != null)
        {
            titleText.text = title;
        }

        // Set labels
        if (tileCountLabel != null) tileCountLabel.text = "Number of Tiles:";
        if (playerCountLabel != null) playerCountLabel.text = "Number of Players:";

        // Set default values
        if (tileCountInput != null)
        {
            tileCountInput.text = defaultTileCount.ToString();
        }

        if (playerCountInput != null)
        {
            playerCountInput.text = defaultPlayerCount.ToString();
        }

        // Clear status
        SetStatus("", Color.white);

        Debug.Log($"[PresetUIManager] Customization panel shown: {title}");
    }

    private SerializableGameData LoadDiceRacePreset()
    {
        // Load from JSON file
        string json = LoadDiceRaceJSON();

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Failed to load Dice Race JSON!");
            return null;
        }

        try
        {
            SerializableGameData data = JsonUtility.FromJson<SerializableGameData>(json);
            Debug.Log($"[PresetUIManager] Dice Race preset loaded: {data.boardLayout.totalTiles} tiles, {data.playerCount} players");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse Dice Race JSON: {e.Message}");
            return null;
        }
    }

    private string LoadDiceRaceJSON()
    {
        // Try to load from Resources folder first
        TextAsset jsonFile = Resources.Load<TextAsset>("DiceRacePreset");
        if (jsonFile != null)
        {
            return jsonFile.text;
        }

        // Try to load from persistent data path
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "DiceRacePreset.json");
        if (System.IO.File.Exists(filePath))
        {
            return System.IO.File.ReadAllText(filePath);
        }

        // Try to load from project root (for development)
        string projectPath = System.IO.Path.Combine(Application.dataPath, "..", "DiceRacePreset.json");
        if (System.IO.File.Exists(projectPath))
        {
            return System.IO.File.ReadAllText(projectPath);
        }

        Debug.LogError("Could not find DiceRacePreset.json in any location!");
        return null;
    }

    private void SaveUpdatedPreset()
    {
        if (currentPresetData == null)
        {
            Debug.LogError("No preset data to save!");
            return;
        }

        try
        {
            // Convert to JSON
            string json = JsonUtility.ToJson(currentPresetData, true);

            // Save to persistent data path
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"{currentPresetType}Preset_Custom.json");
            System.IO.File.WriteAllText(filePath, json);

            Debug.Log($"[PresetUIManager] Updated preset saved to: {filePath}");
            SetStatus("Preset saved successfully!", Color.green);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save preset: {e.Message}");
            SetStatus("Error saving preset!", Color.red);
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }

        if (!string.IsNullOrEmpty(message))
        {
            Debug.Log($"[PresetUIManager] Status: {message}");
        }
    }

    #endregion

    #region Input Validation

    private void OnTileCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int tileCount))
        {
            // Clamp to valid range
            tileCount = Mathf.Clamp(tileCount, minTileCount, maxTileCount);

            // Update input field if clamped
            if (tileCountInput.text != tileCount.ToString())
            {
                tileCountInput.text = tileCount.ToString();
            }

            Debug.Log($"[PresetUIManager] Tile count changed: {tileCount}");
            SetStatus($"Tile count: {tileCount} (Range: {minTileCount}-{maxTileCount})", Color.white);
        }
        else
        {
            SetStatus("Invalid tile count! Please enter a number.", Color.red);
        }
    }

    private void OnPlayerCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int playerCount))
        {
            // Clamp to valid range
            playerCount = Mathf.Clamp(playerCount, minPlayerCount, maxPlayerCount);

            // Update input field if clamped
            if (playerCountInput.text != playerCount.ToString())
            {
                playerCountInput.text = playerCount.ToString();
            }

            Debug.Log($"[PresetUIManager] Player count changed: {playerCount}");
            SetStatus($"Player count: {playerCount} (Range: {minPlayerCount}-{maxPlayerCount})", Color.white);
        }
        else
        {
            SetStatus("Invalid player count! Please enter a number.", Color.red);
        }
    }

    #endregion

    #region Button Handlers

    private void OnConfirmClicked()
    {
        Debug.Log("[PresetUIManager] Confirm button clicked");

        if (currentPresetData == null)
        {
            SetStatus("Error: No preset loaded!", Color.red);
            return;
        }

        // Get values from input fields
        if (!int.TryParse(tileCountInput.text, out int tileCount))
        {
            SetStatus("Invalid tile count!", Color.red);
            return;
        }

        if (!int.TryParse(playerCountInput.text, out int playerCount))
        {
            SetStatus("Invalid player count!", Color.red);
            return;
        }

        // Validate ranges
        tileCount = Mathf.Clamp(tileCount, minTileCount, maxTileCount);
        playerCount = Mathf.Clamp(playerCount, minPlayerCount, maxPlayerCount);

        // Update preset data
        UpdatePresetData(tileCount, playerCount);

        // Save updated preset
        SaveUpdatedPreset();

        SetStatus("Settings applied! Creating lobby...", Color.green);

        Debug.Log($"[PresetUIManager] ? Confirmed: {tileCount} tiles, {playerCount} players");

        // Notify UIManager_Streamlined to create lobby with customized data
        if (UIManager_Streamlined.Instance != null)
        {
            Debug.Log("[PresetUIManager] Notifying UIManager_Streamlined of customization completion");
            UIManager_Streamlined.Instance.OnPresetCustomizationComplete(currentPresetData);
        }
        else
        {
            Debug.LogError("[PresetUIManager] UIManager_Streamlined not found!");
            SetStatus("Error: UIManager not found!", Color.red);
        }

        // Hide panel
        HideCustomizationPanel();
    }

    private void OnCancelClicked()
    {
        Debug.Log("[PresetUIManager] Cancel button clicked");

        SetStatus("Customization cancelled", Color.yellow);

        // Hide panel
        HideCustomizationPanel();

        // TODO: Return to game selection or main menu
    }

    #endregion

    #region Preset Data Updates

    private void UpdatePresetData(int tileCount, int playerCount)
    {
        if (currentPresetData == null) return;

        Debug.Log($"[PresetUIManager] Updating preset: {tileCount} tiles, {playerCount} players");

        // Update player count
        currentPresetData.playerCount = playerCount;
        currentPresetData.rules.minPlayers = Mathf.Min(2, playerCount);
        currentPresetData.rules.maxPlayers = playerCount;

        // Update board layout
        if (currentPresetType == "DiceRace")
        {
            UpdateDiceRaceBoardLayout(tileCount);
        }

        // Update last modified date
        currentPresetData.lastModifiedDate = System.DateTime.Now.ToString("o");

        Debug.Log("[PresetUIManager] ? Preset data updated");
    }

    private void UpdateDiceRaceBoardLayout(int tileCount)
    {
        if (currentPresetData.boardLayout == null)
        {
            Debug.LogError("Board layout is null!");
            return;
        }

        // Store old tile count
        int oldTileCount = currentPresetData.boardLayout.totalTiles;

        // Update board metadata
        currentPresetData.boardLayout.totalTiles = tileCount;
        currentPresetData.boardLayout.columns = tileCount;
        currentPresetData.boardLayout.boardWidth = (tileCount * 70f); // 70 pixels per tile

        // Update tiles list
        if (tileCount > oldTileCount)
        {
            // Add new tiles
            AddDiceRaceTiles(oldTileCount, tileCount);
        }
        else if (tileCount < oldTileCount)
        {
            // Remove excess tiles
            currentPresetData.boardLayout.tiles.RemoveRange(tileCount, oldTileCount - tileCount);
        }

        // Recalculate positions (centered)
        RecalculateTilePositions(tileCount);

        // Update goal tile (last tile)
        UpdateGoalTile(tileCount - 1);

        Debug.Log($"[PresetUIManager] ? Board layout updated: {oldTileCount} ? {tileCount} tiles");
    }

    private void AddDiceRaceTiles(int startIndex, int endIndex)
    {
        // Color pattern: Red, Blue, Yellow, Green
        string[] colors = { "#FF0000", "#0000FF", "#FFFF00", "#00FF00" };
        string[] borderColors = { "#8B0000", "#00008B", "#B8860B", "#006400" };
        string[] textColors = { "#FFFFFF", "#FFFFFF", "#000000", "#000000" };

        for (int i = startIndex; i < endIndex; i++)
        {
            int colorIndex = i % 4;

            var newTile = new SerializableTileData
            {
                tileId = i,
                gridX = i,
                gridY = 0,
                positionX = 0, // Will be calculated in RecalculateTilePositions
                positionY = -200.0f,
                width = 65.0f,
                height = 65.0f,

                tileType = (i == 0) ? "Start" : "Normal",
                tileName = (i == 0) ? "Start" : $"Tile {i + 1}",
                tileDescription = "",

                backgroundColor = colors[colorIndex],
                borderColor = borderColors[colorIndex],
                textColor = textColors[colorIndex],

                shape = "Square",
                borderWidth = 2.0f,
                cornerRadius = 5.0f,

                displayText = (i + 1).ToString(),
                fontSize = 18.0f,
                fontStyle = "Bold",
                textAlignment = "Center",

                isInteractive = false,
                isClickable = false,
                hasShadow = true,
                shadowColor = "#00000080",

                isActive = true,
                category = (i == 0) ? "Start" : "Default"
            };

            currentPresetData.boardLayout.tiles.Add(newTile);
        }

        Debug.Log($"[PresetUIManager] Added {endIndex - startIndex} new tiles");
    }

    private void RecalculateTilePositions(int tileCount)
    {
        float tileWidth = 70.0f; // 65px tile + 5px spacing
        float totalWidth = tileCount * tileWidth;
        float startX = -totalWidth / 2.0f + (tileWidth / 2.0f);

        for (int i = 0; i < currentPresetData.boardLayout.tiles.Count; i++)
        {
            currentPresetData.boardLayout.tiles[i].positionX = startX + (i * tileWidth);
        }

        Debug.Log($"[PresetUIManager] ? Recalculated positions for {tileCount} tiles");
    }

    private void UpdateGoalTile(int goalIndex)
    {
        if (goalIndex < 0 || goalIndex >= currentPresetData.boardLayout.tiles.Count)
        {
            Debug.LogError($"Invalid goal index: {goalIndex}");
            return;
        }

        var goalTile = currentPresetData.boardLayout.tiles[goalIndex];

        goalTile.tileType = "Goal";
        goalTile.tileName = "Goal";
        goalTile.tileDescription = "Finish line - First to reach wins!";
        goalTile.displayText = (goalIndex + 1).ToString();
        goalTile.fontSize = 20.0f;
        goalTile.borderWidth = 3.0f;
        goalTile.hasGlowEffect = true;
        goalTile.glowColor = "#FFD700";
        goalTile.isAnimated = true;
        goalTile.animationType = "Pulse";
        goalTile.category = "Goal";

        // Update tile groups
        if (currentPresetData.boardLayout.tileGroups == null)
        {
            currentPresetData.boardLayout.tileGroups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>();
        }

        currentPresetData.boardLayout.tileGroups["Goal"] = new System.Collections.Generic.List<int> { goalIndex };

        Debug.Log($"[PresetUIManager] ? Updated goal tile at index {goalIndex}");
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Get current tile count from input field
    /// </summary>
    public int GetTileCount()
    {
        if (tileCountInput != null && int.TryParse(tileCountInput.text, out int count))
        {
            return count;
        }
        return 20; // Default
    }

    /// <summary>
    /// Get current player count from input field
    /// </summary>
    public int GetPlayerCount()
    {
        if (playerCountInput != null && int.TryParse(playerCountInput.text, out int count))
        {
            return count;
        }
        return 4; // Default
    }

    /// <summary>
    /// Get current preset data
    /// </summary>
    public SerializableGameData GetCurrentPresetData()
    {
        return currentPresetData;
    }

    #endregion
}

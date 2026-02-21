using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete serializable tile data with all UI information
/// Stores everything needed to recreate a tile visually
/// </summary>
[Serializable]
public class SerializableTileData
{
    // Position & Layout
    public int tileId;
    public int gridX;
    public int gridY;
    public float positionX;
    public float positionY;
    public float width;
    public float height;
    
    // Visual Properties
    public string tileType; // "Property", "Special", "Start", "Goal", "Normal", "Railroad", "Utility"
    public string tileName;
    public string tileDescription;
    
    // Colors (stored as hex strings for JSON compatibility)
    public string backgroundColor;
    public string borderColor;
    public string textColor;
    
    // Shape & Style
    public string shape; // "Rectangle", "Square", "Circle", "Hexagon", "Custom"
    public float borderWidth;
    public float cornerRadius;
    
    // Text & Labels
    public string displayText;
    public float fontSize;
    public string fontStyle; // "Normal", "Bold", "Italic"
    public string textAlignment; // "Left", "Center", "Right", "Top", "Bottom"
    
    // Icon & Image
    public string iconName; // Reference to sprite asset
    public float iconSize;
    public string iconPosition; // "Top", "Center", "Bottom", "Left", "Right"
    
    // Game Properties (for property tiles)
    public int propertyPrice;
    public int propertyRent;
    public int[] rentWithBuildings; // Array of rent values with 1-4 houses + hotel
    public int buildingCost;
    public int mortgageValue;
    public string propertyGroup; // "Brown", "LightBlue", etc.
    
    // Interaction
    public bool isInteractive;
    public bool isClickable;
    public string clickAction; // "Purchase", "ViewDetails", "Special", "None"
    
    // Special Effects
    public bool hasGlowEffect;
    public string glowColor;
    public bool hasShadow;
    public string shadowColor;
    
    // Animation
    public bool isAnimated;
    public string animationType; // "Pulse", "Bounce", "Shake", "Rotate", "None"
    public float animationSpeed;
    
    // Building Indicators (for properties)
    public bool canHaveBuildings;
    public string buildingIndicatorStyle; // "Dots", "Icons", "Numbers", "Bars"
    public string buildingColor;
    
    // Metadata
    public bool isActive;
    public string category; // For filtering/grouping
    public Dictionary<string, string> customProperties; // Extensible custom data
    
    /// <summary>
    /// Default constructor
    /// </summary>
    public SerializableTileData()
    {
        tileId = 0;
        gridX = 0;
        gridY = 0;
        positionX = 0f;
        positionY = 0f;
        width = 70f;
        height = 70f;
        
        tileType = "Normal";
        tileName = "Untitled Tile";
        tileDescription = "";
        
        backgroundColor = "#FFFFFF";
        borderColor = "#000000";
        textColor = "#000000";
        
        shape = "Rectangle";
        borderWidth = 2f;
        cornerRadius = 5f;
        
        displayText = "";
        fontSize = 14f;
        fontStyle = "Normal";
        textAlignment = "Center";
        
        iconName = "";
        iconSize = 32f;
        iconPosition = "Top";
        
        propertyPrice = 0;
        propertyRent = 0;
        rentWithBuildings = new int[5];
        buildingCost = 0;
        mortgageValue = 0;
        propertyGroup = "None";
        
        isInteractive = true;
        isClickable = true;
        clickAction = "None";
        
        hasGlowEffect = false;
        glowColor = "#FFFF00";
        hasShadow = true;
        shadowColor = "#00000080";
        
        isAnimated = false;
        animationType = "None";
        animationSpeed = 1f;
        
        canHaveBuildings = false;
        buildingIndicatorStyle = "Dots";
        buildingColor = "#00FF00";
        
        isActive = true;
        category = "Default";
        customProperties = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Create from MonopolySpace
    /// </summary>
    public static SerializableTileData FromMonopolySpace(MonopolySpace space, int index, Vector2 position, Vector2 size)
    {
        var tileData = new SerializableTileData
        {
            tileId = space.spaceId,
            gridX = index % 10, // Assuming 10 tiles per side
            gridY = index / 10,
            positionX = position.x,
            positionY = position.y,
            width = size.x,
            height = size.y,
            
            tileName = space.spaceName,
            tileDescription = space.description ?? "",
            displayText = space.spaceName,
            
            backgroundColor = ColorToHex(space.spaceColor),
            borderColor = "#000000",
            textColor = "#000000",
            
            propertyPrice = space.price,
            propertyRent = space.rent,
            rentWithBuildings = space.rentWithHouses != null ? (int[])space.rentWithHouses.Clone() : new int[5],
            buildingCost = space.houseCost,
            mortgageValue = space.mortgageValue,
            propertyGroup = space.group.ToString(),
            
            isInteractive = true,
            isClickable = true
        };
        
        // Set tile type and properties based on PropertyType
        switch (space.type)
        {
            case PropertyType.Property:
                tileData.tileType = "Property";
                tileData.clickAction = "Purchase";
                tileData.canHaveBuildings = true;
                break;
            case PropertyType.Railroad:
                tileData.tileType = "Railroad";
                tileData.clickAction = "Purchase";
                tileData.iconName = "railroad_icon";
                break;
            case PropertyType.Utility:
                tileData.tileType = "Utility";
                tileData.clickAction = "Purchase";
                tileData.iconName = "utility_icon";
                break;
            case PropertyType.Go:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "Start";
                tileData.hasGlowEffect = true;
                tileData.glowColor = "#00FF00";
                break;
            case PropertyType.Jail:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "Jail";
                tileData.iconName = "jail_icon";
                break;
            case PropertyType.FreeParking:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "Parking";
                break;
            case PropertyType.GoToJail:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "GoToJail";
                tileData.iconName = "police_icon";
                break;
            case PropertyType.Chance:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "Chance";
                tileData.iconName = "question_icon";
                break;
            case PropertyType.CommunityChest:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "CommunityChest";
                tileData.iconName = "chest_icon";
                break;
            case PropertyType.Tax:
                tileData.tileType = "Special";
                tileData.clickAction = "Special";
                tileData.category = "Tax";
                tileData.iconName = "tax_icon";
                break;
        }
        
        return tileData;
    }
    
    /// <summary>
    /// Convert Unity Color to hex string
    /// </summary>
    public static string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);
        int a = Mathf.RoundToInt(color.a * 255f);
        
        if (a == 255)
        {
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        else
        {
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }
    
    /// <summary>
    /// Convert hex string to Unity Color
    /// </summary>
    public static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        
        hex = hex.Replace("#", "");
        
        if (hex.Length == 6) // RGB
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }
        else if (hex.Length == 8) // RGBA
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, a);
        }
        
        return Color.white;
    }
}

/// <summary>
/// Complete board layout with all tiles and UI configuration
/// </summary>
[Serializable]
public class SerializableBoardData
{
    // Board Metadata
    public string boardName;
    public string boardType; // "Linear", "Square", "Grid", "Custom"
    public int rows;
    public int columns;
    public int totalTiles;
    
    // Board Layout
    public float boardWidth;
    public float boardHeight;
    public float tileSpacing;
    public string layoutPattern; // "Perimeter", "Grid", "Linear", "Circular", "Custom"
    
    // Visual Style
    public string backgroundColor;
    public string borderColor;
    public bool showGrid;
    public string gridColor;
    
    // Tiles
    public List<SerializableTileData> tiles;
    
    // Player Start Positions
    public List<int> startTileIds;
    
    // Special Tile Groups (for quick lookup)
    public Dictionary<string, List<int>> tileGroups; // e.g., "Properties", "Railroads", "Utilities"
    
    // Metadata
    public string createdDate;
    public string lastModifiedDate;
    public string version;
    
    public SerializableBoardData()
    {
        boardName = "Custom Board";
        boardType = "Square";
        rows = 10;
        columns = 10;
        totalTiles = 40;
        
        boardWidth = 800f;
        boardHeight = 800f;
        tileSpacing = 5f;
        layoutPattern = "Perimeter";
        
        backgroundColor = "#F0F0F0";
        borderColor = "#000000";
        showGrid = false;
        gridColor = "#CCCCCC";
        
        tiles = new List<SerializableTileData>();
        startTileIds = new List<int> { 0 };
        tileGroups = new Dictionary<string, List<int>>();
        
        createdDate = DateTime.Now.ToString("o");
        lastModifiedDate = DateTime.Now.ToString("o");
        version = "1.0";
    }
}

/// <summary>
/// UI Element configuration (for panels, buttons, etc.)
/// </summary>
[Serializable]
public class SerializableUIElement
{
    public string elementId;
    public string elementType; // "Panel", "Button", "Text", "Image", "InputField", "Slider"
    public string elementName;
    
    // Position & Size
    public float positionX;
    public float positionY;
    public float width;
    public float height;
    public string anchorPreset; // "TopLeft", "Center", "BottomRight", etc.
    
    // Visual
    public string backgroundColor;
    public string borderColor;
    public float borderWidth;
    public string textContent;
    public float fontSize;
    public string fontStyle;
    public string textColor;
    
    // Behavior
    public bool isInteractive;
    public string clickAction;
    public bool isVisible;
    
    // Hierarchy
    public string parentId;
    public List<string> childIds;
    
    public SerializableUIElement()
    {
        elementId = Guid.NewGuid().ToString();
        elementType = "Panel";
        elementName = "UI Element";
        
        positionX = 0f;
        positionY = 0f;
        width = 100f;
        height = 100f;
        anchorPreset = "Center";
        
        backgroundColor = "#FFFFFF";
        borderColor = "#000000";
        borderWidth = 1f;
        textContent = "";
        fontSize = 14f;
        fontStyle = "Normal";
        textColor = "#000000";
        
        isInteractive = true;
        clickAction = "None";
        isVisible = true;
        
        parentId = "";
        childIds = new List<string>();
    }
}

/// <summary>
/// Complete UI layout configuration
/// </summary>
[Serializable]
public class SerializableUILayout
{
    public string layoutName;
    public string layoutType; // "Monopoly", "DiceRace", "Battleships", "Custom"
    
    // Canvas Settings
    public float canvasWidth;
    public float canvasHeight;
    public string renderMode; // "ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"
    
    // UI Elements
    public List<SerializableUIElement> uiElements;
    
    // Theme
    public string themeName;
    public Dictionary<string, string> themeColors; // Color palette
    
    public SerializableUILayout()
    {
        layoutName = "Default Layout";
        layoutType = "Custom";
        
        canvasWidth = 1920f;
        canvasHeight = 1080f;
        renderMode = "ScreenSpaceOverlay";
        
        uiElements = new List<SerializableUIElement>();
        
        themeName = "Default";
        themeColors = new Dictionary<string, string>
        {
            { "Primary", "#007ACC" },
            { "Secondary", "#F0F0F0" },
            { "Accent", "#FFA500" },
            { "Text", "#000000" },
            { "Background", "#FFFFFF" }
        };
    }
}

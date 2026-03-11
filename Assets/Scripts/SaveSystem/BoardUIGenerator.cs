using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates board UI from serializable tile data
/// Interprets JSON board configurations and creates Unity UI elements
/// </summary>
public class BoardUIGenerator : MonoBehaviour
{
    private static BoardUIGenerator instance;
    public static BoardUIGenerator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BoardUIGenerator>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BoardUIGenerator");
                    instance = go.AddComponent<BoardUIGenerator>();
                }
            }
            return instance;
        }
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject propertyTilePrefab;
    [SerializeField] private GameObject specialTilePrefab;
    
    [Header("Icon Sprites")]
    [SerializeField] private Sprite railroadIcon;
    [SerializeField] private Sprite utilityIcon;
    [SerializeField] private Sprite jailIcon;
    [SerializeField] private Sprite policeIcon;
    [SerializeField] private Sprite questionIcon;
    [SerializeField] private Sprite chestIcon;
    [SerializeField] private Sprite taxIcon;
    
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset normalFont;
    [SerializeField] private TMP_FontAsset boldFont;
    [SerializeField] private TMP_FontAsset italicFont;
    
    private Dictionary<string, Sprite> iconRegistry;
    private Dictionary<string, GameObject> generatedTiles;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // FIXED: Make root GameObject before DontDestroyOnLoad
            if (transform.parent != null)
            {
                Debug.Log("[BoardUIGenerator] Detaching from parent to become root GameObject");
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeIconRegistry();
        generatedTiles = new Dictionary<string, GameObject>();
    }
    
    private void InitializeIconRegistry()
    {
        iconRegistry = new Dictionary<string, Sprite>
        {
            { "railroad_icon", railroadIcon },
            { "utility_icon", utilityIcon },
            { "jail_icon", jailIcon },
            { "police_icon", policeIcon },
            { "question_icon", questionIcon },
            { "chest_icon", chestIcon },
            { "tax_icon", taxIcon }
        };
    }
    
    /// <summary>
    /// Generate complete board from serializable data
    /// </summary>
    public GameObject GenerateBoard(SerializableBoardData boardData, Transform parent)
    {
        if (boardData == null || parent == null)
        {
            Debug.LogError("Cannot generate board: boardData or parent is null!");
            return null;
        }
        
        Debug.Log($"Generating board: {boardData.boardName} with {boardData.tiles.Count} tiles");
        
        // Create board container
        GameObject boardContainer = new GameObject(boardData.boardName);
        boardContainer.transform.SetParent(parent, false);
        
        // Setup board container
        RectTransform boardRect = boardContainer.AddComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(boardData.boardWidth, boardData.boardHeight);
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add background
        Image boardBackground = boardContainer.AddComponent<Image>();
        boardBackground.color = SerializableTileData.HexToColor(boardData.backgroundColor);
        
        // Generate tiles
        generatedTiles.Clear();
        foreach (var tileData in boardData.tiles)
        {
            GameObject tile = GenerateTile(tileData, boardContainer.transform);
            if (tile != null)
            {
                generatedTiles[$"tile_{tileData.tileId}"] = tile;
            }
        }
        
        Debug.Log($"Board generated successfully with {generatedTiles.Count} tiles");
        return boardContainer;
    }
    
    /// <summary>
    /// Generate a single tile from serializable data
    /// </summary>
    public GameObject GenerateTile(SerializableTileData tileData, Transform parent)
    {
        if (tileData == null || parent == null)
        {
            Debug.LogError("Cannot generate tile: tileData or parent is null!");
            return null;
        }
        
        // Choose appropriate prefab
        GameObject prefab = GetTilePrefab(tileData.tileType);
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for tile type: {tileData.tileType}");
            return null;
        }
        
        // Instantiate tile
        GameObject tile = Instantiate(prefab, parent);
        tile.name = $"{tileData.tileName} ({tileData.tileId})";
        
        // Setup RectTransform
        SetupTileTransform(tile, tileData);
        
        // Setup Visual Properties
        SetupTileVisuals(tile, tileData);
        
        // Setup Text
        SetupTileText(tile, tileData);
        
        // Setup Icon
        SetupTileIcon(tile, tileData);
        
        // Setup Effects
        SetupTileEffects(tile, tileData);
        
        // Setup Interaction
        SetupTileInteraction(tile, tileData);
        
        // Store tile data
        var tileComponent = tile.GetComponent<MonopolyTileData>();
        if (tileComponent == null)
        {
            tileComponent = tile.AddComponent<MonopolyTileData>();
        }
        tileComponent.spaceIndex = tileData.tileId;
        
        return tile;
    }
    
    private GameObject GetTilePrefab(string tileType)
    {
        switch (tileType)
        {
            case "Property":
            case "Railroad":
            case "Utility":
                return propertyTilePrefab != null ? propertyTilePrefab : tilePrefab;
            case "Special":
                return specialTilePrefab != null ? specialTilePrefab : tilePrefab;
            default:
                return tilePrefab;
        }
    }
    
    private void SetupTileTransform(GameObject tile, SerializableTileData data)
    {
        RectTransform rt = tile.GetComponent<RectTransform>();
        if (rt == null) rt = tile.AddComponent<RectTransform>();
        
        // Position
        rt.anchoredPosition = new Vector2(data.positionX, data.positionY);
        
        // Size
        rt.sizeDelta = new Vector2(data.width, data.height);
        
        // Anchors (centered)
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
    
    private void SetupTileVisuals(GameObject tile, SerializableTileData data)
    {
        Image tileImage = tile.GetComponent<Image>();
        if (tileImage == null) tileImage = tile.AddComponent<Image>();
        
        // Background color
        tileImage.color = SerializableTileData.HexToColor(data.backgroundColor);
        
        // Border (using Outline component)
        if (data.borderWidth > 0)
        {
            Outline outline = tile.GetComponent<Outline>();
            if (outline == null) outline = tile.AddComponent<Outline>();
            
            outline.effectColor = SerializableTileData.HexToColor(data.borderColor);
            outline.effectDistance = new Vector2(data.borderWidth, data.borderWidth);
        }
        
        // Shadow
        if (data.hasShadow)
        {
            Shadow shadow = tile.GetComponent<Shadow>();
            if (shadow == null) shadow = tile.AddComponent<Shadow>();
            
            shadow.effectColor = SerializableTileData.HexToColor(data.shadowColor);
            shadow.effectDistance = new Vector2(2, -2);
        }
        
        // Raycast target
        tileImage.raycastTarget = data.isClickable;
    }
    
    private void SetupTileText(GameObject tile, SerializableTileData data)
    {
        // Find or create text component
        TextMeshProUGUI textComponent = tile.GetComponentInChildren<TextMeshProUGUI>();
        
        if (textComponent == null && !string.IsNullOrEmpty(data.displayText))
        {
            GameObject textObj = new GameObject("TileText");
            textObj.transform.SetParent(tile.transform, false);
            textComponent = textObj.AddComponent<TextMeshProUGUI>();
            
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        if (textComponent != null)
        {
            textComponent.text = data.displayText;
            textComponent.fontSize = data.fontSize;
            textComponent.color = SerializableTileData.HexToColor(data.textColor);
            
            // Font style
            switch (data.fontStyle)
            {
                case "Bold":
                    textComponent.fontStyle = FontStyles.Bold;
                    if (boldFont != null) textComponent.font = boldFont;
                    break;
                case "Italic":
                    textComponent.fontStyle = FontStyles.Italic;
                    if (italicFont != null) textComponent.font = italicFont;
                    break;
                default:
                    textComponent.fontStyle = FontStyles.Normal;
                    if (normalFont != null) textComponent.font = normalFont;
                    break;
            }
            
            // Text alignment
            textComponent.alignment = ParseTextAlignment(data.textAlignment);
            
            // Disable raycast on text
            textComponent.raycastTarget = false;
        }
    }
    
    private void SetupTileIcon(GameObject tile, SerializableTileData data)
    {
        if (string.IsNullOrEmpty(data.iconName)) return;
        
        // Get icon sprite
        Sprite iconSprite = null;
        if (iconRegistry.ContainsKey(data.iconName))
        {
            iconSprite = iconRegistry[data.iconName];
        }
        
        if (iconSprite == null) return;
        
        // Create icon GameObject
        GameObject iconObj = new GameObject("TileIcon");
        iconObj.transform.SetParent(tile.transform, false);
        
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.raycastTarget = false;
        
        // Position and size icon
        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(data.iconSize, data.iconSize);
        
        Vector2 iconPosition = GetIconPosition(data.iconPosition, data.width, data.height, data.iconSize);
        iconRect.anchoredPosition = iconPosition;
        
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
    }
    
    private Vector2 GetIconPosition(string position, float tileWidth, float tileHeight, float iconSize)
    {
        float padding = 5f;
        
        switch (position)
        {
            case "Top":
                return new Vector2(0, tileHeight / 2 - iconSize / 2 - padding);
            case "Bottom":
                return new Vector2(0, -tileHeight / 2 + iconSize / 2 + padding);
            case "Left":
                return new Vector2(-tileWidth / 2 + iconSize / 2 + padding, 0);
            case "Right":
                return new Vector2(tileWidth / 2 - iconSize / 2 - padding, 0);
            case "Center":
            default:
                return Vector2.zero;
        }
    }
    
    private void SetupTileEffects(GameObject tile, SerializableTileData data)
    {
        // Glow effect
        if (data.hasGlowEffect)
        {
            Outline glow = tile.GetComponent<Outline>();
            if (glow == null) glow = tile.AddComponent<Outline>();
            
            glow.effectColor = SerializableTileData.HexToColor(data.glowColor);
            glow.effectDistance = new Vector2(3, 3);
        }
        
        // Animation
        if (data.isAnimated)
        {
            var animator = tile.GetComponent<TileAnimator>();
            if (animator == null) animator = tile.AddComponent<TileAnimator>();
            
            animator.SetAnimation(data.animationType, data.animationSpeed);
        }
    }
    
    private void SetupTileInteraction(GameObject tile, SerializableTileData data)
    {
        if (!data.isInteractive) return;
        
        Button button = tile.GetComponent<Button>();
        if (button == null && data.isClickable)
        {
            button = tile.AddComponent<Button>();
        }
        
        // Setup click handler
        var clickHandler = tile.GetComponent<MonopolyTileClickHandler>();
        if (clickHandler == null && data.isClickable)
        {
            clickHandler = tile.AddComponent<MonopolyTileClickHandler>();
        }
    }
    
    private TextAlignmentOptions ParseTextAlignment(string alignment)
    {
        switch (alignment)
        {
            case "TopLeft": return TextAlignmentOptions.TopLeft;
            case "Top": return TextAlignmentOptions.Top;
            case "TopRight": return TextAlignmentOptions.TopRight;
            case "Left": return TextAlignmentOptions.Left;
            case "Center": return TextAlignmentOptions.Center;
            case "Right": return TextAlignmentOptions.Right;
            case "BottomLeft": return TextAlignmentOptions.BottomLeft;
            case "Bottom": return TextAlignmentOptions.Bottom;
            case "BottomRight": return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
    
    /// <summary>
    /// Export current board to serializable data
    /// </summary>
    public SerializableBoardData ExportBoardData(string boardName, List<MonopolySpace> spaces, List<GameObject> tiles, Transform boardParent)
    {
        var boardData = new SerializableBoardData
        {
            boardName = boardName,
            boardType = "Square",
            layoutPattern = "Perimeter",
            totalTiles = spaces.Count,
            tiles = new List<SerializableTileData>()
        };
        
        RectTransform parentRect = boardParent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            boardData.boardWidth = parentRect.sizeDelta.x;
            boardData.boardHeight = parentRect.sizeDelta.y;
        }
        
        // Export each tile
        for (int i = 0; i < spaces.Count && i < tiles.Count; i++)
        {
            var space = spaces[i];
            var tile = tiles[i];
            
            RectTransform tileRect = tile.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                var tileData = SerializableTileData.FromMonopolySpace(
                    space, 
                    i, 
                    tileRect.anchoredPosition, 
                    tileRect.sizeDelta
                );
                
                boardData.tiles.Add(tileData);
            }
        }
        
        Debug.Log($"Exported board data: {boardData.tiles.Count} tiles");
        return boardData;
    }
    
    /// <summary>
    /// Clear generated tiles
    /// </summary>
    public void ClearGeneratedTiles()
    {
        foreach (var kvp in generatedTiles)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        generatedTiles.Clear();
    }
}

/// <summary>
/// Simple tile animator component
/// </summary>
public class TileAnimator : MonoBehaviour
{
    private string animationType = "None";
    private float animationSpeed = 1f;
    private float animationTime = 0f;
    
    public void SetAnimation(string type, float speed)
    {
        animationType = type;
        animationSpeed = speed;
    }
    
    private void Update()
    {
        if (animationType == "None") return;
        
        animationTime += Time.deltaTime * animationSpeed;
        
        switch (animationType)
        {
            case "Pulse":
                float scale = 1f + Mathf.Sin(animationTime * Mathf.PI) * 0.1f;
                transform.localScale = Vector3.one * scale;
                break;
                
            case "Bounce":
                float bounce = Mathf.Abs(Mathf.Sin(animationTime * Mathf.PI));
                transform.localPosition = new Vector3(transform.localPosition.x, bounce * 10f, 0);
                break;
                
            case "Rotate":
                transform.Rotate(0, 0, animationSpeed * Time.deltaTime * 10f);
                break;
                
            case "Shake":
                float shake = Mathf.Sin(animationTime * Mathf.PI * 10f) * 2f;
                transform.localPosition = new Vector3(shake, transform.localPosition.y, 0);
                break;
        }
    }
}

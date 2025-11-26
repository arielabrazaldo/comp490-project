using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MonopolyUI : MonoBehaviour
{
    [Header("Property Info Panel")]
    public GameObject propertyInfoPanel;
    public TextMeshProUGUI propertyNameText;
    public TextMeshProUGUI propertyPriceText;
    public TextMeshProUGUI propertyRentText;
    public TextMeshProUGUI propertyOwnerText;
    public TextMeshProUGUI propertyDetailsText;
    public Button buyHouseButton;
    public Button buyHotelButton;
    public Button closeButton;
    
    private int currentPropertyId = -1;

    [Header("Player Status Panel")]
    public GameObject playerStatusPanel;
    public Transform playerStatusContainer;
    public GameObject playerStatusTextPrefab;
    private List<TextMeshProUGUI> playerStatusTexts = new List<TextMeshProUGUI>();
    
    [Header("Legacy - Fixed Array (Optional)")]
    public TextMeshProUGUI[] fixedPlayerStatusTexts = new TextMeshProUGUI[4];

    [Header("Game Log")]
    public ScrollRect gameLogScrollRect;
    public TextMeshProUGUI gameLogText;
    public GameObject gameLogPanel;
    public Button toggleGameLogButton; // NEW: Button to toggle game log

    private MonopolySpace currentPropertyInfo;
    private static MonopolyUI instance;

    public static MonopolyUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MonopolyUI>();
            }
            return instance;
        }
    }

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
        SetupUI();
        
        // Subscribe to Monopoly events
        MonopolyGameManager.OnGameMessage += OnGameMessage;
        MonopolyGameManager.OnPlayerMoneyChanged += UpdatePlayerStatus;
        MonopolyGameManager.OnPropertyPurchased += OnPropertyPurchased;
        MonopolyGameManager.OnHousePurchased += OnHousePurchased;
        MonopolyGameManager.OnHotelPurchased += OnHotelPurchased;
        MonopolyGameManager.OnGameStarted += OnGameStarted; // NEW: Subscribe to game started
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        MonopolyGameManager.OnGameMessage -= OnGameMessage;
        MonopolyGameManager.OnPlayerMoneyChanged -= UpdatePlayerStatus;
        MonopolyGameManager.OnPropertyPurchased -= OnPropertyPurchased;
        MonopolyGameManager.OnHousePurchased -= OnHousePurchased;
        MonopolyGameManager.OnHotelPurchased -= OnHotelPurchased;
        MonopolyGameManager.OnGameStarted -= OnGameStarted;
    }
    
    /// <summary>
    /// NEW: Called when the game starts to ensure player status displays are created
    /// </summary>
    private void OnGameStarted()
    {
        Debug.Log("?? MonopolyUI: Game started, setting up player status displays");
        SetupPlayerStatusDisplays();
        
        // Force initial update of all players
        if (MonopolyGameManager.Instance != null)
        {
            int totalPlayers = MonopolyGameManager.Instance.GetTotalPlayers();
            for (int i = 0; i < totalPlayers; i++)
            {
                var playerData = MonopolyGameManager.Instance.GetPlayer(i);
                UpdatePlayerStatus(i, playerData.money);
            }
        }
    }

    private void SetupUI()
    {
        // Hide panels initially
        if (propertyInfoPanel != null) propertyInfoPanel.SetActive(false);
        if (gameLogPanel != null) gameLogPanel.SetActive(true);
        if (playerStatusPanel != null) playerStatusPanel.SetActive(true);

        // Setup button listeners
        if (buyHouseButton != null)
        {
            buyHouseButton.onClick.RemoveAllListeners();
            buyHouseButton.onClick.AddListener(OnBuyHouseButtonClicked);
            Debug.Log("? Buy House button listener added");
        }
        else
        {
            Debug.LogWarning("?? Buy House button is not assigned in MonopolyUI!");
        }
        
        if (buyHotelButton != null)
        {
            buyHotelButton.onClick.RemoveAllListeners();
            buyHotelButton.onClick.AddListener(OnBuyHotelButtonClicked);
            Debug.Log("? Buy Hotel button listener added");
        }
        else
        {
            Debug.LogWarning("?? Buy Hotel button is not assigned in MonopolyUI!");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            Debug.Log("? Close button listener added");
        }
        
        // NEW: Setup toggle game log button
        if (toggleGameLogButton != null)
        {
            toggleGameLogButton.onClick.RemoveAllListeners();
            toggleGameLogButton.onClick.AddListener(ToggleGameLog);
            Debug.Log("? Toggle Game Log button listener added");
        }
        else
        {
            Debug.LogWarning("?? Toggle Game Log button is not assigned in MonopolyUI!");
        }

        // Initialize game log
        if (gameLogText != null)
        {
            gameLogText.text = "Welcome to Monopoly!\n";
        }
        
        // Setup dynamic player status displays (will be populated when game starts)
        if (MonopolyGameManager.Instance != null && MonopolyGameManager.Instance.GetTotalPlayers() > 0)
        {
            SetupPlayerStatusDisplays();
        }
        else
        {
            Debug.Log("? Waiting for game to start before creating player status displays");
        }
        
        Debug.Log("? MonopolyUI setup complete");
    }
    
    /// <summary>
    /// Setup player status displays dynamically based on player count
    /// </summary>
    private void SetupPlayerStatusDisplays()
    {
        if (MonopolyGameManager.Instance == null)
        {
            Debug.LogWarning("?? MonopolyGameManager.Instance is null, cannot setup player status");
            return;
        }
        
        int totalPlayers = MonopolyGameManager.Instance.GetTotalPlayers();
        
        if (totalPlayers == 0)
        {
            Debug.LogWarning("?? Total players is 0, waiting for game initialization");
            return;
        }
        
        // Use dynamic system if available
        if (playerStatusContainer != null && playerStatusTextPrefab != null)
        {
            // Clear existing
            foreach (var text in playerStatusTexts)
            {
                if (text != null) Destroy(text.gameObject);
            }
            playerStatusTexts.Clear();
            
            // Create new player status displays
            for (int i = 0; i < totalPlayers; i++)
            {
                GameObject statusObj = Instantiate(playerStatusTextPrefab, playerStatusContainer);
                TextMeshProUGUI statusText = statusObj.GetComponent<TextMeshProUGUI>();
                if (statusText != null)
                {
                    playerStatusTexts.Add(statusText);
                    statusText.text = $"Player {i + 1}: $1500";
                    statusText.gameObject.SetActive(true); // Ensure it's visible
                    Debug.Log($"? Created player status display for Player {i + 1}");
                }
                else
                {
                    Debug.LogError($"? PlayerStatusTextPrefab doesn't have TextMeshProUGUI component!");
                }
            }
            
            Debug.Log($"? Created {playerStatusTexts.Count} dynamic player status displays");
            
            // Force layout rebuild
            if (playerStatusContainer != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerStatusContainer as RectTransform);
            }
        }
        else
        {
            if (playerStatusContainer == null) Debug.LogError("? PlayerStatusContainer is null!");
            if (playerStatusTextPrefab == null) Debug.LogError("? PlayerStatusTextPrefab is null!");
            
            // Fall back to fixed array
            playerStatusTexts.Clear();
            for (int i = 0; i < fixedPlayerStatusTexts.Length && i < totalPlayers; i++)
            {
                if (fixedPlayerStatusTexts[i] != null)
                {
                    playerStatusTexts.Add(fixedPlayerStatusTexts[i]);
                    fixedPlayerStatusTexts[i].gameObject.SetActive(true);
                }
            }
            Debug.Log($"?? Using fixed player status array with {playerStatusTexts.Count} displays");
        }
    }

    /// <summary>
    /// Show property information panel with full details and build options
    /// </summary>
    public void ShowPropertyInfo(MonopolySpace space, int spaceId, bool canPurchase = false)
    {
        if (propertyInfoPanel == null) return;

        currentPropertyInfo = space;
        currentPropertyId = spaceId;
        
        // Update property name
        if (propertyNameText != null)
            propertyNameText.text = space.spaceName;
            
        // Update property price
        if (propertyPriceText != null)
            propertyPriceText.text = $"Price: ${space.price}";
            
        // Update rent info
        if (propertyRentText != null)
        {
            if (space.type == PropertyType.Property && space.rentWithHouses != null && space.rentWithHouses.Length > 0)
            {
                propertyRentText.text = $"Base Rent: ${space.rent}\n" +
                                       $"With 1 House: ${space.rentWithHouses[0]}\n" +
                                       $"With 2 Houses: ${space.rentWithHouses[1]}\n" +
                                       $"With 3 Houses: ${space.rentWithHouses[2]}\n" +
                                       $"With 4 Houses: ${space.rentWithHouses[3]}\n" +
                                       $"With Hotel: ${space.rentWithHouses[4]}";
            }
            else
            {
                propertyRentText.text = $"Rent: ${space.rent}";
            }
        }
            
        // Update owner info
        if (propertyOwnerText != null)
        {
            if (space.isOwned)
            {
                propertyOwnerText.text = $"Owner: Player {space.ownerId + 1}";
            }
            else
            {
                propertyOwnerText.text = "Unowned";
            }
        }
        
        // Update property details (house/hotel costs, mortgage value, etc.)
        if (propertyDetailsText != null)
        {
            string details = "";
            
            if (space.type == PropertyType.Property)
            {
                details += $"<b>Building Costs:</b>\n";
                details += $"House: ${space.houseCost}\n";
                details += $"Hotel: ${space.houseCost} (requires 4 houses)\n\n";
            }
            
            details += $"<b>Mortgage Value:</b> ${space.mortgageValue}\n";
            
            // Show current buildings if owned
            if (space.isOwned && MonopolyGameManager.Instance != null)
            {
                var ownership = GetPropertyOwnershipData(spaceId);
                if (ownership.propertyId == spaceId)
                {
                    details += $"\n<b>Current Buildings:</b>\n";
                    if (ownership.hasHotel)
                    {
                        details += "?? Hotel";
                    }
                    else if (ownership.houseCount > 0)
                    {
                        details += $"?? {ownership.houseCount} House{(ownership.houseCount > 1 ? "s" : "")}";
                    }
                    else
                    {
                        details += "No buildings";
                    }
                    
                    if (ownership.isMortgaged)
                    {
                        details += "\n<color=red>(MORTGAGED)</color>";
                    }
                }
            }
            
            propertyDetailsText.text = details;
        }
        
        // Show/hide house/hotel buttons based on ownership and eligibility
        UpdateBuildButtons();
        
        propertyInfoPanel.SetActive(true);
        Debug.Log($"?? Showing property panel for {space.spaceName} (ID: {spaceId})");
    }

    /// <summary>
    /// Update the visibility and interactability of house/hotel build buttons
    /// </summary>
    private void UpdateBuildButtons()
    {
        if (MonopolyGameManager.Instance == null || currentPropertyId < 0)
        {
            if (buyHouseButton != null) buyHouseButton.gameObject.SetActive(false);
            if (buyHotelButton != null) buyHotelButton.gameObject.SetActive(false);
            return;
        }
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        bool isMyTurn = MonopolyGameManager.Instance.IsMyTurn();
        var space = MonopolyGameManager.Instance.GetSpace(currentPropertyId);
        
        // Only show build buttons for properties (not railroads or utilities)
        if (space == null || space.type != PropertyType.Property)
        {
            if (buyHouseButton != null) buyHouseButton.gameObject.SetActive(false);
            if (buyHotelButton != null) buyHotelButton.gameObject.SetActive(false);
            return;
        }
        
        // Check if player owns this property
        bool ownsProperty = false;
        var ownership = GetPropertyOwnershipData(currentPropertyId);
        if (ownership.propertyId == currentPropertyId && ownership.ownerId == myPlayerId)
        {
            ownsProperty = true;
        }
        
        // House button
        if (buyHouseButton != null)
        {
            bool canBuildHouse = ownsProperty && isMyTurn && 
                                 MonopolyGameManager.Instance.CanBuildHouse(myPlayerId, currentPropertyId);
            buyHouseButton.gameObject.SetActive(ownsProperty);
            buyHouseButton.interactable = canBuildHouse;
            
            var buttonText = buyHouseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Buy House (${space.houseCost})";
            }
        }
        
        // Hotel button
        if (buyHotelButton != null)
        {
            bool canBuildHotel = ownsProperty && isMyTurn && 
                                 MonopolyGameManager.Instance.CanBuildHotel(myPlayerId, currentPropertyId);
            buyHotelButton.gameObject.SetActive(ownsProperty);
            buyHotelButton.interactable = canBuildHotel;
            
            var buttonText = buyHotelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Buy Hotel (${space.houseCost})";
            }
        }
    }
    
    private MonopolyGameManager.PropertyOwnership GetPropertyOwnershipData(int propertyId)
    {
        if (MonopolyGameManager.Instance == null) return default;
        
        // Get all properties for all players
        for (int playerId = 0; playerId < MonopolyGameManager.Instance.GetTotalPlayers(); playerId++)
        {
            var playerProperties = MonopolyGameManager.Instance.GetPlayerProperties(playerId);
            if (playerProperties != null)
            {
                foreach (var prop in playerProperties)
                {
                    if (prop.propertyId == propertyId)
                        return prop;
                }
            }
        }
        
        return default;
    }

    public void HidePropertyInfo()
    {
        if (propertyInfoPanel != null)
        {
            propertyInfoPanel.SetActive(false);
            currentPropertyId = -1;
        }
    }
    
    private void OnBuyHouseButtonClicked()
    {
        Debug.Log($"?? Buy house button clicked for property {currentPropertyId}");
        
        if (MonopolyGameManager.Instance != null && currentPropertyId >= 0)
        {
            MonopolyGameManager.Instance.BuyHouse(currentPropertyId);
            StartCoroutine(RefreshPropertyPanelAfterDelay(0.1f));
        }
    }
    
    private void OnBuyHotelButtonClicked()
    {
        Debug.Log($"?? Buy hotel button clicked for property {currentPropertyId}");
        
        if (MonopolyGameManager.Instance != null && currentPropertyId >= 0)
        {
            MonopolyGameManager.Instance.BuyHotel(currentPropertyId);
            StartCoroutine(RefreshPropertyPanelAfterDelay(0.1f));
        }
    }
    
    private System.Collections.IEnumerator RefreshPropertyPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (currentPropertyId >= 0 && propertyInfoPanel != null && propertyInfoPanel.activeSelf)
        {
            var space = MonopolyGameManager.Instance.GetSpace(currentPropertyId);
            if (space != null)
            {
                ShowPropertyInfo(space, currentPropertyId, false);
            }
        }
    }

    private void OnCloseButtonClicked()
    {
        HidePropertyInfo();
    }

    public void UpdatePlayerStatus(int playerId, int money)
    {
        if (MonopolyGameManager.Instance != null)
        {
            int totalPlayers = MonopolyGameManager.Instance.GetTotalPlayers();
            
            // If player status displays haven't been created yet, create them now
            if (playerStatusTexts.Count == 0 && totalPlayers > 0)
            {
                Debug.Log("?? Player status displays not created, creating them now");
                SetupPlayerStatusDisplays();
            }
            
            // Update all player status displays
            for (int i = 0; i < playerStatusTexts.Count && i < totalPlayers; i++)
            {
                if (playerStatusTexts[i] != null)
                {
                    var playerData = MonopolyGameManager.Instance.GetPlayer(i);
                    if (playerData.playerId >= 0)
                    {
                        string status = $"Player {i + 1}: ${playerData.money}";
                        if (playerData.isInJail)
                        {
                            status += " (JAIL)";
                        }
                        if (playerData.isBankrupt)
                        {
                            status += " (BANKRUPT)";
                        }
                        playerStatusTexts[i].text = status;
                        playerStatusTexts[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        playerStatusTexts[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnPropertyPurchased(int playerId, int propertyId)
    {
        if (MonopolyGameManager.Instance != null)
        {
            var space = MonopolyGameManager.Instance.GetSpace(propertyId);
            if (space != null)
            {
                OnGameMessage($"Player {playerId + 1} purchased {space.spaceName}!");
            }
        }
        
        if (currentPropertyId == propertyId && propertyInfoPanel != null && propertyInfoPanel.activeSelf)
        {
            StartCoroutine(RefreshPropertyPanelAfterDelay(0.1f));
        }
    }
    
    private void OnHousePurchased(int playerId, int propertyId, int houseCount)
    {
        if (currentPropertyId == propertyId && propertyInfoPanel != null && propertyInfoPanel.activeSelf)
        {
            StartCoroutine(RefreshPropertyPanelAfterDelay(0.1f));
        }
        
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.UpdatePropertyBuildings(propertyId, houseCount, false);
        }
    }
    
    private void OnHotelPurchased(int playerId, int propertyId)
    {
        if (currentPropertyId == propertyId && propertyInfoPanel != null && propertyInfoPanel.activeSelf)
        {
            StartCoroutine(RefreshPropertyPanelAfterDelay(0.1f));
        }
        
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.UpdatePropertyBuildings(propertyId, 0, true);
        }
    }

    public void OnGameMessage(string message)
    {
        if (gameLogText != null)
        {
            // Keep a scrolling log of last 10 messages
            string[] lines = gameLogText.text.Split('\n');
            if (lines.Length > 10)
            {
                string newLog = "";
                for (int i = lines.Length - 9; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        newLog += lines[i] + "\n";
                    }
                }
                gameLogText.text = newLog + message + "\n";
            }
            else
            {
                gameLogText.text += message + "\n";
            }
            
            if (gameLogScrollRect != null)
            {
                StartCoroutine(ScrollToBottom());
            }
        }
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (gameLogScrollRect != null)
        {
            gameLogScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// NEW: Toggle the game log panel visibility
    /// </summary>
    public void ToggleGameLog()
    {
        if (gameLogPanel != null)
        {
            bool newState = !gameLogPanel.activeSelf;
            gameLogPanel.SetActive(newState);
            
            // Update button text if it has a TextMeshProUGUI child
            if (toggleGameLogButton != null)
            {
                var buttonText = toggleGameLogButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = newState ? "Hide Log" : "Show Log";
                }
            }
            
            Debug.Log($"?? Game log panel {(newState ? "shown" : "hidden")}");
        }
    }

    public void TogglePlayerStatus()
    {
        if (playerStatusPanel != null)
        {
            playerStatusPanel.SetActive(!playerStatusPanel.activeSelf);
        }
    }
}
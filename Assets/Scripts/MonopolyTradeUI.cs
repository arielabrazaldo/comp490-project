using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI Controller for the property trading system
/// </summary>
public class MonopolyTradeUI : MonoBehaviour
{
    [Header("Trade Panel")]
    [SerializeField] private GameObject tradePanelRoot;
    [SerializeField] private GameObject proposeTradePanel;
    [SerializeField] private GameObject reviewTradePanel;
    
    [Header("Propose Trade UI")]
    [SerializeField] private TMP_Dropdown targetPlayerDropdown;
    [SerializeField] private TMP_InputField offeredMoneyInput;
    [SerializeField] private TMP_InputField requestedMoneyInput;
    [SerializeField] private Transform offeredPropertiesContent;
    [SerializeField] private Transform requestedPropertiesContent;
    [SerializeField] private GameObject propertyTogglePrefab;
    [SerializeField] private Button proposeTradeButton;
    [SerializeField] private Button cancelProposeButton;
    
    [Header("Review Trade UI")]
    [SerializeField] private TextMeshProUGUI tradeDescriptionText;
    [SerializeField] private TextMeshProUGUI proposerOffersText;
    [SerializeField] private TextMeshProUGUI targetOffersText;
    [SerializeField] private Button acceptTradeButton;
    [SerializeField] private Button rejectTradeButton;
    
    [Header("Trade List Button")]
    [SerializeField] private Button openTradeButton;
    
    private List<Toggle> offeredPropertyToggles = new List<Toggle>();
    private List<Toggle> requestedPropertyToggles = new List<Toggle>();
    private int selectedTargetPlayerId = -1;

    private void Start()
    {
        Debug.Log("?? MonopolyTradeUI Start() called");
        
        // Subscribe to trade events
        MonopolyTradeManager.OnTradeProposed += OnTradeProposed;
        MonopolyTradeManager.OnTradeResponse += OnTradeResponse;
        MonopolyTradeManager.OnTradeCancelled += OnTradeCancelled;
        
        // Setup button listeners
        if (openTradeButton != null)
        {
            Debug.Log("? Open Trade Button found, adding listener");
            openTradeButton.onClick.AddListener(OpenTradePanel);
        }
        else
        {
            Debug.LogError("? Open Trade Button is NULL! Button not assigned in Inspector!");
        }
        
        if (proposeTradeButton != null) proposeTradeButton.onClick.AddListener(OnProposeTradeClicked);
        if (cancelProposeButton != null) cancelProposeButton.onClick.AddListener(CloseTradePanel);
        if (acceptTradeButton != null) acceptTradeButton.onClick.AddListener(OnAcceptTradeClicked);
        if (rejectTradeButton != null) rejectTradeButton.onClick.AddListener(OnRejectTradeClicked);
        
        // Hide panels initially
        if (tradePanelRoot != null) tradePanelRoot.SetActive(false);
        if (proposeTradePanel != null) proposeTradePanel.SetActive(false);
        if (reviewTradePanel != null) reviewTradePanel.SetActive(false);
        
        Debug.Log("? MonopolyTradeUI initialization complete");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        MonopolyTradeManager.OnTradeProposed -= OnTradeProposed;
        MonopolyTradeManager.OnTradeResponse -= OnTradeResponse;
        MonopolyTradeManager.OnTradeCancelled -= OnTradeCancelled;
    }

    private void OpenTradePanel()
    {
        Debug.Log("?? TRADE BUTTON PRESSED! OpenTradePanel() called");
        
        if (MonopolyGameManager.Instance == null || MonopolyTradeManager.Instance == null)
        {
            if (MonopolyGameManager.Instance == null)
                Debug.LogError("? MonopolyGameManager.Instance is NULL!");
            if (MonopolyTradeManager.Instance == null)
                Debug.LogError("? MonopolyTradeManager.Instance is NULL!");
            return;
        }
        
        Debug.Log("? Both managers found, checking for active trade...");
        
        // Check if there's an active trade
        if (MonopolyTradeManager.Instance.HasActiveTrade())
        {
            Debug.Log("?? Active trade detected");
            var trade = MonopolyTradeManager.Instance.GetCurrentTrade();
            int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
            
            // Show review panel if this trade involves me
            if (trade.targetPlayerId == myPlayerId)
            {
                Debug.Log($"?? This trade is for me (Player {myPlayerId}), showing review panel");
                ShowReviewPanel(trade);
            }
            else if (trade.proposerId == myPlayerId)
            {
                Debug.Log($"? I proposed this trade (Player {myPlayerId}), showing waiting message");
                ShowTradeInProgress();
            }
            return;
        }
        
        Debug.Log("?? No active trade, showing propose panel...");
        // Reset panel state before showing
        ResetProposePanel();
        // Show propose panel
        ShowProposePanel();
    }

    /// <summary>
    /// Reset the propose trade panel to default state
    /// </summary>
    private void ResetProposePanel()
    {
        Debug.Log("?? Resetting trade panel to default state");
        
        // Clear money input fields
        if (offeredMoneyInput != null) offeredMoneyInput.text = "0";
        if (requestedMoneyInput != null) requestedMoneyInput.text = "0";
        
        // Reset all offered property toggles to false
        foreach (var toggle in offeredPropertyToggles)
        {
            if (toggle != null) toggle.isOn = false;
        }
        
        // Reset all requested property toggles to false
        foreach (var toggle in requestedPropertyToggles)
        {
            if (toggle != null) toggle.isOn = false;
        }
        
        Debug.Log("? Trade panel reset complete");
    }

    private void ShowProposePanel()
    {
        Debug.Log("?? ShowProposePanel() called");
        
        if (tradePanelRoot != null) tradePanelRoot.SetActive(true);
        if (proposeTradePanel != null) proposeTradePanel.SetActive(true);
        if (reviewTradePanel != null) reviewTradePanel.SetActive(false);
        
        Debug.Log($"? Panels activated - Root: {tradePanelRoot?.activeSelf}, Propose: {proposeTradePanel?.activeSelf}, Review: {reviewTradePanel?.activeSelf}");
        
        PopulatePlayerDropdown();
        PopulatePropertyLists();
        
        Debug.Log("? Trade panel should now be visible!");
    }

    private void ShowReviewPanel(MonopolyTradeManager.TradeProposal trade)
    {
        if (tradePanelRoot != null) tradePanelRoot.SetActive(true);
        if (proposeTradePanel != null) proposeTradePanel.SetActive(false);
        if (reviewTradePanel != null) reviewTradePanel.SetActive(true);
        
        // Build trade description
        string description = $"Player {trade.proposerId + 1} wants to trade with you!";
        if (tradeDescriptionText != null) tradeDescriptionText.text = description;
        
        // Show what proposer offers
        string proposerOffers = BuildTradeOfferText(trade.proposerId, trade.proposerProperties, trade.proposerMoney);
        if (proposerOffersText != null) proposerOffersText.text = $"<b>They Offer:</b>\n{proposerOffers}";
        
        // Show what target offers (you)
        string targetOffers = BuildTradeOfferText(trade.targetPlayerId, trade.targetProperties, trade.targetMoney);
        if (targetOffersText != null) targetOffersText.text = $"<b>You Offer:</b>\n{targetOffers}";
    }

    private void ShowTradeInProgress()
    {
        if (tradeDescriptionText != null)
        {
            tradeDescriptionText.text = "Trade proposal sent! Waiting for response...";
        }
    }

    private void CloseTradePanel()
    {
        // Reset panel state when closing
        ResetProposePanel();
        
        if (tradePanelRoot != null) tradePanelRoot.SetActive(false);
        if (proposeTradePanel != null) proposeTradePanel.SetActive(false);
        if (reviewTradePanel != null) reviewTradePanel.SetActive(false);
    }

    private void PopulatePlayerDropdown()
    {
        if (targetPlayerDropdown == null || MonopolyGameManager.Instance == null) return;
        
        targetPlayerDropdown.ClearOptions();
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        int totalPlayers = MonopolyGameManager.Instance.GetTotalPlayers();
        
        List<string> playerNames = new List<string>();
        for (int i = 0; i < totalPlayers; i++)
        {
            if (i != myPlayerId)
            {
                var playerData = MonopolyGameManager.Instance.GetPlayer(i);
                if (!playerData.isBankrupt)
                {
                    playerNames.Add($"Player {i + 1} (${playerData.money})");
                }
            }
        }
        
        targetPlayerDropdown.AddOptions(playerNames);
        
        if (playerNames.Count > 0)
        {
            targetPlayerDropdown.onValueChanged.AddListener(OnTargetPlayerChanged);
            OnTargetPlayerChanged(0);
        }
    }

    private void OnTargetPlayerChanged(int index)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        int totalPlayers = MonopolyGameManager.Instance.GetTotalPlayers();
        
        int currentIndex = 0;
        for (int i = 0; i < totalPlayers; i++)
        {
            if (i != myPlayerId)
            {
                var playerData = MonopolyGameManager.Instance.GetPlayer(i);
                if (!playerData.isBankrupt)
                {
                    if (currentIndex == index)
                    {
                        selectedTargetPlayerId = i;
                        PopulateRequestedProperties(i);
                        return;
                    }
                    currentIndex++;
                }
            }
        }
    }

    private void PopulatePropertyLists()
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        // Clear existing toggles
        foreach (var toggle in offeredPropertyToggles)
        {
            if (toggle != null) Destroy(toggle.gameObject);
        }
        offeredPropertyToggles.Clear();
        
        // Get my properties
        var myProperties = MonopolyGameManager.Instance.GetPlayerProperties(myPlayerId);
        
        Debug.Log($"?? Populating MY properties list. Found {myProperties.Count} properties");
        
        foreach (var ownership in myProperties)
        {
            // Can't trade mortgaged properties or properties with buildings
            if (ownership.isMortgaged || ownership.houseCount > 0 || ownership.hasHotel)
            {
                Debug.Log($"  ?? Skipping property {ownership.propertyId} (mortgaged={ownership.isMortgaged}, houses={ownership.houseCount}, hotel={ownership.hasHotel})");
                continue;
            }
            
            var space = MonopolyGameManager.Instance.GetSpace(ownership.propertyId);
            if (space != null && propertyTogglePrefab != null && offeredPropertiesContent != null)
            {
                GameObject toggleGO = Instantiate(propertyTogglePrefab, offeredPropertiesContent);
                Toggle toggle = toggleGO.GetComponent<Toggle>();
                TextMeshProUGUI label = toggleGO.GetComponentInChildren<TextMeshProUGUI>();
                
                // Set toggle to false initially
                if (toggle != null)
                {
                    toggle.isOn = false;
                }
                
                if (label != null)
                {
                    label.text = space.spaceName;
                }
                
                // Store property ID in the toggle's name for later retrieval
                toggleGO.name = $"PropertyToggle_{ownership.propertyId}";
                
                offeredPropertyToggles.Add(toggle);
                Debug.Log($"  ? Added toggle for property {ownership.propertyId}: {space.spaceName} (toggle set to false)");
            }
        }
        
        Debug.Log($"? Total offered property toggles created: {offeredPropertyToggles.Count}");
    }

    private void PopulateRequestedProperties(int targetPlayerId)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        // Clear existing toggles
        foreach (var toggle in requestedPropertyToggles)
        {
            if (toggle != null) Destroy(toggle.gameObject);
        }
        requestedPropertyToggles.Clear();
        
        // Get target player's properties
        var targetProperties = MonopolyGameManager.Instance.GetPlayerProperties(targetPlayerId);
        
        Debug.Log($"?? Populating REQUESTED properties list for Player {targetPlayerId}. Found {targetProperties.Count} properties");
        
        foreach (var ownership in targetProperties)
        {
            // Can't trade mortgaged properties or properties with buildings
            if (ownership.isMortgaged || ownership.houseCount > 0 || ownership.hasHotel)
            {
                Debug.Log($"  ?? Skipping property {ownership.propertyId} (mortgaged={ownership.isMortgaged}, houses={ownership.houseCount}, hotel={ownership.hasHotel})");
                continue;
            }
            
            var space = MonopolyGameManager.Instance.GetSpace(ownership.propertyId);
            if (space != null && propertyTogglePrefab != null && requestedPropertiesContent != null)
            {
                GameObject toggleGO = Instantiate(propertyTogglePrefab, requestedPropertiesContent);
                Toggle toggle = toggleGO.GetComponent<Toggle>();
                TextMeshProUGUI label = toggleGO.GetComponentInChildren<TextMeshProUGUI>();
                
                // Set toggle to false initially
                if (toggle != null)
                {
                    toggle.isOn = false;
                }
                
                if (label != null)
                {
                    label.text = space.spaceName;
                }
                
                // Store property ID in the toggle's name for later retrieval
                toggleGO.name = $"PropertyToggle_{ownership.propertyId}";
                
                requestedPropertyToggles.Add(toggle);
                Debug.Log($"  ? Added toggle for property {ownership.propertyId}: {space.spaceName} (toggle set to false)");
            }
        }
        
        Debug.Log($"? Total requested property toggles created: {requestedPropertyToggles.Count}");
    }

    private void OnProposeTradeClicked()
    {
        Debug.Log("?? Propose Trade button clicked!");
        
        if (MonopolyTradeManager.Instance == null || MonopolyGameManager.Instance == null)
        {
            Debug.LogError("? Managers not found!");
            return;
        }
        
        if (selectedTargetPlayerId < 0)
        {
            Debug.LogError("? No target player selected!");
            return;
        }
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        // Get selected properties
        List<int> offeredProps = GetSelectedProperties(offeredPropertyToggles, myPlayerId);
        List<int> requestedProps = GetSelectedProperties(requestedPropertyToggles, selectedTargetPlayerId);
        
        // Get money amounts
        int offeredMoney = 0;
        int requestedMoney = 0;
        
        if (offeredMoneyInput != null && int.TryParse(offeredMoneyInput.text, out int offered))
        {
            offeredMoney = offered;
        }
        
        if (requestedMoneyInput != null && int.TryParse(requestedMoneyInput.text, out int requested))
        {
            requestedMoney = requested;
        }
        
        // Log the trade details
        Debug.Log($"?? Proposing trade:");
        Debug.Log($"  From: Player {myPlayerId} ? To: Player {selectedTargetPlayerId}");
        Debug.Log($"  My offer: {offeredProps.Count} properties + ${offeredMoney}");
        foreach (int propId in offeredProps)
        {
            var space = MonopolyGameManager.Instance.GetSpace(propId);
            Debug.Log($"    • Property {propId}: {space?.spaceName}");
        }
        Debug.Log($"  My request: {requestedProps.Count} properties + ${requestedMoney}");
        foreach (int propId in requestedProps)
        {
            var space = MonopolyGameManager.Instance.GetSpace(propId);
            Debug.Log($"    • Property {propId}: {space?.spaceName}");
        }
        
        // Propose the trade
        MonopolyTradeManager.Instance.ProposeTrade(selectedTargetPlayerId, offeredProps, requestedProps, offeredMoney, requestedMoney);
        
        CloseTradePanel();
    }

    private List<int> GetSelectedProperties(List<Toggle> toggles, int playerId)
    {
        List<int> selectedProps = new List<int>();
        
        if (toggles == null || toggles.Count == 0)
        {
            Debug.Log($"?? No toggles to check for player {playerId}");
            return selectedProps;
        }
        
        Debug.Log($"?? Checking {toggles.Count} toggles for selected properties (Player {playerId})");
        
        // Extract property ID from each toggle's name
        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i] != null && toggles[i].isOn)
            {
                // Parse property ID from toggle name (format: "PropertyToggle_<propertyId>")
                string toggleName = toggles[i].gameObject.name;
                if (toggleName.StartsWith("PropertyToggle_"))
                {
                    string propertyIdStr = toggleName.Substring("PropertyToggle_".Length);
                    if (int.TryParse(propertyIdStr, out int propertyId))
                    {
                        selectedProps.Add(propertyId);
                        Debug.Log($"  ? Toggle {i} is ON ? Property ID: {propertyId}");
                    }
                    else
                    {
                        Debug.LogError($"  ? Failed to parse property ID from toggle name: {toggleName}");
                    }
                }
                else
                {
                    Debug.LogError($"  ? Toggle {i} has invalid name format: {toggleName}");
                }
            }
            else if (toggles[i] != null)
            {
                Debug.Log($"  ?? Toggle {i} is OFF");
            }
        }
        
        Debug.Log($"?? Selected {selectedProps.Count} properties from {toggles.Count} toggles");
        return selectedProps;
    }

    private string BuildTradeOfferText(int playerId, List<int> properties, int money)
    {
        string text = "";
        
        if (money > 0)
        {
            text += $"• ${money}\n";
        }
        
        if (properties != null && properties.Count > 0)
        {
            foreach (int propId in properties)
            {
                var space = MonopolyGameManager.Instance.GetSpace(propId);
                if (space != null)
                {
                    text += $"• {space.spaceName}\n";
                }
            }
        }
        
        if (string.IsNullOrEmpty(text))
        {
            text = "Nothing";
        }
        
        return text;
    }

    private void OnAcceptTradeClicked()
    {
        if (MonopolyTradeManager.Instance != null)
        {
            MonopolyTradeManager.Instance.AcceptTrade();
        }
        CloseTradePanel();
    }

    private void OnRejectTradeClicked()
    {
        if (MonopolyTradeManager.Instance != null)
        {
            MonopolyTradeManager.Instance.RejectTrade();
        }
        CloseTradePanel();
    }

    private void OnTradeProposed(MonopolyTradeManager.TradeProposal trade)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        // If this trade is for me, show review panel
        if (trade.targetPlayerId == myPlayerId)
        {
            ShowReviewPanel(trade);
        }
    }

    private void OnTradeResponse(bool accepted, string message)
    {
        Debug.Log($"Trade response: {message}");
        CloseTradePanel();
    }

    private void OnTradeCancelled()
    {
        CloseTradePanel();
    }
}

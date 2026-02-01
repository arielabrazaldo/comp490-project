using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI component for the Rule Editor.
/// Provides an interface to configure game rules with toggles, sliders, and input fields.
/// PANELS SHOW/HIDE DYNAMICALLY based on toggle state.
/// CASCADING TOGGLES: Currency enables threshold toggle, threshold toggle enables threshold input.
/// </summary>
public class RuleEditorUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject ruleEditorPanel;
    [SerializeField] private Button closeButton;
    
    [Header("Currency Settings")]
    [SerializeField] private Toggle currencyToggle;
    [SerializeField] private TMP_InputField startingMoneyInput;
    [SerializeField] private TMP_InputField passGoBonusInput;
    [SerializeField] private GameObject currencyDetailsPanel; // Shows when currencyToggle is ON
    
    [Header("Board Settings")]
    [SerializeField] private Toggle separateBoardsToggle;
    [SerializeField] private TextMeshProUGUI boardModeLabel;
    
    [Header("Visibility Settings")]
    [SerializeField] private Toggle enemyVisibilityToggle;
    [SerializeField] private Slider visibilityRangeSlider;
    [SerializeField] private TextMeshProUGUI visibilityRangeLabel;
    [SerializeField] private GameObject visibilityDetailsPanel; // Shows when enemyVisibilityToggle is ON
    
    [Header("Player Settings")]
    [SerializeField] private TMP_InputField minPlayersInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    
    [Header("Dice Mechanics")]
    [SerializeField] private Toggle customDiceToggle;
    [SerializeField] private TMP_InputField numberOfDiceInput;
    [SerializeField] private TMP_InputField diceSidesInput;
    [SerializeField] private Toggle duplicatesExtraTurnToggle;
    [SerializeField] private TMP_InputField duplicatesRequiredInput;
    [SerializeField] private GameObject diceDetailsPanel; // Shows when customDiceToggle is ON
    
    [Header("Resource System")]
    [SerializeField] private Toggle resourcesToggle;
    [SerializeField] private TMP_InputField numberOfResourcesInput;
    [SerializeField] private Transform resourceNamesContainer; // Container for dynamically created resource name inputs
    [SerializeField] private GameObject resourceNameInputPrefab; // Prefab for resource name input field
    [SerializeField] private Toggle resourceCapToggle;
    [SerializeField] private TMP_InputField maxResourcesInput;
    [SerializeField] private GameObject resourceDetailsPanel; // Shows when resourcesToggle is ON
    
    [Header("Victory Conditions")]
    [SerializeField] private Toggle lastPlayerStandingToggle;
    [SerializeField] private Toggle moneyThresholdToggle;
    [SerializeField] private TMP_InputField winningMoneyInput;
    [SerializeField] private GameObject moneyThresholdPanel; // Shows when moneyThresholdToggle is ON
    
    [Header("Advanced Settings")]
    [SerializeField] private Toggle allowBankruptcyToggle;
    [SerializeField] private Toggle allowTradingToggle;
    
    [Header("Presets")]
    [SerializeField] private Button monopolyPresetButton;
    [SerializeField] private Button battleshipsPresetButton;
    [SerializeField] private Button customPresetButton;
    
    [Header("Action Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Standalone Mode")]
    [SerializeField] private bool standaloneMode = false; // Changed to false - panel hidden by default
    
    private GameRules currentRules;
    private bool isInitializing = false;

    private void Awake()
    {
        // Initialize with default rules (all toggles OFF)
        currentRules = CreateDefaultOffRules();
        
        SetupEventListeners();
    }

    private void Start()
    {
        // Hide all detail panels initially
        HideAllDetailPanels();
        
        LoadRulesIntoUI();
        
        // CRITICAL: Hide the main panel on start (unless standalone mode is enabled)
        if (ruleEditorPanel != null)
        {
            if (standaloneMode)
            {
                // STANDALONE MODE: Show panel automatically for testing
                Debug.Log("[RuleEditorUI] STANDALONE MODE ENABLED: Showing rule editor panel automatically");
                ruleEditorPanel.SetActive(true);
                ActivateMainUIElements();
            }
            else
            {
                // NORMAL MODE: Hide panel until "Create Custom Game" is clicked
                Debug.Log("[RuleEditorUI] NORMAL MODE: Hiding rule editor panel (will show when Create button clicked)");
                ruleEditorPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[RuleEditorUI] ruleEditorPanel is NULL! Please assign it in the Inspector.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to rule changes
        if (RuleEditorManager.Instance != null)
        {
            RuleEditorManager.OnRulesChanged += OnRulesChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from rule changes
        if (RuleEditorManager.Instance != null)
        {
            RuleEditorManager.OnRulesChanged -= OnRulesChanged;
        }
    }

    #region Setup

    /// <summary>
    /// Creates default rules with all toggles OFF
    /// </summary>
    private GameRules CreateDefaultOffRules()
    {
        return new GameRules
        {
            // All toggles OFF by default
            enableCurrency = false,
            startingMoney = 1500,
            passGoBonus = 200,
            separatePlayerBoards = false,
            canSeeEnemyTokens = false,
            enemyTokenVisibilityRange = -1,
            minPlayers = 2,
            maxPlayers = 4,
            enableCustomDice = false,
            numberOfDice = 1,
            diceSides = 6,
            duplicatesGrantExtraTurn = false,
            duplicatesRequired = 2,
            enableResources = false,
            numberOfResources = 0,
            resourceNames = new string[0],
            enableResourceCap = false,
            maxResourcesPerType = 10,
            lastPlayerStandingWins = false,
            moneyThresholdWins = false,
            winningMoneyThreshold = 5000,
            allowBankruptcy = false,
            allowTrading = false
        };
    }

    /// <summary>
    /// Hide all detail panels on startup
    /// </summary>
    private void HideAllDetailPanels()
    {
        if (currencyDetailsPanel != null)
        {
            currencyDetailsPanel.SetActive(false);
        }
        
        if (visibilityDetailsPanel != null)
        {
            visibilityDetailsPanel.SetActive(false);
        }
        
        if (moneyThresholdPanel != null)
        {
            moneyThresholdPanel.SetActive(false);
        }
        
        if (diceDetailsPanel != null)
        {
            diceDetailsPanel.SetActive(false);
        }
        
        if (resourceDetailsPanel != null)
        {
            resourceDetailsPanel.SetActive(false);
        }
        
        Debug.Log("[RuleEditorUI] All detail panels hidden (will show dynamically)");
    }

    private void SetupEventListeners()
    {
        // Close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        
        // Currency
        if (currencyToggle != null)
        {
            currencyToggle.onValueChanged.AddListener(OnCurrencyToggleChanged);
        }
        
        if (startingMoneyInput != null)
        {
            startingMoneyInput.onEndEdit.AddListener(OnStartingMoneyChanged);
        }
        
        if (passGoBonusInput != null)
        {
            passGoBonusInput.onEndEdit.AddListener(OnPassGoBonusChanged);
        }
        
        // Board settings
        if (separateBoardsToggle != null)
        {
            separateBoardsToggle.onValueChanged.AddListener(OnSeparateBoardsChanged);
        }
        
        // Visibility
        if (enemyVisibilityToggle != null)
        {
            enemyVisibilityToggle.onValueChanged.AddListener(OnEnemyVisibilityToggleChanged);
        }
        
        if (visibilityRangeSlider != null)
        {
            visibilityRangeSlider.onValueChanged.AddListener(OnVisibilityRangeChanged);
        }
        
        // Player settings
        if (minPlayersInput != null)
        {
            minPlayersInput.onEndEdit.AddListener(OnMinPlayersChanged);
        }
        
        if (maxPlayersInput != null)
        {
            maxPlayersInput.onEndEdit.AddListener(OnMaxPlayersChanged);
        }
        
        // Dice mechanics
        if (customDiceToggle != null)
        {
            customDiceToggle.onValueChanged.AddListener(OnCustomDiceToggleChanged);
        }
        
        if (numberOfDiceInput != null)
        {
            numberOfDiceInput.onEndEdit.AddListener(OnNumberOfDiceChanged);
        }
        
        if (diceSidesInput != null)
        {
            diceSidesInput.onEndEdit.AddListener(OnDiceSidesChanged);
        }
        
        if (duplicatesExtraTurnToggle != null)
        {
            duplicatesExtraTurnToggle.onValueChanged.AddListener(OnDuplicatesExtraTurnToggleChanged);
        }
        
        if (duplicatesRequiredInput != null)
        {
            duplicatesRequiredInput.onEndEdit.AddListener(OnDuplicatesRequiredChanged);
        }
        
        // Resource system
        if (resourcesToggle != null)
        {
            resourcesToggle.onValueChanged.AddListener(OnResourcesToggleChanged);
        }
        
        if (numberOfResourcesInput != null)
        {
            numberOfResourcesInput.onEndEdit.AddListener(OnNumberOfResourcesChanged);
        }
        
        if (resourceCapToggle != null)
        {
            resourceCapToggle.onValueChanged.AddListener(OnResourceCapToggleChanged);
        }
        
        if (maxResourcesInput != null)
        {
            maxResourcesInput.onEndEdit.AddListener(OnMaxResourcesChanged);
        }
        
        // Victory conditions
        if (lastPlayerStandingToggle != null)
        {
            lastPlayerStandingToggle.onValueChanged.AddListener(OnLastPlayerStandingChanged);
        }
        
        if (moneyThresholdToggle != null)
        {
            moneyThresholdToggle.onValueChanged.AddListener(OnMoneyThresholdToggleChanged);
        }
        
        if (winningMoneyInput != null)
        {
            winningMoneyInput.onEndEdit.AddListener(OnWinningMoneyChanged);
        }
        
        // Advanced settings
        if (allowBankruptcyToggle != null)
        {
            allowBankruptcyToggle.onValueChanged.AddListener(OnAllowBankruptcyChanged);
        }
        
        if (allowTradingToggle != null)
        {
            allowTradingToggle.onValueChanged.AddListener(OnAllowTradingChanged);
        }
        
        // Presets
        if (monopolyPresetButton != null)
        {
            monopolyPresetButton.onClick.AddListener(() => LoadPreset(RuleEditorManager.RulePreset.Monopoly));
        }
        
        if (battleshipsPresetButton != null)
        {
            battleshipsPresetButton.onClick.AddListener(() => LoadPreset(RuleEditorManager.RulePreset.Battleships));
        }
        
        if (customPresetButton != null)
        {
            customPresetButton.onClick.AddListener(() => LoadPreset(RuleEditorManager.RulePreset.Custom));
        }
        
        // Action buttons
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyRules);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaults);
        }
    }
    
    /// <summary>
    /// Activate main UI elements (toggles, buttons) but NOT detail panels
    /// Detail panels show/hide dynamically based on toggle state
    /// </summary>
    private void ActivateMainUIElements()
    {
        Debug.Log("[RuleEditorUI] Activating main UI elements (toggles, buttons)");
        
        // Activate main panel
        if (ruleEditorPanel != null)
        {
            ruleEditorPanel.SetActive(true);
        }
        
        // Ensure all toggles are active
        ActivateGameObject(currencyToggle);
        ActivateGameObject(separateBoardsToggle);
        ActivateGameObject(enemyVisibilityToggle);
        ActivateGameObject(customDiceToggle);
        ActivateGameObject(duplicatesExtraTurnToggle);
        ActivateGameObject(resourcesToggle);
        ActivateGameObject(resourceCapToggle);
        ActivateGameObject(lastPlayerStandingToggle);
        ActivateGameObject(moneyThresholdToggle);
        ActivateGameObject(allowBankruptcyToggle);
        ActivateGameObject(allowTradingToggle);
        
        // Ensure all input fields are active
        ActivateGameObject(startingMoneyInput);
        ActivateGameObject(passGoBonusInput);
        ActivateGameObject(minPlayersInput);
        ActivateGameObject(maxPlayersInput);
        ActivateGameObject(numberOfDiceInput);
        ActivateGameObject(diceSidesInput);
        ActivateGameObject(duplicatesRequiredInput);
        ActivateGameObject(numberOfResourcesInput);
        ActivateGameObject(maxResourcesInput);
        ActivateGameObject(winningMoneyInput);
        
        // Ensure slider is active
        ActivateGameObject(visibilityRangeSlider);
        
        // Ensure all buttons are active
        ActivateGameObject(closeButton);
        ActivateGameObject(monopolyPresetButton);
        ActivateGameObject(battleshipsPresetButton);
        ActivateGameObject(customPresetButton);
        ActivateGameObject(applyButton);
        ActivateGameObject(resetButton);
        
        // Ensure all labels are active
        ActivateGameObject(boardModeLabel);
        ActivateGameObject(visibilityRangeLabel);
        ActivateGameObject(statusText);
        
        Debug.Log("[RuleEditorUI] Main UI elements activated (detail panels hidden by default)");
    }
    
    /// <summary>
    /// Helper to activate a component's GameObject
    /// </summary>
    private void ActivateGameObject(Component component)
    {
        if (component != null && component.gameObject != null)
        {
            component.gameObject.SetActive(true);
        }
    }

    #endregion

    #region UI Loading

    private void LoadRulesIntoUI()
    {
        isInitializing = true;
        
        // Currency
        if (currencyToggle != null)
        {
            currencyToggle.isOn = currentRules.enableCurrency;
        }
        
        if (startingMoneyInput != null)
        {
            startingMoneyInput.text = currentRules.startingMoney.ToString();
        }
        
        if (passGoBonusInput != null)
        {
            passGoBonusInput.text = currentRules.passGoBonus.ToString();
        }
        
        UpdateCurrencyPanel();
        
        // Board settings
        if (separateBoardsToggle != null)
        {
            separateBoardsToggle.isOn = currentRules.separatePlayerBoards;
        }
        
        UpdateBoardModeLabel();
        
        // Visibility
        if (enemyVisibilityToggle != null)
        {
            enemyVisibilityToggle.isOn = currentRules.canSeeEnemyTokens;
        }
        
        if (visibilityRangeSlider != null)
        {
            // Slider: 0 = unlimited (-1), 1-20 = range
            if (currentRules.enemyTokenVisibilityRange == -1)
            {
                visibilityRangeSlider.value = 0;
            }
            else
            {
                visibilityRangeSlider.value = Mathf.Clamp(currentRules.enemyTokenVisibilityRange, 1, 20);
            }
        }
        
        UpdateVisibilityPanel();
        UpdateVisibilityRangeLabel();
        
        // Player settings
        if (minPlayersInput != null)
        {
            minPlayersInput.text = currentRules.minPlayers.ToString();
        }
        
        if (maxPlayersInput != null)
        {
            maxPlayersInput.text = currentRules.maxPlayers.ToString();
        }
        
        // Dice mechanics
        if (customDiceToggle != null)
        {
            customDiceToggle.isOn = currentRules.enableCustomDice;
        }
        
        if (numberOfDiceInput != null)
        {
            numberOfDiceInput.text = currentRules.numberOfDice.ToString();
        }
        
        if (diceSidesInput != null)
        {
            diceSidesInput.text = currentRules.diceSides.ToString();
        }
        
        if (duplicatesExtraTurnToggle != null)
        {
            duplicatesExtraTurnToggle.isOn = currentRules.duplicatesGrantExtraTurn;
        }
        
        if (duplicatesRequiredInput != null)
        {
            duplicatesRequiredInput.text = currentRules.duplicatesRequired.ToString();
        }
        
        UpdateDicePanel();
        UpdateDuplicatesInteractability();
        
        // Resource system
        if (resourcesToggle != null)
        {
            resourcesToggle.isOn = currentRules.enableResources;
        }
        
        if (numberOfResourcesInput != null)
        {
            numberOfResourcesInput.text = currentRules.numberOfResources.ToString();
        }
        
        if (resourceCapToggle != null)
        {
            resourceCapToggle.isOn = currentRules.enableResourceCap;
        }
        
        if (maxResourcesInput != null)
        {
            maxResourcesInput.text = currentRules.maxResourcesPerType.ToString();
        }
        
        UpdateResourcePanel();
        UpdateResourceCapInteractability();
        RegenerateResourceNameInputs();
        
        // Victory conditions
        if (lastPlayerStandingToggle != null)
        {
            lastPlayerStandingToggle.isOn = currentRules.lastPlayerStandingWins;
        }
        
        if (moneyThresholdToggle != null)
        {
            moneyThresholdToggle.isOn = currentRules.moneyThresholdWins;
        }
        
        if (winningMoneyInput != null)
        {
            winningMoneyInput.text = currentRules.winningMoneyThreshold.ToString();
        }
        
        // Update cascading toggle states
        UpdateMoneyThresholdInteractability();
        UpdateMoneyThresholdPanel();
        
        // Advanced settings
        if (allowBankruptcyToggle != null)
        {
            allowBankruptcyToggle.isOn = currentRules.allowBankruptcy;
        }
        
        if (allowTradingToggle != null)
        {
            allowTradingToggle.isOn = currentRules.allowTrading;
        }
        
        isInitializing = false;
    }

    #endregion

    #region Event Handlers

    private void OnCurrencyToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.enableCurrency = value;
        UpdateCurrencyPanel(); // Show/hide panel dynamically
        UpdateMoneyThresholdInteractability(); // Enable/disable money threshold toggle
        UpdateMoneyThresholdPanel(); // Money threshold requires currency
        
        string status = value ? "Currency enabled - money threshold toggle enabled" : "Currency disabled - money threshold toggle disabled";
        UpdateStatus(status);
    }
    
    private void OnStartingMoneyChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int money))
        {
            currentRules.startingMoney = Mathf.Max(0, money);
            UpdateStatus($"Starting money: ${currentRules.startingMoney}");
        }
    }
    
    private void OnPassGoBonusChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int bonus))
        {
            currentRules.passGoBonus = Mathf.Max(0, bonus);
            UpdateStatus($"Pass GO bonus: ${currentRules.passGoBonus}");
        }
    }
    
    private void OnSeparateBoardsChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.separatePlayerBoards = value;
        UpdateBoardModeLabel();
        UpdateStatus(value ? "Separate player boards (Battleships-style)" : "Shared board (Monopoly-style)");
    }
    
    private void OnEnemyVisibilityToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.canSeeEnemyTokens = value;
        UpdateVisibilityPanel(); // Show/hide panel dynamically
        
        string status = value ? "Enemy tokens visible - range panel shown" : "Fog of War enabled - panel hidden";
        UpdateStatus(status);
    }
    
    private void OnVisibilityRangeChanged(float value)
    {
        if (isInitializing) return;
        
        // Slider: 0 = unlimited, 1-20 = range
        if (value == 0)
        {
            currentRules.enemyTokenVisibilityRange = -1;
        }
        else
        {
            currentRules.enemyTokenVisibilityRange = Mathf.RoundToInt(value);
        }
        
        UpdateVisibilityRangeLabel();
    }
    
    private void OnMinPlayersChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int min))
        {
            currentRules.minPlayers = Mathf.Max(1, min);
            if (currentRules.maxPlayers < currentRules.minPlayers)
            {
                currentRules.maxPlayers = currentRules.minPlayers;
                if (maxPlayersInput != null)
                {
                    maxPlayersInput.text = currentRules.maxPlayers.ToString();
                }
            }
            UpdateStatus($"Min players: {currentRules.minPlayers}");
        }
    }
    
    private void OnMaxPlayersChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int max))
        {
            currentRules.maxPlayers = Mathf.Max(currentRules.minPlayers, max);
            UpdateStatus($"Max players: {currentRules.maxPlayers}");
        }
    }
    
    private void OnAllowBankruptcyChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.allowBankruptcy = value;
        UpdateStatus($"Bankruptcy: {(value ? "Allowed" : "Disabled")}");
    }
    
    private void OnAllowTradingChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.allowTrading = value;
        UpdateStatus($"Trading: {(value ? "Allowed" : "Disabled")}");
    }
    
    // Victory Conditions Event Handlers
    
    private void OnLastPlayerStandingChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.lastPlayerStandingWins = value;
        UpdateStatus($"Last player standing: {(value ? "ON" : "OFF")}");
    }
    
    private void OnMoneyThresholdToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.moneyThresholdWins = value;
        UpdateMoneyThresholdPanel(); // Show/hide panel and update input interactability
        
        string status = value ? "Money threshold victory enabled - threshold input enabled" : "Money threshold victory disabled - threshold input disabled";
        UpdateStatus(status);
    }
    
    private void OnWinningMoneyChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int money))
        {
            currentRules.winningMoneyThreshold = Mathf.Max(0, money);
            UpdateStatus($"Win threshold: ${currentRules.winningMoneyThreshold}");
        }
    }

    // Dice Mechanics Event Handlers
    
    private void OnCustomDiceToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.enableCustomDice = value;
        UpdateDicePanel();
        
        string status = value ? "Custom dice enabled - dice panel shown" : "Custom dice disabled - dice panel hidden";
        UpdateStatus(status);
    }
    
    private void OnNumberOfDiceChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int dice))
        {
            currentRules.numberOfDice = Mathf.Max(1, dice);
            if (numberOfDiceInput != null)
            {
                numberOfDiceInput.text = currentRules.numberOfDice.ToString();
            }
            UpdateStatus($"Number of dice: {currentRules.numberOfDice}");
        }
    }
    
    private void OnDiceSidesChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int sides))
        {
            currentRules.diceSides = Mathf.Max(2, sides);
            if (diceSidesInput != null)
            {
                diceSidesInput.text = currentRules.diceSides.ToString();
            }
            UpdateStatus($"Dice sides: {currentRules.diceSides}");
        }
    }
    
    private void OnDuplicatesExtraTurnToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.duplicatesGrantExtraTurn = value;
        UpdateDuplicatesInteractability();
        
        string status = value ? "Duplicates grant extra turn - duplicates input enabled" : "Duplicates disabled - duplicates input disabled";
        UpdateStatus(status);
    }
    
    private void OnDuplicatesRequiredChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int required))
        {
            currentRules.duplicatesRequired = Mathf.Max(2, required);
            if (duplicatesRequiredInput != null)
            {
                duplicatesRequiredInput.text = currentRules.duplicatesRequired.ToString();
            }
            UpdateStatus($"Duplicates required: {currentRules.duplicatesRequired}");
        }
    }
    
    // Resource System Event Handlers
    
    private void OnResourcesToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.enableResources = value;
        UpdateResourcePanel();
        
        string status = value ? "Resources enabled - resource panel shown" : "Resources disabled - resource panel hidden";
        UpdateStatus(status);
    }
    
    private void OnNumberOfResourcesChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int count))
        {
            count = Mathf.Max(0, count);
            currentRules.numberOfResources = count;
            
            // Resize resource names array
            if (currentRules.resourceNames == null || currentRules.resourceNames.Length != count)
            {
                string[] newNames = new string[count];
                if (currentRules.resourceNames != null)
                {
                    // Copy existing names
                    for (int i = 0; i < Mathf.Min(currentRules.resourceNames.Length, count); i++)
                    {
                        newNames[i] = currentRules.resourceNames[i];
                    }
                }
                // Fill new slots with default names
                for (int i = currentRules.resourceNames?.Length ?? 0; i < count; i++)
                {
                    newNames[i] = $"Resource {i + 1}";
                }
                currentRules.resourceNames = newNames;
            }
            
            if (numberOfResourcesInput != null)
            {
                numberOfResourcesInput.text = count.ToString();
            }
            
            RegenerateResourceNameInputs();
            UpdateStatus($"Number of resources: {count}");
        }
    }
    
    private void OnResourceCapToggleChanged(bool value)
    {
        if (isInitializing) return;
        
        currentRules.enableResourceCap = value;
        UpdateResourceCapInteractability();
        
        string status = value ? "Resource cap enabled - max resources input enabled" : "Resource cap disabled - max resources input disabled";
        UpdateStatus(status);
    }
    
    private void OnMaxResourcesChanged(string value)
    {
        if (isInitializing) return;
        
        if (int.TryParse(value, out int max))
        {
            currentRules.maxResourcesPerType = Mathf.Max(1, max);
            if (maxResourcesInput != null)
            {
                maxResourcesInput.text = currentRules.maxResourcesPerType.ToString();
            }
            UpdateStatus($"Max resources per type: {currentRules.maxResourcesPerType}");
        }
    }

    #endregion

    #region Panel Updates (Dynamic Show/Hide)

    /// <summary>
    /// DYNAMIC: Show currency panel ONLY when toggle is ON
    /// </summary>
    private void UpdateCurrencyPanel()
    {
        if (currencyDetailsPanel != null)
        {
            bool shouldShow = currentRules.enableCurrency;
            currencyDetailsPanel.SetActive(shouldShow);
            
            Debug.Log($"[RuleEditorUI] Currency panel: {(shouldShow ? "SHOWN" : "HIDDEN")}");
        }
    }
    
    private void UpdateBoardModeLabel()
    {
        if (boardModeLabel != null)
        {
            string mode = currentRules.separatePlayerBoards ? "Separate Boards (Battleships-style)" : "Shared Board (Monopoly-style)";
            boardModeLabel.text = mode;
        }
    }
    
    /// <summary>
    /// DYNAMIC: Show visibility panel ONLY when toggle is ON
    /// </summary>
    private void UpdateVisibilityPanel()
    {
        if (visibilityDetailsPanel != null)
        {
            bool shouldShow = currentRules.canSeeEnemyTokens;
            visibilityDetailsPanel.SetActive(shouldShow);
            
            Debug.Log($"[RuleEditorUI] Visibility panel: {(shouldShow ? "SHOWN" : "HIDDEN")}");
        }
    }
    
    private void UpdateVisibilityRangeLabel()
    {
        if (visibilityRangeLabel != null)
        {
            if (currentRules.enemyTokenVisibilityRange == -1)
            {
                visibilityRangeLabel.text = "Range: Unlimited";
            }
            else
            {
                visibilityRangeLabel.text = $"Range: {currentRules.enemyTokenVisibilityRange} tiles";
            }
        }
    }
    
    /// <summary>
    /// CASCADING: Currency toggle enables/disables money threshold toggle
    /// </summary>
    private void UpdateMoneyThresholdInteractability()
    {
        if (moneyThresholdToggle != null)
        {
            bool shouldEnable = currentRules.enableCurrency;
            moneyThresholdToggle.interactable = shouldEnable;
            
            // If currency is disabled, turn off money threshold
            if (!shouldEnable && moneyThresholdToggle.isOn)
            {
                moneyThresholdToggle.isOn = false;
                currentRules.moneyThresholdWins = false;
            }
            
            Debug.Log($"[RuleEditorUI] Money threshold toggle: {(shouldEnable ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// DYNAMIC: Show money threshold panel ONLY when toggle is ON AND currency is enabled
    /// CASCADING: Threshold input field is enabled ONLY when money threshold toggle is ON
    /// </summary>
    private void UpdateMoneyThresholdPanel()
    {
        if (moneyThresholdPanel != null)
        {
            bool shouldShow = currentRules.moneyThresholdWins && currentRules.enableCurrency;
            moneyThresholdPanel.SetActive(shouldShow);
            
            Debug.Log($"[RuleEditorUI] Money threshold panel: {(shouldShow ? "SHOWN" : "HIDDEN")}");
        }
        
        // Update input field interactability
        if (winningMoneyInput != null)
        {
            bool shouldEnableInput = currentRules.moneyThresholdWins && currentRules.enableCurrency;
            winningMoneyInput.interactable = shouldEnableInput;
            
            Debug.Log($"[RuleEditorUI] Threshold input field: {(shouldEnableInput ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// DYNAMIC: Show dice panel ONLY when toggle is ON
    /// </summary>
    private void UpdateDicePanel()
    {
        if (diceDetailsPanel != null)
        {
            bool shouldShow = currentRules.enableCustomDice;
            diceDetailsPanel.SetActive(shouldShow);
            
            Debug.Log($"[RuleEditorUI] Dice panel: {(shouldShow ? "SHOWN" : "HIDDEN")}");
        }
        
        // Update input field interactability
        if (numberOfDiceInput != null)
        {
            numberOfDiceInput.interactable = currentRules.enableCustomDice;
        }
        
        if (diceSidesInput != null)
        {
            diceSidesInput.interactable = currentRules.enableCustomDice;
        }
        
        if (duplicatesExtraTurnToggle != null)
        {
            duplicatesExtraTurnToggle.interactable = currentRules.enableCustomDice;
        }
        
        UpdateDuplicatesInteractability();
    }
    
    /// <summary>
    /// CASCADING: Duplicates toggle enables/disables duplicates required input
    /// </summary>
    private void UpdateDuplicatesInteractability()
    {
        if (duplicatesRequiredInput != null)
        {
            bool shouldEnable = currentRules.enableCustomDice && currentRules.duplicatesGrantExtraTurn;
            duplicatesRequiredInput.interactable = shouldEnable;
            
            Debug.Log($"[RuleEditorUI] Duplicates required input: {(shouldEnable ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// DYNAMIC: Show resource panel ONLY when toggle is ON
    /// </summary>
    private void UpdateResourcePanel()
    {
        if (resourceDetailsPanel != null)
        {
            bool shouldShow = currentRules.enableResources;
            resourceDetailsPanel.SetActive(shouldShow);
            
            Debug.Log($"[RuleEditorUI] Resource panel: {(shouldShow ? "SHOWN" : "HIDDEN")}");
        }
        
        // Update input field interactability
        if (numberOfResourcesInput != null)
        {
            numberOfResourcesInput.interactable = currentRules.enableResources;
        }
        
        if (resourceCapToggle != null)
        {
            resourceCapToggle.interactable = currentRules.enableResources;
        }
        
        UpdateResourceCapInteractability();
    }
    
    /// <summary>
    /// CASCADING: Resource cap toggle enables/disables max resources input
    /// </summary>
    private void UpdateResourceCapInteractability()
    {
        if (maxResourcesInput != null)
        {
            bool shouldEnable = currentRules.enableResources && currentRules.enableResourceCap;
            maxResourcesInput.interactable = shouldEnable;
            
            Debug.Log($"[RuleEditorUI] Max resources input: {(shouldEnable ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// Dynamically generates resource name input fields based on numberOfResources
    /// </summary>
    private void RegenerateResourceNameInputs()
    {
        if (resourceNamesContainer == null || resourceNameInputPrefab == null)
        {
            Debug.LogWarning("[RuleEditorUI] ResourceNamesContainer or prefab is null");
            return;
        }
        
        Debug.Log($"[RuleEditorUI] Regenerating resource name inputs for {currentRules.numberOfResources} resources");
        
        // Clear existing inputs
        foreach (Transform child in resourceNamesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Wait for destruction to complete, then create new inputs
        StartCoroutine(CreateResourceInputsAfterClear());
    }
    
    /// <summary>
    /// Creates resource input fields after old ones are destroyed (prevents layout issues)
    /// </summary>
    private System.Collections.IEnumerator CreateResourceInputsAfterClear()
    {
        // Wait one frame for Destroy() to take effect
        yield return null;
        
        // Create new inputs based on count
        if (currentRules.enableResources && currentRules.numberOfResources > 0)
        {
            for (int i = 0; i < currentRules.numberOfResources; i++)
            {
                GameObject inputObj = Instantiate(resourceNameInputPrefab, resourceNamesContainer);
                TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
                
                if (inputField != null)
                {
                    // Set placeholder text
                    if (inputField.placeholder is TextMeshProUGUI placeholder)
                    {
                        placeholder.text = $"Resource {i + 1} Name";
                    }
                    
                    // Set current value
                    if (currentRules.resourceNames != null && i < currentRules.resourceNames.Length)
                    {
                        inputField.text = currentRules.resourceNames[i];
                    }
                    
                    // Add listener (capture index in closure)
                    int index = i;
                    inputField.onEndEdit.AddListener((value) => OnResourceNameChanged(index, value));
                }
            }
            
            // Wait another frame for instantiation to complete
            yield return null;
            
            // Force layout rebuild after adding all inputs
            Debug.Log("[RuleEditorUI] Forcing layout rebuild for resource inputs");
            
            // Rebuild from bottom up
            Canvas.ForceUpdateCanvases();
            
            if (resourceNamesContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(resourceNamesContainer as RectTransform);
                Debug.Log($"[RuleEditorUI] ResourceNamesContainer size: {(resourceNamesContainer as RectTransform).rect.height}");
            }
            
            // Rebuild parent panel
            if (resourceDetailsPanel != null)
            {
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(resourceDetailsPanel.transform as RectTransform);
                Debug.Log($"[RuleEditorUI] ResourceDetailsPanel size: {(resourceDetailsPanel.transform as RectTransform).rect.height}");
            }
            
            // Rebuild scroll view content if it exists
            Transform scrollContent = FindScrollViewContent();
            if (scrollContent != null)
            {
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent as RectTransform);
                Debug.Log("[RuleEditorUI] Rebuilt scroll view content");
            }
        }
        
        Debug.Log($"[RuleEditorUI] Successfully created {currentRules.numberOfResources} resource name inputs");
    }
    
    /// <summary>
    /// Finds the scroll view content panel by searching up the hierarchy
    /// </summary>
    private Transform FindScrollViewContent()
    {
        Transform current = resourceDetailsPanel?.transform;
        
        while (current != null)
        {
            // Look for a GameObject named "Content" that's a child of "Viewport"
            if (current.name == "Content" && current.parent != null && current.parent.name == "Viewport")
            {
                return current;
            }
            current = current.parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// Called when a resource name input is changed
    /// </summary>
    private void OnResourceNameChanged(int index, string value)
    {
        if (isInitializing) return;
        
        if (currentRules.resourceNames != null && index >= 0 && index < currentRules.resourceNames.Length)
        {
            currentRules.resourceNames[index] = value;
            UpdateStatus($"Resource {index + 1} name: {value}");
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            // Auto-clear after 3 seconds
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), 3f);
        }
        
        Debug.Log($"[RuleEditor] {message}");
    }
    
    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    #endregion

    #region Actions

    private void ApplyRules()
    {
        // Validate rules
        if (!currentRules.ValidateRules(out string errorMessage))
        {
            UpdateStatus($"Error: {errorMessage}");
            Debug.LogError($"Invalid rules: {errorMessage}");
            return;
        }
        
        // Apply to manager
        if (RuleEditorManager.Instance != null)
        {
            RuleEditorManager.Instance.SetRules(currentRules);
            UpdateStatus("Rules applied successfully!");
        }
        else
        {
            UpdateStatus("Error: RuleEditorManager not found!");
            Debug.LogError("RuleEditorManager.Instance is null!");
            return;
        }

        // Close rule editor panel
        ClosePanel();

        // Show main menu panel or trigger lobby creation
        // Check for UIManager or UIManager_Streamlined
        UIManager_Streamlined streamlinedUI = FindFirstObjectByType<UIManager_Streamlined>();
        if (streamlinedUI != null)
        {
            // Call the public OnRulesConfigured method which will:
            // 1. Save the game
            // 2. Create lobby
            // 3. Show lobby panel
            streamlinedUI.OnRulesConfigured(currentRules);
            Debug.Log("[RuleEditorUI] Called UIManager_Streamlined.OnRulesConfigured");
        }
        else
        {
            // Fallback to old UIManager
            UIManager oldUI = FindFirstObjectByType<UIManager>();
            if (oldUI != null)
            {
                oldUI.OnRulesConfigured(currentRules);
                Debug.Log("[RuleEditorUI] Called UIManager.OnRulesConfigured");
            }
            else
            {
                Debug.LogError("[RuleEditorUI] No UIManager found! Cannot proceed with rule application.");
                UpdateStatus("Error: No UIManager found!");
            }
        }
    }
    
    private void ResetToDefaults()
    {
        currentRules = CreateDefaultOffRules(); // All toggles OFF
        HideAllDetailPanels(); // Hide all panels
        LoadRulesIntoUI();
        UpdateStatus("Reset to defaults (all toggles OFF)");
    }
    
    private void LoadPreset(RuleEditorManager.RulePreset preset)
    {
        switch (preset)
        {
            case RuleEditorManager.RulePreset.Monopoly:
                currentRules = GameRules.CreateMonopolyRules();
                UpdateStatus("Loaded Monopoly preset");
                break;
                
            case RuleEditorManager.RulePreset.Battleships:
                currentRules = GameRules.CreateBattleshipsRules();
                UpdateStatus("Loaded Battleships preset");
                break;
                
            case RuleEditorManager.RulePreset.Custom:
                currentRules = GameRules.CreateCustomRules();
                UpdateStatus("Loaded Custom preset");
                break;
        }
        
        LoadRulesIntoUI();
    }
    
    private void OnRulesChanged(GameRules newRules)
    {
        currentRules = newRules.Clone();
        LoadRulesIntoUI();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Show the Rule Editor panel (called by RuleEditorManager)
    /// </summary>
    public void Show()
    {
        ShowPanel();
    }

    public void ShowPanel()
    {
        if (ruleEditorPanel != null)
        {
            ruleEditorPanel.SetActive(true);
            ActivateMainUIElements(); // Activate toggles/buttons but not detail panels
            LoadRulesIntoUI(); // This will update panel visibility based on rules
        }
    }
    
    public void ClosePanel()
    {
        if (ruleEditorPanel != null)
        {
            ruleEditorPanel.SetActive(false);
            Debug.Log("[RuleEditorUI] Rule editor panel closed");
        }

        // Show main menu panel when closing
        UIManager_Streamlined streamlinedUI = FindFirstObjectByType<UIManager_Streamlined>();
        if (streamlinedUI != null)
        {
            streamlinedUI.ShowMainMenuPublic();
            Debug.Log("[RuleEditorUI] Returned to main menu");
        }
        else
        {
            // Fallback to old UIManager
            UIManager oldUI = FindFirstObjectByType<UIManager>();
            if (oldUI != null)
            {
                // Call the old UIManager's ShowMainMenu via reflection
                var showMethod = oldUI.GetType().GetMethod("ShowMainMenu",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (showMethod != null)
                {
                    showMethod.Invoke(oldUI, null);
                    Debug.Log("[RuleEditorUI] Returned to main menu (old UIManager)");
                }
            }
            else
            {
                Debug.LogWarning("[RuleEditorUI] No UIManager found to show main menu!");
            }
        }
    }
    
    public void TogglePanel()
    {
        if (ruleEditorPanel != null)
        {
            if (ruleEditorPanel.activeSelf)
            {
                ClosePanel();
            }
            else
            {
                ShowPanel();
            }
        }
    }
    
    /// <summary>
    /// Context menu for testing - activate main UI elements
    /// </summary>
    [ContextMenu("Activate Main UI Elements")]
    public void ActivateMainUIElementsPublic()
    {
        ActivateMainUIElements();
        Debug.Log("[RuleEditorUI] Manual main UI activation triggered");
    }
    
    /// <summary>
    /// Context menu for testing - hide all detail panels
    /// </summary>
    [ContextMenu("Hide All Detail Panels")]
    public void HideAllDetailPanelsPublic()
    {
        HideAllDetailPanels();
        Debug.Log("[RuleEditorUI] Manual panel hiding triggered");
    }

    #endregion
}

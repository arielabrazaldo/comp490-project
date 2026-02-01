using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Manages game rules and synchronizes them across the network.
/// Allows runtime configuration of game rules that affect gameplay.
/// </summary>
public class RuleEditorManager : NetworkBehaviour
{
    private static RuleEditorManager instance;
    public static RuleEditorManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RuleEditorManager>();
            }
            return instance;
        }
    }

    [Header("Current Rules")]
    [SerializeField] private GameRules currentRules;
    
    [Header("Network Sync")]
    private NetworkVariable<bool> networkEnableCurrency = new NetworkVariable<bool>(true);
    private NetworkVariable<int> networkStartingMoney = new NetworkVariable<int>(1500);
    private NetworkVariable<int> networkPassGoBonus = new NetworkVariable<int>(200);
    private NetworkVariable<bool> networkSeparateBoards = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> networkCanSeeEnemies = new NetworkVariable<bool>(true);
    private NetworkVariable<int> networkVisibilityRange = new NetworkVariable<int>(-1);

    // Events
    public static event Action<GameRules> OnRulesChanged;
    public static event Action<bool> OnCurrencyToggled;
    public static event Action<bool> OnSeparateBoardsToggled;
    public static event Action<bool, int> OnEnemyVisibilityChanged;

    public enum RulePreset
    {
        Monopoly,
        Battleships,
        Custom
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
            // CRITICAL FIX: Must be a root GameObject for DontDestroyOnLoad to work
            // BUT: Don't detach if this has a NetworkObject component (it will cause errors)
            if (transform.parent != null && GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                Debug.Log("[RuleEditorManager] Detaching from parent to become root GameObject");
                transform.SetParent(null);
            }
            else if (transform.parent != null)
            {
                Debug.LogWarning("[RuleEditorManager] Has NetworkObject component - cannot detach from parent. DontDestroyOnLoad may not work if nested.");
            }
            
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize with custom rules
        if (currentRules == null)
        {
            currentRules = GameRules.CreateCustomRules();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsServer)
        {
            // Clients subscribe to network variable changes
            networkEnableCurrency.OnValueChanged += OnNetworkCurrencyChanged;
            networkSeparateBoards.OnValueChanged += OnNetworkSeparateBoardsChanged;
            networkCanSeeEnemies.OnValueChanged += OnNetworkEnemyVisibilityChanged;
            
            // Apply initial values from server
            SyncFromNetwork();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            networkEnableCurrency.OnValueChanged -= OnNetworkCurrencyChanged;
            networkSeparateBoards.OnValueChanged -= OnNetworkSeparateBoardsChanged;
            networkCanSeeEnemies.OnValueChanged -= OnNetworkEnemyVisibilityChanged;
        }
        
        base.OnNetworkDespawn();
    }

    #region Public API

    /// <summary>
    /// Sets the current rules. Host only.
    /// </summary>
    public void SetRules(GameRules rules)
    {
        if (!rules.ValidateRules(out string errorMessage))
        {
            Debug.LogError($"Invalid rules: {errorMessage}");
            return;
        }
        
        currentRules = rules.Clone();
        
        if (IsServer)
        {
            SyncToNetwork();
        }
        else if (IsClient)
        {
            RequestSetRulesServerRpc(
                rules.enableCurrency,
                rules.startingMoney,
                rules.passGoBonus,
                rules.separatePlayerBoards,
                rules.canSeeEnemyTokens,
                rules.enemyTokenVisibilityRange
            );
        }
        
        OnRulesChanged?.Invoke(currentRules);
        Debug.Log($"Rules updated: {currentRules.GetRulesSummary()}");
    }
    
    /// <summary>
    /// Gets the current rules
    /// </summary>
    public GameRules GetCurrentRules()
    {
        return currentRules.Clone();
    }
    
    /// <summary>
    /// Loads a preset
    /// </summary>
    public void LoadPreset(RulePreset preset)
    {
        GameRules newRules = preset switch
        {
            RulePreset.Monopoly => GameRules.CreateMonopolyRules(),
            RulePreset.Battleships => GameRules.CreateBattleshipsRules(),
            _ => GameRules.CreateCustomRules()
        };
        
        SetRules(newRules);
    }
    
    /// <summary>
    /// Toggle currency system
    /// </summary>
    public void ToggleCurrency(bool enabled)
    {
        currentRules.enableCurrency = enabled;
        
        if (IsServer)
        {
            networkEnableCurrency.Value = enabled;
        }
        else if (IsClient)
        {
            RequestToggleCurrencyServerRpc(enabled);
        }
        
        OnCurrencyToggled?.Invoke(enabled);
    }
    
    /// <summary>
    /// Toggle separate player boards
    /// </summary>
    public void ToggleSeparateBoards(bool enabled)
    {
        currentRules.separatePlayerBoards = enabled;
        
        if (IsServer)
        {
            networkSeparateBoards.Value = enabled;
        }
        else if (IsClient)
        {
            RequestToggleSeparateBoardsServerRpc(enabled);
        }
        
        OnSeparateBoardsToggled?.Invoke(enabled);
    }
    
    /// <summary>
    /// Set enemy token visibility
    /// </summary>
    public void SetEnemyVisibility(bool canSee, int range = -1)
    {
        currentRules.canSeeEnemyTokens = canSee;
        currentRules.enemyTokenVisibilityRange = range;
        
        if (IsServer)
        {
            networkCanSeeEnemies.Value = canSee;
            networkVisibilityRange.Value = range;
        }
        else if (IsClient)
        {
            RequestSetEnemyVisibilityServerRpc(canSee, range);
        }
        
        OnEnemyVisibilityChanged?.Invoke(canSee, range);
    }

    #endregion

    #region Query Methods

    public bool IsCurrencyEnabled() => currentRules.enableCurrency;
    public int GetStartingMoney() => currentRules.startingMoney;
    public int GetPassGoBonus() => currentRules.passGoBonus;
    public bool UseSeparateBoards() => currentRules.separatePlayerBoards;
    public bool CanSeeEnemyTokens() => currentRules.canSeeEnemyTokens;
    public int GetEnemyTokenVisibilityRange() => currentRules.enemyTokenVisibilityRange;
    public bool AllowsBankruptcy() => currentRules.allowBankruptcy;
    public bool AllowsTrading() => currentRules.allowTrading;

    #endregion

    #region Network Sync

    private void SyncToNetwork()
    {
        networkEnableCurrency.Value = currentRules.enableCurrency;
        networkStartingMoney.Value = currentRules.startingMoney;
        networkPassGoBonus.Value = currentRules.passGoBonus;
        networkSeparateBoards.Value = currentRules.separatePlayerBoards;
        networkCanSeeEnemies.Value = currentRules.canSeeEnemyTokens;
        networkVisibilityRange.Value = currentRules.enemyTokenVisibilityRange;
        
        BroadcastRulesChangedClientRpc(
            currentRules.enableCurrency,
            currentRules.startingMoney,
            currentRules.passGoBonus,
            currentRules.separatePlayerBoards,
            currentRules.canSeeEnemyTokens,
            currentRules.enemyTokenVisibilityRange
        );
    }
    
    private void SyncFromNetwork()
    {
        currentRules.enableCurrency = networkEnableCurrency.Value;
        currentRules.startingMoney = networkStartingMoney.Value;
        currentRules.passGoBonus = networkPassGoBonus.Value;
        currentRules.separatePlayerBoards = networkSeparateBoards.Value;
        currentRules.canSeeEnemyTokens = networkCanSeeEnemies.Value;
        currentRules.enemyTokenVisibilityRange = networkVisibilityRange.Value;
        
        OnRulesChanged?.Invoke(currentRules);
    }

    #endregion

    #region Network Callbacks

    private void OnNetworkCurrencyChanged(bool oldValue, bool newValue)
    {
        currentRules.enableCurrency = newValue;
        OnCurrencyToggled?.Invoke(newValue);
        OnRulesChanged?.Invoke(currentRules);
    }
    
    private void OnNetworkSeparateBoardsChanged(bool oldValue, bool newValue)
    {
        currentRules.separatePlayerBoards = newValue;
        OnSeparateBoardsToggled?.Invoke(newValue);
        OnRulesChanged?.Invoke(currentRules);
    }
    
    private void OnNetworkEnemyVisibilityChanged(bool oldValue, bool newValue)
    {
        currentRules.canSeeEnemyTokens = newValue;
        OnEnemyVisibilityChanged?.Invoke(newValue, currentRules.enemyTokenVisibilityRange);
        OnRulesChanged?.Invoke(currentRules);
    }

    #endregion

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetRulesServerRpc(bool enableCurrency, int startingMoney, int passGoBonus, 
        bool separateBoards, bool canSeeEnemies, int visibilityRange)
    {
        currentRules.enableCurrency = enableCurrency;
        currentRules.startingMoney = startingMoney;
        currentRules.passGoBonus = passGoBonus;
        currentRules.separatePlayerBoards = separateBoards;
        currentRules.canSeeEnemyTokens = canSeeEnemies;
        currentRules.enemyTokenVisibilityRange = visibilityRange;
        
        SyncToNetwork();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestToggleCurrencyServerRpc(bool enabled)
    {
        networkEnableCurrency.Value = enabled;
        currentRules.enableCurrency = enabled;
        OnCurrencyToggled?.Invoke(enabled);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestToggleSeparateBoardsServerRpc(bool enabled)
    {
        networkSeparateBoards.Value = enabled;
        currentRules.separatePlayerBoards = enabled;
        OnSeparateBoardsToggled?.Invoke(enabled);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestSetEnemyVisibilityServerRpc(bool canSee, int range)
    {
        networkCanSeeEnemies.Value = canSee;
        networkVisibilityRange.Value = range;
        currentRules.canSeeEnemyTokens = canSee;
        currentRules.enemyTokenVisibilityRange = range;
        OnEnemyVisibilityChanged?.Invoke(canSee, range);
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void BroadcastRulesChangedClientRpc(bool enableCurrency, int startingMoney, int passGoBonus,
        bool separateBoards, bool canSeeEnemies, int visibilityRange)
    {
        if (IsServer) return; // Server already has the rules
        
        currentRules.enableCurrency = enableCurrency;
        currentRules.startingMoney = startingMoney;
        currentRules.passGoBonus = passGoBonus;
        currentRules.separatePlayerBoards = separateBoards;
        currentRules.canSeeEnemyTokens = canSeeEnemies;
        currentRules.enemyTokenVisibilityRange = visibilityRange;
        
        OnRulesChanged?.Invoke(currentRules);
    }

    #endregion

    #region UI Integration

    /// <summary>
    /// Show the Rule Editor UI
    /// This should be called by UIManager when the player clicks "Create Custom Game"
    /// </summary>
    public void ShowRuleEditor()
    {
        // Find RuleEditorUI in the scene
        RuleEditorUI ruleEditorUI = FindFirstObjectByType<RuleEditorUI>();
        
        if (ruleEditorUI != null)
        {
            ruleEditorUI.Show();
            Debug.Log("[RuleEditorManager] Showing Rule Editor UI");
        }
        else
        {
            Debug.LogError("[RuleEditorManager] RuleEditorUI not found in scene! Please add RuleEditorUI to your scene.");
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Print Current Rules")]
    private void PrintCurrentRules()
    {
        Debug.Log(currentRules.GetRulesSummary());
    }
    
    [ContextMenu("Load Monopoly Preset")]
    private void LoadMonopolyPreset()
    {
        LoadPreset(RulePreset.Monopoly);
    }
    
    [ContextMenu("Load Battleships Preset")]
    private void LoadBattleshipsPreset()
    {
        LoadPreset(RulePreset.Battleships);
    }
    
    [ContextMenu("Load Custom Preset")]
    private void LoadCustomPreset()
    {
        LoadPreset(RulePreset.Custom);
    }

    #endregion
}

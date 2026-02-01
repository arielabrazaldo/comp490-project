using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Helper class to apply GameRules to existing game managers.
/// This bridges the rule editor with Monopoly and Battleships game logic.
/// </summary>
public class RuleApplicationHelper : MonoBehaviour
{
    private static RuleApplicationHelper instance;
    public static RuleApplicationHelper Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RuleApplicationHelper>();
            }
            return instance;
        }
    }

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

    private void OnEnable()
    {
        // Subscribe to rule changes
        RuleEditorManager.OnRulesChanged += OnRulesChanged;
        RuleEditorManager.OnCurrencyToggled += OnCurrencyToggled;
        RuleEditorManager.OnSeparateBoardsToggled += OnSeparateBoardsToggled;
        RuleEditorManager.OnEnemyVisibilityChanged += OnEnemyVisibilityChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from rule changes
        RuleEditorManager.OnRulesChanged -= OnRulesChanged;
        RuleEditorManager.OnCurrencyToggled -= OnCurrencyToggled;
        RuleEditorManager.OnSeparateBoardsToggled -= OnSeparateBoardsToggled;
        RuleEditorManager.OnEnemyVisibilityChanged -= OnEnemyVisibilityChanged;
    }

    #region Rule Application

    /// <summary>
    /// Apply rules to the appropriate game manager based on current game mode
    /// </summary>
    private void OnRulesChanged(GameRules rules)
    {
        Debug.Log($"[RuleApplicationHelper] Applying new rules: {rules.GetRulesSummary()}");
        
        // Apply to Monopoly if it exists
        if (MonopolyGameManager.Instance != null)
        {
            ApplyRulesToMonopoly(rules);
        }
        
        // Apply to Battleships if it exists
        if (BattleshipsGameManager.Instance != null)
        {
            ApplyRulesToBattleships(rules);
        }
    }
    
    /// <summary>
    /// Apply rules to Monopoly game manager
    /// </summary>
    private void ApplyRulesToMonopoly(GameRules rules)
    {
        Debug.Log("[RuleApplicationHelper] Applying rules to Monopoly");
        
        // Currency is core to Monopoly, so just log if disabled
        if (!rules.enableCurrency)
        {
            Debug.LogWarning("Currency disabled - Monopoly may not function correctly");
        }
        
        // Monopoly always uses shared board
        if (rules.separatePlayerBoards)
        {
            Debug.LogWarning("Separate boards not supported in Monopoly");
        }
        
        // Enemy visibility is always on in Monopoly
        if (!rules.canSeeEnemyTokens)
        {
            Debug.LogWarning("Enemy token visibility disabled - Monopoly shows all players");
        }
        
        // Note: You can extend MonopolyGameManager to use these rules
        // For now, this serves as a validation layer
    }
    
    /// <summary>
    /// Apply rules to Battleships game manager
    /// </summary>
    private void ApplyRulesToBattleships(GameRules rules)
    {
        Debug.Log("[RuleApplicationHelper] Applying rules to Battleships");
        
        // Battleships doesn't use currency by default
        if (rules.enableCurrency)
        {
            Debug.Log("Currency enabled in Battleships - this is a custom variant");
        }
        
        // Battleships always uses separate boards
        if (!rules.separatePlayerBoards)
        {
            Debug.LogWarning("Shared board not typically used in Battleships");
        }
        
        // Battleships typically hides enemy ships
        if (rules.canSeeEnemyTokens)
        {
            Debug.Log("Enemy token visibility enabled - this changes standard Battleships gameplay");
        }
    }

    #endregion

    #region Specific Rule Handlers

    private void OnCurrencyToggled(bool enabled)
    {
        Debug.Log($"[RuleApplicationHelper] Currency {(enabled ? "enabled" : "disabled")}");
        
        // Update UI currency displays
        if (MonopolyUI.Instance != null)
        {
            // Enable/disable money displays in Monopoly UI
            // MonopolyUI.Instance.SetCurrencyVisible(enabled);
        }
    }
    
    private void OnSeparateBoardsToggled(bool enabled)
    {
        Debug.Log($"[RuleApplicationHelper] Board mode: {(enabled ? "Separate" : "Shared")}");
        
        // This would require significant game mode changes
        // For now, just log the change
    }
    
    private void OnEnemyVisibilityChanged(bool canSee, int range)
    {
        string rangeText = range == -1 ? "unlimited" : $"{range} tiles";
        Debug.Log($"[RuleApplicationHelper] Enemy visibility: {(canSee ? $"Yes ({rangeText})" : "No")}");
        
        // Update token rendering based on visibility rules
        UpdateTokenVisibility(canSee, range);
    }

    #endregion

    #region Token Visibility

    /// <summary>
    /// Update token visibility based on current rules
    /// </summary>
    public void UpdateTokenVisibility(bool canSee, int range)
    {
        if (!canSee)
        {
            // Hide all enemy tokens
            HideAllEnemyTokens();
            return;
        }
        
        if (range == -1)
        {
            // Show all enemy tokens
            ShowAllEnemyTokens();
            return;
        }
        
        // Show only tokens within range
        UpdateTokenVisibilityByRange(range);
    }
    
    /// <summary>
    /// Hide all enemy tokens
    /// </summary>
    private void HideAllEnemyTokens()
    {
        Debug.Log("[RuleApplicationHelper] Hiding all enemy tokens");
        
        // For Monopoly
        if (MonopolyBoardManager.Instance != null)
        {
            // You would implement this in MonopolyBoardManager
            // MonopolyBoardManager.Instance.SetEnemyTokensVisible(false);
        }
        
        // For Battleships - already handles this internally
    }
    
    /// <summary>
    /// Show all enemy tokens
    /// </summary>
    private void ShowAllEnemyTokens()
    {
        Debug.Log("[RuleApplicationHelper] Showing all enemy tokens");
        
        // For Monopoly
        if (MonopolyBoardManager.Instance != null)
        {
            // You would implement this in MonopolyBoardManager
            // MonopolyBoardManager.Instance.SetEnemyTokensVisible(true);
        }
    }
    
    /// <summary>
    /// Update token visibility based on range from player
    /// </summary>
    private void UpdateTokenVisibilityByRange(int range)
    {
        Debug.Log($"[RuleApplicationHelper] Updating token visibility with range: {range}");
        
        // Get local player position
        int localPlayerId = GetLocalPlayerId();
        Vector2Int playerPosition = GetPlayerPosition(localPlayerId);
        
        // For Monopoly
        if (MonopolyBoardManager.Instance != null)
        {
            // You would implement range-based visibility in MonopolyBoardManager
            // MonopolyBoardManager.Instance.UpdateTokenVisibilityByRange(playerPosition, range);
        }
    }
    
    /// <summary>
    /// Check if an enemy token should be visible based on rules
    /// </summary>
    public bool ShouldShowEnemyToken(Vector2Int tokenPosition, Vector2Int playerPosition)
    {
        if (RuleEditorManager.Instance == null)
        {
            return true; // Default to visible
        }
        
        if (!RuleEditorManager.Instance.CanSeeEnemyTokens())
        {
            return false;
        }
        
        int range = RuleEditorManager.Instance.GetEnemyTokenVisibilityRange();
        if (range == -1)
        {
            return true; // Unlimited range
        }
        
        // Calculate Manhattan distance
        int distance = Mathf.Abs(tokenPosition.x - playerPosition.x) + 
                      Mathf.Abs(tokenPosition.y - playerPosition.y);
        
        return distance <= range;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get local player ID from network
    /// </summary>
    private int GetLocalPlayerId()
    {
        // Use custom NetworkManager wrapper
        if (NetworkManager.Instance != null)
        {
            var unityNetworkManager = NetworkManager.Instance.GetUnityNetworkManager();
            if (unityNetworkManager != null && unityNetworkManager.LocalClient != null)
            {
                return (int)unityNetworkManager.LocalClientId;
            }
        }
        return 0;
    }
    
    /// <summary>
    /// Get player position from appropriate game manager
    /// </summary>
    private Vector2Int GetPlayerPosition(int playerId)
    {
        // Try Monopoly first
        if (MonopolyGameManager.Instance != null)
        {
            var player = MonopolyGameManager.Instance.GetPlayer(playerId);
            // Convert Monopoly position (0-39) to 2D board coordinates
            // This is a simplified conversion - adjust based on your board layout
            int x = player.position % 10;
            int y = player.position / 10;
            return new Vector2Int(x, y);
        }
        
        // Try Battleships
        if (BattleshipsGameManager.Instance != null)
        {
            // Battleships doesn't have player tokens on a shared board
            // This would need to be implemented if using hybrid rules
            return Vector2Int.zero;
        }
        
        return Vector2Int.zero;
    }
    
    /// <summary>
    /// Check if currency should be displayed for current rules
    /// </summary>
    public bool ShouldDisplayCurrency()
    {
        if (RuleEditorManager.Instance != null)
        {
            return RuleEditorManager.Instance.IsCurrencyEnabled();
        }
        return true; // Default to showing currency
    }
    
    /// <summary>
    /// Get starting money from current rules
    /// </summary>
    public int GetStartingMoney()
    {
        if (RuleEditorManager.Instance != null)
        {
            var rules = RuleEditorManager.Instance.GetCurrentRules();
            return rules.startingMoney;
        }
        return 1500; // Default Monopoly starting money
    }

    #endregion

    #region Debug

    [ContextMenu("Print Current Rule Application Status")]
    private void PrintRuleApplicationStatus()
    {
        Debug.Log("=== Rule Application Status ===");
        
        if (RuleEditorManager.Instance != null)
        {
            var rules = RuleEditorManager.Instance.GetCurrentRules();
            Debug.Log(rules.GetRulesSummary());
            
            Debug.Log($"Currency Display: {ShouldDisplayCurrency()}");
            Debug.Log($"Starting Money: ${GetStartingMoney()}");
            Debug.Log($"Can See Enemies: {RuleEditorManager.Instance.CanSeeEnemyTokens()}");
            Debug.Log($"Visibility Range: {RuleEditorManager.Instance.GetEnemyTokenVisibilityRange()}");
        }
        else
        {
            Debug.LogWarning("RuleEditorManager not found!");
        }
        
        Debug.Log($"Monopoly Active: {MonopolyGameManager.Instance != null}");
        Debug.Log($"Battleships Active: {BattleshipsGameManager.Instance != null}");
    }

    #endregion
}

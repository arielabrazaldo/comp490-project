using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Validates BattleshipsUIManager references and helps find missing assignments
/// Add this to your BattleshipsUIManager GameObject and run validation
/// </summary>
public class BattleshipsUIValidator : MonoBehaviour
{
    [Header("Auto-Find References")]
    [Tooltip("Attempt to auto-find missing references by name")]
    public bool autoFindReferences = true;
    
    [ContextMenu("Validate All References")]
    public void ValidateReferences()
    {
        Debug.Log("=== BattleshipsUIManager Reference Validation ===");
        
        BattleshipsUIManager uiManager = GetComponent<BattleshipsUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("? No BattleshipsUIManager component found on this GameObject!");
            return;
        }
        
        int missingCount = 0;
        int totalFields = 0;
        
        // Use reflection to check all serialized fields
        var fields = typeof(BattleshipsUIManager).GetFields(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance
        );
        
        foreach (var field in fields)
        {
            // Check if field has SerializeField attribute
            if (System.Attribute.IsDefined(field, typeof(SerializeField)))
            {
                totalFields++;
                object value = field.GetValue(uiManager);
                
                if (value == null || (value is UnityEngine.Object unityObj && !unityObj))
                {
                    string fieldName = field.Name;
                    System.Type fieldType = field.FieldType;
                    
                    Debug.LogWarning($"?? Missing: {fieldName} ({fieldType.Name})");
                    missingCount++;
                    
                    // Try to auto-find if enabled
                    if (autoFindReferences)
                    {
                        TryAutoFind(uiManager, field);
                    }
                }
                else
                {
                    Debug.Log($"? Assigned: {field.Name}");
                }
            }
        }
        
        Debug.Log($"\n=== Validation Complete ===");
        Debug.Log($"Total Fields: {totalFields}");
        Debug.Log($"Missing: {missingCount}");
        Debug.Log($"Assigned: {totalFields - missingCount}");
        
        if (missingCount == 0)
        {
            Debug.Log("?? All references are properly assigned!");
        }
        else
        {
            Debug.LogWarning($"?? {missingCount} references need to be assigned in the Inspector!");
        }
    }
    
    private void TryAutoFind(BattleshipsUIManager uiManager, System.Reflection.FieldInfo field)
    {
        string fieldName = field.Name;
        System.Type fieldType = field.FieldType;
        
        // Try to find by name in scene
        GameObject found = null;
        
        // Common naming patterns
        string[] searchPatterns = {
            fieldName,
            fieldName.Replace("Panel", ""),
            fieldName.Replace("Button", ""),
            char.ToUpper(fieldName[0]) + fieldName.Substring(1), // Capitalize
            "Battleships" + char.ToUpper(fieldName[0]) + fieldName.Substring(1)
        };
        
        foreach (string pattern in searchPatterns)
        {
            found = GameObject.Find(pattern);
            if (found != null) break;
        }
        
        if (found != null)
        {
            if (fieldType == typeof(GameObject))
            {
                field.SetValue(uiManager, found);
                Debug.Log($"? Auto-found: {fieldName} ? {found.name}");
            }
            else if (fieldType == typeof(Transform))
            {
                field.SetValue(uiManager, found.transform);
                Debug.Log($"? Auto-found: {fieldName} ? {found.name}");
            }
            else
            {
                // Try to get component
                var component = found.GetComponent(fieldType);
                if (component != null)
                {
                    field.SetValue(uiManager, component);
                    Debug.Log($"? Auto-found: {fieldName} ? {found.name}.{fieldType.Name}");
                }
            }
        }
    }
    
    [ContextMenu("Create Missing Panels")]
    public void CreateMissingPanels()
    {
        Debug.Log("=== Creating Missing Panels ===");
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("? No Canvas found in scene! Cannot create panels.");
            return;
        }
        
        // Create waiting panel if missing
        if (GameObject.Find("BattleshipsWaitingPanel") == null)
        {
            CreateWaitingPanel(canvas.transform);
        }
        
        // Create ship placement panel if missing
        if (GameObject.Find("BattleshipsShipPlacementPanel") == null)
        {
            CreateShipPlacementPanel(canvas.transform);
        }
        
        // Create combat panel if missing
        if (GameObject.Find("BattleshipsCombatPanel") == null)
        {
            CreateCombatPanel(canvas.transform);
        }
        
        // Create game over panel if missing
        if (GameObject.Find("BattleshipsGameOverPanel") == null)
        {
            CreateGameOverPanel(canvas.transform);
        }
        
        Debug.Log("? Panel creation complete! Re-run validation to check references.");
    }
    
    private void CreateWaitingPanel(Transform parent)
    {
        GameObject panel = new GameObject("BattleshipsWaitingPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);
        
        GameObject textObj = new GameObject("WaitingText");
        textObj.transform.SetParent(panel.transform);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Waiting for other players...";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        panel.SetActive(false);
        
        Debug.Log("? Created: BattleshipsWaitingPanel");
    }
    
    private void CreateShipPlacementPanel(Transform parent)
    {
        GameObject panel = new GameObject("BattleshipsShipPlacementPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        panel.SetActive(false);
        
        Debug.Log("? Created: BattleshipsShipPlacementPanel (needs child UI elements)");
        Debug.Log("   ? Add board containers, ship buttons, and status text manually");
    }
    
    private void CreateCombatPanel(Transform parent)
    {
        GameObject panel = new GameObject("BattleshipsCombatPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        panel.SetActive(false);
        
        Debug.Log("? Created: BattleshipsCombatPanel (needs child UI elements)");
    }
    
    private void CreateGameOverPanel(Transform parent)
    {
        GameObject panel = new GameObject("BattleshipsGameOverPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);
        
        GameObject textObj = new GameObject("WinnerText");
        textObj.transform.SetParent(panel.transform);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.6f);
        textRt.anchorMax = new Vector2(0.5f, 0.6f);
        textRt.sizeDelta = new Vector2(600, 100);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Player 1 Wins!";
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.yellow;
        
        panel.SetActive(false);
        
        Debug.Log("? Created: BattleshipsGameOverPanel");
    }
}

using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity Editor menu items for quick manager diagnostics and fixes.
/// Access via Tools > Manager Diagnostics in the Unity menu bar.
/// </summary>
public static class ManagerDiagnosticMenu
{
    [MenuItem("Tools/Manager Diagnostics/Run Full Diagnostics")]
    public static void RunFullDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("   RUNNING FULL MANAGER DIAGNOSTICS    ");
        Debug.Log("========================================");

        CheckStandardGameLibrary();
        CheckGameSaveManager();
        CheckUIManager();
        CheckSceneSetup();

        Debug.Log("========================================");
        Debug.Log("      DIAGNOSTICS COMPLETE              ");
        Debug.Log("========================================");
    }

    [MenuItem("Tools/Manager Diagnostics/Check StandardGameLibrary")]
    public static void CheckStandardGameLibrary()
    {
        Debug.Log("\n--- Checking StandardGameLibrary ---");

        StandardGameLibrary library = Object.FindFirstObjectByType<StandardGameLibrary>();
        
        if (library == null)
        {
            Debug.LogError("? StandardGameLibrary NOT FOUND in scene!");
            Debug.LogError("   Create a GameObject and add StandardGameLibrary component");
            return;
        }

        Debug.Log($"? Found: {library.gameObject.name}");
        Debug.Log($"   Active: {library.gameObject.activeInHierarchy}");
        Debug.Log($"   Enabled: {library.enabled}");

        if (Application.isPlaying)
        {
            if (StandardGameLibrary.Instance != null)
            {
                var games = StandardGameLibrary.Instance.GetAllStandardGames();
                Debug.Log($"? Instance valid - {games.Count} standard games loaded");
            }
            else
            {
                Debug.LogError("? Instance is NULL during Play Mode!");
            }
        }
        else
        {
            Debug.Log("?? Not in Play Mode - cannot test Instance");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Check GameSaveManager")]
    public static void CheckGameSaveManager()
    {
        Debug.Log("\n--- Checking GameSaveManager ---");

        GameSaveManager saveManager = Object.FindFirstObjectByType<GameSaveManager>();
        
        if (saveManager == null)
        {
            Debug.LogError("? GameSaveManager NOT FOUND in scene!");
            Debug.LogError("   Create a GameObject and add GameSaveManager component");
            return;
        }

        Debug.Log($"? Found: {saveManager.gameObject.name}");
        Debug.Log($"   Active: {saveManager.gameObject.activeInHierarchy}");
        Debug.Log($"   Enabled: {saveManager.enabled}");

        if (Application.isPlaying)
        {
            if (GameSaveManager.Instance != null)
            {
                string saveDir = GameSaveManager.Instance.GetSaveDirectory();
                var games = GameSaveManager.Instance.LoadAllGames();
                Debug.Log($"? Instance valid - {games.Count} custom games found");
                Debug.Log($"   Save directory: {saveDir}");
            }
            else
            {
                Debug.LogError("? Instance is NULL during Play Mode!");
            }
        }
        else
        {
            Debug.Log("?? Not in Play Mode - cannot test Instance");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Check UIManager_Streamlined")]
    public static void CheckUIManager()
    {
        Debug.Log("\n--- Checking UIManager_Streamlined ---");

        UIManager_Streamlined uiManager = Object.FindFirstObjectByType<UIManager_Streamlined>();
        
        if (uiManager == null)
        {
            Debug.LogError("? UIManager_Streamlined NOT FOUND in scene!");
            Debug.LogError("   Make sure you're using UIManager_Streamlined, NOT UIManager");
            return;
        }

        Debug.Log($"? Found: {uiManager.gameObject.name}");
        Debug.Log($"   Active: {uiManager.gameObject.activeInHierarchy}");
        Debug.Log($"   Enabled: {uiManager.enabled}");

        if (Application.isPlaying)
        {
            if (UIManager_Streamlined.Instance != null)
            {
                Debug.Log("? Instance valid during Play Mode");
            }
            else
            {
                Debug.LogError("? Instance is NULL during Play Mode!");
            }
        }
        else
        {
            Debug.Log("?? Not in Play Mode - cannot test Instance");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Check Scene Setup")]
    public static void CheckSceneSetup()
    {
        Debug.Log("\n--- Checking Scene Setup ---");

        // Check for old UIManager (should NOT exist)
        var oldUIManager = Object.FindFirstObjectByType<UIManager>();
        if (oldUIManager != null)
        {
            Debug.LogWarning("?? Old UIManager found! You should be using UIManager_Streamlined");
            Debug.LogWarning($"   Found on: {oldUIManager.gameObject.name}");
        }

        // Check for required managers
        int managersFound = 0;
        
        if (Object.FindFirstObjectByType<StandardGameLibrary>() != null) managersFound++;
        if (Object.FindFirstObjectByType<GameSaveManager>() != null) managersFound++;
        if (Object.FindFirstObjectByType<UIManager_Streamlined>() != null) managersFound++;

        Debug.Log($"Managers found: {managersFound}/3");

        if (managersFound == 3)
        {
            Debug.Log("? All required managers present in scene");
        }
        else
        {
            Debug.LogWarning($"?? Missing {3 - managersFound} manager(s)");
            Debug.LogWarning("   Use 'Create Missing Managers' to fix");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Create Missing Managers")]
    public static void CreateMissingManagers()
    {
        Debug.Log("\n--- Creating Missing Managers ---");

        bool created = false;

        // Create StandardGameLibrary if missing
        if (Object.FindFirstObjectByType<StandardGameLibrary>() == null)
        {
            GameObject obj = new GameObject("StandardGameLibrary");
            obj.AddComponent<StandardGameLibrary>();
            Debug.Log("? Created StandardGameLibrary");
            created = true;
        }

        // Create GameSaveManager if missing
        if (Object.FindFirstObjectByType<GameSaveManager>() == null)
        {
            GameObject obj = new GameObject("GameSaveManager");
            obj.AddComponent<GameSaveManager>();
            Debug.Log("? Created GameSaveManager");
            created = true;
        }

        // Check for UIManager_Streamlined
        if (Object.FindFirstObjectByType<UIManager_Streamlined>() == null)
        {
            Debug.LogWarning("?? UIManager_Streamlined not found");
            Debug.LogWarning("   Cannot auto-create (requires UI references)");
            Debug.LogWarning("   Please add manually to your UI GameObject");
        }

        if (!created)
        {
            Debug.Log("? All managers already exist - nothing to create");
        }
        else
        {
            Debug.Log("\n? Manager creation complete!");
            Debug.Log("   Remember to save your scene (Ctrl+S)");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Open Save Directory")]
    public static void OpenSaveDirectory()
    {
        string saveDir = System.IO.Path.Combine(Application.persistentDataPath, "CustomGames");

        if (System.IO.Directory.Exists(saveDir))
        {
            EditorUtility.RevealInFinder(saveDir);
            Debug.Log($"Opened save directory: {saveDir}");
        }
        else
        {
            Debug.LogWarning($"Save directory doesn't exist yet: {saveDir}");
            Debug.LogWarning("It will be created when GameSaveManager initializes");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Clear All Saved Games")]
    public static void ClearAllSavedGames()
    {
        if (!EditorUtility.DisplayDialog(
            "Clear All Saved Games?",
            "This will delete all custom saved games from disk.\n\nThis action cannot be undone!",
            "Delete All",
            "Cancel"))
        {
            return;
        }

        string saveDir = System.IO.Path.Combine(Application.persistentDataPath, "CustomGames");

        if (System.IO.Directory.Exists(saveDir))
        {
            string[] files = System.IO.Directory.GetFiles(saveDir, "*.json");
            
            foreach (string file in files)
            {
                System.IO.File.Delete(file);
            }

            Debug.Log($"? Deleted {files.Length} saved game(s)");
        }
        else
        {
            Debug.Log("No saved games directory found - nothing to delete");
        }
    }

    [MenuItem("Tools/Manager Diagnostics/Show Help")]
    public static void ShowHelp()
    {
        string help = @"
Manager Diagnostics Help
========================

MENU ITEMS:
- Run Full Diagnostics: Checks all managers and scene setup
- Check StandardGameLibrary: Verifies standard games library
- Check GameSaveManager: Verifies save system
- Check UIManager_Streamlined: Verifies UI manager
- Check Scene Setup: Overall scene configuration check
- Create Missing Managers: Auto-creates missing manager GameObjects
- Open Save Directory: Opens folder with saved custom games
- Clear All Saved Games: Deletes all custom games (WARNING: Cannot undo!)

REQUIRED MANAGERS:
1. StandardGameLibrary - Provides built-in games (Monopoly, Battleships, Dice Race)
2. GameSaveManager - Saves/loads custom games to/from disk
3. UIManager_Streamlined - Manages all UI panels

EXPECTED BEHAVIOR:
- Managers should exist in hierarchy
- During Play Mode, managers move to DontDestroyOnLoad
- 'Host Game' menu should show 3 standard games by default
- Custom games appear after creating them in Rule Editor

COMMON ISSUES:
- Empty saved games list ? Run 'Create Missing Managers'
- Managers not persisting ? Check they're active and enabled
- Old UIManager ? Switch to UIManager_Streamlined

For detailed troubleshooting, see:
- SAVED_GAMES_LIST_FIX_AND_DIAGNOSTIC.md
- MANAGER_DIAGNOSTIC_VISUAL_GUIDE.md
";

        Debug.Log(help);
        
        EditorUtility.DisplayDialog(
            "Manager Diagnostics Help",
            "Check the Console for detailed help information.\n\n" +
            "Also see these files for more details:\n" +
            "- SAVED_GAMES_LIST_FIX_AND_DIAGNOSTIC.md\n" +
            "- MANAGER_DIAGNOSTIC_VISUAL_GUIDE.md",
            "OK");
    }

    // Menu item validation (only show some options during Play Mode)
    [MenuItem("Tools/Manager Diagnostics/Run Full Diagnostics", true)]
    public static bool ValidateRunDiagnostics()
    {
        return true; // Always available
    }

    [MenuItem("Tools/Manager Diagnostics/Create Missing Managers", true)]
    public static bool ValidateCreateManagers()
    {
        return !Application.isPlaying; // Only available in Edit Mode
    }

    [MenuItem("Tools/Manager Diagnostics/Clear All Saved Games", true)]
    public static bool ValidateClearSavedGames()
    {
        return !Application.isPlaying; // Only available in Edit Mode
    }
}

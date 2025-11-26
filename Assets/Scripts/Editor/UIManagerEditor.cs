using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector helper for UIManager to show new Monopoly fields
/// This will ensure all new fields are visible in the inspector
/// </summary>
[CustomEditor(typeof(UIManager))]
public class UIManagerEditor : Editor
{
    private bool showValidation = false;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        UIManager uiManager = (UIManager)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Monopoly Setup Helper", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);
        
        // Validation section
        showValidation = EditorGUILayout.Foldout(showValidation, "Validation Tools");
        if (showValidation)
        {
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("Validate All UI References"))
            {
                uiManager.ValidateAllUIReferences();
            }
            
            if (GUILayout.Button("Toggle Game Mode (Testing)"))
            {
                uiManager.ToggleGameMode();
            }
            
            if (GUILayout.Button("Refresh Inspector"))
            {
                uiManager.RefreshUIManagerComponent();
                EditorUtility.SetDirty(uiManager);
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        // Help section
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Missing UI Elements?", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("If you don't see the new Monopoly fields:", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("1. Click 'Refresh Inspector' above", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("2. Or remove and re-add UIManager component", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3. Use 'Validate All UI References' to check", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("?? Tip: Use MonopolyUISetupHelper to auto-create UI elements!", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
        
        // Mark as dirty if changed
        if (GUI.changed)
        {
            EditorUtility.SetDirty(uiManager);
        }
    }
}
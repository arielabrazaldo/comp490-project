using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Sits on the shared overlay GameObject that is placed once in the Unity hierarchy.
/// Both RuleEditorUI and BoardEditorUI reference this component.
///
/// Flow:
///   Open()  -> shows selection panel listing all custom games
///   user picks a game  -> onGameSelected callback fires
///   caller decides whether to show naming panel (new game) or confirmation panel (overwrite)
///   ShowNamingPanel()  -> caller calls ConfirmName() which fires onNameConfirmed
///   ShowConfirmationPanel() -> user picks "Return to Menu" or "Continue Editing"
/// </summary>
public class SharedGameSelectionOverlay : MonoBehaviour
{
    [Header("Overlay Root")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private GameObject backgroundContent; // content hidden while overlay is open

    [Header("Selection Panel")]
    [SerializeField] private GameObject customGameSelectionPanel;
    [SerializeField] private Transform  customGamesListContent;
    [SerializeField] private GameObject customGameItemPrefab;
    [SerializeField] private Button     overwriteGameButton;
    [SerializeField] private Button     newGameFromTemplateButton;
    [SerializeField] private Button     backFromSelectionButton;
    [SerializeField] private TextMeshProUGUI selectedGameInfoText;
    [SerializeField] private TextMeshProUGUI noCustomGamesText;

    [Header("Naming Panel")]
    [SerializeField] private GameObject     namingPanel;
    [SerializeField] private TMP_InputField gameNameInput;
    [SerializeField] private Button         confirmNameButton;
    [SerializeField] private Button         cancelNameButton;

    [Header("Confirmation Panel")]
    [SerializeField] private GameObject      confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button          returnToMenuButton;
    [SerializeField] private Button          continueEditingButton;

    [Header("Warning Panel")]
    [SerializeField] private GameObject      warningPanel;
    [SerializeField] private TextMeshProUGUI warningMessageText;
    [SerializeField] private Button          warningConfirmButton;
    [SerializeField] private Button          warningCancelButton;

    // ?? runtime state ????????????????????????????????????????????????????
    private List<SavedGameInfo> customGamesList    = new List<SavedGameInfo>();
    private SavedGameInfo       selectedCustomGame = null;

    // Callbacks registered by the active editor
    private System.Action<SavedGameInfo> onOverwriteClicked;        // user chose "Edit existing"
    private System.Action<SavedGameInfo> onNewFromTemplateClicked;  // user chose "Use as template"
    private System.Action<string>        onNameConfirmed;           // user confirmed a name
    private System.Action                onCancelled;               // user cancelled / closed
    private System.Action                onReturnToMenu;            // from confirmation panel
    private System.Action                onContinueEditing;         // from confirmation panel
    private System.Action                onWarningConfirmed;        // from warning panel
    private System.Action                onWarningCancelled;        // from warning panel

    // Background panel supplied by the active caller (overrides the serialized backgroundContent)
    private GameObject activeBackground = null;

    // ?? Unity lifecycle ??????????????????????????????????????????????????

    private void Awake()
    {
        WireButtons();
        HideAll();
    }

    // ?? public API ???????????????????????????????????????????????????????

    /// <summary>
    /// Register the background panel that should be hidden while this overlay is open.
    /// Call this from the editor's Start() before opening any panel.
    /// Overrides the serialized backgroundContent field for the active caller.
    /// </summary>
    public void RegisterBackground(GameObject background)
    {
        activeBackground = background;
    }

    /// <summary>
    /// Open the selection panel.
    /// </summary>
    /// <param name="onOverwrite">Called when user clicks "Edit Existing" with the selected game.</param>
    /// <param name="onNewTemplate">Called when user clicks "Use as Template" with the selected game.</param>
    /// <param name="onCancel">Called when user clicks Back without selecting.</param>
    public void OpenSelectionPanel(
        System.Action<SavedGameInfo> onOverwrite,
        System.Action<SavedGameInfo> onNewTemplate,
        System.Action                onCancel)
    {
        this.onOverwriteClicked       = onOverwrite;
        this.onNewFromTemplateClicked = onNewTemplate;
        this.onCancelled              = onCancel;

        SetOverlayVisible(true);
        if (namingPanel      != null) namingPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (customGameSelectionPanel != null) customGameSelectionPanel.SetActive(true);

        RefreshList();
    }

    /// <summary>
    /// Open the naming panel (called when a new game needs a name).
    /// </summary>
    /// <param name="prefillName">Name to pre-fill (e.g. when editing existing game).</param>
    /// <param name="onConfirm">Called with the entered name when user confirms.</param>
    /// <param name="onCancel">Called when user cancels.</param>
    public void OpenNamingPanel(
        string          prefillName,
        System.Action<string> onConfirm,
        System.Action         onCancel)
    {
        this.onNameConfirmed = onConfirm;
        this.onCancelled     = onCancel;

        SetOverlayVisible(true);
        if (customGameSelectionPanel != null) customGameSelectionPanel.SetActive(false);
        if (confirmationPanel        != null) confirmationPanel.SetActive(false);

        if (namingPanel != null)
        {
            namingPanel.SetActive(true);
            if (gameNameInput != null)
            {
                gameNameInput.text = prefillName ?? "";
                gameNameInput.Select();
                gameNameInput.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// Show the confirmation panel after a successful save.
    /// </summary>
    /// <param name="savedName">The name of the saved game to show in the message.</param>
    /// <param name="wasOverwrite">True if an existing game was updated, false if newly created.</param>
    /// <param name="onReturn">Called when user clicks "Return to Menu".</param>
    /// <param name="onContinue">Called when user clicks "Continue Editing".</param>
    public void OpenConfirmationPanel(
        string        savedName,
        bool          wasOverwrite,
        System.Action onReturn,
        System.Action onContinue)
    {
        this.onReturnToMenu    = onReturn;
        this.onContinueEditing = onContinue;

        SetOverlayVisible(true);
        if (customGameSelectionPanel != null) customGameSelectionPanel.SetActive(false);
        if (namingPanel              != null) namingPanel.SetActive(false);

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            if (confirmationText != null)
            {
                confirmationText.text = wasOverwrite
                    ? $"'{savedName}' updated successfully!"
                    : $"'{savedName}' created successfully!";
            }
        }
    }

    /// <summary>
    /// Show a warning message asking the user to confirm a destructive action.
    /// The overlay background is NOT hidden — the warning floats over the current view.
    /// </summary>
    /// <param name="message">Warning text to display.</param>
    /// <param name="onConfirm">Called when user clicks Confirm.</param>
    /// <param name="onCancel">Called when user clicks Cancel.</param>
    public void OpenWarningPanel(string message, System.Action onConfirm, System.Action onCancel)
    {
        this.onWarningConfirmed = onConfirm;
        this.onWarningCancelled = onCancel;

        if (warningPanel == null)
        {
            Debug.LogError("[SharedGameSelectionOverlay] warningPanel is not assigned in the Inspector.");
            return;
        }

        // Show only the warning panel; hide others but keep overlay root visible
        if (customGameSelectionPanel != null) customGameSelectionPanel.SetActive(false);
        if (namingPanel              != null) namingPanel.SetActive(false);
        if (confirmationPanel        != null) confirmationPanel.SetActive(false);
        if (overlayPanel             != null) overlayPanel.SetActive(true);

        if (warningMessageText != null) warningMessageText.text = message;
        warningPanel.SetActive(true);
    }

    /// <summary>
    /// Hide every overlay panel and restore the background content.
    /// </summary>
    public void HideAll()
    {
        SetOverlayVisible(false);
        if (customGameSelectionPanel != null) customGameSelectionPanel.SetActive(false);
        if (namingPanel              != null) namingPanel.SetActive(false);
        if (confirmationPanel        != null) confirmationPanel.SetActive(false);
        if (warningPanel             != null) warningPanel.SetActive(false);
    }

    // ?? private helpers ??????????????????????????????????????????????????

    private void SetOverlayVisible(bool visible)
    {
        if (overlayPanel != null) overlayPanel.SetActive(visible);

        // Prefer the runtime-registered background; fall back to the serialized field
        GameObject bg = activeBackground != null ? activeBackground : backgroundContent;
        if (bg != null) bg.SetActive(!visible);
    }

    private void WireButtons()
    {
        backFromSelectionButton?.onClick.AddListener(OnBackClicked);
        overwriteGameButton?.onClick.AddListener(OnOverwriteClicked);
        newGameFromTemplateButton?.onClick.AddListener(OnNewFromTemplateClicked);
        confirmNameButton?.onClick.AddListener(OnConfirmNameClicked);
        cancelNameButton?.onClick.AddListener(OnCancelNameClicked);
        returnToMenuButton?.onClick.AddListener(OnReturnToMenuClicked);
        continueEditingButton?.onClick.AddListener(OnContinueEditingClicked);
        warningConfirmButton?.onClick.AddListener(OnWarningConfirmClicked);
        warningCancelButton?.onClick.AddListener(OnWarningCancelClicked);
    }

    private void RefreshList()
    {
        customGamesList.Clear();
        selectedCustomGame = null;

        if (overwriteGameButton     != null) overwriteGameButton.interactable     = false;
        if (newGameFromTemplateButton != null) newGameFromTemplateButton.interactable = false;
        if (selectedGameInfoText    != null) selectedGameInfoText.text            = "Select a custom game";

        if (customGamesListContent == null || customGameItemPrefab == null)
        {
            Debug.LogWarning("[SharedGameSelectionOverlay] List content or prefab not assigned.");
            return;
        }

        // Clear old items
        foreach (Transform child in customGamesListContent)
            Destroy(child.gameObject);

        // Load from manager
        UIManager_Streamlined streamlined = FindFirstObjectByType<UIManager_Streamlined>();
        if (streamlined != null)
        {
            customGamesList = streamlined.PopulateCustomGamesList(
                customGamesListContent,
                customGameItemPrefab,
                OnGameItemClicked);
        }
        else
        {
            Debug.LogError("[SharedGameSelectionOverlay] UIManager_Streamlined not found.");
        }

        bool hasGames = customGamesList.Count > 0;
        if (noCustomGamesText != null)
        {
            noCustomGamesText.gameObject.SetActive(!hasGames);
            if (!hasGames)
                noCustomGamesText.text = "No custom games found.\nCreate a custom game first!";
        }

        Debug.Log($"[SharedGameSelectionOverlay] Refreshed list — {customGamesList.Count} games.");
    }

    private void OnGameItemClicked(SavedGameInfo info)
    {
        selectedCustomGame = info;

        int min = info.rules?.minPlayers ?? 2;
        int max = info.rules?.maxPlayers ?? 4;

        if (selectedGameInfoText != null)
            selectedGameInfoText.text =
                $"<b>{info.gameName}</b>\nPlayers: {min}-{max}\n{info.GetDescription()}";

        if (overwriteGameButton     != null) overwriteGameButton.interactable     = true;
        if (newGameFromTemplateButton != null) newGameFromTemplateButton.interactable = true;

        Debug.Log($"[SharedGameSelectionOverlay] Selected: {info.gameName}");
    }

    // ?? button handlers ??????????????????????????????????????????????????

    private void OnBackClicked()
    {
        HideAll();
        onCancelled?.Invoke();
    }

    private void OnOverwriteClicked()
    {
        if (selectedCustomGame == null) return;
        SavedGameInfo game = selectedCustomGame;
        HideAll();
        onOverwriteClicked?.Invoke(game);
    }

    private void OnNewFromTemplateClicked()
    {
        if (selectedCustomGame == null) return;
        SavedGameInfo game = selectedCustomGame;
        HideAll();
        onNewFromTemplateClicked?.Invoke(game);
    }

    private void OnConfirmNameClicked()
    {
        if (gameNameInput == null) return;
        string name = gameNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[SharedGameSelectionOverlay] Game name is empty.");
            return;
        }
        if (namingPanel != null) namingPanel.SetActive(false);
        onNameConfirmed?.Invoke(name);
    }

    private void OnCancelNameClicked()
    {
        HideAll();
        onCancelled?.Invoke();
    }

    private void OnReturnToMenuClicked()
    {
        HideAll();
        onReturnToMenu?.Invoke();
    }

    private void OnContinueEditingClicked()
    {
        HideAll();
        onContinueEditing?.Invoke();
    }

    private void OnWarningConfirmClicked()
    {
        HideAll();
        onWarningConfirmed?.Invoke();
    }

    private void OnWarningCancelClicked()
    {
        HideAll();
        onWarningCancelled?.Invoke();
    }
}

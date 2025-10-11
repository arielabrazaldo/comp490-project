using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board Setup")]
    public GameObject tilePrefab;
    public Transform tileParent; // assign the parent container (usually this object)
    public int totalTiles;

    [Header("Player Tokens")]
    public GameObject[] playerTokenPrefabs; // assign different colored tokens
    private List<GameObject> playerTokens = new List<GameObject>();
    private int[] playerPositions;

    [Header("UI Elements")]
    public GameObject startPanel;
    public TMP_InputField tileInputField;
    public Button startButton;

    [Header("Player Prompt")]
    public GameObject playerPromptPanel;
    public TMP_InputField playerCountInputField;
    public Button confirmPlayersButton;

    [Header("Color Pattern")]
    public Color[] tileColors; // assign in Inspector

    private int numberOfPlayers = 1;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        confirmPlayersButton.onClick.AddListener(OnConfirmPlayers);

        tileParent.gameObject.SetActive(false); // hide board
        playerPromptPanel.SetActive(false); // hide how many players UI
    }

    public void OnStartButtonClicked()
    {
        if (int.TryParse(tileInputField.text,out int result))
        {
            totalTiles = result;
        }
        else
        {
            totalTiles = 20; // default fallback
        }

        startPanel.SetActive(false); // hide start UI
        playerPromptPanel.SetActive(true); // show how many players UI

    }

    public void OnConfirmPlayers()
    {
        if (int.TryParse(playerCountInputField.text, out int result))
        {
            numberOfPlayers = Mathf.Clamp(result, 1, playerTokenPrefabs.Length);
        }
        else
        {
            numberOfPlayers = 1;
        }

        playerPromptPanel.SetActive(false); // hide how many players UI
        tileParent.gameObject.SetActive(true); // show board

        // generate board
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        GameObject firstTile = null; // reference for first tile

        // output for debugging purposes
        Debug.Log("Generating board with " + totalTiles + " tiles");

        for (int i = 1; i <= totalTiles; i++)
        {
            GameObject tile = Instantiate(tilePrefab, tileParent);

            // save reference to first tile
            if (i == 1)
            {
                firstTile = tile;
            }

            // set the number text
            Text numberText = tile.GetComponentInChildren<Text>();
            if (numberText != null)
            {
                numberText.text = i.ToString();
            }

            // set background color
            Image tileImage = tile.GetComponent<Image>();
            if (tileImage != null && tileColors.Length > 0)
            {
                tileImage.color = tileColors[(i - 1) % tileColors.Length];
            }

            // output for debugging purposes
            Debug.Log("Created tile #" + i);
        }

        // Spawn player tokens on the first tile (only if this is standalone mode)
        if (firstTile != null && playerTokenPrefabs.Length >= numberOfPlayers)
        {
            SpawnPlayerTokens(firstTile);
        }
    }

    /// <summary>
    /// Spawn player tokens (used by standalone BoardGenerator or integrated GameSetupManager)
    /// </summary>
    private void SpawnPlayerTokens(GameObject firstTile)
    {
        playerPositions = new int[numberOfPlayers];

        for (int p = 0; p < numberOfPlayers; p++)
        {
            GameObject token = Instantiate(playerTokenPrefabs[p], firstTile.transform);

            RectTransform rt = token.GetComponent<RectTransform>();
            if (rt != null)
            {
                float spacing = 40f; // adjust token spacing if needed

                // calculate layout grid size (2x2 for 1-4 players)
                int cols = Mathf.Min(2, numberOfPlayers);
                int rows = Mathf.CeilToInt(numberOfPlayers / 2f);

                // dynamic position inside tile
                float xOffset = (p % cols - (cols - 1) / 2f) * spacing;
                float yOffset = ((rows - 1) / 2f - p / cols) * spacing;

                rt.anchoredPosition = new Vector2(xOffset, yOffset);
                rt.localScale = Vector3.one * 0.1f; // adjust token scale (sizing) for visibility
            }

            playerTokens.Add(token);
            playerPositions[p] = 0; // All players start at position 0 (tile 1)

            // output for debugging purposes
            Debug.Log("Player token #" + (p + 1) + " spawned.");
        }
    }

    /// <summary>
    /// Get player token GameObjects (for integration with GameSetupManager)
    /// </summary>
    public List<GameObject> GetPlayerTokens()
    {
        return new List<GameObject>(playerTokens);
    }

    /// <summary>
    /// Get player positions array (for integration with GameSetupManager)
    /// </summary>
    public int[] GetPlayerPositions()
    {
        return playerPositions?.Clone() as int[];
    }

    /// <summary>
    /// Set the number of players (for integration with GameSetupManager)
    /// </summary>
    public void SetNumberOfPlayers(int count)
    {
        numberOfPlayers = Mathf.Clamp(count, 1, playerTokenPrefabs.Length);
    }

    /// <summary>
    /// Check if tokens are spawned
    /// </summary>
    public bool HasPlayerTokens()
    {
        return playerTokens != null && playerTokens.Count > 0;
    }
}


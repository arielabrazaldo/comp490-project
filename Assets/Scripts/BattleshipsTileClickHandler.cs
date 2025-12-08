using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// Handles tile click events for Battleships game boards
/// Attached to each tile GameObject
/// NEW: Supports single-click preview and double-click placement
/// </summary>
public class BattleshipsTileClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector2Int tilePosition;
    private bool isPlayerBoard;
    private int targetPlayerId = -1;
    
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f; // 300ms for double-click
    
    // For debugging
    private int clickCount = 0;

    /// <summary>
    /// Set the tile's position on the board
    /// </summary>
    public void SetTilePosition(Vector2Int position)
    {
        tilePosition = position;
        Debug.Log($"Tile position set to: {position}");
    }

    /// <summary>
    /// Set whether this is a player board or enemy board tile
    /// </summary>
    public void SetBoardType(bool isPlayer)
    {
        isPlayerBoard = isPlayer;
        Debug.Log($"Tile board type set: {(isPlayer ? "Player" : "Enemy")} at {tilePosition}");
    }

    /// <summary>
    /// Set the target player ID for attacks (for enemy board)
    /// </summary>
    public void SetTargetPlayerId(int playerId)
    {
        targetPlayerId = playerId;
    }

    /// <summary>
    /// Handle pointer click events (IPointerClickHandler)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        clickCount++;
        Debug.Log($"[Click {clickCount}] Tile clicked at {tilePosition}, Board: {(isPlayerBoard ? "Player" : "Enemy")}");

        if (BattleshipsUIManager.Instance == null)
        {
            Debug.LogError("BattleshipsUIManager.Instance is NULL!");
            return;
        }

        if (BattleshipsGameManager.Instance == null)
        {
            Debug.LogError("BattleshipsGameManager.Instance is NULL!");
            return;
        }

        var gameState = BattleshipsGameManager.Instance.GetGameState();
        Debug.Log($"Current game state: {gameState}");

        // Handle different game states
        switch (gameState)
        {
            case BattleshipsGameManager.GameState.PlacingShips:
                if (isPlayerBoard)
                {
                    // Check for double-click
                    float timeSinceLastClick = Time.time - lastClickTime;
                    lastClickTime = Time.time;
                    
                    if (timeSinceLastClick < DOUBLE_CLICK_TIME)
                    {
                        // Double-click detected - place ship
                        Debug.Log($"? Double-click detected! Placing ship at {tilePosition}");
                        BattleshipsUIManager.Instance.OnTilePlacementClick(tilePosition);
                    }
                    else
                    {
                        // Single-click - show preview
                        Debug.Log($"??? Single-click - showing preview at {tilePosition}");
                        BattleshipsUIManager.Instance.ShowShipPreview(tilePosition);
                    }
                }
                else
                {
                    Debug.LogWarning("? Cannot place ships on enemy board!");
                }
                break;

            case BattleshipsGameManager.GameState.InProgress:
                if (!isPlayerBoard)
                {
                    // Attack enemy tile
                    Debug.Log($"?? Attacking enemy tile at {tilePosition}");
                    
                    if (targetPlayerId >= 0)
                    {
                        BattleshipsUIManager.Instance.OnTileAttackClick(tilePosition, targetPlayerId);
                    }
                    else
                    {
                        Debug.LogError($"? Target player ID not set for enemy tile at {tilePosition}");
                    }
                }
                else
                {
                    Debug.LogWarning("? Cannot attack your own board!");
                }
                break;

            default:
                Debug.LogWarning($"? Clicks not handled in game state: {gameState}");
                break;
        }
    }
    
    /// <summary>
    /// Handle mouse enter (hover) - show preview
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BattleshipsGameManager.Instance == null || BattleshipsUIManager.Instance == null) return;
        
        // Show preview on hover during ship placement
        if (BattleshipsGameManager.Instance.GetGameState() == BattleshipsGameManager.GameState.PlacingShips && isPlayerBoard)
        {
            BattleshipsUIManager.Instance.ShowShipPreview(tilePosition, forceUpdate: false);
        }
    }
    
    /// <summary>
    /// Handle mouse exit - clear preview (but keep position tracked for rotation)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (BattleshipsUIManager.Instance == null) return;
        
        // Clear preview when mouse leaves (only during placement)
        if (BattleshipsGameManager.Instance != null && 
            BattleshipsGameManager.Instance.GetGameState() == BattleshipsGameManager.GameState.PlacingShips)
        {
            BattleshipsUIManager.Instance.ClearShipPreview();
        }
    }
}

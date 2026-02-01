using UnityEngine;

/// <summary>
/// Movement module for hybrid games - pulls from all game managers
/// </summary>
public class HybridMovementModule : MonoBehaviour
{
    private GameRules rules;

    public void Initialize(GameRules gameRules)
    {
        rules = gameRules;
        Debug.Log("[HybridMovementModule] Initialized");
    }

    /// <summary>
    /// Move player on board
    /// Combines logic from NetworkGameManager and MonopolyGameManager
    /// </summary>
    public int MovePlayer(int playerId, int currentPosition, int spaces, GameRules activeRules)
    {
        int newPosition;
        
        if (activeRules.separatePlayerBoards)
        {
            // Separate boards - simple linear movement (like Battleships)
            newPosition = currentPosition + spaces;
            int maxPosition = activeRules.tilesPerSide * activeRules.tilesPerSide - 1;
            newPosition = Mathf.Min(newPosition, maxPosition);
        }
        else
        {
            // Shared board - loop movement (like Monopoly/Dice Race)
            int boardSize = GetBoardSize(activeRules);
            newPosition = (currentPosition + spaces) % boardSize;
            
            // Check if passed start (like passing GO in Monopoly)
            if (newPosition < currentPosition)
            {
                Debug.Log($"[HybridMovementModule] Player {playerId} passed start!");
                // Trigger passing start event
                OnPassedStart(playerId);
            }
        }
        
        Debug.Log($"[HybridMovementModule] Player {playerId} moved from {currentPosition} to {newPosition}");
        return newPosition;
    }

    /// <summary>
    /// Get board size based on configuration
    /// </summary>
    private int GetBoardSize(GameRules activeRules)
    {
        // For Monopoly-like games: 40 spaces
        // For Dice Race: configured tile count
        if (activeRules.enableCurrency && activeRules.canPurchaseProperties)
        {
            return 40; // Monopoly standard
        }
        else
        {
            return activeRules.tilesPerSide;
        }
    }

    /// <summary>
    /// Trigger when player passes start position
    /// From MonopolyGameManager passing GO
    /// </summary>
    private void OnPassedStart(int playerId)
    {
        // This would trigger currency rewards if currency module is active
        // The HybridGameManager handles this through modules
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage($"Player {playerId + 1} passed START!");
        }
    }

    /// <summary>
    /// Teleport player to specific position (for special spaces)
    /// </summary>
    public int TeleportPlayer(int playerId, int currentPosition, int targetPosition)
    {
        Debug.Log($"[HybridMovementModule] Player {playerId} teleported from {currentPosition} to {targetPosition}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage($"Player {playerId + 1} teleported to position {targetPosition}!");
        }
        return targetPosition;
    }

    /// <summary>
    /// Calculate distance between two positions
    /// </summary>
    public int CalculateDistance(int pos1, int pos2, bool isSeparateBoards)
    {
        if (isSeparateBoards)
        {
            // Simple distance for grid-based boards
            return Mathf.Abs(pos2 - pos1);
        }
        else
        {
            // Loop distance for circular boards
            int boardSize = rules.tilesPerSide;
            int forward = (pos2 - pos1 + boardSize) % boardSize;
            int backward = (pos1 - pos2 + boardSize) % boardSize;
            return Mathf.Min(forward, backward);
        }
    }

    /// <summary>
    /// Check if position is valid
    /// </summary>
    public bool IsValidPosition(int position)
    {
        if (rules.separatePlayerBoards)
        {
            int maxPosition = rules.tilesPerSide * rules.tilesPerSide - 1;
            return position >= 0 && position <= maxPosition;
        }
        else
        {
            return position >= 0 && position < rules.tilesPerSide;
        }
    }
}

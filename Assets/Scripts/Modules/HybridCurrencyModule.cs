using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Currency module for hybrid games - pulls from MonopolyGameManager
/// </summary>
public class HybridCurrencyModule : MonoBehaviour
{
    private int startingMoney;

    public void Initialize(int starting)
    {
        startingMoney = starting;
        Debug.Log($"[HybridCurrencyModule] Initialized with ${starting}");
    }

    /// <summary>
    /// Add money to player (from MonopolyGameManager.AddMoney)
    /// </summary>
    public void AddMoney(HybridPlayerData player, int amount)
    {
        player.money += amount;
        Debug.Log($"[HybridCurrencyModule] Player {player.playerId} gained ${amount} (total: ${player.money})");
        
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage($"Player {player.playerId + 1} gained ${amount}");
        }
    }

    /// <summary>
    /// Remove money from player (from MonopolyGameManager.RemoveMoney)
    /// </summary>
    public bool RemoveMoney(HybridPlayerData player, int amount)
    {
        if (player.money >= amount)
        {
            player.money -= amount;
            Debug.Log($"[HybridCurrencyModule] Player {player.playerId} paid ${amount} (remaining: ${player.money})");
            
            if (HybridGameManager.Instance != null)
            {
                HybridGameManager.Instance.BroadcastGameMessage($"Player {player.playerId + 1} paid ${amount}");
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"[HybridCurrencyModule] Player {player.playerId} cannot afford ${amount} (has: ${player.money})");
            return false;
        }
    }

    /// <summary>
    /// Transfer money between players (from MonopolyGameManager trading logic)
    /// </summary>
    public bool TransferMoney(HybridPlayerData fromPlayer, HybridPlayerData toPlayer, int amount)
    {
        if (RemoveMoney(fromPlayer, amount))
        {
            AddMoney(toPlayer, amount);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if player is bankrupt (from MonopolyGameManager)
    /// </summary>
    public bool IsBankrupt(HybridPlayerData player)
    {
        return player.money <= 0;
    }

    /// <summary>
    /// Process currency events on a space (like passing GO, tax spaces, etc.)
    /// </summary>
    public void ProcessCurrencySpace(int playerId, int position, List<HybridPlayerData> players)
    {
        var player = players[playerId];
        
        // Example: Every 10 spaces, give bonus (like passing GO)
        if (position % 10 == 0 && position > 0)
        {
            int bonus = 200;
            AddMoney(player, bonus);
            if (HybridGameManager.Instance != null)
            {
                HybridGameManager.Instance.BroadcastGameMessage($"Player {playerId + 1} passed checkpoint! +${bonus}");
            }
        }
    }

    /// <summary>
    /// Get starting money amount
    /// </summary>
    public int GetStartingMoney()
    {
        return startingMoney;
    }
}

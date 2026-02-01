using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Combat module for hybrid games - pulls from BattleshipsGameManager
/// </summary>
public class HybridCombatModule : MonoBehaviour
{
    private GameRules rules;
    private Dictionary<int, CombatData> playerCombatData = new Dictionary<int, CombatData>();

    public void Initialize(GameRules gameRules)
    {
        rules = gameRules;
        Debug.Log("[HybridCombatModule] Initialized");
    }

    /// <summary>
    /// Initialize combat data for a player
    /// </summary>
    public void InitializePlayerCombat(int playerId, int health = 100)
    {
        if (!playerCombatData.ContainsKey(playerId))
        {
            playerCombatData[playerId] = new CombatData
            {
                playerId = playerId,
                health = health,
                maxHealth = health,
                isAlive = true
            };
            
            Debug.Log($"[HybridCombatModule] Initialized combat for player {playerId}");
        }
    }

    /// <summary>
    /// Process combat space when player lands on it
    /// Inspired by BattleshipsGameManager attack logic
    /// </summary>
    public void ProcessCombatSpace(int playerId, int position, List<HybridPlayerData> players)
    {
        // Combat spaces occur at certain intervals
        if (position % 7 == 0 && position > 0)
        {
            // Combat event - random enemy encounter or PvP
            Debug.Log($"[HybridCombatModule] Player {playerId} entered combat space!");
            
            if (rules.separatePlayerBoards)
            {
                // PvE combat (player vs environment)
                ProcessPvECombat(playerId, players);
            }
            else
            {
                // PvP combat (attack nearest player)
                ProcessPvPCombat(playerId, players);
            }
        }
    }

    /// <summary>
    /// Process Player vs Environment combat
    /// </summary>
    private void ProcessPvECombat(int playerId, List<HybridPlayerData> players)
    {
        InitializePlayerCombat(playerId);
        
        int damage = Random.Range(5, 20);
        var combatData = playerCombatData[playerId];
        
        combatData.health -= damage;
        
        string message = $"Player {playerId + 1} encountered an enemy! Took {damage} damage (Health: {combatData.health}/{combatData.maxHealth})";
        Debug.Log($"[HybridCombatModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }
        
        // Check if player is eliminated
        if (combatData.health <= 0)
        {
            combatData.isAlive = false;
            players[playerId].isActive = false;
            
            string eliminatedMessage = $"Player {playerId + 1} has been eliminated!";
            Debug.Log($"[HybridCombatModule] {eliminatedMessage}");
            if (HybridGameManager.Instance != null)
            {
                HybridGameManager.Instance.BroadcastGameMessage(eliminatedMessage);
            }
        }
    }

    /// <summary>
    /// Process Player vs Player combat
    /// Similar to BattleshipsGameManager attack mechanics
    /// </summary>
    private void ProcessPvPCombat(int attackerId, List<HybridPlayerData> players)
    {
        InitializePlayerCombat(attackerId);
        
        // Find nearest opponent
        int targetId = FindNearestOpponent(attackerId, players);
        if (targetId == -1)
        {
            Debug.Log($"[HybridCombatModule] No valid targets for player {attackerId}");
            return;
        }
        
        InitializePlayerCombat(targetId);
        
        // Execute attack
        int damage = Random.Range(10, 30);
        var targetCombat = playerCombatData[targetId];
        
        targetCombat.health -= damage;
        
        string message = $"Player {attackerId + 1} attacked Player {targetId + 1} for {damage} damage! (Target Health: {targetCombat.health}/{targetCombat.maxHealth})";
        Debug.Log($"[HybridCombatModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }
        
        // Check if target is eliminated
        if (targetCombat.health <= 0)
        {
            targetCombat.isAlive = false;
            players[targetId].isActive = false;
            
            string eliminatedMessage = $"Player {targetId + 1} has been eliminated by Player {attackerId + 1}!";
            Debug.Log($"[HybridCombatModule] {eliminatedMessage}");
            if (HybridGameManager.Instance != null)
            {
                HybridGameManager.Instance.BroadcastGameMessage(eliminatedMessage);
            }
        }
    }

    /// <summary>
    /// Find nearest opponent based on position
    /// </summary>
    private int FindNearestOpponent(int attackerId, List<HybridPlayerData> players)
    {
        int attackerPos = players[attackerId].position;
        int nearestId = -1;
        int minDistance = int.MaxValue;
        
        for (int i = 0; i < players.Count; i++)
        {
            if (i == attackerId || !players[i].isActive) continue;
            
            int distance = Mathf.Abs(players[i].position - attackerPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestId = i;
            }
        }
        
        return nearestId;
    }

    /// <summary>
    /// Direct attack from one player to another
    /// </summary>
    public bool Attack(int attackerId, int targetId, List<HybridPlayerData> players)
    {
        if (!players[targetId].isActive)
        {
            Debug.LogWarning($"[HybridCombatModule] Target {targetId} is not active");
            return false;
        }
        
        InitializePlayerCombat(attackerId);
        InitializePlayerCombat(targetId);
        
        // Calculate damage
        int damage = Random.Range(15, 35);
        var targetCombat = playerCombatData[targetId];
        
        targetCombat.health -= damage;
        
        string message = $"Player {attackerId + 1} attacked Player {targetId + 1} for {damage} damage!";
        Debug.Log($"[HybridCombatModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }
        
        // Check elimination
        if (targetCombat.health <= 0)
        {
            targetCombat.isAlive = false;
            players[targetId].isActive = false;
            return true; // Target eliminated
        }
        
        return false;
    }

    /// <summary>
    /// Heal player
    /// </summary>
    public void Heal(int playerId, int amount)
    {
        InitializePlayerCombat(playerId);
        
        var combatData = playerCombatData[playerId];
        combatData.health = Mathf.Min(combatData.health + amount, combatData.maxHealth);
        
        string message = $"Player {playerId + 1} healed for {amount} HP (Health: {combatData.health}/{combatData.maxHealth})";
        Debug.Log($"[HybridCombatModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }
    }

    /// <summary>
    /// Check if player has won by eliminating all enemies
    /// From BattleshipsGameManager win condition
    /// </summary>
    public bool HasPlayerWon(int playerId, List<HybridPlayerData> players)
    {
        int aliveCount = 0;
        foreach (var player in players)
        {
            if (player.isActive) aliveCount++;
        }
        
        // Player wins if they're the only one alive
        return aliveCount == 1 && players[playerId].isActive;
    }

    /// <summary>
    /// Get combat data for player
    /// </summary>
    public CombatData GetCombatData(int playerId)
    {
        if (!playerCombatData.ContainsKey(playerId))
        {
            InitializePlayerCombat(playerId);
        }
        return playerCombatData[playerId];
    }

    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsPlayerAlive(int playerId)
    {
        if (!playerCombatData.ContainsKey(playerId))
        {
            return true; // Assume alive if not initialized
        }
        return playerCombatData[playerId].isAlive;
    }
}

/// <summary>
/// Combat data for a player
/// </summary>
[System.Serializable]
public class CombatData
{
    public int playerId;
    public int health;
    public int maxHealth;
    public bool isAlive;
}

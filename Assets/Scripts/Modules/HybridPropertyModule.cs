using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Property module for hybrid games - pulls from MonopolyGameManager
/// </summary>
public class HybridPropertyModule : MonoBehaviour
{
    private GameRules rules;
    private HybridCurrencyModule currencyModule;
    private Dictionary<int, PropertyData> properties = new Dictionary<int, PropertyData>();

    public void Initialize(GameRules gameRules, HybridCurrencyModule currency)
    {
        rules = gameRules;
        currencyModule = currency;
        
        // Create properties at certain positions
        GenerateProperties();
        
        Debug.Log($"[HybridPropertyModule] Initialized with {properties.Count} properties");
    }

    /// <summary>
    /// Generate properties on the board
    /// </summary>
    private void GenerateProperties()
    {
        // Generate properties every 3-5 spaces (similar to Monopoly distribution)
        int totalSpaces = rules.tilesPerSide;
        int propertyCount = Mathf.Max(2, totalSpaces / 4); // At least 2 properties
        
        for (int i = 0; i < propertyCount; i++)
        {
            int position = Random.Range(1, totalSpaces);
            
            // Skip if property already exists at this position
            if (properties.ContainsKey(position)) continue;
            
            // Create property with random price
            int basePrice = 100;
            int price = basePrice + (i * 50);
            
            properties[position] = new PropertyData
            {
                position = position,
                purchasePrice = price,
                rentPrice = price / 10,
                ownerId = -1, // -1 means unowned
                propertyName = $"Property {i + 1}"
            };
        }
        
        Debug.Log($"[HybridPropertyModule] Generated {properties.Count} properties");
    }

    /// <summary>
    /// Process property space when player lands on it
    /// Pulls logic from MonopolyGameManager.ProcessPropertySpace
    /// </summary>
    public void ProcessPropertySpace(int playerId, int position, List<HybridPlayerData> players)
    {
        if (!properties.ContainsKey(position))
        {
            return; // Not a property space
        }

        var property = properties[position];
        var player = players[playerId];

        if (property.ownerId == -1)
        {
            // Unowned property - offer to purchase
            if (rules.canPurchaseProperties)
            {
                OfferPropertyPurchase(playerId, position, player, property);
            }
        }
        else if (property.ownerId != playerId)
        {
            // Owned by another player - pay rent
            PayRent(playerId, position, players, property);
        }
        else
        {
            // Player owns this property
            if (HybridGameManager.Instance != null)
            {
                HybridGameManager.Instance.BroadcastGameMessage($"Player {playerId + 1} landed on their own property");
            }
        }
    }

    /// <summary>
    /// Offer property purchase to player
    /// From MonopolyGameManager
    /// </summary>
    private void OfferPropertyPurchase(int playerId, int position, HybridPlayerData player, PropertyData property)
    {
        string message = $"Player {playerId + 1} can purchase {property.propertyName} for ${property.purchasePrice}";
        Debug.Log($"[HybridPropertyModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }
        
        // Auto-purchase if player has enough money (can be expanded to UI prompt)
        if (player.money >= property.purchasePrice)
        {
            PurchaseProperty(playerId, position, player, property);
        }
    }

    /// <summary>
    /// Purchase property
    /// From MonopolyGameManager.PurchaseProperty
    /// </summary>
    public bool PurchaseProperty(int playerId, int position, HybridPlayerData player, PropertyData property)
    {
        if (currencyModule != null)
        {
            if (currencyModule.RemoveMoney(player, property.purchasePrice))
            {
                property.ownerId = playerId;
                player.ownedProperties.Add(position);
                
                string message = $"Player {playerId + 1} purchased {property.propertyName} for ${property.purchasePrice}";
                Debug.Log($"[HybridPropertyModule] {message}");
                if (HybridGameManager.Instance != null)
                {
                    HybridGameManager.Instance.BroadcastGameMessage(message);
                }
                
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Pay rent to property owner
    /// From MonopolyGameManager rent collection
    /// </summary>
    private void PayRent(int playerId, int position, List<HybridPlayerData> players, PropertyData property)
    {
        var player = players[playerId];
        var owner = players[property.ownerId];
        
        if (currencyModule != null)
        {
            int rentAmount = property.rentPrice;
            
            if (currencyModule.RemoveMoney(player, rentAmount))
            {
                currencyModule.AddMoney(owner, rentAmount);
                
                string message = $"Player {playerId + 1} paid ${rentAmount} rent to Player {property.ownerId + 1}";
                Debug.Log($"[HybridPropertyModule] {message}");
                if (HybridGameManager.Instance != null)
                {
                    HybridGameManager.Instance.BroadcastGameMessage(message);
                }
            }
            else
            {
                // Player can't afford rent - bankruptcy
                string message = $"Player {playerId + 1} cannot afford rent! Bankrupt!";
                Debug.Log($"[HybridPropertyModule] {message}");
                if (HybridGameManager.Instance != null)
                {
                    HybridGameManager.Instance.BroadcastGameMessage(message);
                }
                
                player.isActive = false;
            }
        }
    }

    /// <summary>
    /// Trade property between players
    /// From MonopolyTradeManager
    /// </summary>
    public bool TradeProperty(int fromPlayerId, int toPlayerId, int propertyPosition, int money, List<HybridPlayerData> players)
    {
        if (!rules.enablePropertyTrading)
        {
            Debug.LogWarning("[HybridPropertyModule] Property trading is disabled");
            return false;
        }

        if (!properties.ContainsKey(propertyPosition))
        {
            Debug.LogWarning($"[HybridPropertyModule] Property at position {propertyPosition} does not exist");
            return false;
        }

        var property = properties[propertyPosition];
        if (property.ownerId != fromPlayerId)
        {
            Debug.LogWarning($"[HybridPropertyModule] Player {fromPlayerId} does not own property at {propertyPosition}");
            return false;
        }

        var fromPlayer = players[fromPlayerId];
        var toPlayer = players[toPlayerId];

        // Transfer money
        if (money > 0 && currencyModule != null)
        {
            if (!currencyModule.TransferMoney(toPlayer, fromPlayer, money))
            {
                return false;
            }
        }

        // Transfer property
        property.ownerId = toPlayerId;
        fromPlayer.ownedProperties.Remove(propertyPosition);
        toPlayer.ownedProperties.Add(propertyPosition);

        string message = $"Player {fromPlayerId + 1} traded {property.propertyName} to Player {toPlayerId + 1} for ${money}";
        Debug.Log($"[HybridPropertyModule] {message}");
        if (HybridGameManager.Instance != null)
        {
            HybridGameManager.Instance.BroadcastGameMessage(message);
        }

        return true;
    }

    /// <summary>
    /// Get property data at position
    /// </summary>
    public PropertyData GetProperty(int position)
    {
        return properties.ContainsKey(position) ? properties[position] : null;
    }

    /// <summary>
    /// Get all properties
    /// </summary>
    public Dictionary<int, PropertyData> GetAllProperties()
    {
        return properties;
    }

    /// <summary>
    /// Get properties owned by player
    /// </summary>
    public List<PropertyData> GetPlayerProperties(int playerId)
    {
        var playerProps = new List<PropertyData>();
        foreach (var kvp in properties)
        {
            if (kvp.Value.ownerId == playerId)
            {
                playerProps.Add(kvp.Value);
            }
        }
        return playerProps;
    }
}

/// <summary>
/// Property data structure
/// </summary>
[System.Serializable]
public class PropertyData
{
    public int position;
    public string propertyName;
    public int purchasePrice;
    public int rentPrice;
    public int ownerId; // -1 = unowned
}

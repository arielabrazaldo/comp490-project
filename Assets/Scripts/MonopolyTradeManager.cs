using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages property trading between players in Monopoly
/// </summary>
public class MonopolyTradeManager : NetworkBehaviour
{
    private static MonopolyTradeManager instance;
    public static MonopolyTradeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MonopolyTradeManager>();
            }
            return instance;
        }
    }

    // Current active trade proposal
    private NetworkVariable<TradeProposal> currentTrade = new NetworkVariable<TradeProposal>(
        new TradeProposal(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Events for UI
    public static event Action<TradeProposal> OnTradeProposed;
    public static event Action<bool, string> OnTradeResponse; // accepted, message
    public static event Action OnTradeCancelled;
    public static event Action<string> OnGameMessage; // NEW: For trade messages

    [Serializable]
    public struct TradeProposal : INetworkSerializable, IEquatable<TradeProposal>
    {
        public int proposerId;
        public int targetPlayerId;
        public int proposerMoney;
        public int targetMoney;
        public List<int> proposerProperties;
        public List<int> targetProperties;
        public bool isActive;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref proposerId);
            serializer.SerializeValue(ref targetPlayerId);
            serializer.SerializeValue(ref proposerMoney);
            serializer.SerializeValue(ref targetMoney);
            
            // Serialize property lists
            if (serializer.IsReader)
            {
                int proposerCount = 0;
                int targetCount = 0;
                serializer.SerializeValue(ref proposerCount);
                serializer.SerializeValue(ref targetCount);
                
                proposerProperties = new List<int>(proposerCount);
                targetProperties = new List<int>(targetCount);
                
                for (int i = 0; i < proposerCount; i++)
                {
                    int propId = 0;
                    serializer.SerializeValue(ref propId);
                    proposerProperties.Add(propId);
                }
                
                for (int i = 0; i < targetCount; i++)
                {
                    int propId = 0;
                    serializer.SerializeValue(ref propId);
                    targetProperties.Add(propId);
                }
            }
            else
            {
                int proposerCount = proposerProperties?.Count ?? 0;
                int targetCount = targetProperties?.Count ?? 0;
                serializer.SerializeValue(ref proposerCount);
                serializer.SerializeValue(ref targetCount);
                
                if (proposerProperties != null)
                {
                    for (int i = 0; i < proposerCount; i++)
                    {
                        int propId = proposerProperties[i];
                        serializer.SerializeValue(ref propId);
                    }
                }
                
                if (targetProperties != null)
                {
                    for (int i = 0; i < targetCount; i++)
                    {
                        int propId = targetProperties[i];
                        serializer.SerializeValue(ref propId);
                    }
                }
            }
            
            serializer.SerializeValue(ref isActive);
        }

        public bool Equals(TradeProposal other)
        {
            return proposerId == other.proposerId &&
                   targetPlayerId == other.targetPlayerId &&
                   proposerMoney == other.proposerMoney &&
                   targetMoney == other.targetMoney &&
                   isActive == other.isActive;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        currentTrade.OnValueChanged += OnTradeChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentTrade.OnValueChanged -= OnTradeChanged;
    }

    private void OnTradeChanged(TradeProposal previousValue, TradeProposal newValue)
    {
        if (newValue.isActive)
        {
            OnTradeProposed?.Invoke(newValue);
        }
    }

    /// <summary>
    /// Propose a trade to another player
    /// </summary>
    public void ProposeTrade(int targetPlayerId, List<int> offeredProperties, List<int> requestedProperties, int offeredMoney, int requestedMoney)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        // Validate the trade
        if (!ValidateTrade(myPlayerId, targetPlayerId, offeredProperties, offeredMoney))
        {
            OnTradeResponse?.Invoke(false, "Invalid trade proposal!");
            return;
        }
        
        ProposeTradeServerRpc(myPlayerId, targetPlayerId, offeredProperties.ToArray(), requestedProperties.ToArray(), offeredMoney, requestedMoney);
    }

    /// <summary>
    /// Accept the current trade proposal
    /// </summary>
    public void AcceptTrade()
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        if (!currentTrade.Value.isActive || currentTrade.Value.targetPlayerId != myPlayerId)
        {
            OnTradeResponse?.Invoke(false, "No valid trade to accept!");
            return;
        }
        
        RespondToTradeServerRpc(true);
    }

    /// <summary>
    /// Reject the current trade proposal
    /// </summary>
    public void RejectTrade()
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        if (!currentTrade.Value.isActive || currentTrade.Value.targetPlayerId != myPlayerId)
        {
            return;
        }
        
        RespondToTradeServerRpc(false);
    }

    /// <summary>
    /// Cancel an active trade proposal (proposer only)
    /// </summary>
    public void CancelTrade()
    {
        if (MonopolyGameManager.Instance == null) return;
        
        int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
        
        if (!currentTrade.Value.isActive || currentTrade.Value.proposerId != myPlayerId)
        {
            return;
        }
        
        CancelTradeServerRpc();
    }

    private bool ValidateTrade(int proposerId, int targetPlayerId, List<int> offeredProperties, int offeredMoney)
    {
        if (MonopolyGameManager.Instance == null) return false;
        
        // Check if there's already an active trade
        if (currentTrade.Value.isActive)
        {
            return false;
        }
        
        // Check if proposer has enough money
        var proposerData = MonopolyGameManager.Instance.GetPlayer(proposerId);
        if (proposerData.money < offeredMoney)
        {
            return false;
        }
        
        // Check if proposer owns all offered properties
        var ownedProperties = MonopolyGameManager.Instance.GetPlayerProperties(proposerId);
        foreach (int propId in offeredProperties)
        {
            bool owns = false;
            foreach (var ownership in ownedProperties)
            {
                if (ownership.propertyId == propId)
                {
                    // Can't trade mortgaged properties or properties with buildings
                    if (ownership.isMortgaged || ownership.houseCount > 0 || ownership.hasHotel)
                    {
                        return false;
                    }
                    owns = true;
                    break;
                }
            }
            if (!owns) return false;
        }
        
        return true;
    }

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void ProposeTradeServerRpc(int proposerId, int targetPlayerId, int[] offeredProps, int[] requestedProps, int offeredMoney, int requestedMoney, ServerRpcParams rpcParams = default)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        // Double-check validation on server
        var proposerData = MonopolyGameManager.Instance.GetPlayer(proposerId);
        var targetData = MonopolyGameManager.Instance.GetPlayer(targetPlayerId);
        
        if (proposerData.money < offeredMoney || targetData.money < requestedMoney)
        {
            TradeResponseClientRpc(false, "Insufficient funds for trade!", rpcParams.Receive.SenderClientId);
            return;
        }
        
        // Create trade proposal
        var trade = new TradeProposal
        {
            proposerId = proposerId,
            targetPlayerId = targetPlayerId,
            proposerMoney = offeredMoney,
            targetMoney = requestedMoney,
            proposerProperties = new List<int>(offeredProps),
            targetProperties = new List<int>(requestedProps),
            isActive = true
        };
        
        currentTrade.Value = trade;
        
        TradeProposedClientRpc(proposerId, targetPlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespondToTradeServerRpc(bool accepted, ServerRpcParams rpcParams = default)
    {
        if (!currentTrade.Value.isActive || MonopolyGameManager.Instance == null)
        {
            return;
        }
        
        var trade = currentTrade.Value;
        
        if (accepted)
        {
            // Execute the trade
            ExecuteTrade(trade);
            TradeResponseClientRpc(true, "Trade completed successfully!");
        }
        else
        {
            TradeResponseClientRpc(false, "Trade rejected.");
        }
        
        // Clear the trade
        var emptyTrade = new TradeProposal { isActive = false };
        currentTrade.Value = emptyTrade;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CancelTradeServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!currentTrade.Value.isActive) return;
        
        var emptyTrade = new TradeProposal { isActive = false };
        currentTrade.Value = emptyTrade;
        
        TradeCancelledClientRpc();
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void TradeProposedClientRpc(int proposerId, int targetPlayerId)
    {
        string message = $"Player {proposerId + 1} proposed a trade to Player {targetPlayerId + 1}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
    }

    [ClientRpc]
    private void TradeResponseClientRpc(bool accepted, string message, ulong targetClientId = 0)
    {
        OnTradeResponse?.Invoke(accepted, message);
        Debug.Log(message);
    }

    [ClientRpc]
    private void TradeCancelledClientRpc()
    {
        OnTradeCancelled?.Invoke();
        Debug.Log("Trade cancelled.");
    }

    #endregion

    private void ExecuteTrade(TradeProposal trade)
    {
        if (MonopolyGameManager.Instance == null) return;
        
        var proposer = MonopolyGameManager.Instance.GetPlayer(trade.proposerId);
        var target = MonopolyGameManager.Instance.GetPlayer(trade.targetPlayerId);
        
        // Transfer money
        proposer.money -= trade.proposerMoney;
        proposer.money += trade.targetMoney;
        target.money -= trade.targetMoney;
        target.money += trade.proposerMoney;
        
        // Update player data
        MonopolyGameManager.Instance.UpdatePlayerData(trade.proposerId, proposer);
        MonopolyGameManager.Instance.UpdatePlayerData(trade.targetPlayerId, target);
        
        // Transfer properties
        foreach (int propId in trade.proposerProperties)
        {
            MonopolyGameManager.Instance.TransferProperty(propId, trade.targetPlayerId);
        }
        
        foreach (int propId in trade.targetProperties)
        {
            MonopolyGameManager.Instance.TransferProperty(propId, trade.proposerId);
        }
        
        TradeCompletedClientRpc(trade.proposerId, trade.targetPlayerId);
    }

    [ClientRpc]
    private void TradeCompletedClientRpc(int proposerId, int targetPlayerId)
    {
        string message = $"Trade completed between Player {proposerId + 1} and Player {targetPlayerId + 1}!";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
    }

    /// <summary>
    /// Get the current active trade proposal
    /// </summary>
    public TradeProposal GetCurrentTrade()
    {
        return currentTrade.Value;
    }

    /// <summary>
    /// Check if there's an active trade
    /// </summary>
    public bool HasActiveTrade()
    {
        return currentTrade.Value.isActive;
    }
}

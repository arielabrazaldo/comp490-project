using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// OBSOLETE: Use HybridGameManager with GameRules.CreateMonopolyRules() instead.
/// This manager is deprecated and will be removed in a future update.
/// All Monopoly games should now route through HybridGameManager + Modules.
/// See STANDARD_GAME_LIBRARY_IMPLEMENTATION.md for migration guide.
/// </summary>
[Obsolete("Use HybridGameManager with GameRules.CreateMonopolyRules() and CustomGameSpawner instead.")]
public class MonopolyGameManager : NetworkBehaviour
{
    private static MonopolyGameManager instance;
    public static MonopolyGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MonopolyGameManager>();
            }
            return instance;
        }
    }

    [Header("Game Configuration")]
    [SerializeField] private int minPlayers = 1; // Allow single player for testing
    [SerializeField] private int maxPlayers = 8; // Allow up to 8 players
    
    [Header("Game State")]
    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0);
    
    // Player data
    private NetworkList<MonopolyPlayerData> players;
    
    // Board data  
    private List<MonopolySpace> board;
    private NetworkList<PropertyOwnership> propertyOwnerships;
    
    // Game variables
    private NetworkVariable<int> currentDiceRoll = new NetworkVariable<int>(0);
    private NetworkVariable<bool> hasRolledDoubles = new NetworkVariable<bool>(false);
    private NetworkVariable<int> doublesCount = new NetworkVariable<int>(0);
    
    // Card decks
    private List<ChanceCard> chanceCards;
    private List<CommunityChestCard> communityChestCards;
    private int chanceCardIndex = 0;
    private int communityChestCardIndex = 0;
    
    // Events for UI updates
    public static event Action<int> OnPlayerTurnChanged;
    public static event Action<int, int> OnPlayerMoved;
    public static event Action<int, int> OnPlayerMoneyChanged;
    public static event Action<int, int> OnPropertyPurchased;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnGameStarted;
    public static event Action<string> OnGameMessage;
    public static event Action<int, int, int> OnHousePurchased; // playerId, propertyId, houseCount
    public static event Action<int, int> OnHotelPurchased; // playerId, propertyId

    [Serializable]
    public struct MonopolyPlayerData : INetworkSerializable, System.IEquatable<MonopolyPlayerData>
    {
        public int playerId;
        public int money;
        public int position;
        public bool isInJail;
        public int jailTurns;
        public bool isBankrupt;
        public int getOutOfJailFreeCards; // NEW: Track Get Out of Jail Free cards
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref money);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref isInJail);
            serializer.SerializeValue(ref jailTurns);
            serializer.SerializeValue(ref isBankrupt);
            serializer.SerializeValue(ref getOutOfJailFreeCards);
        }

        public bool Equals(MonopolyPlayerData other)
        {
            return playerId == other.playerId &&
                   money == other.money &&
                   position == other.position &&
                   isInJail == other.isInJail &&
                   jailTurns == other.jailTurns &&
                   isBankrupt == other.isBankrupt &&
                   getOutOfJailFreeCards == other.getOutOfJailFreeCards;
        }

        public override bool Equals(object obj)
        {
            return obj is MonopolyPlayerData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(playerId, money, position, isInJail, jailTurns, isBankrupt, getOutOfJailFreeCards);
        }
    }

    [Serializable]
    public struct PropertyOwnership : INetworkSerializable, System.IEquatable<PropertyOwnership>
    {
        public int propertyId;
        public int ownerId;
        public int houseCount;
        public bool hasHotel;
        public bool isMortgaged;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref propertyId);
            serializer.SerializeValue(ref ownerId);
            serializer.SerializeValue(ref houseCount);
            serializer.SerializeValue(ref hasHotel);
            serializer.SerializeValue(ref isMortgaged);
        }

        public bool Equals(PropertyOwnership other)
        {
            return propertyId == other.propertyId &&
                   ownerId == other.ownerId &&
                   houseCount == other.houseCount &&
                   hasHotel == other.hasHotel &&
                   isMortgaged == other.isMortgaged;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyOwnership other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(propertyId, ownerId, houseCount, hasHotel, isMortgaged);
        }
    }

    public enum GameState
    {
        WaitingToStart,
        InProgress,
        GameOver
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Initialize NetworkLists
        players = new NetworkList<MonopolyPlayerData>();
        propertyOwnerships = new NetworkList<PropertyOwnership>();
        
        // Initialize board
        board = MonopolyBoard.CreateStandardBoard();
        
        // Initialize card decks
        InitializeCardDecks();
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to network variable changes
        currentPlayerTurn.OnValueChanged += OnCurrentPlayerTurnChanged;
        gameState.OnValueChanged += OnGameStateValueChanged;
        
        // Subscribe to list changes
        players.OnListChanged += OnPlayersChanged;
        propertyOwnerships.OnListChanged += OnPropertyOwnershipsChanged;

        Debug.Log($"MonopolyGameManager spawned. IsHost: {IsHost}, IsClient: {IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        currentPlayerTurn.OnValueChanged -= OnCurrentPlayerTurnChanged;
        gameState.OnValueChanged -= OnGameStateValueChanged;
        players.OnListChanged -= OnPlayersChanged;
        propertyOwnerships.OnListChanged -= OnPropertyOwnershipsChanged;
    }

    #region Network Event Handlers

    private void OnCurrentPlayerTurnChanged(int previousValue, int newValue)
    {
        Debug.Log($"Player turn changed from {previousValue} to {newValue}");
        OnPlayerTurnChanged?.Invoke(newValue);
    }

    private void OnGameStateValueChanged(GameState previousValue, GameState newValue)
    {
        Debug.Log($"Game state changed from {previousValue} to {newValue}");
        OnGameStateChanged?.Invoke(newValue);
        
        if (newValue == GameState.InProgress)
        {
            OnGameStarted?.Invoke();
        }
    }

    private void OnPlayersChanged(NetworkListEvent<MonopolyPlayerData> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<MonopolyPlayerData>.EventType.Value)
        {
            var player = changeEvent.Value;
            OnPlayerMoneyChanged?.Invoke(player.playerId, player.money);
            OnPlayerMoved?.Invoke(player.playerId, player.position);
        }
    }

    private void OnPropertyOwnershipsChanged(NetworkListEvent<PropertyOwnership> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<PropertyOwnership>.EventType.Add)
        {
            var ownership = changeEvent.Value;
            OnPropertyPurchased?.Invoke(ownership.ownerId, ownership.propertyId);
        }
        else if (changeEvent.Type == NetworkListEvent<PropertyOwnership>.EventType.Value)
        {
            var ownership = changeEvent.Value;
            // Check if houses or hotel were added
            if (ownership.hasHotel)
            {
                OnHotelPurchased?.Invoke(ownership.ownerId, ownership.propertyId);
            }
            else if (ownership.houseCount > 0)
            {
                OnHousePurchased?.Invoke(ownership.ownerId, ownership.propertyId, ownership.houseCount);
            }
        }
    }

    #endregion

    #region Card Deck Initialization

    /// <summary>
    /// Initialize Chance and Community Chest card decks
    /// </summary>
    private void InitializeCardDecks()
    {
        chanceCards = new List<ChanceCard>();
        communityChestCards = new List<CommunityChestCard>();
        
        // Chance Cards
        chanceCards.Add(new ChanceCard { description = "Advance to GO (Collect $200)", type = ChanceCard.ChanceCardType.AdvanceToGo, targetPosition = 0, value = 200 });
        chanceCards.Add(new ChanceCard { description = "Advance to Illinois Avenue", type = ChanceCard.ChanceCardType.AdvanceToSpace, targetPosition = 24 });
        chanceCards.Add(new ChanceCard { description = "Advance to St. Charles Place", type = ChanceCard.ChanceCardType.AdvanceToSpace, targetPosition = 11 });
        chanceCards.Add(new ChanceCard { description = "Advance token to nearest Utility", type = ChanceCard.ChanceCardType.AdvanceToSpace, value = -1 }); // Special handling
        chanceCards.Add(new ChanceCard { description = "Advance token to nearest Railroad", type = ChanceCard.ChanceCardType.AdvanceToSpace, value = -2 }); // Special handling
        chanceCards.Add(new ChanceCard { description = "Bank pays you dividend of $50", type = ChanceCard.ChanceCardType.CollectMoney, value = 50 });
        chanceCards.Add(new ChanceCard { description = "Get Out of Jail Free", type = ChanceCard.ChanceCardType.GetOutOfJail, value = 0 });
        chanceCards.Add(new ChanceCard { description = "Go Back 3 Spaces", type = ChanceCard.ChanceCardType.GoBack, value = 3 });
        chanceCards.Add(new ChanceCard { description = "Go to Jail", type = ChanceCard.ChanceCardType.GoToJail });
        chanceCards.Add(new ChanceCard { description = "Make general repairs on all your property: $25 per house, $100 per hotel", type = ChanceCard.ChanceCardType.Repairs, value = 25 });
        chanceCards.Add(new ChanceCard { description = "Pay poor tax of $15", type = ChanceCard.ChanceCardType.PayMoney, value = 15 });
        chanceCards.Add(new ChanceCard { description = "Take a trip to Reading Railroad", type = ChanceCard.ChanceCardType.AdvanceToSpace, targetPosition = 5 });
        chanceCards.Add(new ChanceCard { description = "Take a walk on the Boardwalk", type = ChanceCard.ChanceCardType.AdvanceToSpace, targetPosition = 39 });
        chanceCards.Add(new ChanceCard { description = "You have been elected Chairman of the Board - Pay each player $50", type = ChanceCard.ChanceCardType.PayPlayers, value = 50 });
        chanceCards.Add(new ChanceCard { description = "Your building loan matures - Collect $150", type = ChanceCard.ChanceCardType.CollectMoney, value = 150 });
        chanceCards.Add(new ChanceCard { description = "You have won a crossword competition - Collect $100", type = ChanceCard.ChanceCardType.CollectMoney, value = 100 });
        
        // Community Chest Cards
        communityChestCards.Add(new CommunityChestCard { description = "Advance to GO (Collect $200)", type = CommunityChestCard.CommunityChestCardType.AdvanceToGo, value = 200 });
        communityChestCards.Add(new CommunityChestCard { description = "Bank error in your favor - Collect $200", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 200 });
        communityChestCards.Add(new CommunityChestCard { description = "Doctor's fees - Pay $50", type = CommunityChestCard.CommunityChestCardType.PayMoney, value = 50 });
        communityChestCards.Add(new CommunityChestCard { description = "From sale of stock you get $50", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 50 });
        communityChestCards.Add(new CommunityChestCard { description = "Get Out of Jail Free", type = CommunityChestCard.CommunityChestCardType.GetOutOfJail });
        communityChestCards.Add(new CommunityChestCard { description = "Go to Jail", type = CommunityChestCard.CommunityChestCardType.GoToJail });
        communityChestCards.Add(new CommunityChestCard { description = "Grand Opera Night - Collect $50 from every player", type = CommunityChestCard.CommunityChestCardType.CollectFromPlayers, value = 50 });
        communityChestCards.Add(new CommunityChestCard { description = "Holiday Fund matures - Receive $100", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 100 });
        communityChestCards.Add(new CommunityChestCard { description = "Income tax refund - Collect $20", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 20 });
        communityChestCards.Add(new CommunityChestCard { description = "It is your birthday - Collect $10 from every player", type = CommunityChestCard.CommunityChestCardType.CollectFromPlayers, value = 10 });
        communityChestCards.Add(new CommunityChestCard { description = "Life insurance matures - Collect $100", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 100 });
        communityChestCards.Add(new CommunityChestCard { description = "Hospital fees - Pay $100", type = CommunityChestCard.CommunityChestCardType.PayMoney, value = 100 });
        communityChestCards.Add(new CommunityChestCard { description = "School fees - Pay $50", type = CommunityChestCard.CommunityChestCardType.PayMoney, value = 50 });
        communityChestCards.Add(new CommunityChestCard { description = "Receive $25 consultancy fee", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 25 });
        communityChestCards.Add(new CommunityChestCard { description = "You are assessed for street repairs: $40 per house, $115 per hotel", type = CommunityChestCard.CommunityChestCardType.Repairs, value = 40 });
        communityChestCards.Add(new CommunityChestCard { description = "You have won second prize in a beauty contest - Collect $10", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 10 });
        communityChestCards.Add(new CommunityChestCard { description = "You inherit $100", type = CommunityChestCard.CommunityChestCardType.CollectMoney, value = 100 });
        
        // Shuffle decks (host will do this when game starts)
        Debug.Log($"Initialized {chanceCards.Count} Chance cards and {communityChestCards.Count} Community Chest cards");
    }
    
    /// <summary>
    /// Shuffle card decks (Host only)
    /// </summary>
    private void ShuffleCardDecks()
    {
        if (!IsHost) return;
        
        // Shuffle Chance cards
        for (int i = chanceCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = chanceCards[i];
            chanceCards[i] = chanceCards[randomIndex];
            chanceCards[randomIndex] = temp;
        }
        
        // Shuffle Community Chest cards
        for (int i = communityChestCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = communityChestCards[i];
            communityChestCards[i] = communityChestCards[randomIndex];
            communityChestCards[randomIndex] = temp;
        }
        
        chanceCardIndex = 0;
        communityChestCardIndex = 0;
        
        Debug.Log("Card decks shuffled");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initialize the Monopoly game with player count (Host only)
    /// </summary>
    public void InitializeGame(int playerCount)
    {
        if (!IsHost)
        {
            Debug.LogWarning("Only the host can initialize the game!");
            return;
        }

        // Ensure we have at least 1 player
        if (playerCount < 1)
        {
            Debug.LogError($"Invalid player count: {playerCount}. Using 1 player instead.");
            playerCount = 1;
        }
        
        // Clamp to allowed range
        playerCount = Mathf.Clamp(playerCount, minPlayers, maxPlayers);
        Debug.Log($"Initializing Monopoly game with {playerCount} players (min: {minPlayers}, max: {maxPlayers})");
        
        totalPlayers.Value = playerCount;
        
        // Initialize players
        players.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            var playerData = new MonopolyPlayerData
            {
                playerId = i,
                money = 1500, // Starting money
                position = 0, // Start at GO
                isInJail = false,
                jailTurns = 0,
                isBankrupt = false,
                getOutOfJailFreeCards = 0 // NEW
            };
            players.Add(playerData);
            Debug.Log($"Created player {i} with $1500");
        }
        
        // Initialize property ownerships
        propertyOwnerships.Clear();
        
        // Shuffle card decks
        ShuffleCardDecks();
        
        // Start the game
        gameState.Value = GameState.InProgress;
        currentPlayerTurn.Value = 0;
        doublesCount.Value = 0;
        
        Debug.Log($"Monopoly game initialized with {players.Count} players");
        
        // Notify all clients that the game has started
        StartGameClientRpc();
    }
    
    /// <summary>
    /// Get the configured min/max player limits
    /// </summary>
    public (int min, int max) GetPlayerLimits()
    {
        return (minPlayers, maxPlayers);
    }
    
    /// <summary>
    /// Get total number of players in the game
    /// </summary>
    public int GetTotalPlayers()
    {
        return totalPlayers.Value;
    }

    /// <summary>
    /// Roll dice for current player (can be called by any client)
    /// </summary>
    public void RollDice()
    {
        if (gameState.Value != GameState.InProgress)
        {
            Debug.LogWarning("Cannot roll dice - game not in progress");
            return;
        }

        if (!IsMyTurn())
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        // Roll two dice
        int die1 = UnityEngine.Random.Range(1, 7);
        int die2 = UnityEngine.Random.Range(1, 7);
        int total = die1 + die2;
        bool isDoubles = die1 == die2;

        Debug.Log($"Player {GetCurrentPlayerId()} rolled: {die1} + {die2} = {total} {(isDoubles ? "(Doubles!)" : "")}");

        // Send roll to server
        RollDiceServerRpc(die1, die2, total, isDoubles);
    }

    /// <summary>
    /// Purchase property (can be called by any client)
    /// </summary>
    public void PurchaseProperty()
    {
        if (!IsMyTurn()) return;
        
        int playerId = GetMyPlayerId();
        var player = players[playerId];
        var space = board[player.position];
        
        if (space.type != PropertyType.Property && space.type != PropertyType.Railroad && space.type != PropertyType.Utility)
        {
            OnGameMessage?.Invoke("This property cannot be purchased!");
            return;
        }
        
        if (IsPropertyOwned(space.spaceId))
        {
            OnGameMessage?.Invoke("This property is already owned!");
            return;
        }
        
        if (player.money < space.price)
        {
            OnGameMessage?.Invoke("Not enough money to purchase this property!");
            return;
        }
        
        PurchasePropertyServerRpc(playerId, space.spaceId);
    }

    /// <summary>
    /// NEW: Buy a house on a property (can be called by any client)
    /// </summary>
    public void BuyHouse(int propertyId)
    {
        if (!IsMyTurn()) return;
        
        int playerId = GetMyPlayerId();
        BuyHouseServerRpc(playerId, propertyId);
    }
    
    /// <summary>
    /// NEW: Buy a hotel on a property (can be called by any client)
    /// </summary>
    public void BuyHotel(int propertyId)
    {
        if (!IsMyTurn()) return;
        
        int playerId = GetMyPlayerId();
        BuyHotelServerRpc(playerId, propertyId);
    }
    
    /// <summary>
    /// NEW: Use Get Out of Jail Free card
    /// </summary>
    public void UseGetOutOfJailFreeCard()
    {
        if (!IsMyTurn()) return;
        
        int playerId = GetMyPlayerId();
        var player = players[playerId];
        
        if (!player.isInJail)
        {
            OnGameMessage?.Invoke("You're not in jail!");
            return;
        }
        
        if (player.getOutOfJailFreeCards <= 0)
        {
            OnGameMessage?.Invoke("You don't have a Get Out of Jail Free card!");
            return;
        }
        
        UseGetOutOfJailFreeCardServerRpc(playerId);
    }
    
    /// <summary>
    /// NEW: Pay to get out of jail
    /// </summary>
    public void PayToGetOutOfJail()
    {
        if (!IsMyTurn()) return;
        
        int playerId = GetMyPlayerId();
        var player = players[playerId];
        
        if (!player.isInJail)
        {
            OnGameMessage?.Invoke("You're not in jail!");
            return;
        }
        
        if (player.money < 50)
        {
            OnGameMessage?.Invoke("Not enough money to pay $50 fine!");
            return;
        }
        
        PayToGetOutOfJailServerRpc(playerId);
    }

    /// <summary>
    /// End current player's turn
    /// </summary>
    public void EndTurn()
    {
        if (!IsMyTurn()) return;
        EndTurnServerRpc();
    }

    #endregion

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void RollDiceServerRpc(int die1, int die2, int total, bool isDoubles, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdFromClientId(senderId);
        
        if (playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Player {playerId} tried to roll out of turn!");
            return;
        }

        currentDiceRoll.Value = total;
        hasRolledDoubles.Value = isDoubles;
        
        var player = players[playerId];
        
        // Handle doubles
        if (isDoubles)
        {
            doublesCount.Value++;
            if (doublesCount.Value >= 3)
            {
                // Go to jail for rolling 3 doubles
                SendPlayerToJail(playerId);
                doublesCount.Value = 0;
                NextPlayerTurn();
                return;
            }
        }
        else
        {
            doublesCount.Value = 0;
        }
        
        // Handle jail
        if (player.isInJail)
        {
            if (isDoubles)
            {
                // Get out of jail
                player.isInJail = false;
                player.jailTurns = 0;
                players[playerId] = player;
                GameMessageClientRpc($"Player {playerId + 1} rolled doubles and got out of jail!");
            }
            else
            {
                player.jailTurns++;
                if (player.jailTurns >= 3)
                {
                    // Must pay to get out
                    player.money -= 50;
                    player.isInJail = false;
                    player.jailTurns = 0;
                    GameMessageClientRpc($"Player {playerId + 1} paid $50 to get out of jail!");
                }
                else
                {
                    players[playerId] = player;
                    GameMessageClientRpc($"Player {playerId + 1} stays in jail. Turn {player.jailTurns}/3");
                    if (!isDoubles) NextPlayerTurn();
                    return;
                }
                players[playerId] = player;
            }
        }
        
        // Move player
        MovePlayer(playerId, total);
        
        // Notify all clients about the move
        PlayerMovedClientRpc(playerId, die1, die2, total, isDoubles, players[playerId].position);
        
        // Check if player gets another turn (rolled doubles and not in jail)
        if (!isDoubles)
        {
            NextPlayerTurn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PurchasePropertyServerRpc(int playerId, int propertyId, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int actualPlayerId = GetPlayerIdFromClientId(senderId);
        
        if (actualPlayerId != playerId || playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Invalid purchase attempt by player {actualPlayerId}");
            return;
        }
        
        var player = players[playerId];
        var space = board[propertyId];
        
        if (player.money >= space.price && !IsPropertyOwned(propertyId))
        {
            // Deduct money
            player.money -= space.price;
            players[playerId] = player;
            
            // Add property ownership
            var ownership = new PropertyOwnership
            {
                propertyId = propertyId,
                ownerId = playerId,
                houseCount = 0,
                hasHotel = false,
                isMortgaged = false
            };
            propertyOwnerships.Add(ownership);
            
            PropertyPurchasedClientRpc(playerId, propertyId, space.price);
        }
    }

    /// <summary>
    /// NEW: Server RPC for buying a house
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void BuyHouseServerRpc(int playerId, int propertyId, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int actualPlayerId = GetPlayerIdFromClientId(senderId);
        
        if (actualPlayerId != playerId || playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Invalid house purchase attempt by player {actualPlayerId}");
            return;
        }
        
        var space = board[propertyId];
        if (space.type != PropertyType.Property)
        {
            GameMessageClientRpc("Can only build houses on properties!");
            return;
        }
        
        // Find property ownership
        int ownershipIndex = -1;
        PropertyOwnership ownership = default;
        for (int i = 0; i < propertyOwnerships.Count; i++)
        {
            if (propertyOwnerships[i].propertyId == propertyId)
            {
                ownership = propertyOwnerships[i];
                ownershipIndex = i;
                break;
            }
        }
        
        if (ownershipIndex == -1 || ownership.ownerId != playerId)
        {
            GameMessageClientRpc("You don't own this property!");
            return;
        }
        
        if (ownership.hasHotel)
        {
            GameMessageClientRpc("This property already has a hotel!");
            return;
        }
        
        if (ownership.houseCount >= 4)
        {
            GameMessageClientRpc("Maximum 4 houses! Buy a hotel instead.");
            return;
        }
        
        // Check if player has monopoly
        if (!HasMonopoly(playerId, space.group))
        {
            GameMessageClientRpc("You need a monopoly to build houses!");
            return;
        }
        
        var player = players[playerId];
        int houseCost = space.houseCost;
        
        if (player.money < houseCost)
        {
            GameMessageClientRpc($"Not enough money! House costs ${houseCost}");
            return;
        }
        
        // Buy the house
        player.money -= houseCost;
        players[playerId] = player;
        
        ownership.houseCount++;
        propertyOwnerships[ownershipIndex] = ownership;
        
        HousePurchasedClientRpc(playerId, propertyId, ownership.houseCount, houseCost);
    }
    
    /// <summary>
    /// NEW: Server RPC for buying a hotel
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void BuyHotelServerRpc(int playerId, int propertyId, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int actualPlayerId = GetPlayerIdFromClientId(senderId);
        
        if (actualPlayerId != playerId || playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Invalid hotel purchase attempt by player {actualPlayerId}");
            return;
        }
        
        var space = board[propertyId];
        if (space.type != PropertyType.Property)
        {
            GameMessageClientRpc("Can only build hotels on properties!");
            return;
        }
        
        // Find property ownership
        int ownershipIndex = -1;
        PropertyOwnership ownership = default;
        for (int i = 0; i < propertyOwnerships.Count; i++)
        {
            if (propertyOwnerships[i].propertyId == propertyId)
            {
                ownership = propertyOwnerships[i];
                ownershipIndex = i;
                break;
            }
        }
        
        if (ownershipIndex == -1 || ownership.ownerId != playerId)
        {
            GameMessageClientRpc("You don't own this property!");
            return;
        }
        
        if (ownership.hasHotel)
        {
            GameMessageClientRpc("This property already has a hotel!");
            return;
        }
        
        if (ownership.houseCount < 4)
        {
            GameMessageClientRpc("You need 4 houses before building a hotel!");
            return;
        }
        
        // Check if player has monopoly
        if (!HasMonopoly(playerId, space.group))
        {
            GameMessageClientRpc("You need a monopoly to build hotels!");
            return;
        }
        
        var player = players[playerId];
        int hotelCost = space.houseCost; // Hotel costs same as a house
        
        if (player.money < hotelCost)
        {
            GameMessageClientRpc($"Not enough money! Hotel costs ${hotelCost}");
            return;
        }
        
        // Buy the hotel
        player.money -= hotelCost;
        players[playerId] = player;
        
        ownership.houseCount = 0; // Remove houses
        ownership.hasHotel = true;
        propertyOwnerships[ownershipIndex] = ownership;
        
        HotelPurchasedClientRpc(playerId, propertyId, hotelCost);
    }
    
    /// <summary>
    /// NEW: Server RPC for using Get Out of Jail Free card
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void UseGetOutOfJailFreeCardServerRpc(int playerId, ServerRpcParams rpcParams = default)
    {
        var player = players[playerId];
        
        if (!player.isInJail || player.getOutOfJailFreeCards <= 0)
        {
            return;
        }
        
        player.isInJail = false;
        player.jailTurns = 0;
        player.getOutOfJailFreeCards--;
        players[playerId] = player;
        
        GameMessageClientRpc($"Player {playerId + 1} used Get Out of Jail Free card!");
    }
    
    /// <summary>
    /// NEW: Server RPC for paying to get out of jail
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void PayToGetOutOfJailServerRpc(int playerId, ServerRpcParams rpcParams = default)
    {
        var player = players[playerId];
        
        if (!player.isInJail || player.money < 50)
        {
            return;
        }
        
        player.isInJail = false;
        player.jailTurns = 0;
        player.money -= 50;
        players[playerId] = player;
        
        GameMessageClientRpc($"Player {playerId + 1} paid $50 to get out of jail!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdFromClientId(senderId);
        
        if (playerId != currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Player {playerId} tried to end turn out of turn!");
            return;
        }
        
        if (!hasRolledDoubles.Value)
        {
            NextPlayerTurn();
        }
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Monopoly game started!");
        
        // Ensure board data is initialized on all clients
        if (board == null || board.Count == 0)
        {
            board = MonopolyBoard.CreateStandardBoard();
            Debug.Log("? Board data initialized on client");
        }
        
        // Generate the board on all clients
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.GenerateBoard();
            Debug.Log("? Board generated on client");
        }
        else
        {
            Debug.LogError("? MonopolyBoardManager.Instance is null!");
        }
        
        // Ensure UI Manager knows about game start
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnNetworkGameStarted();
        }
    }

    [ClientRpc]
    private void PlayerMovedClientRpc(int playerId, int die1, int die2, int total, bool isDoubles, int newPosition)
    {
        string doublesText = isDoubles ? " (Doubles!)" : "";
        string spaceName = board[newPosition].spaceName;
        string message = $"Player {playerId + 1} rolled {die1}+{die2}={total}{doublesText} and landed on {spaceName}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
        
        // Update visual representation
        if (MonopolyBoardManager.Instance != null)
        {
            MonopolyBoardManager.Instance.MovePlayerToken(playerId, newPosition);
        }
    }

    [ClientRpc]
    private void PropertyPurchasedClientRpc(int playerId, int propertyId, int price)
    {
        string message = $"Player {playerId + 1} purchased {board[propertyId].spaceName} for ${price}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
    }

    /// <summary>
    /// NEW: Client RPC for house purchase
    /// </summary>
    [ClientRpc]
    private void HousePurchasedClientRpc(int playerId, int propertyId, int houseCount, int cost)
    {
        string message = $"Player {playerId + 1} built house #{houseCount} on {board[propertyId].spaceName} for ${cost}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
        OnHousePurchased?.Invoke(playerId, propertyId, houseCount);
    }
    
    /// <summary>
    /// NEW: Client RPC for hotel purchase
    /// </summary>
    [ClientRpc]
    private void HotelPurchasedClientRpc(int playerId, int propertyId, int cost)
    {
        string message = $"Player {playerId + 1} built a hotel on {board[propertyId].spaceName} for ${cost}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
        OnHotelPurchased?.Invoke(playerId, propertyId);
    }

    [ClientRpc]
    private void GameMessageClientRpc(string message)
    {
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
    }
    
    /// <summary>
    /// NEW: Client RPC for drawing cards
    /// </summary>
    [ClientRpc]
    private void CardDrawnClientRpc(int playerId, string cardDescription, bool isChance)
    {
        string cardType = isChance ? "Chance" : "Community Chest";
        string message = $"  ? Drew {cardType}: {cardDescription}";
        Debug.Log(message);
        
        // Invoke the event which the UI will append
        OnGameMessage?.Invoke(message);
    }

    /// <summary>
    /// NEW: Client RPC for property transfer
    /// </summary>
    [ClientRpc]
    private void PropertyTransferredClientRpc(int propertyId, int newOwnerId)
    {
        string spaceName = board[propertyId].spaceName;
        string message = $"{spaceName} transferred to Player {newOwnerId + 1}";
        Debug.Log(message);
        OnGameMessage?.Invoke(message);
    }

    #endregion

    #region Private Methods

    private void MovePlayer(int playerId, int spaces)
    {
        var player = players[playerId];
        int oldPosition = player.position;
        int newPosition = (player.position + spaces) % 40;
        
        // Check if player passed GO
        if (newPosition < oldPosition)
        {
            player.money += 200; // Collect $200 for passing GO
            GameMessageClientRpc($"Player {playerId + 1} passed GO and collected $200!");
        }
        
        player.position = newPosition;
        players[playerId] = player;
        
        // Handle landing on space
        HandleSpaceLanding(playerId, newPosition);
    }

    private void HandleSpaceLanding(int playerId, int spaceId)
    {
        var space = board[spaceId];
        var player = players[playerId];
        
        switch (space.type)
        {
            case PropertyType.Property:
            case PropertyType.Railroad:
            case PropertyType.Utility:
                HandlePropertyLanding(playerId, spaceId);
                break;
                
            case PropertyType.Tax:
                HandleTaxSpace(playerId, spaceId);
                break;
                
            case PropertyType.GoToJail:
                SendPlayerToJail(playerId);
                GameMessageClientRpc($"  ? Sent to Jail! (landed on Go To Jail space)");
                break;
                
            case PropertyType.Chance:
                DrawChanceCard(playerId);
                break;
                
            case PropertyType.CommunityChest:
                DrawCommunityChestCard(playerId);
                break;
        }
    }

    private void HandlePropertyLanding(int playerId, int spaceId)
    {
        var space = board[spaceId];
        
        if (!IsPropertyOwned(spaceId))
        {
            GameMessageClientRpc($"Player {playerId + 1} can purchase {space.spaceName} for ${space.price}");
            return;
        }
        
        var ownership = GetPropertyOwnership(spaceId);
        if (ownership.ownerId == playerId)
        {
            GameMessageClientRpc($"Player {playerId + 1} owns {space.spaceName}");
            return;
        }
        
        // Pay rent
        int rent = CalculateRent(spaceId, currentDiceRoll.Value);
        if (rent > 0)
        {
            var player = players[playerId];
            var owner = players[ownership.ownerId];
            
            player.money -= rent;
            owner.money += rent;
            
            players[playerId] = player;
            players[ownership.ownerId] = owner;
            
            GameMessageClientRpc($"Player {playerId + 1} paid ${rent} rent to Player {ownership.ownerId + 1} for {space.spaceName}");
            
            // Check for bankruptcy
            if (player.money < 0)
            {
                HandleBankruptcy(playerId);
            }
        }
    }

    private void HandleTaxSpace(int playerId, int spaceId)
    {
        var player = players[playerId];
        int tax = spaceId == 4 ? 200 : 75; // Income Tax or Luxury Tax
        
        player.money -= tax;
        players[playerId] = player;
        
        string taxName = spaceId == 4 ? "Income Tax" : "Luxury Tax";
        GameMessageClientRpc($"Player {playerId + 1} paid ${tax} for {taxName}");
        
        if (player.money < 0)
        {
            HandleBankruptcy(playerId);
        }
    }

    /// <summary>
    /// NEW: Draw a Chance card
    /// </summary>
    private void DrawChanceCard(int playerId)
    {
        if (!IsHost) return;
        
        var card = chanceCards[chanceCardIndex];
        chanceCardIndex = (chanceCardIndex + 1) % chanceCards.Count;
        
        CardDrawnClientRpc(playerId, card.description, true);
        
        // Execute card effect
        ExecuteChanceCard(playerId, card);
    }
    
    /// <summary>
    /// NEW: Draw a Community Chest card
    /// </summary>
    private void DrawCommunityChestCard(int playerId)
    {
        if (!IsHost) return;
        
        var card = communityChestCards[communityChestCardIndex];
        communityChestCardIndex = (communityChestCardIndex + 1) % communityChestCards.Count;
        
        CardDrawnClientRpc(playerId, card.description, false);
        
        // Execute card effect
        ExecuteCommunityChestCard(playerId, card);
    }
    
    /// <summary>
    /// NEW: Execute Chance card effect
    /// </summary>
    private void ExecuteChanceCard(int playerId, ChanceCard card)
    {
        var player = players[playerId];
        
        switch (card.type)
        {
            case ChanceCard.ChanceCardType.AdvanceToGo:
                player.position = 0;
                player.money += 200;
                players[playerId] = player;
                break;
                
            case ChanceCard.ChanceCardType.AdvanceToSpace:
                if (card.value == -1) // Nearest utility
                {
                    int nearestUtility = player.position < 12 ? 12 : (player.position < 28 ? 28 : 12);
                    player.position = nearestUtility;
                }
                else if (card.value == -2) // Nearest railroad
                {
                    int[] railroads = { 5, 15, 25, 35 };
                    int nearestRailroad = 5;
                    foreach (int railroad in railroads)
                    {
                        if (railroad > player.position)
                        {
                            nearestRailroad = railroad;
                            break;
                        }
                    }
                    player.position = nearestRailroad;
                }
                else
                {
                    player.position = card.targetPosition;
                }
                players[playerId] = player;
                HandleSpaceLanding(playerId, player.position);
                break;
                
            case ChanceCard.ChanceCardType.GoBack:
                int newPos = (player.position - card.value + 40) % 40;
                player.position = newPos;
                players[playerId] = player;
                HandleSpaceLanding(playerId, player.position);
                break;
                
            case ChanceCard.ChanceCardType.CollectMoney:
                player.money += card.value;
                players[playerId] = player;
                break;
                
            case ChanceCard.ChanceCardType.PayMoney:
                player.money -= card.value;
                players[playerId] = player;
                if (player.money < 0) HandleBankruptcy(playerId);
                break;
                
            case ChanceCard.ChanceCardType.PayPlayers:
                int totalPaid = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    if (i != playerId)
                    {
                        var otherPlayer = players[i];
                        otherPlayer.money += card.value;
                        players[i] = otherPlayer;
                        player.money -= card.value;
                        totalPaid += card.value;
                    }
                }
                players[playerId] = player;
                if (player.money < 0) HandleBankruptcy(playerId);
                break;
                
            case ChanceCard.ChanceCardType.GoToJail:
                SendPlayerToJail(playerId);
                break;
                
            case ChanceCard.ChanceCardType.GetOutOfJail:
                player.getOutOfJailFreeCards++;
                players[playerId] = player;
                break;
                
            case ChanceCard.ChanceCardType.Repairs:
                int repairCost = CalculateRepairCost(playerId, card.value, card.value * 4);
                player.money -= repairCost;
                players[playerId] = player;
                if (player.money < 0) HandleBankruptcy(playerId);
                break;
        }
    }
    
    /// <summary>
    /// NEW: Execute Community Chest card effect
    /// </summary>
    private void ExecuteCommunityChestCard(int playerId, CommunityChestCard card)
    {
        var player = players[playerId];
        
        switch (card.type)
        {
            case CommunityChestCard.CommunityChestCardType.AdvanceToGo:
                player.position = 0;
                player.money += 200;
                players[playerId] = player;
                break;
                
            case CommunityChestCard.CommunityChestCardType.CollectMoney:
                player.money += card.value;
                players[playerId] = player;
                break;
                
            case CommunityChestCard.CommunityChestCardType.PayMoney:
                player.money -= card.value;
                players[playerId] = player;
                if (player.money < 0) HandleBankruptcy(playerId);
                break;
                
            case CommunityChestCard.CommunityChestCardType.CollectFromPlayers:
                int totalCollected = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    if (i != playerId)
                    {
                        var otherPlayer = players[i];
                        otherPlayer.money -= card.value;
                        players[i] = otherPlayer;
                        player.money += card.value;
                        totalCollected += card.value;
                    }
                }
                players[playerId] = player;
                break;
                
            case CommunityChestCard.CommunityChestCardType.GoToJail:
                SendPlayerToJail(playerId);
                break;
                
            case CommunityChestCard.CommunityChestCardType.GetOutOfJail:
                player.getOutOfJailFreeCards++;
                players[playerId] = player;
                break;
                
            case CommunityChestCard.CommunityChestCardType.Repairs:
                int repairCost = CalculateRepairCost(playerId, card.value, card.value == 40 ? 115 : card.value * 4);
                player.money -= repairCost;
                players[playerId] = player;
                if (player.money < 0) HandleBankruptcy(playerId);
                break;
        }
    }
    
    /// <summary>
    /// NEW: Calculate repair costs based on houses and hotels
    /// </summary>
    private int CalculateRepairCost(int playerId, int perHouse, int perHotel)
    {
        int totalCost = 0;
        
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.ownerId == playerId)
            {
                if (ownership.hasHotel)
                {
                    totalCost += perHotel;
                }
                else
                {
                    totalCost += ownership.houseCount * perHouse;
                }
            }
        }
        
        return totalCost;
    }

    private void SendPlayerToJail(int playerId)
    {
        var player = players[playerId];
        player.position = 10; // Jail position
        player.isInJail = true;
        player.jailTurns = 0;
        players[playerId] = player;
        
        // Message will be sent by caller (card effect, go to jail space, or 3 doubles)
    }

    private void HandleBankruptcy(int playerId)
    {
        var player = players[playerId];
        player.isBankrupt = true;
        players[playerId] = player;
        
        GameMessageClientRpc($"Player {playerId + 1} is bankrupt!");
        
        // Check for game end
        int activePlayers = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].isBankrupt) activePlayers++;
        }
        
        if (activePlayers <= 1)
        {
            gameState.Value = GameState.GameOver;
            GameMessageClientRpc("Game Over! Last player standing wins!");
        }
    }

    private void NextPlayerTurn()
    {
        doublesCount.Value = 0;
        
        // Find next non-bankrupt player
        int nextPlayer = (currentPlayerTurn.Value + 1) % totalPlayers.Value;
        while (players[nextPlayer].isBankrupt && nextPlayer != currentPlayerTurn.Value)
        {
            nextPlayer = (nextPlayer + 1) % totalPlayers.Value;
        }
        
        currentPlayerTurn.Value = nextPlayer;
    }

    private bool IsPropertyOwned(int propertyId)
    {
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.propertyId == propertyId)
                return true;
        }
        return false;
    }

    private PropertyOwnership GetPropertyOwnership(int propertyId)
    {
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.propertyId == propertyId)
                return ownership;
        }
        return default;
    }

    private int CalculateRent(int spaceId, int diceRoll)
    {
        var space = board[spaceId];
        var ownership = GetPropertyOwnership(spaceId);
        
        if (ownership.isMortgaged) return 0;
        
        switch (space.type)
        {
            case PropertyType.Property:
                if (ownership.hasHotel) return space.rentWithHouses[4];
                if (ownership.houseCount > 0) return space.rentWithHouses[ownership.houseCount - 1];
                
                // Check for monopoly
                if (HasMonopoly(ownership.ownerId, space.group))
                    return space.rent * 2;
                return space.rent;
                
            case PropertyType.Railroad:
                int railroadCount = CountOwnedRailroads(ownership.ownerId);
                return 25 * (int)Mathf.Pow(2, railroadCount - 1);
                
            case PropertyType.Utility:
                int utilityCount = CountOwnedUtilities(ownership.ownerId);
                return diceRoll * (utilityCount == 1 ? 4 : 10);
        }
        
        return 0;
    }

    private bool HasMonopoly(int playerId, PropertyGroup group)
    {
        var groupProperties = new List<int>();
        for (int i = 0; i < board.Count; i++)
        {
            if (board[i].group == group && board[i].type == PropertyType.Property)
                groupProperties.Add(i);
        }
        
        foreach (int propertyId in groupProperties)
        {
            var ownership = GetPropertyOwnership(propertyId);
            if (ownership.ownerId != playerId)
                return false;
        }
        
        return groupProperties.Count > 0;
    }

    private int CountOwnedRailroads(int playerId)
    {
        int count = 0;
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.ownerId == playerId && board[ownership.propertyId].type == PropertyType.Railroad)
                count++;
        }
        return count;
    }

    private int CountOwnedUtilities(int playerId)
    {
        int count = 0;
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.ownerId == playerId && board[ownership.propertyId].type == PropertyType.Utility)
                count++;
        }
        return count;
    }

    #endregion

    #region Utility Methods

    public bool IsMyTurn()
    {
        if (!IsSpawned) return false;
        
        int currentPlayerId = GetCurrentPlayerId();
        int myPlayerId = GetMyPlayerId();
        
        return currentPlayerId == myPlayerId;
    }

    public int GetCurrentPlayerId()
    {
        return currentPlayerTurn.Value;
    }

    public int GetMyPlayerId()
    {
        if (!IsSpawned) return -1;
        
        var connectedClients = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        var sortedClientIds = new List<ulong>(connectedClients);
        sortedClientIds.Sort();
        
        var myClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        
        return sortedClientIds.IndexOf(myClientId);
    }

    private int GetPlayerIdFromClientId(ulong clientId)
    {
        var connectedClients = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        var sortedClientIds = new List<ulong>(connectedClients);
        sortedClientIds.Sort();
        
        return sortedClientIds.IndexOf(clientId);
    }

    public GameState GetGameState()
    {
        return gameState.Value;
    }

    public MonopolyPlayerData GetPlayer(int playerId)
    {
        if (playerId >= 0 && playerId < players.Count)
            return players[playerId];
        return default;
    }

    public MonopolySpace GetSpace(int spaceId)
    {
        if (spaceId >= 0 && spaceId < board.Count)
            return board[spaceId];
        return null;
    }

    public List<PropertyOwnership> GetPlayerProperties(int playerId)
    {
        var properties = new List<PropertyOwnership>();
        foreach (var ownership in propertyOwnerships)
        {
            if (ownership.ownerId == playerId)
                properties.Add(ownership);
        }
        return properties;
    }
    
    /// <summary>
    /// NEW: Check if player can build houses on a property
    /// </summary>
    public bool CanBuildHouse(int playerId, int propertyId)
    {
        var space = board[propertyId];
        if (space.type != PropertyType.Property) return false;
        
        var ownership = GetPropertyOwnership(propertyId);
        if (ownership.ownerId != playerId) return false;
        if (ownership.hasHotel) return false;
        if (ownership.houseCount >= 4) return false;
        if (!HasMonopoly(playerId, space.group)) return false;
        
        var player = players[playerId];
        return player.money >= space.houseCost;
    }
    
    /// <summary>
    /// NEW: Check if player can build a hotel on a property
    /// </summary>
    public bool CanBuildHotel(int playerId, int propertyId)
    {
        var space = board[propertyId];
        if (space.type != PropertyType.Property) return false;
        
        var ownership = GetPropertyOwnership(propertyId);
        if (ownership.ownerId != playerId) return false;
        if (ownership.hasHotel) return false;
        if (ownership.houseCount < 4) return false;
        if (!HasMonopoly(playerId, space.group)) return false;
        
        var player = players[playerId];
        return player.money >= space.houseCost;
    }

    /// <summary>
    /// NEW: Update player data (for trading)
    /// </summary>
    public void UpdatePlayerData(int playerId, MonopolyPlayerData newData)
    {
        if (!IsHost) return;
        
        if (playerId >= 0 && playerId < players.Count)
        {
            players[playerId] = newData;
        }
    }
    
    /// <summary>
    /// NEW: Transfer property ownership (for trading)
    /// </summary>
    public void TransferProperty(int propertyId, int newOwnerId)
    {
        if (!IsHost) return;
        
        for (int i = 0; i < propertyOwnerships.Count; i++)
        {
            if (propertyOwnerships[i].propertyId == propertyId)
            {
                var ownership = propertyOwnerships[i];
                ownership.ownerId = newOwnerId;
                propertyOwnerships[i] = ownership;
                
                PropertyTransferredClientRpc(propertyId, newOwnerId);
                break;
            }
        }
    }

    #endregion
}
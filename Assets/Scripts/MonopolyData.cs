using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public enum PropertyType
{
    Property,
    Railroad,
    Utility,
    Tax,
    Chance,
    CommunityChest,
    Go,
    Jail,
    GoToJail,
    FreeParking
}

[Serializable]
public enum PropertyGroup
{
    Brown,
    LightBlue,
    Pink,
    Orange,
    Red,
    Yellow,
    Green,
    DarkBlue,
    Railroad,
    Utility,
    Special
}

[Serializable]
public class MonopolySpace
{
    public int spaceId;
    public string spaceName;
    public PropertyType type;
    public PropertyGroup group;
    public int price;
    public int rent;
    public int[] rentWithHouses = new int[5]; // 1-4 houses + hotel
    public int houseCost;
    public int mortgageValue;
    public Color spaceColor = Color.white;
    public string description;
    
    // Special properties
    public bool isOwned;
    public int ownerId = -1;
    public int houseCount;
    public bool hasHotel;
    public bool isMortgaged;

    public MonopolySpace(int id, string name, PropertyType propertyType, PropertyGroup propertyGroup, int cost = 0)
    {
        spaceId = id;
        spaceName = name;
        type = propertyType;
        group = propertyGroup;
        price = cost;
        mortgageValue = cost / 2;
        
        // Set default colors for property groups
        switch (group)
        {
            case PropertyGroup.Brown:
                spaceColor = new Color(0.55f, 0.27f, 0.07f); // Brown
                break;
            case PropertyGroup.LightBlue:
                spaceColor = new Color(0.68f, 0.85f, 0.90f); // Light Blue
                break;
            case PropertyGroup.Pink:
                spaceColor = new Color(1f, 0.75f, 0.80f); // Pink
                break;
            case PropertyGroup.Orange:
                spaceColor = new Color(1f, 0.65f, 0f); // Orange
                break;
            case PropertyGroup.Red:
                spaceColor = Color.red;
                break;
            case PropertyGroup.Yellow:
                spaceColor = Color.yellow;
                break;
            case PropertyGroup.Green:
                spaceColor = Color.green;
                break;
            case PropertyGroup.DarkBlue:
                spaceColor = new Color(0f, 0f, 0.55f); // Dark Blue
                break;
            case PropertyGroup.Railroad:
                spaceColor = Color.black;
                break;
            case PropertyGroup.Utility:
                spaceColor = Color.gray;
                break;
            default:
                spaceColor = Color.white;
                break;
        }
    }

    public int GetCurrentRent()
    {
        if (isMortgaged || !isOwned) return 0;
        
        if (hasHotel) return rentWithHouses[4];
        if (houseCount > 0) return rentWithHouses[houseCount - 1];
        return rent;
    }
}

[Serializable]
public class MonopolyPlayer
{
    public int playerId;
    public string playerName;
    public int money;
    public int currentPosition;
    public bool isInJail;
    public int jailTurns;
    public bool hasGetOutOfJailCard;
    public List<int> ownedProperties;
    public bool isBankrupt;
    
    public MonopolyPlayer(int id, string name)
    {
        playerId = id;
        playerName = name;
        money = 1500; // Starting money in Monopoly
        currentPosition = 0; // Start at GO
        isInJail = false;
        jailTurns = 0;
        hasGetOutOfJailCard = false;
        ownedProperties = new List<int>();
        isBankrupt = false;
    }
}

public static class MonopolyBoard
{
    public static List<MonopolySpace> CreateStandardBoard()
    {
        var spaces = new List<MonopolySpace>();
        
        // Standard Monopoly board - 40 spaces
        spaces.Add(CreateSpecialSpace(0, "GO", PropertyType.Go, new Color(0.2f, 0.8f, 0.2f))); // Green for GO
        spaces.Add(CreateProperty(1, "Mediterranean Avenue", PropertyGroup.Brown, 60, 2, new int[]{10, 30, 90, 160, 250}, 50));
        spaces.Add(CreateSpecialSpace(2, "Community Chest", PropertyType.CommunityChest, new Color(1f, 0.9f, 0.6f))); // Light yellow
        spaces.Add(CreateProperty(3, "Baltic Avenue", PropertyGroup.Brown, 60, 4, new int[]{20, 60, 180, 320, 450}, 50));
        spaces.Add(CreateSpecialSpace(4, "Income Tax", PropertyType.Tax, new Color(0.9f, 0.9f, 0.9f))); // Light gray
        spaces.Add(CreateRailroad(5, "Reading Railroad", 200));
        spaces.Add(CreateProperty(6, "Oriental Avenue", PropertyGroup.LightBlue, 100, 6, new int[]{30, 90, 270, 400, 550}, 50));
        spaces.Add(CreateSpecialSpace(7, "Chance", PropertyType.Chance, new Color(1f, 0.7f, 0.5f))); // Light orange
        spaces.Add(CreateProperty(8, "Vermont Avenue", PropertyGroup.LightBlue, 100, 6, new int[]{30, 90, 270, 400, 550}, 50));
        spaces.Add(CreateProperty(9, "Connecticut Avenue", PropertyGroup.LightBlue, 120, 8, new int[]{40, 100, 300, 450, 600}, 50));
        
        spaces.Add(CreateSpecialSpace(10, "Jail/Just Visiting", PropertyType.Jail, new Color(1f, 0.6f, 0.2f))); // Orange
        spaces.Add(CreateProperty(11, "St. Charles Place", PropertyGroup.Pink, 140, 10, new int[]{50, 150, 450, 625, 750}, 100));
        spaces.Add(CreateUtility(12, "Electric Company", 150));
        spaces.Add(CreateProperty(13, "States Avenue", PropertyGroup.Pink, 140, 10, new int[]{50, 150, 450, 625, 750}, 100));
        spaces.Add(CreateProperty(14, "Virginia Avenue", PropertyGroup.Pink, 160, 12, new int[]{60, 180, 500, 700, 900}, 100));
        spaces.Add(CreateRailroad(15, "Pennsylvania Railroad", 200));
        spaces.Add(CreateProperty(16, "St. James Place", PropertyGroup.Orange, 180, 14, new int[]{70, 200, 550, 750, 950}, 100));
        spaces.Add(CreateSpecialSpace(17, "Community Chest", PropertyType.CommunityChest, new Color(1f, 0.9f, 0.6f))); // Light yellow
        spaces.Add(CreateProperty(18, "Tennessee Avenue", PropertyGroup.Orange, 180, 14, new int[]{70, 200, 550, 750, 950}, 100));
        spaces.Add(CreateProperty(19, "New York Avenue", PropertyGroup.Orange, 200, 16, new int[]{80, 220, 600, 800, 1000}, 100));
        
        spaces.Add(CreateSpecialSpace(20, "Free Parking", PropertyType.FreeParking, new Color(0.8f, 0.2f, 0.2f))); // Red
        spaces.Add(CreateProperty(21, "Kentucky Avenue", PropertyGroup.Red, 220, 18, new int[]{90, 250, 700, 875, 1050}, 150));
        spaces.Add(CreateSpecialSpace(22, "Chance", PropertyType.Chance, new Color(1f, 0.7f, 0.5f))); // Light orange
        spaces.Add(CreateProperty(23, "Indiana Avenue", PropertyGroup.Red, 220, 18, new int[]{90, 250, 700, 875, 1050}, 150));
        spaces.Add(CreateProperty(24, "Illinois Avenue", PropertyGroup.Red, 240, 20, new int[]{100, 300, 750, 925, 1100}, 150));
        spaces.Add(CreateRailroad(25, "B. & O. Railroad", 200));
        spaces.Add(CreateProperty(26, "Atlantic Avenue", PropertyGroup.Yellow, 260, 22, new int[]{110, 330, 800, 975, 1150}, 150));
        spaces.Add(CreateProperty(27, "Ventnor Avenue", PropertyGroup.Yellow, 260, 22, new int[]{110, 330, 800, 975, 1150}, 150));
        spaces.Add(CreateUtility(28, "Water Works", 150));
        spaces.Add(CreateProperty(29, "Marvin Gardens", PropertyGroup.Yellow, 280, 24, new int[]{120, 360, 850, 1025, 1200}, 150));
        
        spaces.Add(CreateSpecialSpace(30, "Go To Jail", PropertyType.GoToJail, new Color(0.2f, 0.2f, 0.2f))); // Dark gray
        spaces.Add(CreateProperty(31, "Pacific Avenue", PropertyGroup.Green, 300, 26, new int[]{130, 390, 900, 1100, 1275}, 200));
        spaces.Add(CreateProperty(32, "North Carolina Avenue", PropertyGroup.Green, 300, 26, new int[]{130, 390, 900, 1100, 1275}, 200));
        spaces.Add(CreateSpecialSpace(33, "Community Chest", PropertyType.CommunityChest, new Color(1f, 0.9f, 0.6f))); // Light yellow
        spaces.Add(CreateProperty(34, "Pennsylvania Avenue", PropertyGroup.Green, 320, 28, new int[]{150, 450, 1000, 1200, 1400}, 200));
        spaces.Add(CreateRailroad(35, "Short Line", 200));
        spaces.Add(CreateSpecialSpace(36, "Chance", PropertyType.Chance, new Color(1f, 0.7f, 0.5f))); // Light orange
        spaces.Add(CreateProperty(37, "Park Place", PropertyGroup.DarkBlue, 350, 35, new int[]{175, 500, 1100, 1300, 1500}, 200));
        spaces.Add(CreateSpecialSpace(38, "Luxury Tax", PropertyType.Tax, new Color(0.9f, 0.9f, 0.9f))); // Light gray
        spaces.Add(CreateProperty(39, "Boardwalk", PropertyGroup.DarkBlue, 400, 50, new int[]{200, 600, 1400, 1700, 2000}, 200));
        
        return spaces;
    }
    
    private static MonopolySpace CreateProperty(int id, string name, PropertyGroup group, int price, int baseRent, int[] rentWithHouses, int houseCost)
    {
        var space = new MonopolySpace(id, name, PropertyType.Property, group, price);
        space.rent = baseRent;
        space.rentWithHouses = rentWithHouses;
        space.houseCost = houseCost;
        return space;
    }
    
    private static MonopolySpace CreateRailroad(int id, string name, int price)
    {
        var space = new MonopolySpace(id, name, PropertyType.Railroad, PropertyGroup.Railroad, price);
        space.rent = 25; // Base rent for 1 railroad
        return space;
    }
    
    private static MonopolySpace CreateUtility(int id, string name, int price)
    {
        var space = new MonopolySpace(id, name, PropertyType.Utility, PropertyGroup.Utility, price);
        space.rent = 0; // Calculated based on dice roll
        return space;
    }

    private static MonopolySpace CreateSpecialSpace(int id, string name, PropertyType type, Color color)
    {
        var space = new MonopolySpace(id, name, type, PropertyGroup.Special, 0);
        space.spaceColor = color;
        return space;
    }
}

[Serializable]
public class ChanceCard
{
    public string description;
    public ChanceCardType type;
    public int value;
    public int targetPosition;
    
    public enum ChanceCardType
    {
        AdvanceToGo,
        AdvanceToSpace,
        GoBack,
        PayMoney,
        CollectMoney,
        PayPlayers,
        CollectFromPlayers,
        GoToJail,
        GetOutOfJail,
        Repairs
    }
}

[Serializable]
public class CommunityChestCard
{
    public string description;
    public CommunityChestCardType type;
    public int value;
    
    public enum CommunityChestCardType
    {
        CollectMoney,
        PayMoney,
        GoToJail,
        GetOutOfJail,
        AdvanceToGo,
        CollectFromPlayers,
        PayPlayers,
        Repairs
    }
}
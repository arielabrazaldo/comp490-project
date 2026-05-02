using System;
using UnityEngine;

/// <summary>
/// Game Type Constants:
/// 0 = Unknown
/// 1 = Monopoly
/// 2 = Battleships
/// 3 = Dice Race
/// 4 = Hybrid/Custom
/// </summary>
[Serializable]
public class SavedGameInfo
{
    public string gameName;
    public int gameType;  // 1=Monopoly, 2=Battleships, 3=DiceRace, 4=Hybrid
    public int playerCount;
    public GameRules rules;
    public bool isStandardGame; // True for built-in games, false for custom games
    public DateTime createdDate;
    public DateTime lastModifiedDate;

    /// <summary>
    /// Constructor for games
    /// </summary>
    public SavedGameInfo(string gameName, int gameType, int playerCount, GameRules rules, bool isStandardGame = false)
    {
        this.gameName = gameName;
        this.gameType = gameType;
        this.playerCount = playerCount;
        this.rules = rules;
        this.isStandardGame = isStandardGame;
        this.createdDate = DateTime.Now;
        this.lastModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Get display name for the game type
    /// </summary>
    public string GetGameTypeName()
    {
        return gameType switch
        {
            1 => "Monopoly",
            2 => "Battleships",
            3 => "Dice Race",
            4 => "Hybrid",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get a display-friendly description of the game
    /// </summary>
    public string GetDescription()
    {
        string typeName = GetGameTypeName();
        if (isStandardGame)
        {
            return $"Standard {typeName} • {playerCount} Players";
        }
        else
        {
            return $"Custom {typeName} • {playerCount} Players \n" +
            $"Modified {lastModifiedDate:MMM dd h:mm tt}";
        }
    }

    /// <summary>
    /// Get a short summary of the game
    /// </summary>
    public string GetShortSummary()
    {
        return $"{gameName} ({playerCount}P)";
    }

    /// <summary>
    /// Clone this saved game info
    /// </summary>
    public SavedGameInfo Clone()
    {
        return new SavedGameInfo(
            gameName,
            gameType,
            playerCount,
            rules?.Clone(),
            isStandardGame
        )
        {
            createdDate = this.createdDate,
            lastModifiedDate = this.lastModifiedDate
        };
    }
}

using System;
using UnityEngine;

/// <summary>
/// Represents a saved game configuration that can be loaded and hosted.
/// Used for both standard games (Monopoly, Battleships, Dice Race)
/// and custom user-created games.
/// </summary>
[Serializable]
public class SavedGameInfo
{
    public string gameName;
    public string gameType;
    public int playerCount;
    public GameRules rules;
    public bool isStandardGame; // True for built-in games, false for custom games
    public DateTime createdDate;
    public DateTime lastModifiedDate;

    /// <summary>
    /// Constructor for standard games
    /// </summary>
    public SavedGameInfo(string gameName, string gameType, int playerCount, GameRules rules, bool isStandardGame = false)
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
    /// Get a display-friendly description of the game
    /// </summary>
    public string GetDescription()
    {
        if (isStandardGame)
        {
            return $"Standard {gameType} • {playerCount} Players";
        }
        else
        {
            return $"Custom {gameType} • {playerCount} Players • Modified {lastModifiedDate:MMM dd}";
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

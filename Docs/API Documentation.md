Draft API and User Documentation

**Name and Short Description:** AnalyzeGameRules — Analyzes a GameRules configuration and determines the closest matching game type (Monopoly, Battleships, DiceRace, or Hybrid). This is the core method that bridges the Rule Editor with game spawning by classifying custom rule sets.

**Signature:** public DetectedGameType AnalyzeGameRules(GameRules rules)

**Parameters:**

| Name | Type | Description |
| :---- | :---- | :---- |
| rules | GameRules | The game rules configuration to analyze. Contains settings for currency, properties, combat, board layout, and win conditions. |

**Return Value:** 

| Type | Description |
| :---- | :---- |
| DetectedGameType | An enum indicating the detected game type. Returns a single enum value and the method never returns null. |

**DetectedGameType Values:**

| Value | Description |
| :---- | :---- |
| Monopoly | Property trading game with currency, rent, and bankruptcy |
| Battleships | Naval combat with hidden boards and ship placement |
| DiceRace | Simple race to a goal with no complex mechanics |
| Hybrid | Custom blend when no single type scores ≥ 0.6 |
| Unknown | Could not determine type (null rules) |

**Errors / Exceptions:**

| Condition | Behavior |
| :---- | :---- |
| rules is null | Logs error, returns DetectedGameType.Unknown |

**Example Usage:**

GameRules customRules \= new GameRules  
{  
    enableCurrency \= true,  
    canPurchaseProperties \= true,  
    enableRentCollection \= true,  
    separatePlayerBoards \= false  
};

DetectedGameType gameType \= CustomGameAnalyzer.Instance.AnalyzeGameRules(customRules);

if (gameType \== DetectedGameType.Monopoly)  
{  
    // Spawn Monopoly game manager  
    CustomGameSpawner.Instance.SpawnMonopolyGame(customRules);  
}  
else if (gameType \== DetectedGameType.Hybrid)  
{  
    // Spawn hybrid game with modular components  
    CustomGameSpawner.Instance.SpawnHybridGame(customRules);  
}

**Important Notes:**

* Singleton pattern: Access via CustomGameAnalyzer.Instance (auto-creates if missing)  
* Hybrid threshold: Returns Hybrid when max score \< 0.6, allowing HybridGameManager to handle mixed rule sets  
* Debug logging: Outputs detailed score breakdown to Unity console for troubleshooting



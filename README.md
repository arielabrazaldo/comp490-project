# BoardSmith

A Unity-based board game framework that supports multiple game types — including **Monopoly**, **Battleships**, and **Dice Race** — as well as fully custom hybrid games designed through an in-game Rule Editor and Board Editor.

---

## Features

### Game Modes
| Mode | Description |
|------|-------------|
| **Monopoly** | Classic property-buying, rent-collection board game with trading and bankruptcy |
| **Battleships** | Separate-board combat game with ship placement, HP, and attack mechanics |
| **Dice Race** | First-to-reach-the-goal racing game with configurable win tiles |
| **Custom / Hybrid** | Player-designed games combining rules from all of the above |

### Rule Editor
Configure every aspect of your game through a live UI panel:
- **Currency System** — Enable money, set starting amounts and Pass-GO bonuses
- **Board Settings** — Shared or separate player boards, enemy board visibility
- **Movement** — Standard dice, custom dice, fixed movement, or non-linear (adjacent-tile) movement
- **Resource System** — Named resources with optional per-type caps
- **Victory Conditions** — Last player standing, money threshold, reach a specific tile
- **Combat Settings** — Combat range (land-on / adjacent / infinite), HP, static or dice-roll damage, special turn rules
- **Advanced Settings** — Bankruptcy, trading, attacked tiles blocking, board-attack restrictions
- **Cascading Toggles** — Dependent settings automatically enable/disable based on parent toggles

### Board Editor
- Visual tile-based board designer
- Designate goal tiles for the *Reach Specific Tile* win condition
- Saves boards as JSON alongside their rule sets

### Preset System
- One-click **Monopoly**, **Battleships**, and **Dice Race** presets
- Load any saved custom game as a template

### Save System
- Games (rules + board) saved as JSON
- Overwrite or create new saves from the editor
- Shared game selection overlay used by both the Rule Editor and Board Editor

### Networking
- Multiplayer lobby system via `LobbyManager` and `NetworkManager`
- `NetworkGameManager` coordinates game state across clients

### Modular Architecture
- `HybridGameManager` orchestrates gameplay through swappable modules:
  - `HybridMovementModule`
  - `HybridBoardModule`
  - `HybridCombatModule`
  - `HybridPropertyModule`
  - `HybridCurrencyModule`

---

## Project Structure


Assets/
<br>├── Scripts/
<br>│   ├── GameRules.cs                  # Core rule data model & presets
<br>│   ├── RuleEditorUI.cs               # Rule Editor UI component
<br>│   ├── RuleEditorManager.cs          # Rule Editor singleton manager
<br>│   ├── BoardEditorUI.cs              # Board Editor UI component
<br>│   ├── HybridGameManager.cs          # Main game orchestrator
<br>│   ├── Modules/                      # Pluggable gameplay modules
<br>│   ├── SaveSystem/                   # JSON save/load utilities
<br>│   ├── UI/                           # Shared UI components & managers
<br>│   ├── Utilities/                    # Diagnostics and helpers
<br>│   └── Examples/                     # Usage examples
<br>├── Editor/
<br>│   ├── RuleEditorTests.cs
<br>│   ├── EditModeTests.asmdef
<br>│   └── ...                           # Additional editor tests
<br>└── Tests/
<br>    └── GameSaveManagerTests.cs


---

## Getting Started

### Running a Preset Game
1. Open the main scene in Unity.
2. In the **Rule Editor**, click **Monopoly**, **Battleships**, or **Dice Race** preset.
3. Click **Apply** to save and launch.

### Creating a Custom Game
1. Click **Create Custom Game** from the main menu.
2. Configure rules in the **Rule Editor** panel.
3. Switch to the **Board Editor** to design your board layout.
4. Click **Apply** — enter a name and save your game.

### Loading a Saved Game
1. Click **Custom** in the preset selector.
2. Choose an existing saved game from the list.
3. Select **Edit** to modify it or **Use as Template** to create a new game from it.

---

## Key Scripts

| Script | Purpose |
|--------|---------|
| `GameRules.cs` | Serialisable rule set; includes `CreateMonopolyRules()` and `CreateBattleshipsRules()` factory methods |
| `RuleEditorUI.cs` | Drives the Rule Editor panel — cascading toggles, panel show/hide, preset loading |
| `RuleEditorManager.cs` | Singleton that holds the active `GameRules` and broadcasts `OnRulesChanged` |
| `HybridGameManager.cs` | Coordinates all gameplay modules at runtime |
| `SharedGameSelectionOverlay.cs` | Reusable overlay for game selection, naming, and confirmation |
| `BoardJSONUtility.cs` | Save and load board layouts as JSON |
| `CustomGameAnalyzer.cs` | Analyses a `GameRules` instance to classify the game type |

---

## Win Conditions

| Enum Value | Triggered By |
|---|---|
| `ReachGoal` | Standard Dice Race finish line |
| `LastPlayerStanding` | All other players eliminated |
| `HighestScore` | Highest score at game end |
| `EliminateAllEnemies` | All enemies destroyed (Battleships) |
| `MoneyThreshold` | Player reaches a money target (requires Currency enabled) |
| `ReachSpecificTile` | Player reaches a designated board tile |

---

## Testing

Editor-mode tests are located in `Assets/Editor/` and use the Unity Test Framework.  
Runtime tests are in `Assets/Tests/`.

Run tests via **Window > General > Test Runner** in the Unity Editor.

---

## Requirements

### For Players
- Windows, macOS, or Linux (depending on the build target)
- No additional software required — run the built executable directly

### For Developers
- Unity 2022.3 LTS or newer
- TextMeshPro package
- Unity Netcode for GameObjects *(for multiplayer features)*

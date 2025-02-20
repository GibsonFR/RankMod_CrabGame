# RankMod

A ranked Elo system mod for Crab Game.

## Features
- Ranked matchmaking using Elo-based calculations.
- Real-time Elo updates based on player performance.
- Custom chat commands (`!elo`, `!ranked`, `!help`).
- Persistent player data storage.

**Note:** The Elo calculation relies on GameState variables such as `"Playing"` and `"GameOver"`, and ranks players based on their elimination order. If your mod alters these aspects of the game, compatibility issues may arise. 

## Installation
1. Download the latest `RankMod.dll` from the [Releases](https://github.com/GibsonFR/RankMod/releases) page.
2. Place it in your `BepInEx/plugins/` folder.
3. Start the game, and the mod will be activated automatically.

## Commands
- `!elo` → Displays your current Elo rating.
- `!ranked` → Enables or disables ranked mode.
- `!help` → Lists available commands.

## Configuration
- Modify the `config.txt` file located in `BepInEx/plugins/RankMod/` (if you know what you're doing).
- Editable settings include:
  - Initial Elo value
  - K-Factor adjustment
  - Rank mode toggle

## Upcoming Features
This is the initial release, and many additional features will be added soon:
- **Leaderboard** to display the highest-ranked players.
- **Rank-based tiers** (Bronze, Silver, Gold, etc.) based on Elo.
- **Expanded player statistics**, including win/loss ratios and performance graphs.
- **More in-depth matchmaking mechanics** for ranked mode.
- **UI enhancements** to integrate ranking details directly into the game.

## Known Issues
- Elo may not update correctly if during the game over both players died at the same time.

## Contribution
- Issues and feature requests can be submitted via GitHub Issues.
- Pull requests are welcome for improvements and bug fixes.

## Contact
**Developer:** Gibson  
GitHub: [GibsonFR](https://github.com/GibsonFR)  
Discord: `gib_son`

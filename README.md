# RankMod

A ranked Elo system mod for Crab Game.

## Features
- Ranked matchmaking using Elo-based calculations.
- Real-time Elo updates based on player performance.
- Persistent player data storage with automatic migration for future updates.
- Custom chat commands (`.elo`, `.ranked`, `.help`, `.lastElo`, `.dev`).
- Dynamic database management that adds missing properties and removes deprecated ones.

**Note:** The Elo calculation relies on GameState variables such as `"Playing"` and `"GameOver"`, and ranks players based on their elimination order. If your mod alters these aspects of the game, compatibility issues may arise. 

## Installation
1. Download the latest `RankMod.dll` from the [Releases](https://github.com/GibsonFR/RankMod_CrabGame/releases) page.
2. Place it in your `BepInEx/plugins/` folder.
3. Start the game, and the mod will be activated automatically.

## Commands
- `.elo` → Displays your current Elo rating.
- `.ranked` → Enables or disables ranked mode.
- `.help` → Lists available commands.
- `.lastElo [player]` → Restores the previous Elo of a specific player or all players (`*`).
- `.dev` → Displays information about the developer.

## Configuration
- Modify the `config.txt` file located in `BepInEx/plugins/RankMod/` (if you know what you're doing).
- Editable settings include:
  - Initial Elo value
  - K-Factor adjustment
  - Rank mode toggle
  - **Customizable command symbol** (e.g., `!`, `/`, etc.)

## Latest Updates
- **Automatic database migration** to support new property formats.
- **New `lastElo` command** to restore previous Elo for individual players or all players.
- **New `dev` command** to display developer information.
- **Help command redesign** for improved readability.
- **New configuration option** to change the command symbol.
- **Command messages are now light blue and smaller**, thanks to [lammas321](https://github.com/lammas321).
- **Optimized database handling** to prevent unnecessary file writes and ensure structure integrity.

## Upcoming Features
This is an evolving project, and additional features will be added soon:
- **Leaderboard** to display the highest-ranked players.
- **Rank-based tiers** (Bronze, Silver, Gold, etc.) based on Elo.
- **Expanded player statistics**, including win/loss ratios and performance graphs.
- **More in-depth matchmaking mechanics** for ranked mode.
- **UI enhancements** to integrate ranking details directly into the game.

## Known Issues
- Elo may not update correctly if two players die simultaneously during Game Over.

## Contribution
- Issues and feature requests can be submitted via GitHub Issues.
- Pull requests are welcome for improvements and bug fixes.

## Contact
**Developer:** Gibson  
GitHub: [GibsonFR](https://github.com/GibsonFR)  
Discord: `gib_son`

# RankMod

A ranked Elo system mod for Crab Game.

## Features
- **Ranked matchmaking** using an **Elo-based** rating system.
- **Real-time Elo updates** based on player performance.
- **Automatic database migration**, ensuring compatibility with future updates.
- **New Elo-based ranking system** (Bronze, Silver, Gold, etc.).
- **Full support for mods like TagMod and Infection** (multi-ranking adjustments).
- **Leaderboard system** to track top players.
- **Optimized performance** and improved ranked matchmaking.
- **Customizable chat commands** (`.elo`, `.ranked`, `.help`, `.lastElo`, `.dev`, `.leaderboard`).

**Note:**  
The Elo system is based on `"Playing"` and `"GameOver"` states, ranking players according to their elimination order. Mods that modify these aspects may cause compatibility issues.

---

## Installation
1. Download the latest `RankMod.dll` from the [Releases](https://github.com/GibsonFR/RankMod_CrabGame/releases) page.
2. Place the file in your `BepInEx/plugins/` folder.
3. Launch the game—RankMod will activate automatically.

---

## Commands
- **`.elo`** → Displays your current Elo rating.
- **`.ranked`** → Toggles ranked mode on/off.
- **`.help`** → Lists all available commands.
- **`.lastElo [player]`** → Restores a player's last Elo (`*` restores for all players).
- **`.dev`** → Displays developer information.
- **`.leaderboard`** → Shows the top-ranked players.

---

## Configuration
- Edit the `config.txt` file in `BepInEx/plugins/RankMod/`.
- Customizable settings include:
  - Initial Elo value
  - K-Factor adjustments
  - Rank mode toggle
  - **Customizable command symbol** (e.g., `!`, `/`, `.`).

---

## Latest Updates
- **Added `leaderboard` command** → View the highest-ranked players.
- **New Elo-based ranking system** → Players are assigned **Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster, Challenger** ranks.
- **Improved Elo calculations** → Now supports **multi-ranking** (players tied in the same position get proper adjustments).
- **Full compatibility with mods like TagMod and Infection**.
- **Fixed `ranked` command** → Ensures proper toggling.
- **Optimized database handling** → Reduces unnecessary file writes for better performance.

---

## Upcoming Features
- **More detailed player statistics** (win/loss ratio, performance graphs).
- **Enhanced matchmaking mechanics** for ranked mode.
- **UI improvements** to display Elo and rank in-game.

---

## Known Issues
- Elo updates may not work correctly if two players die at the exact same time.

---

## Contribution
- Report issues or request features via [GitHub Issues](https://github.com/GibsonFR/RankMod_CrabGame/issues).
- Contributions via pull requests are welcome.

---

## Contact
**Developer:** Gibson  
GitHub: [GibsonFR](https://github.com/GibsonFR)  
Discord: `gib_son`

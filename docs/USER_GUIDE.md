# GameShelf user guide

## Start and open the app

Run the command in the main [README](../README.md). Open the address printed in the terminal. The first launch creates six editable demo games so you can explore the interface immediately.

## Build your library

1. Open **My library** and choose **Add a game**.
2. Enter a title, platform, and genre. Choose one of the five play states.
3. Optionally add a score from 1 to 10, hours played, notes, and a favorite marker.
4. Choose **Add to library**. The new game appears in your collection.

Select a game's cover to read its full details and notes. Use **Edit** to change it. The heart on a card toggles favorites. **Delete** opens a confirmation before the game is removed.

## Find games

The search field matches title, platform, or genre without regard to letter case. Status chips filter the list. The sort menu offers recently added, title, highest score, and most played. You can combine search, status, and sort.

## Read your progress

**Overview** shows totals, games currently being played, completion rate, and recent additions. **Insights** shows the status breakdown, average score, top genres, and favorites. Games without a score do not affect the average.

## Keep your data

Your collection is stored in `src/GameShelf.Web/App_Data/games.json` by default. Stop the app before copying the file to make a backup. To move the data file elsewhere, set `GAMESHELF_DATA_FILE` to an absolute path before starting the app. If the file does not exist, a new demo collection is created. If the file is damaged, the app shows a loading error; restore a backup rather than deleting the file unless you want to reset to demo data.

The app is intended for one local collection. Anyone who can access a running instance can use that collection, so do not expose it publicly without adding authentication.

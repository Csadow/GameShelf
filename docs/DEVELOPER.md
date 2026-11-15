# GameShelf developer notes

## Project layout

- `src/GameShelf.Core`: immutable models, pure catalog operations, result type, repository contract, and JSON implementation.
- `src/GameShelf.Web`: Blazor pages, reusable components, CSS, and dependency injection.
- `tests/GameShelf.Tests`: C# checks for domain rules and persistence.
- `tests/browser_smoke.py`: optional Chromium test using isolated temporary data.
- `presentation`: editable final PowerPoint deck.

## Data flow

1. A Blazor component creates a `GameDraft` from a form or an existing entry.
2. It calls `IGameRepository.SaveAsync`.
3. `JsonGameRepository` reads the current file under a semaphore, finds any existing entry, and passes the draft, ID, and current time into `GameCatalog.Build`.
4. `GameCatalog.Build` trims and validates input and returns a new immutable `GameEntry` in a `Result<T>`.
5. The repository writes the updated collection to a temporary file and moves it over the previous file.
6. The page reloads the collection and renders the new state.

Expected validation failures return a `Result<T>` error. I/O faults remain exceptions and are reported by the UI as storage errors. This keeps normal user input out of exception handling.

## Design decisions

- The core rules have no UI dependency. Passing the ID and time as arguments keeps `GameCatalog.Build` deterministic for a given input.
- LINQ operations return materialized arrays, so filtering and statistics do not change the input collection.
- A single repository instance and `SemaphoreSlim` prevent lost updates within one app process. Separate processes should not write the same JSON file simultaneously.
- `System.Text.Json` saves enums by name to make the file easier to inspect.
- Covers are CSS gradients based on a stable title hash. This avoids an external image dependency and keeps the demo usable offline.

## Extend the app

For a new game field, update `GameEntry`, `GameDraft`, validation in `GameCatalog.Build`, the editor, the card or details view, and tests. Existing JSON files may need a migration if the new field cannot use a default value.

For multiple users, implement a database-backed `IGameRepository`, add account identity and authorization, then register that implementation in `Program.cs`. Do not use the current JSON repository for concurrent writes from multiple app processes.

For an external game catalog, keep API calls in a separate service and map responses into `GameDraft` values before calling the domain validation. Keep external calls out of `GameCatalog`.

## Verify changes

```bash
dotnet build GameShelf.sln -m:1
dotnet run --project tests/GameShelf.Tests/GameShelf.Tests.csproj
python tests/browser_smoke.py  # optional: needs Chromium and websockets
```

The browser test runs with a temporary data file and does not alter the regular collection. See `tests/requirements-browser.txt` for its Python dependency.

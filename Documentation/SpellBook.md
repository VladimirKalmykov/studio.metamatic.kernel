Spell book
==

Save persistent data with Repository.LocalStorage
--

Static local storage gives you a way to save data between game sessions

Get data from local storage:
```csharp
// 1. Specify unique path for the data (e.g. mytestdata/player1)
// 2. Give it a default value factory (e.g. new PersistentState())
// 3. Provide cancellation token
var state = await Repository.LocalStorage.TryGet<PersistentState>("mytestdata/player1", () => new PersistentState(), cancellationToken);
```

Save data to local storage:
```csharp
// 1. Specify unique path for the data (e.g. mytestdata/player1)
// 2. Provide the data to be saved
// 3. Provide cancellation token
await Repository.LocalStorage.Set("mytestdata/player1", state, cancellationToken);
```

It also has `delete` and `exists` methods.

Control cursor via CursorService
--

Cursor service provides a way to control the cursor in the game.

Show cursor:
```csharp
var cursorService = await Service.Resolve<CursorService>();
using var cursorShow = cursorService.SetCursor(new CursorService.CursorConfig(true));
await lifecycle;
```

Hide cursor:
```csharp
var cursorService = await Service.Resolve<CursorService>();
using var cursorHide = cursorService.SetCursor(new CursorService.CursorConfig(false));
await lifecycle;
```

Show cursor with custom texture and [hot spot](https://developer.apple.com/documentation/appkit/nscursor/1529096-hotspot):
```csharp
var cursorService = await Service.Resolve<CursorService>();
using var cursorCustom = cursorService.SetCursor(new CursorService.CursorConfig(true, "cursor.png", Vector2.zero));
```
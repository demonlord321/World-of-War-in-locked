# World of War-in-locked

An RPG with dynamic growth, written in C#. The rules are kept separate from the graphics so the game can move to Unity later.

## Run it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Works on Linux and Windows.

```
dotnet run --project src/WarInLocked.Game
```

Mouse and keyboard both work:

- **Main menu:** Up/Down and Enter, or click.
- **Character creator:** type a name, pick a Race and a Stat Distribution (click, or Tab to move between fields and the arrow keys to change a choice), and the stats update live. **Create** is disabled until the name is filled in. Esc goes back.
- **Story:** after Create, the opening text types itself out. Any key or click finishes the typing, and the next one carries on to the forest.
- **Forest of Beginnings:** Arrows/WASD to walk, Esc for the menu. Stand next to Colonel Powers and a **Talk (E)** prompt appears. Press **E** or **Enter** to talk.
- **Dialogue:** Up/Down (or W/S) and Enter, or click an option (1-3 also work). Enter, E or a click moves past a line with no options. Esc does nothing while talking, and the slimes wait until you are done.

## Test it

```
dotnet build
dotnet test
```

## Layout

| Path | What it is |
|------|------------|
| `src/WarInLocked.Core` | All game rules. `netstandard2.1`, C# 9, no engine references, so Unity can load it as-is. |
| `src/WarInLocked.Game` | MonoGame DesktopGL front end (`net8.0`). A small screen system (`Screens/`) plus simple widgets (`Ui/`). Text is drawn with FontStashSharp from a bundled DejaVu Sans font, so there is no content pipeline to set up. |
| `tests/WarInLocked.Core.Tests` | xUnit tests for Core. |
| `QUESTIONS.md` | Open design questions for Dyllan. |

## Where things are

- Stat bonuses per distribution (issue #3): `src/WarInLocked.Core/Characters/StatDistributions.cs`
- Race modifier placeholder (all zeros): `StatDistributions.GetRaceModifier`
- Main menu stub (issue #1): `src/WarInLocked.Game/Screens/MainMenuScreen.cs`
- Character creator (issue #2): `src/WarInLocked.Game/Screens/CharacterCreatorScreen.cs`

The font is DejaVu Sans, licensed under the Bitstream Vera license (see `src/WarInLocked.Game/Assets/Fonts/DejaVu-LICENSE.txt`).

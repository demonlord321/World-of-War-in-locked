using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Bare-bones main menu stub (issue #1 is Mapmaker's to style): a title, New Game and Exit.
/// Keyboard: Up/Down and Enter. Mouse: click.
/// </summary>
public sealed class MainMenuScreen : Screen
{
    private readonly Button _newGame = new(new Rectangle(500, 310, 280, 56), "New Game");
    private readonly Button _exit = new(new Rectangle(500, 386, 280, 56), "Exit");
    private int _focus;

    public override void Update(GameTime time, InputState input)
    {
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.Tab)) _focus = (_focus + 1) % 2;
        if (input.Pressed(Keys.Up)) _focus = (_focus + 1) % 2;
        if (input.MouseMoved)
        {
            if (_newGame.IsHovered(input)) _focus = 0;
            else if (_exit.IsHovered(input)) _focus = 1;
        }

        bool activate = input.Pressed(Keys.Enter) || input.Pressed(Keys.Space);
        if (_newGame.WasClicked(input) || (activate && _focus == 0))
            Screens.Replace(new CharacterCreatorScreen());
        else if (_exit.WasClicked(input) || (activate && _focus == 1))
            Screens.Exit();
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;

        // Framed panel behind the menu.
        var panel = new Rectangle(w / 2 - 300, 100, 600, 420);
        gfx.Rect(sb, panel, Theme.Panel);
        gfx.Border(sb, panel, Theme.PanelBorder, 2);
        gfx.Border(sb, new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 16, panel.Height - 16), Theme.PanelBorder * 0.5f, 1);

        // Title with a soft drop shadow, then the tagline between two accent rules.
        var titleArea = new Rectangle(0, 130, w, 80);
        gfx.TextCentered(sb, "World of War-in-locked", new Rectangle(titleArea.X + 3, titleArea.Y + 3, titleArea.Width, titleArea.Height), Color.Black * 0.6f, 52f);
        gfx.TextCentered(sb, "World of War-in-locked", titleArea, Theme.Accent, 52f);
        gfx.Rect(sb, new Rectangle(w / 2 - 220, 222, 440, 2), Theme.Accent * 0.7f);
        gfx.TextCentered(sb, "An RPG with dynamic growth", new Rectangle(0, 228, w, 36), Theme.TextDim, 22f);
        gfx.Rect(sb, new Rectangle(w / 2 - 220, 268, 440, 2), Theme.Accent * 0.7f);

        _newGame.Draw(sb, gfx, input, _focus == 0);
        _exit.Draw(sb, gfx, input, _focus == 1);

        gfx.TextCentered(sb, "Up/Down to choose  -  Enter to select", new Rectangle(0, h - 60, w, 30), Theme.TextDim, 18f);
    }
}

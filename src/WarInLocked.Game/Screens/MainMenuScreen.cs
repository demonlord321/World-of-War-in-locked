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
    private readonly Button _newGame = new(new Rectangle(500, 320, 280, 56), "New Game");
    private readonly Button _exit = new(new Rectangle(500, 396, 280, 56), "Exit");
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
        gfx.TextCentered(sb, "World of War-in-locked", new Rectangle(0, 140, WarInLockedGame.ScreenWidth, 80), Theme.Accent, 56f);
        _newGame.Draw(sb, gfx, input, _focus == 0);
        _exit.Draw(sb, gfx, input, _focus == 1);
    }
}

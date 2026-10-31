using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>Holds the current screen. Screens switch with <see cref="Replace"/>; there is no stack.</summary>
public sealed class ScreenManager
{
    private readonly Action _exit;

    public ScreenManager(Action exit) => _exit = exit;

    public Screen? Current { get; private set; }

    public void Replace(Screen screen)
    {
        screen.Screens = this;
        Current = screen;
    }

    public void Exit() => _exit();

    public void Update(GameTime time, InputState input) => Current?.Update(time, input);

    public void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time) => Current?.Draw(sb, gfx, input, time);
}

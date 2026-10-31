using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>One full-window screen (main menu, character creator, ...). Subclass this to add a screen.</summary>
public abstract class Screen
{
    /// <summary>Set by the <see cref="ScreenManager"/>; use it to go to another screen or exit.</summary>
    public ScreenManager Screens { get; internal set; } = null!;

    public virtual void Update(GameTime time, InputState input) { }

    public abstract void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time);
}

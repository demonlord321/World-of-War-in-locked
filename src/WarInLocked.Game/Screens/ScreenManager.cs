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

    /// <summary>#35: the world keeps running while a menu (I, K, Titles, Shop) is open over it. Set when a ForestScreen is shown,
    /// kept while an overlay menu is current, cleared by any other screen (main menu, creator, story).</summary>
    public ForestScreen? World { get; private set; }
    private static readonly InputState NoInput = new();

    public void Replace(Screen screen)
    {
        screen.Screens = this;
        Current = screen;
        if (screen is ForestScreen fs) World = fs;
        else if (!IsOverlayMenu(screen)) World = null;
    }

    private static bool IsOverlayMenu(Screen s) => s is CharacterMenuScreen or AbilitiesScreen or TitlesScreen or ShopScreen;

    public void Exit() => _exit();

    public void Update(GameTime time, InputState input)
    {
        // No pausing in menus (#35): the world ticks without input underneath the open menu.
        if (World != null && Current != World && Current != null && IsOverlayMenu(Current)) World.Update(time, NoInput);
        Current?.Update(time, input);
    }

    public void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time) => Current?.Draw(sb, gfx, input, time);
}

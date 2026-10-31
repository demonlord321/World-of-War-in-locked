using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Game.Screens;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game;

/// <summary>The MonoGame host: owns the window, input and the screen manager.</summary>
public sealed class WarInLockedGame : Microsoft.Xna.Framework.Game
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private SpriteBatch _spriteBatch = null!;
    private Gfx _gfx = null!;
    private ScreenManager _screens = null!;

    public WarInLockedGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight,
        };
        IsMouseVisible = true;
        Window.Title = "World of War-in-locked";
        Window.TextInput += (_, e) => _input.AddTypedChar(e.Character);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _gfx = new Gfx(GraphicsDevice);
        _screens = new ScreenManager(Exit);
        _screens.Replace(new MainMenuScreen());
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        _screens.Update(gameTime, _input);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Theme.Background);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _screens.Draw(_spriteBatch, _gfx, _input, gameTime);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}

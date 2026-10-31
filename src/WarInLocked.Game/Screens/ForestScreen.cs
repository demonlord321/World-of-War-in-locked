using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.World;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #5: the Forest of Beginning. The player walks the map (arrows/WASD), placeholder
/// slimes wander about, and Colonel Powers stands at the N marker. No combat or talking yet:
/// <see cref="NpcPositions"/> is where #6/#7 hook in.
/// </summary>
public sealed class ForestScreen : Screen
{
    public const int TileSize = 32;
    private const float PlayerSpeed = 160f;     // pixels per second
    private const int PlayerSize = 22;          // smaller than a tile so corners don't snag

    private static readonly Color GrassA = new(58, 110, 52), GrassB = new(62, 116, 56);
    private static readonly Color PathColor = new(150, 120, 80), WaterColor = new(50, 100, 170);
    private static readonly Color TreeTrunk = new(80, 56, 34), TreeTop = new(30, 72, 34);
    private static readonly Color WallColor = new(28, 40, 28), SlimeColor = new(110, 210, 120);

    private readonly Character _character;
    private readonly TileMap _map;
    private readonly List<Wanderer> _slimes = new();
    private Vector2 _player;     // top-left, in pixels
    private Vector2 _camera;

    /// <summary>NPC tiles by id; "colonel-powers" is the first one on the map.</summary>
    public Dictionary<string, Point> NpcPositions { get; } = new();

    public ForestScreen(Character character)
    {
        _character = character;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "forest-of-beginning.txt");
        _map = TileMap.Parse(File.ReadAllText(path));

        var rng = new Random();
        foreach (var s in _map.SlimeSpawns) _slimes.Add(new Wanderer(s.X, s.Y, rng));
        if (_map.Npcs.Count > 0) NpcPositions["colonel-powers"] = new Point(_map.Npcs[0].X, _map.Npcs[0].Y);

        var (sx, sy) = _map.PlayerStart;
        _player = new Vector2(sx * TileSize + (TileSize - PlayerSize) / 2f, sy * TileSize + (TileSize - PlayerSize) / 2f);
    }

    public override void Update(GameTime time, InputState input)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        var k = Keyboard.GetState();
        var dir = Vector2.Zero;
        if (k.IsKeyDown(Keys.Left) || k.IsKeyDown(Keys.A)) dir.X -= 1;
        if (k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D)) dir.X += 1;
        if (k.IsKeyDown(Keys.Up) || k.IsKeyDown(Keys.W)) dir.Y -= 1;
        if (k.IsKeyDown(Keys.Down) || k.IsKeyDown(Keys.S)) dir.Y += 1;
        if (dir != Vector2.Zero) dir.Normalize();

        // Move one axis at a time so the player slides along walls.
        TryMove(new Vector2(dir.X * PlayerSpeed * dt, 0));
        TryMove(new Vector2(0, dir.Y * PlayerSpeed * dt));

        var playerTile = new Point((int)((_player.X + PlayerSize / 2f) / TileSize), (int)((_player.Y + PlayerSize / 2f) / TileSize));
        foreach (var slime in _slimes)
            slime.Update(dt, _map, (x, y) =>
                (x == playerTile.X && y == playerTile.Y) || IsNpc(x, y) ||
                _slimes.Exists(o => o != slime && o.TileX == x && o.TileY == y));

        // Clamped follow camera.
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        _camera.X = MathHelper.Clamp(_player.X + PlayerSize / 2f - w / 2f, 0, Math.Max(0, _map.Width * TileSize - w));
        _camera.Y = MathHelper.Clamp(_player.Y + PlayerSize / 2f - h / 2f, 0, Math.Max(0, _map.Height * TileSize - h));

        if (input.Pressed(Keys.Escape)) Screens.Replace(new MainMenuScreen());
    }

    private bool IsNpc(int x, int y)
    {
        foreach (var p in NpcPositions.Values) if (p.X == x && p.Y == y) return true;
        return false;
    }

    private void TryMove(Vector2 delta)
    {
        if (delta == Vector2.Zero) return;
        var next = _player + delta;
        int left = (int)(next.X / TileSize), right = (int)((next.X + PlayerSize - 1) / TileSize);
        int top = (int)(next.Y / TileSize), bottom = (int)((next.Y + PlayerSize - 1) / TileSize);
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
                if (!_map.IsWalkable(x, y) || IsNpc(x, y)) return;
        _player = next;
    }

    private Rectangle Screen(float x, float y, int w, int h) => new((int)(x - _camera.X), (int)(y - _camera.Y), w, h);

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        int x0 = Math.Max(0, (int)(_camera.X / TileSize)), y0 = Math.Max(0, (int)(_camera.Y / TileSize));
        int x1 = Math.Min(_map.Width - 1, (int)((_camera.X + w) / TileSize)), y1 = Math.Min(_map.Height - 1, (int)((_camera.Y + h) / TileSize));

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var r = Screen(x * TileSize, y * TileSize, TileSize, TileSize);
                var ground = ((x + y) & 1) == 0 ? GrassA : GrassB;
                switch (_map[x, y])
                {
                    case Tile.Path: gfx.Rect(sb, r, PathColor); break;
                    case Tile.Water: gfx.Rect(sb, r, WaterColor); break;
                    case Tile.Wall: gfx.Rect(sb, r, WallColor); break;
                    case Tile.Tree:
                        gfx.Rect(sb, r, ground);
                        gfx.Rect(sb, new Rectangle(r.X + 13, r.Y + 20, 6, 12), TreeTrunk);
                        gfx.Rect(sb, new Rectangle(r.X + 3, r.Y + 2, 26, 20), TreeTop);
                        break;
                    default: gfx.Rect(sb, r, ground); break;
                }
            }

        foreach (var s in _slimes)
        {
            var r = Screen(s.X * TileSize + 6, s.Y * TileSize + 12, 20, 16);
            gfx.Rect(sb, r, SlimeColor);
            gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 4, 3, 3), Color.Black);
            gfx.Rect(sb, new Rectangle(r.X + 13, r.Y + 4, 3, 3), Color.Black);
        }

        foreach (var (id, p) in NpcPositions)
        {
            var r = Screen(p.X * TileSize + 5, p.Y * TileSize + 2, 22, 28);
            gfx.Rect(sb, r, new Color(90, 100, 60));
            gfx.Border(sb, r, Color.Black, 2);
            gfx.TextCentered(sb, "Colonel Powers", Screen(p.X * TileSize - 60, p.Y * TileSize - 22, 152, 20), Theme.Text, 14f);
        }

        var pr = Screen(_player.X, _player.Y, PlayerSize, PlayerSize);
        gfx.Rect(sb, pr, Theme.Accent);
        gfx.Border(sb, pr, Color.Black, 2);

        gfx.Rect(sb, new Rectangle(0, 0, w, 36), Color.Black * 0.55f);
        gfx.TextLeft(sb, $"The Forest of Beginning  -  {_character.Name}", new Rectangle(0, 0, w, 36), Theme.Text, 18f);
        gfx.TextLeft(sb, "Arrows/WASD to move  -  Esc for menu", new Rectangle(w - 340, 0, 340, 36), Theme.TextDim, 16f);
    }
}

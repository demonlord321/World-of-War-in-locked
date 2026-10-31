using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WarInLocked.Game.Ui;

/// <summary>Small drawing helpers: filled rectangles and text (no content pipeline needed).</summary>
public sealed class Gfx
{
    private readonly Texture2D _pixel;
    private readonly FontSystem _fonts;

    public Gfx(GraphicsDevice device)
    {
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _fonts = new FontSystem();
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "DejaVuSans.ttf");
        _fonts.AddFont(File.ReadAllBytes(path));
    }

    public void Rect(SpriteBatch sb, Rectangle r, Color color) => sb.Draw(_pixel, r, color);

    public void Border(SpriteBatch sb, Rectangle r, Color color, int thickness = 2)
    {
        Rect(sb, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        Rect(sb, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        Rect(sb, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        Rect(sb, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }

    public void Text(SpriteBatch sb, string text, Vector2 pos, Color color, float size = 24f)
        => _fonts.GetFont(size).DrawText(sb, text, pos, color);

    public Vector2 Measure(string text, float size = 24f) => _fonts.GetFont(size).MeasureString(text);

    /// <summary>Draws text centred inside a rectangle.</summary>
    public void TextCentered(SpriteBatch sb, string text, Rectangle area, Color color, float size = 24f)
    {
        var m = Measure(text, size);
        Text(sb, text, new Vector2(area.X + (area.Width - m.X) / 2f, area.Y + (area.Height - m.Y) / 2f), color, size);
    }

    /// <summary>Draws text vertically centred, starting at the left of a rectangle plus padding.</summary>
    public void TextLeft(SpriteBatch sb, string text, Rectangle area, Color color, float size = 24f, int pad = 12)
    {
        var m = Measure(text, size);
        Text(sb, text, new Vector2(area.X + pad, area.Y + (area.Height - m.Y) / 2f), color, size);
    }
}

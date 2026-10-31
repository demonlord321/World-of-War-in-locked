using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Full-screen story text (issue #4), typed out letter by letter.
/// Any key or click finishes the typing; the next one continues to the next screen.
/// </summary>
public sealed class StoryScreen : Screen
{
    private const float CharsPerSecond = 45f;
    private const float TextSize = 28f;

    private readonly string _text;
    private readonly Func<Screen> _next;
    private float _shown;
    private List<string>? _lines;

    public StoryScreen(string text, Func<Screen> next)
    {
        _text = text;
        _next = next;
    }

    private bool Done => _shown >= _text.Length;

    public override void Update(GameTime time, InputState input)
    {
        _shown += (float)time.ElapsedGameTime.TotalSeconds * CharsPerSecond;

        if (input.AnyKeyPressed() || input.Clicked)
        {
            input.ConsumeClick();
            if (Done) Screens.Replace(_next());
            else _shown = _text.Length;
        }
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        const int margin = 140;
        _lines ??= gfx.Wrap(_text, w - margin * 2, TextSize);

        const int lineHeight = 44;
        int y = (h - _lines.Count * lineHeight) / 2 - 20;
        int remaining = (int)_shown;
        foreach (string line in _lines)
        {
            if (remaining <= 0) break;
            string visible = line.Length <= remaining ? line : line.Substring(0, remaining);
            gfx.Text(sb, visible, new Vector2(margin, y), Theme.Text, TextSize);
            remaining -= line.Length + 1; // +1 for the space the wrap removed
            y += lineHeight;
        }

        string hint = Done ? "Press any key to continue" : "Press any key to skip";
        bool blink = !Done || (int)(time.TotalGameTime.TotalSeconds * 2) % 2 == 0;
        if (blink)
            gfx.TextCentered(sb, hint, new Rectangle(0, h - 70, w, 30), Theme.TextDim, 18f);
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Core.Characters;

namespace WarInLocked.Game.Ui;

/// <summary>A single-line text input. Only accepts characters the Core name rules allow.</summary>
public sealed class TextField
{
    public Rectangle Bounds;
    public string Text = string.Empty;
    public int MaxLength = NameValidator.MaxLength;

    public TextField(Rectangle bounds) => Bounds = bounds;

    /// <summary>Applies this frame's typing. Returns true if the text changed.</summary>
    public bool HandleTyping(InputState input)
    {
        bool changed = false;
        foreach (char c in input.TypedChars)
        {
            if (c == '\b')
            {
                if (Text.Length > 0)
                {
                    Text = Text.Substring(0, Text.Length - 1);
                    changed = true;
                }
            }
            else if (NameValidator.IsAllowedCharacter(c) && Text.Length < MaxLength)
            {
                Text += c;
                changed = true;
            }
        }
        return changed;
    }

    public void Draw(SpriteBatch sb, Gfx gfx, GameTime time, bool focused, string placeholder)
    {
        gfx.Rect(sb, Bounds, Theme.Panel);
        gfx.Border(sb, Bounds, focused ? Theme.Accent : Theme.PanelBorder);
        if (Text.Length == 0 && !focused)
        {
            gfx.TextLeft(sb, placeholder, Bounds, Theme.TextDim);
            return;
        }
        gfx.TextLeft(sb, Text, Bounds, Theme.Text);
        bool caretOn = (int)(time.TotalGameTime.TotalSeconds * 2) % 2 == 0;
        if (focused && caretOn)
        {
            float x = Bounds.X + 12 + gfx.Measure(Text).X + 2;
            gfx.Rect(sb, new Rectangle((int)x, Bounds.Y + 9, 2, Bounds.Height - 18), Theme.Text);
        }
    }
}

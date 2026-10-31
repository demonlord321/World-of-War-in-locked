using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Dialogue;

namespace WarInLocked.Game.Ui;

/// <summary>
/// The dialogue box at the bottom of the screen (issue #6/#7): speaker name, the line, and the player's options.
/// Up/Down (or W/S) and Enter, E or Space to pick; 1-9 jump to an option; the mouse hovers and clicks.
/// Esc is deliberately ignored.
/// </summary>
public sealed class DialogueBox
{
    private static readonly Rectangle Box = new(40, 456, 1200, 244);
    private const int OptionTop = 570;
    private const int OptionHeight = 36;
    private int _selected;

    private static Rectangle OptionBounds(int i) => new(Box.X + 24, OptionTop + i * OptionHeight, Box.Width - 48, OptionHeight - 4);

    /// <summary>Handles one frame of input for the running dialogue.</summary>
    public void Update(DialogueRunner dialogue, InputState input)
    {
        if (dialogue.IsEnded) return;

        bool confirm = input.Pressed(Keys.Enter) || input.Pressed(Keys.E) || input.Pressed(Keys.Space);

        if (!dialogue.HasOptions)
        {
            bool clicked = input.Clicked && Box.Contains(input.MousePosition);
            if (clicked) input.ConsumeClick();
            if (confirm || clicked) dialogue.Advance();
            return;
        }

        int count = dialogue.Options.Count;
        if (_selected >= count) _selected = 0;
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) _selected = (_selected + 1) % count;
        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) _selected = (_selected + count - 1) % count;

        if (input.MouseMoved)
            for (int i = 0; i < count; i++)
                if (OptionBounds(i).Contains(input.MousePosition)) _selected = i;

        int chosen = -1;
        if (confirm) chosen = _selected;
        if (input.Clicked)
        {
            for (int i = 0; i < count; i++)
            {
                if (OptionBounds(i).Contains(input.MousePosition))
                {
                    chosen = i;
                    input.ConsumeClick();
                    break;
                }
            }
        }
        for (int i = 0; i < Math.Min(count, 9); i++)
            if (input.Pressed(Keys.D1 + i)) chosen = i;

        if (chosen >= 0)
        {
            dialogue.Choose(chosen);
            _selected = 0;
        }
    }

    public void Draw(SpriteBatch sb, Gfx gfx, DialogueRunner dialogue)
    {
        if (dialogue.IsEnded) return;

        gfx.Rect(sb, Box, Theme.Panel * 0.97f);
        gfx.Border(sb, Box, Theme.PanelBorder, 2);

        var tab = new Rectangle(Box.X + 20, Box.Y - 18, 280, 36);
        gfx.Rect(sb, tab, Theme.Button);
        gfx.Border(sb, tab, Theme.Accent, 2);
        gfx.TextCentered(sb, dialogue.Speaker, tab, Theme.Accent, 22f);

        int y = Box.Y + 32;
        foreach (string line in gfx.Wrap(dialogue.Text, Box.Width - 56, 22f))
        {
            gfx.Text(sb, line, new Vector2(Box.X + 28, y), Theme.Text, 22f);
            y += 30;
        }

        if (dialogue.HasOptions)
        {
            var options = dialogue.Options;
            for (int i = 0; i < options.Count; i++)
            {
                var r = OptionBounds(i);
                bool sel = i == _selected;
                if (sel) gfx.Rect(sb, r, Theme.ButtonHover);
                gfx.TextLeft(sb, (sel ? "> " : "  ") + (i + 1) + ". " + options[i], r, sel ? Theme.Accent : Theme.Text, 20f);
            }
            gfx.Text(sb, "Up/Down + Enter, or click", new Vector2(Box.Right - 250, Box.Bottom - 26), Theme.TextDim, 15f);
        }
        else
        {
            gfx.Text(sb, "Enter / E / click to continue", new Vector2(Box.Right - 280, Box.Bottom - 30), Theme.TextDim, 16f);
        }
    }
}

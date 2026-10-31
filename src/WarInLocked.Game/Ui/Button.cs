using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WarInLocked.Game.Ui;

public sealed class Button
{
    public Rectangle Bounds;
    public string Label;
    public bool Enabled = true;

    public Button(Rectangle bounds, string label)
    {
        Bounds = bounds;
        Label = label;
    }

    public bool IsHovered(InputState input) => Enabled && Bounds.Contains(input.MousePosition);

    /// <summary>True if the mouse clicked this button (and the click is then used up).</summary>
    public bool WasClicked(InputState input)
    {
        if (!input.Clicked || !IsHovered(input)) return false;
        input.ConsumeClick();
        return true;
    }

    public void Draw(SpriteBatch sb, Gfx gfx, InputState input, bool focused)
    {
        Color fill = !Enabled ? Theme.ButtonDisabled : (IsHovered(input) || focused) ? Theme.ButtonHover : Theme.Button;
        gfx.Rect(sb, Bounds, fill);
        gfx.Border(sb, Bounds, focused ? Theme.Accent : Theme.PanelBorder);
        gfx.TextCentered(sb, Label, Bounds, Enabled ? Theme.Text : Theme.TextDim);
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WarInLocked.Game.Ui;

/// <summary>
/// A dropdown list. Mouse: click to open, click an item. Keyboard (when focused):
/// arrow keys change the value, Enter/Space opens the list, then arrows + Enter pick, Esc closes.
/// </summary>
public sealed class Dropdown<T> where T : struct, Enum
{
    private readonly T[] _items = Enum.GetValues<T>();
    private int _highlight;

    public Rectangle Bounds;
    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; }
    public T Selected => _items[SelectedIndex];

    public Dropdown(Rectangle bounds) => Bounds = bounds;

    private Rectangle ItemBounds(int i) => new(Bounds.X, Bounds.Bottom + i * Bounds.Height, Bounds.Width, Bounds.Height);

    public void Close() => IsOpen = false;

    private void Open()
    {
        IsOpen = true;
        _highlight = SelectedIndex;
    }

    private void Select(int index)
    {
        SelectedIndex = (index % _items.Length + _items.Length) % _items.Length;
    }

    /// <summary>Handles mouse and keyboard. Returns true if the selection changed. Returns clicked = true when the dropdown was clicked.</summary>
    public bool Update(InputState input, bool focused, out bool clicked)
    {
        int before = SelectedIndex;
        clicked = false;

        if (input.Clicked)
        {
            if (Bounds.Contains(input.MousePosition))
            {
                if (IsOpen) Close(); else Open();
                clicked = true;
                input.ConsumeClick();
            }
            else if (IsOpen)
            {
                for (int i = 0; i < _items.Length; i++)
                {
                    if (ItemBounds(i).Contains(input.MousePosition))
                    {
                        Select(i);
                        break;
                    }
                }
                // Any click while open picks an item or just closes the list, and goes no further.
                Close();
                input.ConsumeClick();
            }
        }

        if (IsOpen && input.MouseMoved)
        {
            for (int i = 0; i < _items.Length; i++)
                if (ItemBounds(i).Contains(input.MousePosition)) _highlight = i;
        }

        if (focused)
        {
            if (IsOpen)
            {
                if (input.Pressed(Keys.Down)) _highlight = (_highlight + 1) % _items.Length;
                if (input.Pressed(Keys.Up)) _highlight = (_highlight + _items.Length - 1) % _items.Length;
                if (input.Pressed(Keys.Enter) || input.Pressed(Keys.Space))
                {
                    Select(_highlight);
                    Close();
                }
                else if (input.Pressed(Keys.Escape)) Close();
            }
            else
            {
                if (input.Pressed(Keys.Right) || input.Pressed(Keys.Down)) Select(SelectedIndex + 1);
                if (input.Pressed(Keys.Left) || input.Pressed(Keys.Up)) Select(SelectedIndex - 1);
                if (input.Pressed(Keys.Enter) || input.Pressed(Keys.Space)) Open();
            }
        }
        return SelectedIndex != before;
    }

    /// <summary>Draws the closed box. Call <see cref="DrawList"/> later so the open list is on top.</summary>
    public void DrawBox(SpriteBatch sb, Gfx gfx, InputState input, bool focused)
    {
        gfx.Rect(sb, Bounds, Bounds.Contains(input.MousePosition) ? Theme.ButtonHover : Theme.Button);
        gfx.Border(sb, Bounds, focused ? Theme.Accent : Theme.PanelBorder);
        gfx.TextLeft(sb, Selected.ToString(), Bounds, Theme.Text);
        gfx.TextLeft(sb, IsOpen ? "^" : "v", new Rectangle(Bounds.Right - 36, Bounds.Y, 36, Bounds.Height), Theme.TextDim, 20f, 8);
    }

    public void DrawList(SpriteBatch sb, Gfx gfx)
    {
        if (!IsOpen) return;
        for (int i = 0; i < _items.Length; i++)
        {
            var r = ItemBounds(i);
            gfx.Rect(sb, r, i == _highlight ? Theme.ButtonHover : Theme.Panel);
            gfx.Border(sb, r, Theme.PanelBorder, 1);
            gfx.TextLeft(sb, _items[i].ToString(), r, i == SelectedIndex ? Theme.Accent : Theme.Text);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #27: the titles screen, opened from the character (I) menu with T or its Titles button.
/// Left = the titles earned so far (the worn one is marked), right = the focused title's name, description and bonus lines
/// and what wearing it does to the stats. Enter or click wears it (or takes it off if it is the worn one); only one is worn at a time.
/// T or Esc returns to the SAME character menu instance. Pure drawing and input: Core does the equipping
/// (<see cref="Character.EquipTitle"/> / <see cref="Character.UnequipTitle"/>).
/// </summary>
public sealed class TitlesScreen : Screen
{
    private const int RowHeight = 64;
    private static readonly Color SlotFill = new(26, 29, 40);

    private readonly Screen _back;
    private readonly Character _character;
    private int _focus;
    private string? _message;

    public TitlesScreen(Screen back, Character character)
    {
        _back = back;
        _character = character;
    }

    private static Rectangle ListPanel => new(30, 76, 400, 548);
    private static Rectangle DetailPanel => new(450, 76, 800, 548);
    private static Rectangle RowRect(int i) => new(ListPanel.X + 12, ListPanel.Y + 12 + i * (RowHeight + 8), ListPanel.Width - 24, RowHeight);
    private static Rectangle ActionButton => new(DetailPanel.X + 24, DetailPanel.Bottom - 70, 260, 46);

    public override void Update(GameTime time, InputState input)
    {
        if (input.Pressed(Keys.T) || input.Pressed(Keys.Escape))
        {
            Screens.Replace(_back);
            return;
        }

        var list = _character.Titles;
        if (list.Count == 0) return;
        _focus = Math.Clamp(_focus, 0, list.Count - 1);

        bool activate = input.Pressed(Keys.Enter);
        if (input.MouseMoved)
            for (int i = 0; i < list.Count; i++)
                if (RowRect(i).Contains(input.MousePosition)) _focus = i;
        if (input.Clicked)
        {
            if (ActionButton.Contains(input.MousePosition)) { activate = true; input.ConsumeClick(); }
            else
                for (int i = 0; i < list.Count; i++)
                    if (RowRect(i).Contains(input.MousePosition)) { _focus = i; input.ConsumeClick(); break; }
        }

        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) _focus = Math.Max(0, _focus - 1);
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) _focus = Math.Min(list.Count - 1, _focus + 1);

        if (activate)
        {
            var title = list[_focus];
            if (_character.EquippedTitle?.Id == title.Id)
                _message = _character.UnequipTitle() ? $"{title.Name} taken off" : null;
            else
                _message = _character.EquipTitle(title.Id) ? $"{title.Name} equipped" : null;
        }
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Theme.Background);
        gfx.Rect(sb, new Rectangle(0, 0, w, 56), Theme.Panel);
        gfx.TextLeft(sb, $"Titles  -  {_character.Name}", new Rectangle(0, 0, w, 56), Theme.Accent, 26f, 30);
        gfx.TextLeft(sb, "W/S or mouse: select  -  Enter/click: equip or unequip  -  T/Esc: back", new Rectangle(w - 780, 0, 780, 56), Theme.TextDim, 16f);

        foreach (var p in new[] { ListPanel, DetailPanel })
        {
            gfx.Rect(sb, p, Theme.Panel);
            gfx.Border(sb, p, Theme.PanelBorder, 2);
        }

        var list = _character.Titles;
        if (list.Count == 0)
        {
            gfx.TextCentered(sb, "No titles yet", ListPanel, Theme.TextDim, 20f);
            gfx.TextCentered(sb, "Titles are earned from quests.", DetailPanel, Theme.TextDim, 18f);
        }
        else
        {
            _focus = Math.Clamp(_focus, 0, list.Count - 1);
            for (int i = 0; i < list.Count; i++) DrawRow(sb, gfx, list[i], i);
            DrawDetails(sb, gfx, list[_focus]);
        }

        var tip = new Rectangle(30, 636, 1220, 54);
        gfx.Rect(sb, tip, Theme.Panel);
        gfx.Border(sb, tip, Theme.PanelBorder, 2);
        string worn = _character.EquippedTitle == null ? "No title worn" : $"Wearing: {_character.EquippedTitle.Name}";
        if (_message != null) worn += "     |     " + _message;
        gfx.TextLeft(sb, worn, tip, Theme.Text, 20f, 20);
    }

    private void DrawRow(SpriteBatch sb, Gfx gfx, Title title, int i)
    {
        var r = RowRect(i);
        bool focus = i == _focus, worn = _character.EquippedTitle?.Id == title.Id;
        gfx.Rect(sb, r, focus ? Theme.ButtonHover : SlotFill);
        gfx.Border(sb, r, focus ? Theme.Accent : Theme.PanelBorder, 2);
        gfx.Text(sb, title.Name, new Vector2(r.X + 14, r.Y + 8), Theme.Text, 22f);
        gfx.Text(sb, worn ? "Equipped" : "Not worn", new Vector2(r.X + 14, r.Y + 38), worn ? Theme.Positive : Theme.TextDim, 15f);
        if (worn)
        {
            var badge = new Rectangle(r.Right - 44, r.Y + 18, 28, 28);
            gfx.Rect(sb, badge, Theme.Positive * 0.35f);
            gfx.Border(sb, badge, Theme.Positive, 2);
            gfx.TextCentered(sb, "E", badge, Theme.Positive, 18f);
        }
    }

    private void DrawDetails(SpriteBatch sb, Gfx gfx, Title title)
    {
        var p = DetailPanel;
        bool worn = _character.EquippedTitle?.Id == title.Id;
        int x = p.X + 24, y = p.Y + 18;
        gfx.Text(sb, title.Name, new Vector2(x, y), Theme.Text, 32f);
        string state = worn ? "Equipped" : "Not worn";
        var sm = gfx.Measure(state, 18f);
        gfx.Text(sb, state, new Vector2(p.Right - 24 - sm.X, y + 10), worn ? Theme.Positive : Theme.TextDim, 18f);
        y += 56;

        string desc = string.IsNullOrWhiteSpace(title.Description) ? "No description." : title.Description;
        foreach (var line in gfx.Wrap(desc, p.Width - 48, 19f))
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.TextDim, 19f);
            y += 27;
        }
        y += 14;
        foreach (var line in title.BonusLines)
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.Accent, 21f);
            y += 30;
        }

        // What wearing it does right now: the stat values with and without the bonus.
        y += 14;
        if (title.AllStatsBonus != 0)
        {
            gfx.Text(sb, worn ? "Stats now include the title bonus." : "Stats if you wear it:", new Vector2(x, y), Theme.TextDim, 16f);
            y += 28;
            int col = 0;
            foreach (var stat in StatTypes.All)
            {
                int baseValue = _character.Stats[stat];
                int shown = baseValue + title.AllStatsBonus;
                var pos = new Vector2(x + (col % 2) * 300, y + (col / 2) * 26);
                gfx.Text(sb, $"{stat}  {baseValue} -> {shown}", pos, Theme.Text, 17f);
                col++;
            }
        }

        var b = ActionButton;
        bool hover = b.Contains(Mouse.GetState().Position);
        gfx.Rect(sb, b, hover ? Theme.ButtonHover : Theme.Button);
        gfx.Border(sb, b, Theme.Accent, 2);
        gfx.TextCentered(sb, worn ? "Unequip (Enter)" : "Equip (Enter)", b, Theme.Text, 22f);
    }
}

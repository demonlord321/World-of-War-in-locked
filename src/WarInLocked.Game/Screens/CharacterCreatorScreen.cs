using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Character creator (issues #2 and #3): Name, Race, Stat Distribution and the live stat list.
/// All rules live in Core's <see cref="CharacterCreation"/>; this screen only handles input and drawing.
/// Keyboard: Tab / Shift+Tab moves between fields, arrows change a dropdown, Enter confirms, Esc goes back.
/// </summary>
public sealed class CharacterCreatorScreen : Screen
{
    private enum Field { Name, Race, Distribution, Create, Back }
    private const int FieldCount = 5;

    private readonly CharacterCreation _creation = new();
    private readonly TextField _name = new(new Rectangle(60, 170, 420, 48));
    private readonly Dropdown<Race> _race = new(new Rectangle(60, 270, 420, 48));
    private readonly Dropdown<StatDistribution> _distribution = new(new Rectangle(60, 370, 420, 48));
    private readonly Button _create = new(new Rectangle(60, 520, 200, 56), "Create");
    private readonly Button _back = new(new Rectangle(280, 520, 200, 56), "Back");
    private Field _focus = Field.Name;

    public override void Update(GameTime time, InputState input)
    {
        // Dropdowns first, so an open list takes the click before anything underneath.
        _race.Update(input, _focus == Field.Race, out bool raceClicked);
        _distribution.Update(input, _focus == Field.Distribution, out bool distClicked);
        if (raceClicked) { _focus = Field.Race; _distribution.Close(); }
        if (distClicked) { _focus = Field.Distribution; _race.Close(); }

        if (input.Clicked && _name.Bounds.Contains(input.MousePosition))
        {
            _focus = Field.Name;
            input.ConsumeClick();
        }

        bool listOpen = _race.IsOpen || _distribution.IsOpen;

        if (!listOpen && input.Pressed(Keys.Tab))
            Move(input.Shift ? -1 : 1);

        if (_focus == Field.Name)
        {
            _name.HandleTyping(input);
            if (input.Pressed(Keys.Enter)) Move(1);
        }

        _creation.Name = _name.Text;
        _creation.Race = _race.Selected;
        _creation.Distribution = _distribution.Selected;
        _create.Enabled = _creation.CanCreate;

        bool activate = input.Pressed(Keys.Enter) || input.Pressed(Keys.Space);
        if (_create.WasClicked(input) || (activate && _focus == Field.Create && _create.Enabled))
        {
            Screens.Replace(new ForestScreen(_creation.Create()));
            return;
        }
        if (_back.WasClicked(input) || (activate && _focus == Field.Back) || (!listOpen && input.Pressed(Keys.Escape)))
        {
            Screens.Replace(new MainMenuScreen());
        }
    }

    private void Move(int step)
    {
        _focus = (Field)(((int)_focus + step + FieldCount) % FieldCount);
        if (_focus == Field.Create && !_create.Enabled) _focus = (Field)(((int)_focus + step + FieldCount) % FieldCount);
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        gfx.Text(sb, "Create Your Character", new Vector2(60, 50), Theme.Accent, 40f);

        gfx.Text(sb, "Name", new Vector2(60, 136), Theme.TextDim, 20f);
        gfx.Text(sb, "Race", new Vector2(60, 236), Theme.TextDim, 20f);
        gfx.Text(sb, "Stat Distribution", new Vector2(60, 336), Theme.TextDim, 20f);

        _name.Draw(sb, gfx, time, _focus == Field.Name, "Type a name...");
        if (!_creation.CanCreate)
            gfx.Text(sb, "Enter a name to continue.", new Vector2(60, 476), Theme.TextDim, 18f);

        _create.Draw(sb, gfx, input, _focus == Field.Create);
        _back.Draw(sb, gfx, input, _focus == Field.Back);

        DrawStats(sb, gfx);

        // Dropdown boxes then lists, so an open list sits on top of everything else.
        _race.DrawBox(sb, gfx, input, _focus == Field.Race);
        _distribution.DrawBox(sb, gfx, input, _focus == Field.Distribution);
        _distribution.DrawList(sb, gfx);
        _race.DrawList(sb, gfx);
    }

    private void DrawStats(SpriteBatch sb, Gfx gfx)
    {
        var panel = new Rectangle(600, 130, 620, 520);
        gfx.Rect(sb, panel, Theme.Panel);
        gfx.Border(sb, panel, Theme.PanelBorder);
        gfx.Text(sb, "Stats", new Vector2(panel.X + 24, panel.Y + 16), Theme.Accent, 28f);
        gfx.Text(sb, "Base 8", new Vector2(panel.X + 340, panel.Y + 24), Theme.TextDim, 18f);
        gfx.Text(sb, "Bonus", new Vector2(panel.X + 450, panel.Y + 24), Theme.TextDim, 18f);
        gfx.Text(sb, "Final", new Vector2(panel.X + 540, panel.Y + 24), Theme.TextDim, 18f);

        var stats = _creation.Stats;
        var bonus = _creation.Bonus;
        int y = panel.Y + 76;
        foreach (var stat in StatTypes.All)
        {
            int delta = stats[stat] - StatDistributions.BaseStat;
            gfx.Text(sb, stat.ToString(), new Vector2(panel.X + 24, y), Theme.Text, 26f);
            gfx.Text(sb, StatDistributions.BaseStat.ToString(), new Vector2(panel.X + 350, y), Theme.TextDim, 26f);
            gfx.Text(sb, FormatDelta(delta), new Vector2(panel.X + 450, y), DeltaColor(delta), 26f);
            gfx.Text(sb, stats[stat].ToString(), new Vector2(panel.X + 545, y), Theme.Text, 26f);
            y += 48;
        }
        gfx.Text(sb, "Total bonus: " + FormatDelta(bonus.Total), new Vector2(panel.X + 24, y + 4), Theme.TextDim, 20f);
    }

    public static string FormatDelta(int d) => d > 0 ? "+" + d : d.ToString();

    private static Color DeltaColor(int d) => d > 0 ? Theme.Positive : d < 0 ? Theme.Negative : Theme.TextDim;
}

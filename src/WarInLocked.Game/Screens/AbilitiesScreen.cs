using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Quests;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #20: the abilities screen. Left = the learned abilities, right = details for the focused one
/// (level, use progress, description, damage now, energy cost, cooldown, weapon requirement and why it cannot be used now).
/// Opened from the forest with K; K or Esc returns to the SAME <see cref="ForestScreen"/> instance.
/// Mouse hover or Up/Down moves the focus; keys 1-5 put the focused ability on that hotbar slot
/// (an ability sits in one slot only, so moving it swaps with whatever was there). Pure drawing and input: all numbers come from Core.
/// </summary>
public sealed class AbilitiesScreen : Screen
{
    private const int RowHeight = 64;
    private static readonly Color SlotFill = new(26, 29, 40);
    private static readonly Color BarBack = new(58, 48, 14), BarFill = new(240, 200, 50);

    private readonly Screen _back;
    private readonly Character _character;
    private int _focus;
    private string? _message;

    public AbilitiesScreen(Screen back, Character character)
    {
        _back = back;
        _character = character;
        _character.Combat.SyncHotbar();   // abilities learned since the last Update are on the bar
    }

    private static Rectangle ListPanel => new(30, 76, 400, 548);
    private static Rectangle DetailPanel => new(450, 76, 800, 548);
    private static Rectangle RowRect(int i) => new(ListPanel.X + 12, ListPanel.Y + 12 + i * (RowHeight + 8), ListPanel.Width - 24, RowHeight);

    public override void Update(GameTime time, InputState input)
    {
        if (input.Pressed(Keys.K) || input.Pressed(Keys.Escape))
        {
            Screens.Replace(_back);
            return;
        }

        var list = _character.LearnedAbilities;
        if (list.Count == 0) return;
        _focus = Math.Clamp(_focus, 0, list.Count - 1);

        if (input.MouseMoved)
            for (int i = 0; i < list.Count; i++)
                if (RowRect(i).Contains(input.MousePosition)) _focus = i;
        if (input.Clicked)
            for (int i = 0; i < list.Count; i++)
                if (RowRect(i).Contains(input.MousePosition)) { _focus = i; input.ConsumeClick(); break; }

        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) _focus = Math.Max(0, _focus - 1);
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) _focus = Math.Min(list.Count - 1, _focus + 1);

        for (int slot = 0; slot < Hotbar.SlotCount; slot++)
            if (input.Pressed(Keys.D1 + slot) || input.Pressed(Keys.NumPad1 + slot)) AssignToSlot(list[_focus], slot);
    }

    /// <summary>Puts the ability in a hotbar slot; if it was on the bar already, its old slot gets whatever the new slot held.</summary>
    private void AssignToSlot(Ability ability, int slot)
    {
        var bar = _character.Combat.Hotbar;
        string? displaced = bar.Get(slot).AbilityId;
        if (displaced == ability.Id) { _message = $"{ability.Name} is already on slot {slot + 1}"; return; }
        int old = bar.IndexOf(ability.Id);
        string? displacedItem = bar.Get(slot).ItemId;   // a potion on the target slot swaps too (#26)
        bar.Assign(slot, ability.Id);
        if (old >= 0)
        {
            if (displacedItem != null) bar.AssignItem(old, displacedItem);
            else bar.Assign(old, displaced);
        }
        string? other = displaced != null ? AbilityLibrary.NameOf(displaced) : displacedItem != null ? WarInLocked.Core.Items.ItemLibrary.Get(displacedItem)?.Name : null;
        _message = other != null && old >= 0
            ? $"{ability.Name} to slot {slot + 1}; {other} to slot {old + 1}"
            : $"{ability.Name} to slot {slot + 1}";
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Theme.Background);

        gfx.Rect(sb, new Rectangle(0, 0, w, 56), Theme.Panel);
        gfx.TextLeft(sb, $"Abilities  -  {_character.Name}", new Rectangle(0, 0, w, 56), Theme.Accent, 26f, 30);
        gfx.TextLeft(sb, "Up/Down/mouse: select  -  1-5: put on hotbar slot  -  K/Esc: close", new Rectangle(w - 780, 0, 780, 56), Theme.TextDim, 16f);

        foreach (var p in new[] { ListPanel, DetailPanel })
        {
            gfx.Rect(sb, p, Theme.Panel);
            gfx.Border(sb, p, Theme.PanelBorder, 2);
        }

        var list = _character.LearnedAbilities;
        if (list.Count == 0)
        {
            gfx.TextCentered(sb, "No abilities learned yet", ListPanel, Theme.TextDim, 20f);
            gfx.TextCentered(sb, "Complete quests to learn abilities.", DetailPanel, Theme.TextDim, 20f);
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
        gfx.TextLeft(sb, _message ?? (list.Count == 0 ? "Nothing to show yet" : "Press 1-5 to put the selected ability on that hotbar slot"), tip, _message != null ? Theme.Accent : Theme.TextDim, 18f, 16);
    }

    private string SlotText(Ability a)
    {
        int slot = _character.Combat.Hotbar.IndexOf(a.Id);
        return slot >= 0 ? $"Slot {slot + 1}" : "Not on hotbar";
    }

    private void DrawRow(SpriteBatch sb, Gfx gfx, Ability a, int i)
    {
        var r = RowRect(i);
        bool focus = i == _focus;
        gfx.Rect(sb, r, focus ? Theme.ButtonHover : SlotFill);
        gfx.Border(sb, r, focus ? Theme.Accent : Theme.PanelBorder, 2);
        bool blocked = CombatController.CannotCastReason(_character, a.Definition) != null;
        gfx.Text(sb, a.Name, new Vector2(r.X + 12, r.Y + 8), blocked ? Theme.TextDim : Theme.Text, 20f);
        gfx.Text(sb, SlotText(a), new Vector2(r.X + 12, r.Y + 36), Theme.TextDim, 14f);
        string lv = $"Lv {a.Level}";
        gfx.Text(sb, lv, new Vector2(r.Right - 12 - gfx.Measure(lv, 20f).X, r.Y + 8), Theme.Accent, 20f);
    }

    private void DrawDetails(SpriteBatch sb, Gfx gfx, Ability a)
    {
        var d = a.Definition;
        var p = DetailPanel;
        int x = p.X + 28, width = p.Width - 56, y = p.Y + 24;

        gfx.Text(sb, a.Name, new Vector2(x, y), Theme.Text, 34f);
        string lv = $"Lv {a.Level}";
        gfx.Text(sb, lv, new Vector2(p.Right - 28 - gfx.Measure(lv, 30f).X, y + 4), Theme.Accent, 30f);
        y += 56;

        // Use progress toward the next level.
        var bar = new Rectangle(x, y, width, 26);
        gfx.Rect(sb, bar, BarBack);
        double f = Math.Clamp(a.Fraction, 0, 1);
        if (f > 0) gfx.Rect(sb, new Rectangle(bar.X, bar.Y, Math.Max(1, (int)(bar.Width * f)), bar.Height), BarFill);
        gfx.Border(sb, bar, Color.Black, 1);
        gfx.TextCentered(sb, $"Uses {a.UsesIntoLevel}/{a.UsesForNextLevel} to Lv {a.Level + 1}", bar, Color.White, 16f);
        y += 46;

        foreach (var line in gfx.Wrap(d.Description, width, 20f))
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.TextDim, 20f);
            y += 28;
        }
        y += 18;

        void Row(string label, string value, Color? color = null)
        {
            gfx.Text(sb, label, new Vector2(x, y), Theme.TextDim, 20f);
            gfx.Text(sb, value, new Vector2(x + 200, y), color ?? Theme.Text, 20f);
            y += 34;
        }

        string scaling = d.Kind == AbilityKind.Magic ? "Intelligence" : "Strength";
        Row("Damage now", $"{CombatController.AbilityDamage(_character, a)}  ({scaling})");
        Row("Energy cost", d.EnergyCost.ToString());
        Row("Cooldown", $"{d.CooldownSeconds:0.#} s");
        Row("Hotbar", SlotText(a));
        if (d.RequiresWeapon) Row("Requires", "a weapon", Theme.Accent);

        string? reason = CombatController.CannotCastReason(_character, d);
        if (reason != null)
        {
            y += 10;
            gfx.Text(sb, reason, new Vector2(x, y), Theme.Negative, 24f);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Quests;

namespace WarInLocked.Game.Ui;

/// <summary>
/// Issues #11/#12: the in-game overlay. Top-right player panel (name, level, health and energy bars),
/// the bottom-centre hotbar (keys 1-5, cooldown shading) and the slime health bar / hit flicker.
/// Pure drawing: every number comes from Core (<see cref="Character"/>, <see cref="Hotbar"/>, <see cref="Enemy"/>).
/// </summary>
public static class Hud
{
    private static readonly Color HealthColor = new(200, 56, 56), HealthBack = new(60, 22, 24);
    private static readonly Color EnergyColor = new(48, 170, 190), EnergyBack = new(18, 46, 54);

    private const int PanelWidth = 280, PanelHeight = 92;
    private const int SlotSize = 54, SlotGap = 8;

    /// <summary>Draws the player panel and the hotbar in screen space.</summary>
    public static void Draw(SpriteBatch sb, Gfx gfx, Character character)
    {
        DrawPlayerPanel(sb, gfx, character);
        DrawHotbar(sb, gfx, character.Combat.Hotbar);
    }

    private static void DrawPlayerPanel(SpriteBatch sb, Gfx gfx, Character c)
    {
        var panel = new Rectangle(WarInLockedGame.ScreenWidth - PanelWidth - 10, 44, PanelWidth, PanelHeight);
        gfx.Rect(sb, panel, Color.Black * 0.65f);
        gfx.Border(sb, panel, Theme.PanelBorder, 2);

        string level = $"Lv {c.Level}";
        var lm = gfx.Measure(level, 18f);
        gfx.Text(sb, c.Name, new Vector2(panel.X + 12, panel.Y + 6), Theme.Text, 18f);
        gfx.Text(sb, level, new Vector2(panel.Right - 12 - lm.X, panel.Y + 6), Theme.Accent, 18f);

        Bar(sb, gfx, new Rectangle(panel.X + 12, panel.Y + 34, panel.Width - 24, 20), c.Health, c.MaxHealth, HealthColor, HealthBack, "HP");
        Bar(sb, gfx, new Rectangle(panel.X + 12, panel.Y + 62, panel.Width - 24, 20), c.Energy, c.MaxEnergy, EnergyColor, EnergyBack, "EN");
    }

    private static void Bar(SpriteBatch sb, Gfx gfx, Rectangle r, int value, int max, Color fill, Color back, string label)
    {
        gfx.Rect(sb, r, back);
        float f = max <= 0 ? 0 : MathHelper.Clamp(value / (float)max, 0f, 1f);
        if (f > 0) gfx.Rect(sb, new Rectangle(r.X, r.Y, Math.Max(1, (int)(r.Width * f)), r.Height), fill);
        gfx.Border(sb, r, Color.Black, 1);
        gfx.TextLeft(sb, label, r, Color.White, 13f, 6);
        gfx.TextCentered(sb, $"{value}/{max}", r, Color.White, 14f);
    }

    private static void DrawHotbar(SpriteBatch sb, Gfx gfx, Hotbar hotbar)
    {
        int total = Hotbar.SlotCount * SlotSize + (Hotbar.SlotCount - 1) * SlotGap;
        int x = (WarInLockedGame.ScreenWidth - total) / 2;
        int y = WarInLockedGame.ScreenHeight - SlotSize - 14;

        for (int i = 0; i < Hotbar.SlotCount; i++)       // slot index 0-4 = keys 1-5
        {
            var slot = hotbar.Get(i);
            var r = new Rectangle(x + i * (SlotSize + SlotGap), y, SlotSize, SlotSize);
            gfx.Rect(sb, r, slot.IsEmpty ? Color.Black * 0.35f : Theme.Panel * 0.92f);

            if (!slot.IsEmpty)
            {
                gfx.TextCentered(sb, AbilityLibrary.NameOf(slot.AbilityId), r, Theme.Text, 14f);
                // Cooldown: dark overlay from the top that shrinks as the ability becomes ready.
                double p = slot.CooldownProgress;
                if (p > 0)
                {
                    int hgt = (int)Math.Ceiling(SlotSize * p);
                    gfx.Rect(sb, new Rectangle(r.X, r.Y, SlotSize, hgt), Color.Black * 0.7f);
                    gfx.TextCentered(sb, slot.CooldownRemaining.ToString("0.0"), new Rectangle(r.X, r.Y + SlotSize / 2, SlotSize, SlotSize / 2), Theme.Accent, 15f);
                }
            }

            gfx.Border(sb, r, slot.IsEmpty ? Theme.PanelBorder * 0.5f : Theme.Accent, 2);
            gfx.Text(sb, (i + 1).ToString(), new Vector2(r.X + 4, r.Y + 1), slot.IsEmpty ? Theme.TextDim * 0.6f : Theme.TextDim, 13f);
        }
    }

    /// <summary>The slime body colour: flickers white/red for a moment after a hit.</summary>
    public static Color SlimeTint(Enemy? enemy, Color normal, GameTime time)
    {
        if (enemy == null || enemy.DamageFlashFraction <= 0) return normal;
        // Alternate white and red about every 0.05s while the flash fraction counts 1 -> 0.
        bool white = (int)(enemy.DamageFlashFraction * 10) % 2 == 0;
        return white ? Color.White : new Color(235, 60, 60);
    }

    /// <summary>A small health bar above a slime's head, only while it is damaged. <paramref name="body"/> is the slime's screen rectangle.</summary>
    public static void DrawSlimeBar(SpriteBatch sb, Gfx gfx, Enemy? enemy, Rectangle body)
    {
        if (enemy == null || !enemy.IsDamaged) return;
        var bar = new Rectangle(body.X - 2, body.Y - 8, body.Width + 4, 5);
        gfx.Rect(sb, bar, HealthBack);
        gfx.Rect(sb, new Rectangle(bar.X, bar.Y, Math.Max(1, (int)(bar.Width * enemy.HpFraction)), bar.Height), HealthColor);
        gfx.Border(sb, bar, Color.Black, 1);
    }
}

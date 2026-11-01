using System.Diagnostics;
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

    private static readonly Color XpColor = new(240, 200, 50), XpBack = new(58, 48, 14);

    private const int PanelWidth = 280, PanelHeight = 118;
    private const double LevelUpSeconds = 2.0;

    // Level-up banner state (#13). Hud is static, so it remembers the last level it saw per character
    // and starts a 2 second banner whenever the level goes up (the first time it sees a character it only records the level).
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static Character? _seen;
    private static int _seenLevel;
    private static double _flashStart = double.NegativeInfinity;
    private static int _flashLevel;
    private const int SlotSize = 54, SlotGap = 8;

    /// <summary>Draws the player panel and the hotbar in screen space.</summary>
    public static void Draw(SpriteBatch sb, Gfx gfx, Character character)
    {
        TrackLevel(character);
        DrawPlayerPanel(sb, gfx, character);
        DrawHotbar(sb, gfx, character.Combat.Hotbar);
        DrawLevelUpBanner(sb, gfx);
    }

    private static void TrackLevel(Character c)
    {
        if (!ReferenceEquals(_seen, c)) { _seen = c; _seenLevel = c.Level; _flashStart = double.NegativeInfinity; return; }
        if (c.Level > _seenLevel) { _flashStart = Clock.Elapsed.TotalSeconds; _flashLevel = c.Level; }
        _seenLevel = c.Level;
    }

    /// <summary>"LEVEL UP!" banner for about 2 seconds: fades out over the last half second and pulses a little.</summary>
    private static void DrawLevelUpBanner(SpriteBatch sb, Gfx gfx)
    {
        double t = Clock.Elapsed.TotalSeconds - _flashStart;
        if (t < 0 || t >= LevelUpSeconds) return;
        float alpha = (float)Math.Min(1.0, (LevelUpSeconds - t) / 0.5);
        float size = 44f + (float)Math.Sin(t * 10) * 2f;
        int w = WarInLockedGame.ScreenWidth;
        var box = new Rectangle((w - 420) / 2, 150, 420, 96);
        gfx.Rect(sb, box, Color.Black * (0.78f * alpha));
        gfx.Border(sb, box, XpColor * alpha, 3);
        gfx.TextCentered(sb, "LEVEL UP!", new Rectangle(box.X, box.Y + 4, box.Width, 56), XpColor * alpha, size);
        gfx.TextCentered(sb, $"You reached level {_flashLevel}", new Rectangle(box.X, box.Y + 58, box.Width, 30), Theme.Text * alpha, 20f);
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
        XpBar(sb, gfx, new Rectangle(panel.X + 12, panel.Y + 90, panel.Width - 24, 18), c);

        if (c.FreeStatPoints > 0)
        {
            // Gentle pulse so the hint is noticed without being loud.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Clock.Elapsed.TotalSeconds * 4);
            string hint = $"+{c.FreeStatPoints} pts  -  Press I to spend";
            var hr = new Rectangle(panel.X, panel.Bottom + 4, panel.Width, 26);
            gfx.Rect(sb, hr, Color.Black * 0.65f);
            gfx.Border(sb, hr, XpColor * pulse, 1);
            gfx.TextCentered(sb, hint, hr, XpColor * pulse, 16f);
        }
    }

    private static void XpBar(SpriteBatch sb, Gfx gfx, Rectangle r, Character c)
    {
        gfx.Rect(sb, r, XpBack);
        float f = Math.Clamp(c.XpFraction, 0f, 1f);
        if (f > 0) gfx.Rect(sb, new Rectangle(r.X, r.Y, Math.Max(1, (int)(r.Width * f)), r.Height), XpColor);
        gfx.Border(sb, r, Color.Black, 1);
        gfx.TextCentered(sb, $"XP {c.XpIntoLevel}/{c.XpForNextLevel}", r, Color.White, 13f);
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

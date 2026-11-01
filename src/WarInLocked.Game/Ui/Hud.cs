using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
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

    private const int PanelWidth = 280, PanelHeight = 136;
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
        DrawHotbar(sb, gfx, character);
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

        // #21: copper under the XP bar, small and right-aligned.
        // Money as gold / silver / copper coins, right-aligned.
        DrawMoney(sb, gfx, c.Copper, panel.Right - 12, panel.Y + 113, 14f, Theme.TextDim);

        // #29: "In combat 12s" under the panel while the combat window is open (healing potions are refused during it).
        int below = panel.Bottom + 4;
        DrawClock(sb, gfx, c, new Rectangle(panel.X, below, panel.Width, 26));   // #56: game time with a sun or moon (night comes from Core's IsNight)
        below += 30;
        if (c.InCombat)
        {
            var cr = new Rectangle(panel.X, below, panel.Width, 24);
            gfx.Rect(sb, cr, Color.Black * 0.65f);
            gfx.Border(sb, cr, new Color(220, 60, 60), 1);
            gfx.TextCentered(sb, $"In combat {(int)Math.Ceiling(c.CombatCooldownRemaining)}s", cr, new Color(255, 110, 100), 16f);
            below += 28;
        }

        if (c.FreeStatPoints > 0)
        {
            // Gentle pulse so the hint is noticed without being loud.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Clock.Elapsed.TotalSeconds * 4);
            string hint = $"+{c.FreeStatPoints} pts  -  Press I to spend";
            var hr = new Rectangle(panel.X, below, panel.Width, 26);
            gfx.Rect(sb, hr, Color.Black * 0.65f);
            gfx.Border(sb, hr, XpColor * pulse, 1);
            gfx.TextCentered(sb, hint, hr, XpColor * pulse, 16f);
        }
    }

    /// <summary>
    /// #56/#57: "Day 1, 8:00 am" with a small sun by day and a moon at night. Text and the night flag both come from Core's clock
    /// (<see cref="WarInLocked.Core.World.GameClock.Text"/>, <see cref="WarInLocked.Core.World.GameClock.IsNight"/>); no hours are hard-coded here.
    /// </summary>
    public static void DrawClock(SpriteBatch sb, Gfx gfx, Character c, Rectangle box)
    {
        bool night = c.Clock.IsNight;
        gfx.Rect(sb, box, Color.Black * 0.65f);
        gfx.Border(sb, box, night ? new Color(110, 140, 220) : Theme.PanelBorder, 1);
        var ic = new Rectangle(box.X + 9, box.Y + 5, 16, 16);
        if (night)
        {
            // Crescent moon: a pale disc with a dark bite out of its upper right.
            var moon = new Color(225, 230, 255);
            gfx.Rect(sb, new Rectangle(ic.X + 2, ic.Y + 4, 12, 8), moon);
            gfx.Rect(sb, new Rectangle(ic.X + 4, ic.Y + 2, 8, 12), moon);
            gfx.Rect(sb, new Rectangle(ic.X + 7, ic.Y + 1, 9, 9), new Color(14, 16, 28));
        }
        else
        {
            // Sun: a golden square disc with eight short rays.
            var sun = new Color(255, 205, 60);
            gfx.Rect(sb, new Rectangle(ic.X + 4, ic.Y + 4, 8, 8), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 7, ic.Y, 2, 3), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 7, ic.Y + 13, 2, 3), sun);
            gfx.Rect(sb, new Rectangle(ic.X, ic.Y + 7, 3, 2), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 13, ic.Y + 7, 3, 2), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 1, ic.Y + 1, 2, 2), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 13, ic.Y + 1, 2, 2), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 1, ic.Y + 13, 2, 2), sun);
            gfx.Rect(sb, new Rectangle(ic.X + 13, ic.Y + 13, 2, 2), sun);
        }
        gfx.TextLeft(sb, c.Clock.Text, new Rectangle(box.X + 34, box.Y, box.Width - 40, box.Height), night ? new Color(190, 205, 255) : Theme.Text, 16f, 0);
    }

    /// <summary>The copper colour, shared by the coin dots.</summary>
    public static readonly Color CopperColor = new(205, 127, 50), CopperLight = new(240, 176, 110);

    public static readonly Color GoldColor = new(230, 190, 60), GoldLight = new(255, 235, 150);
    public static readonly Color SilverColor = new(192, 196, 204), SilverLight = new(240, 242, 246);

    /// <summary>A small round copper coin (10x10) drawn from rectangles, top-left at (x, y).</summary>
    public static void CoinDot(SpriteBatch sb, Gfx gfx, int x, int y) => CoinDot(sb, gfx, x, y, CopperColor, CopperLight);

    /// <summary>A small round coin (10x10) in the given colours, top-left at (x, y).</summary>
    public static void CoinDot(SpriteBatch sb, Gfx gfx, int x, int y, Color body, Color shine)
    {
        gfx.Rect(sb, new Rectangle(x + 2, y, 6, 10), body);
        gfx.Rect(sb, new Rectangle(x, y + 2, 10, 6), body);
        gfx.Rect(sb, new Rectangle(x + 1, y + 1, 8, 8), body);
        gfx.Rect(sb, new Rectangle(x + 3, y + 2, 3, 2), shine);
    }

    /// <summary>
    /// Draws a copper total as coin dots + amounts ("1g 23s 45c"), hiding leading zero
    /// denominations, right-aligned so the last character ends at <paramref name="right"/>.
    /// Returns the drawn width.
    /// </summary>
    public static int DrawMoney(SpriteBatch sb, Gfx gfx, int total, int right, int y, float size, Color textColor)
    {
        long g = Money.Gold(total);
        int s = Money.Silver(total), c = Money.CopperPart(total);
        var parts = new System.Collections.Generic.List<(string Text, Color Body, Color Shine)>();
        if (g > 0) parts.Add(($"{g}g", GoldColor, GoldLight));
        if (g > 0 || s > 0) parts.Add(($"{s}s", SilverColor, SilverLight));
        parts.Add(($"{c}c", CopperColor, CopperLight));

        const int dot = 10, dotGap = 3, partGap = 8;
        int width = 0;
        var widths = new int[parts.Count];
        for (int i = 0; i < parts.Count; i++)
        {
            widths[i] = (int)Math.Ceiling(gfx.Measure(parts[i].Text, size).X);
            width += dot + dotGap + widths[i] + (i > 0 ? partGap : 0);
        }
        int x = right - width;
        int dotY = y + (int)(size * 0.6f) - dot / 2 + 1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0) x += partGap;
            CoinDot(sb, gfx, x, dotY, parts[i].Body, parts[i].Shine);
            x += dot + dotGap;
            gfx.Text(sb, parts[i].Text, new Vector2(x, y), textColor, size);
            x += widths[i];
        }
        return width;
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

    private static void DrawHotbar(SpriteBatch sb, Gfx gfx, Character character)
    {
        var hotbar = character.Combat.Hotbar;
        int total = Hotbar.SlotCount * SlotSize + (Hotbar.SlotCount - 1) * SlotGap;
        int x = (WarInLockedGame.ScreenWidth - total) / 2;
        int y = WarInLockedGame.ScreenHeight - SlotSize - 14;

        for (int i = 0; i < Hotbar.SlotCount; i++)       // slot index 0-4 = keys 1-5
        {
            var slot = hotbar.Get(i);
            var r = new Rectangle(x + i * (SlotSize + SlotGap), y, SlotSize, SlotSize);
            gfx.Rect(sb, r, slot.IsEmpty ? Color.Black * 0.35f : Theme.Panel * 0.92f);

            // An ability that cannot be cast right now (Slash unarmed), or an item with none left, is dimmed with a dark veil and a red border.
            bool dim = false;
            if (slot.IsItem)
            {
                // #26: a consumable. Name wrapped small, the count left in the bag bottom-right, and the shared item cooldown overlay.
                var item = ItemLibrary.Get(slot.ItemId);
                int count = character.Inventory.CountOf(slot.ItemId);
                dim = count == 0;
                // The slot is small: a short name is wrapped over two lines; a long one (Potion of lesser healing) shows initials (PLH).
                string full = item?.Name ?? slot.ItemId;
                var lines = gfx.Wrap(full, SlotSize - 8, 12f);
                var nameColor = dim ? Theme.TextDim * 0.6f : Theme.Text;
                if (lines.Count <= 2)
                {
                    int ty = r.Y + 16;
                    foreach (var line in lines)
                    {
                        gfx.TextCentered(sb, line, new Rectangle(r.X, ty, SlotSize, 14), nameColor, 12f);
                        ty += 14;
                    }
                }
                else
                {
                    string abbr = string.Concat(full.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 2).Select(w => char.ToUpperInvariant(w[0])));
                    gfx.TextCentered(sb, abbr, new Rectangle(r.X, r.Y + 12, SlotSize, 24), nameColor, 20f);
                }
                string cnt = "x" + count;
                var cm = gfx.Measure(cnt, 14f);
                gfx.Text(sb, cnt, new Vector2(r.Right - 4 - cm.X, r.Bottom - 3 - cm.Y), dim ? Theme.Negative : Theme.Accent, 14f);
                if (dim) gfx.Rect(sb, r, Color.Black * 0.55f);
            }
            else if (!slot.IsEmpty)
            {
                var def = AbilityLibrary.Get(slot.AbilityId);
                dim = def != null && CombatController.CannotCastReason(character, def) != null;
                gfx.TextCentered(sb, AbilityLibrary.NameOf(slot.AbilityId), r, dim ? Theme.TextDim * 0.6f : Theme.Text, 14f);
                if (dim) gfx.Rect(sb, r, Color.Black * 0.55f);
            }
            if (!slot.IsEmpty)
            {
                // Cooldown: dark overlay from the top that shrinks as the ability becomes ready.
                double p = slot.CooldownProgress;
                if (p > 0)
                {
                    int hgt = (int)Math.Ceiling(SlotSize * p);
                    gfx.Rect(sb, new Rectangle(r.X, r.Y, SlotSize, hgt), Color.Black * 0.7f);
                    gfx.TextCentered(sb, slot.CooldownRemaining.ToString("0.0"), new Rectangle(r.X, r.Y + SlotSize / 2, SlotSize, SlotSize / 2), Theme.Accent, 15f);
                }
            }

            gfx.Border(sb, r, slot.IsEmpty ? Theme.PanelBorder * 0.5f : dim ? Theme.Negative * 0.6f : Theme.Accent, 2);
            gfx.Text(sb, (i + 1).ToString(), new Vector2(r.X + 4, r.Y + 1), slot.IsEmpty ? Theme.TextDim * 0.6f : Theme.TextDim, 13f);
        }
    }

    /// <summary>The slime body colour: flickers white/red for a moment after a hit.</summary>
    /// <summary>How long before an aggro'd slime's attack its wind-up pulse shows.</summary>
    public const double WindUpSeconds = 0.5;

    public static Color SlimeTint(Enemy? enemy, Color normal, GameTime time)
    {
        if (enemy == null) return normal;
        if (enemy.DamageFlashFraction <= 0)
        {
            // #29: an aggro'd slime is reddish; it pulses in the last half second before its attack and flashes bright when it strikes.
            if (enemy.AttackFlashFraction > 0) return new Color(255, 235, 150);
            if (!enemy.IsAggro) return normal;
            var angry = Color.Lerp(normal, new Color(225, 55, 50), 0.6f);
            if (enemy.AttackCooldownRemaining > 0 && enemy.AttackCooldownRemaining <= WindUpSeconds)
            {
                bool bright = (int)(time.TotalGameTime.TotalSeconds * 10) % 2 == 0;
                return bright ? new Color(255, 120, 90) : angry;
            }
            return angry;
        }
        // Alternate white and red about every 0.05s while the flash fraction counts 1 -> 0.
        bool white = (int)(enemy.DamageFlashFraction * 10) % 2 == 0;
        return white ? Color.White : new Color(235, 60, 60);
    }

    /// <summary>
    /// Slime level tag colour (#24): 1 green, 2 yellow, 3 orange, 4 red, 5 deep purple (Path slimes). Above 5 (night slimes, base + 10, #57)
    /// it runs from hot crimson-pink to a dark violet-magenta as the level rises, so it never looks like levels 4 or 5.
    /// </summary>
    public static Color SlimeLevelColor(int level) =>
        level <= 1 ? new Color(110, 220, 110) : level == 2 ? new Color(240, 220, 70) : level == 3 ? new Color(245, 150, 50)
        : level == 4 ? new Color(235, 50, 50) : level == 5 ? new Color(150, 70, 210)
        : Color.Lerp(new Color(245, 55, 120), new Color(175, 50, 175), Math.Clamp((level - 6) / 9f, 0f, 1f));

    /// <summary>
    /// Above a slime's head: a "Lv N" tag, always visible and coloured by level (#24), and a small health bar while it is
    /// damaged. The bar fills by <see cref="Enemy.HpFraction"/>, which is relative to that slime's own max health.
    /// <paramref name="body"/> is the slime's screen rectangle.
    /// </summary>
    public static void DrawSlimeBar(SpriteBatch sb, Gfx gfx, Enemy? enemy, Rectangle body)
    {
        if (enemy == null) return;
        string tag = $"Lv {enemy.Level}";
        // #57: a night slime (base level + 10) also shows a small "+10" so the player sees why it is so strong.
        string bonus = enemy.IsNightEmpowered ? $" (+{enemy.Level - enemy.BaseLevel})" : "";
        var m = gfx.Measure(tag, 12f);
        var bm = bonus.Length > 0 ? gfx.Measure(bonus, 10f) : Vector2.Zero;
        var pos = new Vector2(body.X + (body.Width - m.X - bm.X) / 2f, body.Y - 8 - m.Y - 1);
        gfx.Text(sb, tag, pos + new Vector2(1, 1), Color.Black * 0.9f, 12f);   // shadow so it reads on grass and paths
        gfx.Text(sb, tag, pos, SlimeLevelColor(enemy.Level), 12f);
        if (bonus.Length > 0)
        {
            var bpos = new Vector2(pos.X + m.X, pos.Y + 2);
            gfx.Text(sb, bonus, bpos + new Vector2(1, 1), Color.Black * 0.9f, 10f);
            gfx.Text(sb, bonus, bpos, new Color(170, 190, 255), 10f);
        }
        if (enemy.IsAggro)
        {
            var bang = gfx.Measure("!", 18f);
            var bp = new Vector2(body.X + (body.Width - bang.X) / 2f, pos.Y - bang.Y - 1);
            gfx.Text(sb, "!", bp + new Vector2(1, 1), Color.Black, 18f);
            gfx.Text(sb, "!", bp, new Color(255, 70, 60), 18f);
        }
        if (!enemy.IsDamaged) return;
        var bar = new Rectangle(body.X - 2, body.Y - 8, body.Width + 4, 5);
        gfx.Rect(sb, bar, HealthBack);
        gfx.Rect(sb, new Rectangle(bar.X, bar.Y, Math.Max(1, (int)(bar.Width * enemy.HpFraction)), bar.Height), HealthColor);
        gfx.Border(sb, bar, Color.Black, 1);
    }
}

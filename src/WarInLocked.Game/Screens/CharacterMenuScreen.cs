using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issues #9/#10: the character menu. Left half = the 10 equipment slots around a body outline,
/// right half = character header, the 8 stats and the 10 inventory slots (5x2).
/// Opened from the forest with I; I or Esc returns to the SAME <see cref="ForestScreen"/> instance.
/// Mouse hover or the arrow keys move a focus cursor over all 20 slots; the tooltip bar shows the focused slot.
/// </summary>
public sealed class CharacterMenuScreen : Screen
{
    private readonly Screen _back;
    private readonly Character _character;

    private const int InventoryCount = 10;
    private string? _message;
    private static string[] EquipmentSlotNames => EquipmentSlots.All.Select(EquipmentSlots.Label).ToArray();

    private const int EquipCount = 10;
    private readonly Rectangle[] _slots = new Rectangle[EquipCount + 10]; // 0-9 equipment, 10-19 inventory
    private int _focus;

    private static readonly Color SlotFill = new(26, 29, 40);
    private static readonly Color Outline = new(52, 58, 80);

    public CharacterMenuScreen(Screen back, Character character)
    {
        _back = back;
        _character = character;
        BuildLayout();
    }

    private void BuildLayout()
    {
        const int cx = 330, w = 110, h = 52;
        Rectangle Centre(int y) => new(cx - w / 2, y, w, h);
        _slots[0] = Centre(130);                              // Head
        _slots[1] = Centre(192);                              // Shoulder
        _slots[2] = Centre(254);                              // Chest
        _slots[3] = Centre(316);                              // Legs
        _slots[4] = Centre(378);                              // Feet
        _slots[5] = new Rectangle(115, 254, w, h);            // Main Hand
        _slots[6] = new Rectangle(cx + 80, 254, w, h);        // Off Hand (mirrors Main Hand)
        for (int i = 0; i < 3; i++) _slots[7 + i] = new Rectangle(cx - 185 + i * (w + 20), 490, w, h);

        const int box = 80, gap = 10, gx = 730, gy = 395;
        for (int i = 0; i < 10; i++)
            _slots[EquipCount + i] = new Rectangle(gx + (i % 5) * (box + gap), gy + (i / 5) * (box + gap), box, box);
    }

    public override void Update(GameTime time, InputState input)
    {
        if (input.Pressed(Keys.I) || input.Pressed(Keys.Escape))
        {
            Screens.Replace(_back);
            return;
        }

        if (input.MouseMoved)
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Contains(input.MousePosition)) _focus = i;

        bool activate = input.Pressed(Keys.Enter);
        if (input.Clicked)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Contains(input.MousePosition)) { _focus = i; activate = true; input.ConsumeClick(); break; }
        }
        if (activate) Activate(_focus);

        if (input.Pressed(Keys.Left) || input.Pressed(Keys.A)) Move(-1, 0);
        if (input.Pressed(Keys.Right) || input.Pressed(Keys.D)) Move(1, 0);
        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) Move(0, -1);
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) Move(0, 1);
    }

    /// <summary>Enter or click: equips the item in an inventory slot, or unequips the item in an equipment slot (Core rules).</summary>
    private void Activate(int i)
    {
        if (i < EquipCount)
        {
            var slot = EquipmentSlots.All[i];
            var item = _character.Equipment.Get(slot);
            var result = _character.Equipment.Unequip(slot);
            _message = result switch
            {
                UnequipResult.Success => $"Unequipped {item!.Name}",
                UnequipResult.InventoryFull => "Inventory is full",
                _ => null,
            };
        }
        else
        {
            var stack = _character.Inventory.Get(i - EquipCount);
            var result = _character.Equipment.Equip(i - EquipCount);
            _message = result switch
            {
                EquipResult.Success => $"Equipped {stack!.Item.Name}",
                EquipResult.NotEquippable => $"{stack!.Item.Name} cannot be equipped",
                EquipResult.WrongSlot => $"{stack!.Item.Name} does not fit there",
                _ => null,
            };
        }
    }

    /// <summary>Moves the focus to the nearest slot in the given direction (spatial, so it works across both halves).</summary>
    private void Move(int dx, int dy)
    {
        var from = _slots[_focus].Center;
        int best = -1;
        float bestScore = float.MaxValue;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i == _focus) continue;
            var d = _slots[i].Center - from;
            int along = d.X * dx + d.Y * dy;
            if (along <= 0) continue;
            int across = Math.Abs(d.X * dy) + Math.Abs(d.Y * dx);
            if (across > along * 2 + 40) continue;
            float score = along + across * 2f;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        if (best >= 0) _focus = best;
    }

    private string SlotType(int i) => i < EquipCount ? EquipmentSlotNames[i] : $"Inventory slot {i - EquipCount + 1}";
    private string? ItemAt(int i)
    {
        if (i < EquipCount) return _character.Equipment.Get(EquipmentSlots.All[i])?.Name;
        var stack = _character.Inventory.Get(i - EquipCount);
        return stack?.ToString();
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Theme.Background);

        gfx.Rect(sb, new Rectangle(0, 0, w, 56), Theme.Panel);
        gfx.TextLeft(sb, $"Character  -  {_character.Name}", new Rectangle(0, 0, w, 56), Theme.Accent, 26f, 30);
        gfx.TextLeft(sb, "Arrows/mouse: select  -  Enter/click: equip or unequip  -  I/Esc: close", new Rectangle(w - 700, 0, 700, 56), Theme.TextDim, 16f);

        var left = new Rectangle(30, 76, 600, 548);
        var right = new Rectangle(650, 76, 600, 548);
        foreach (var p in new[] { left, right })
        {
            gfx.Rect(sb, p, Theme.Panel);
            gfx.Border(sb, p, Theme.PanelBorder, 2);
        }

        DrawEquipmentPanel(sb, gfx, left);
        DrawRightPanel(sb, gfx, right);

        for (int i = 0; i < _slots.Length; i++) DrawSlot(sb, gfx, i);

        // Tooltip line along the bottom.
        var tip = new Rectangle(30, 636, 1220, 54);
        gfx.Rect(sb, tip, Theme.Panel);
        gfx.Border(sb, tip, Theme.PanelBorder, 2);
        string? item = ItemAt(_focus);
        string text = item == null ? $"{SlotType(_focus)}  -  Empty" : $"{item}  -  {SlotType(_focus)}";
        if (_message != null) text += "     |     " + _message;
        gfx.TextLeft(sb, text, tip, item == null ? Theme.TextDim : Theme.Text, 20f, 20);
    }

    private void DrawEquipmentPanel(SpriteBatch sb, Gfx gfx, Rectangle panel)
    {
        gfx.TextLeft(sb, "Equipment", new Rectangle(panel.X, panel.Y + 8, panel.Width, 36), Theme.Accent, 22f, 20);

        // Simple body outline from rectangles, drawn behind the slots.
        void Part(int x, int y, int pw, int ph) { gfx.Rect(sb, new Rectangle(x, y, pw, ph), Outline); }
        Part(330 - 10, 182, 20, 12);                       // neck
        Part(330 - 75, 122, 150, 62);                      // head frame
        gfx.Rect(sb, new Rectangle(330 - 71, 126, 142, 54), Theme.Panel);
        Part(330 - 75, 186, 150, 126);                     // torso
        gfx.Rect(sb, new Rectangle(330 - 71, 190, 142, 118), Theme.Panel);
        Part(225, 272, 30, 10);                            // main-hand arm
        Part(405, 272, 30, 10);                            // off-hand arm
        Part(330 - 62, 312, 56, 126);                      // left leg
        Part(330 + 6, 312, 56, 126);                       // right leg
        gfx.Rect(sb, new Rectangle(330 - 58, 316, 48, 118), Theme.Panel);
        gfx.Rect(sb, new Rectangle(330 + 10, 316, 48, 118), Theme.Panel);

        gfx.Text(sb, "Accessories", new Vector2(_slots[7].X, _slots[7].Y - 26), Theme.TextDim, 16f);
    }

    private void DrawRightPanel(SpriteBatch sb, Gfx gfx, Rectangle panel)
    {
        gfx.Text(sb, _character.Name, new Vector2(panel.X + 24, panel.Y + 12), Theme.Text, 28f);
        gfx.Text(sb, $"{_character.Race}  -  {_character.Distribution}", new Vector2(panel.X + 24, panel.Y + 50), Theme.Accent, 18f);

        gfx.Text(sb, "Stats", new Vector2(panel.X + 24, panel.Y + 90), Theme.TextDim, 16f);
        for (int i = 0; i < StatTypes.Count; i++)
        {
            var stat = StatTypes.All[i];
            int col = i / 4, row = i % 4;
            int x = panel.X + 24 + col * 280, y = panel.Y + 116 + row * 30;
            gfx.Text(sb, stat.ToString(), new Vector2(x, y), Theme.Text, 18f);
            var v = _character.Stats[stat].ToString();
            gfx.Text(sb, v, new Vector2(x + 240 - gfx.Measure(v, 18f).X, y), Theme.Accent, 18f);
        }

        gfx.Text(sb, "Inventory", new Vector2(panel.X + 24, _slots[EquipCount].Y - 38), Theme.Accent, 22f);
    }

    private void DrawSlot(SpriteBatch sb, Gfx gfx, int i)
    {
        var r = _slots[i];
        bool focus = i == _focus;
        gfx.Rect(sb, r, focus ? Theme.ButtonHover : SlotFill);
        gfx.Border(sb, r, focus ? Theme.Accent : Theme.PanelBorder, 2);

        string? item = ItemAt(i);
        string? label = i < EquipCount ? EquipmentSlotNames[i] : null;
        if (label != null) gfx.TextCentered(sb, label, new Rectangle(r.X, r.Y + 4, r.Width, 16), Theme.TextDim, 12f);
        var body = label != null ? new Rectangle(r.X, r.Y + 20, r.Width, r.Height - 22) : r;
        gfx.TextCentered(sb, item ?? "Empty", body, item == null ? Theme.TextDim * 0.7f : Theme.Text, item == null ? 14f : 15f);
    }
}

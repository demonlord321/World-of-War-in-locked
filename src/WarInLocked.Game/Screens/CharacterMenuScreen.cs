using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issues #9/#10: the character menu. Left half = the 10 equipment slots around a body outline,
/// right half = character header, the 8 stats and the 10 inventory slots (5x2).
/// Opened from the forest with I; I or Esc returns to the SAME <see cref="ForestScreen"/> instance.
/// Mouse hover or the arrow keys move a focus cursor over all 20 slots; the tooltip bar shows the focused slot.
/// Issue #13: the 8 stats are focusable rows with [-] [+] buttons for spending free stat points. Points can be taken
/// back only if they were added since this menu was opened (Core has no lock-in; closing the menu locks them).
/// </summary>
public sealed class CharacterMenuScreen : Screen
{
    private readonly Screen _back;
    private readonly Character _character;

    private const int InventoryCount = 10;
    private string? _message;
    private static string[] EquipmentSlotNames => EquipmentSlots.All.Select(EquipmentSlots.Label).ToArray();

    private const int EquipCount = 10;
    private const int StatBase = EquipCount + InventoryCount;                  // 20-27 = the 8 stat rows
    private readonly Rectangle[] _slots = new Rectangle[StatBase + 8];          // 0-9 equipment, 10-19 inventory, 20-27 stats
    private readonly Rectangle[] _minus = new Rectangle[8], _plus = new Rectangle[8];
    private readonly int[] _lockedAssigned = new int[8];                        // points already assigned when the menu opened
    private int _focus;

    private static readonly Color SlotFill = new(26, 29, 40);
    private static readonly Color Outline = new(52, 58, 80);

    private readonly Point _playerTile;   // where X drops items (#62)

    public CharacterMenuScreen(Screen back, Character character, Point playerTile = default)
    {
        _playerTile = playerTile;
        _back = back;
        _character = character;
        foreach (var stat in StatTypes.All) _lockedAssigned[(int)stat] = character.AssignedPoints(stat);
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
        for (int i = 0; i < 3; i++) _slots[7 + i] = new Rectangle(cx - 185 + i * (w + 20), 452, w, h);

        const int box = 80, gap = 10, gx = 730, gy = 445;
        for (int i = 0; i < 10; i++)
            _slots[EquipCount + i] = new Rectangle(gx + (i % 5) * (box + gap), gy + (i / 5) * (box + gap), box, box);

        // Stat rows: name, value, [-] and [+] buttons (single column, left of the "Free points" box).
        for (int i = 0; i < StatTypes.Count; i++)
        {
            var row = new Rectangle(674, 190 + i * 26, 290, 26);
            _slots[StatBase + i] = row;
            _minus[i] = new Rectangle(row.X + 210, row.Y + 2, 32, 22);
            _plus[i] = new Rectangle(row.X + 250, row.Y + 2, 32, 22);
        }
    }

    public override void Update(GameTime time, InputState input)
    {
        if (_dropSlot >= 0) { UpdateDropSlider(input); return; }
        if (input.Pressed(Keys.I) || input.Pressed(Keys.Escape))
        {
            Screens.Replace(_back);
            return;
        }

        // T (or the Titles button) opens the titles screen (#27); it gets this same menu back so the point lock is kept.
        if (input.Pressed(Keys.T) || (input.Clicked && TitlesButton.Contains(input.MousePosition)))
        {
            input.ConsumeClick();
            Screens.Replace(new TitlesScreen(this, _character));
            return;
        }

        // Items (#26): U uses the focused inventory item, keys 1-5 put it on that hotbar slot.
        if (_focus >= EquipCount && _focus < StatBase)
        {
            if (input.Pressed(Keys.U)) UseItem(_focus - EquipCount);
            if (input.Pressed(Keys.X)) DropFocused(_focus - EquipCount, Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift));
            for (int slot = 0; slot < Hotbar.SlotCount; slot++)
                if (input.Pressed(Keys.D1 + slot) || input.Pressed(Keys.NumPad1 + slot)) AssignToHotbar(_focus - EquipCount, slot);
        }

        if (input.MouseMoved)
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Contains(input.MousePosition)) _focus = i;

        bool activate = input.Pressed(Keys.Enter);
        if (input.Clicked)
        {
            var mouse = input.MousePosition;
            for (int s = 0; s < StatTypes.Count; s++)
            {
                if (_plus[s].Contains(mouse)) { _focus = StatBase + s; AddPoint(s); input.ConsumeClick(); break; }
                if (_minus[s].Contains(mouse)) { _focus = StatBase + s; RemovePoint(s); input.ConsumeClick(); break; }
            }
            if (input.Clicked)
                for (int i = 0; i < StatBase; i++)      // stat rows only react to their buttons, not to a click on the row
                    if (_slots[i].Contains(mouse)) { _focus = i; activate = true; input.ConsumeClick(); break; }
        }

        bool onStat = _focus >= StatBase;
        int stat = _focus - StatBase;
        if (onStat)
        {
            if (input.Pressed(Keys.Enter)) { AddPoint(stat); activate = false; }
            if (input.Pressed(Keys.Right) || input.Pressed(Keys.OemPlus) || input.Pressed(Keys.Add)) AddPoint(stat);
            if (input.Pressed(Keys.Left) || input.Pressed(Keys.OemMinus) || input.Pressed(Keys.Subtract)) RemovePoint(stat);
        }
        if (activate && !onStat) Activate(_focus);

        // On a stat row Left/Right spend points instead of moving; A/D still move the cursor sideways everywhere.
        if (input.Pressed(Keys.A) || (!onStat && input.Pressed(Keys.Left))) Move(-1, 0);
        if (input.Pressed(Keys.D) || (!onStat && input.Pressed(Keys.Right))) Move(1, 0);
        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) Move(0, -1);
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) Move(0, 1);
    }

    private int NewPoints(int s) => _character.AssignedPoints(StatTypes.All[s]) - _lockedAssigned[s];

    private void AddPoint(int s)
    {
        var stat = StatTypes.All[s];
        _message = _character.AssignPoint(stat) ? $"{stat} +1" : "No free points";
    }

    /// <summary>Takes back a point added since the menu opened; older points are locked in.</summary>
    private void RemovePoint(int s)
    {
        var stat = StatTypes.All[s];
        if (NewPoints(s) <= 0) { _message = _character.AssignedPoints(stat) > 0 ? "Points from before this menu are locked in" : null; return; }
        _message = _character.UnassignPoint(stat) ? $"{stat} -1" : null;
    }

    private int _dropSlot = -1, _dropAmount = 1;

    /// <summary>#62 drop slider: Left/Right (A/D) by 1, Up/Down (W/S) by 10, Enter drops, Esc/X cancels.</summary>
    private void UpdateDropSlider(InputState input)
    {
        var stack = _character.Inventory.Get(_dropSlot);
        if (stack == null || input.Pressed(Keys.Escape) || input.Pressed(Keys.X)) { _dropSlot = -1; return; }
        int max = stack.Count;
        if (input.Pressed(Keys.Right) || input.Pressed(Keys.D)) _dropAmount++;
        if (input.Pressed(Keys.Left) || input.Pressed(Keys.A)) _dropAmount--;
        if (input.Pressed(Keys.Up) || input.Pressed(Keys.W)) _dropAmount += 10;
        if (input.Pressed(Keys.Down) || input.Pressed(Keys.S)) _dropAmount -= 10;
        _dropAmount = Math.Clamp(_dropAmount, 1, max);
        if (input.Pressed(Keys.Enter))
        {
            var r = _character.DropItem(_dropSlot, _dropAmount, _playerTile.X, _playerTile.Y);
            _message = r.Message ?? (r.Dropped ? $"Dropped {r.Item.Name}" + (r.Count > 1 ? $" x{r.Count}" : "") : null);
            _dropSlot = -1;
        }
    }

    private void DrawDropSlider(SpriteBatch sb, Gfx gfx, int w, int h)
    {
        if (_dropSlot < 0) return;
        var stack = _character.Inventory.Get(_dropSlot);
        if (stack == null) return;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * 0.5f);
        var p = new Rectangle(w / 2 - 220, h / 2 - 80, 440, 160);
        gfx.Rect(sb, p, Theme.Background);
        gfx.Border(sb, p, Theme.Accent, 2);
        gfx.TextCentered(sb, $"Drop how many {stack.Item.Name}?", new Rectangle(p.X, p.Y + 10, p.Width, 30), Theme.Text, 22f);
        var track = new Rectangle(p.X + 40, p.Y + 66, p.Width - 80, 8);
        gfx.Rect(sb, track, Theme.TextDim * 0.5f);
        float f = stack.Count <= 1 ? 1f : (_dropAmount - 1) / (float)(stack.Count - 1);
        gfx.Rect(sb, new Rectangle(track.X, track.Y, (int)(track.Width * f), track.Height), Theme.Accent);
        gfx.Rect(sb, new Rectangle(track.X + (int)(track.Width * f) - 6, track.Y - 8, 12, 24), Theme.Text);
        gfx.TextCentered(sb, $"{_dropAmount} / {stack.Count}", new Rectangle(p.X, p.Y + 86, p.Width, 28), Theme.Text, 20f);
        gfx.TextCentered(sb, "Left/Right 1  -  Up/Down 10  -  Enter drop  -  Esc cancel", new Rectangle(p.X, p.Bottom - 32, p.Width, 24), Theme.TextDim, 14f);
    }

    /// <summary>#62: X drops one of the focused bag item onto the player's tile, Shift+X the whole stack. Equipped gear is not in the bag; quest items are refused by Core.</summary>
    private void DropFocused(int invSlot, bool wholeStack)
    {
        var stack = _character.Inventory.Get(invSlot);
        if (stack == null) { _message = null; return; }
        if (!wholeStack && stack.Count > 1) { _dropSlot = invSlot; _dropAmount = 1; _message = null; return; }   // slider for 1..Count
        var r = wholeStack ? _character.DropStack(invSlot, _playerTile.X, _playerTile.Y) : _character.DropItem(invSlot, 1, _playerTile.X, _playerTile.Y);
        _message = r.Message ?? (r.Dropped ? $"Dropped {r.Item.Name}" + (r.Count > 1 ? $" x{r.Count}" : "") : null);
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
            if (stack != null && stack.Item.IsUsable) { UseItem(i - EquipCount); return; }   // Enter on a potion drinks it
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

    private static Rectangle TitlesButton => new(1116, 160, 110, 24);

    /// <summary>Uses a consumable from an inventory slot (#26). Core decides: cooldown, full health and None left are refused.</summary>
    private void UseItem(int invSlot)
    {
        var stack = _character.Inventory.Get(invSlot);
        if (stack == null || !stack.Item.IsUsable) { _message = stack == null ? null : $"{stack.Item.Name} cannot be used"; return; }
        var result = _character.UseItem(stack.Item.Id);
        _message = result.Success ? $"{result.Item.Name}: healed {result.Healed} HP" : result.Reason;
    }

    /// <summary>Puts the consumable in an inventory slot on a hotbar slot (#26); keys 1-5 then use it in the forest.</summary>
    private void AssignToHotbar(int invSlot, int slot)
    {
        var stack = _character.Inventory.Get(invSlot);
        if (stack == null || !stack.Item.IsUsable) { _message = stack == null ? null : $"{stack.Item.Name} cannot go on the hotbar"; return; }
        var bar = _character.Combat.Hotbar;
        string? displaced = bar.Get(slot).AbilityId;
        string? displacedItem = bar.Get(slot).ItemId;
        if (displacedItem == stack.Item.Id) { _message = $"{stack.Item.Name} is already on slot {slot + 1}"; return; }
        int old = bar.IndexOfItem(stack.Item.Id);
        _character.Combat.AssignItemToHotbar(slot, stack.Item.Id);
        if (old >= 0)
        {
            if (displaced != null) bar.Assign(old, displaced);
            else if (displacedItem != null) bar.AssignItem(old, displacedItem);
            else bar.Assign(old, null);
        }
        _message = $"{stack.Item.Name} to hotbar slot {slot + 1}";
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

    private string SlotType(int i) => i >= StatBase ? "Stat" : i < EquipCount ? EquipmentSlotNames[i] : $"Inventory slot {i - EquipCount + 1}";
    private string StatTip(int s)
    {
        var st = StatTypes.All[s];
        int bonus = _character.TitleBonus(st);
        string t = bonus != 0 ? $"{st} {_character.EffectiveStats[st]} ({_character.Stats[st]} base +{bonus} {_character.EquippedTitle!.Name})  -  Stat"
                              : $"{st} {_character.Stats[st]}  -  Stat";
        if (_character.FreeStatPoints > 0) t += "  -  Right/+/Enter add";
        if (NewPoints(s) > 0) t += "  -  Left/- remove";
        return t;
    }

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
        gfx.TextLeft(sb, "Arrows/mouse: select  -  Enter: equip/use  -  U: use  -  1-5: hotbar  -  X: drop (Shift+X: all)  -  T: titles  -  I/Esc: close", new Rectangle(w - 900, 0, 900, 56), Theme.TextDim, 15f);

        var left = new Rectangle(30, 76, 600, 548);
        var right = new Rectangle(650, 76, 600, 548);
        foreach (var p in new[] { left, right })
        {
            gfx.Rect(sb, p, Theme.Panel);
            gfx.Border(sb, p, Theme.PanelBorder, 2);
        }

        DrawEquipmentPanel(sb, gfx, left);
        DrawRightPanel(sb, gfx, input, right);
        DrawDetailsPanel(sb, gfx, new Rectangle(left.X + 10, 514, left.Width - 20, 102));

        for (int i = 0; i < StatBase; i++) DrawSlot(sb, gfx, i);

        // Tooltip line along the bottom.
        var tip = new Rectangle(30, 636, 1220, 54);
        gfx.Rect(sb, tip, Theme.Panel);
        gfx.Border(sb, tip, Theme.PanelBorder, 2);
        string? item = _focus >= StatBase ? null : ItemAt(_focus);
        string text = _focus >= StatBase ? StatTip(_focus - StatBase) : item == null ? $"{SlotType(_focus)}  -  Empty" : $"{item}  -  {SlotType(_focus)}";
        if (_message != null) text += "     |     " + _message;
        gfx.TextLeft(sb, text, tip, item == null && _focus < StatBase ? Theme.TextDim : Theme.Text, 20f, 20);
        DrawDropSlider(sb, gfx, w, h);
    }

    /// <summary>
    /// #22: details of the focused slot's item (bottom of the left panel): name and slot type, the flavour text wrapped,
    /// the bonus lines in gold and the Enter hint. Empty slots and stat rows show a dim hint instead.
    /// </summary>
    private void DrawDetailsPanel(SpriteBatch sb, Gfx gfx, Rectangle r)
    {
        gfx.Rect(sb, r, Color.Black * 0.35f);
        gfx.Border(sb, r, Theme.PanelBorder, 2);

        Item? item = null;
        if (_focus < EquipCount) item = _character.Equipment.Get(EquipmentSlots.All[_focus]);
        else if (_focus < StatBase) item = _character.Inventory.Get(_focus - EquipCount)?.Item;
        if (item == null)
        {
            string hint = _focus >= StatBase ? "Select an equipment or inventory slot to see its item." : $"{SlotType(_focus)}  -  Empty";
            gfx.TextCentered(sb, hint, r, Theme.TextDim, 16f);
            return;
        }

        int x = r.X + 14, y = r.Y + 8;
        string slotType = item.EquipSlot is not { } es ? "Item" : es == EquipSlot.Accessory ? "Accessory" : EquipmentSlots.Label(Enum.Parse<EquipmentSlot>(es.ToString()));
        gfx.Text(sb, item.Name, new Vector2(x, y), Theme.Text, 20f);
        var tm = gfx.Measure(slotType, 15f);
        gfx.Text(sb, slotType, new Vector2(r.Right - 14 - tm.X, y + 4), Theme.TextDim, 15f);

        // Left column: flavour text. Right column: bonus and effect lines in gold. Usable items (#26) show a status line
        // (the Core reason in red, or the cooldown) bottom-left and the key hints bottom-right.
        bool usable = item.IsUsable && _focus >= EquipCount;
        int descWidth = usable ? 300 : 360;
        int ly = y + 32;
        string desc = string.IsNullOrWhiteSpace(item.Description) ? "No description." : item.Description;
        foreach (var line in gfx.Wrap(desc, descWidth, 14f).Take(usable ? 2 : 3))
        {
            gfx.Text(sb, line, new Vector2(x, ly), Theme.TextDim, 14f);
            ly += 19;
        }

        int bx = x + descWidth + 24, by = y + 34;
        foreach (var bonus in item.DetailLines)
        {
            gfx.Text(sb, bonus, new Vector2(bx, by), Theme.Accent, usable ? 16f : 17f);
            by += usable ? 20 : 22;
        }

        if (usable)
        {
            string? why = _character.CanUse(item.Id);
            string status = why == null ? "Ready to use"
                : why == ItemUseResult.OnCooldownText ? $"On cooldown {_character.ItemCooldownRemaining:0.0}s (runs in the forest)"
                : why;
            gfx.Text(sb, status, new Vector2(x, r.Bottom - 26), why == null ? Theme.Positive : Theme.Negative, 14f);
            gfx.Text(sb, "Enter/U: use  -  1-5: hotbar  -  X: drop", new Vector2(bx, r.Bottom - 26), Theme.TextDim, 14f);
            return;
        }
        string enter = _focus < EquipCount ? "Enter to unequip" : item.IsEquippable ? "Enter to equip" : "Cannot be equipped";
        gfx.Text(sb, enter, new Vector2(bx, r.Bottom - 26), Theme.TextDim, 14f);
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

        gfx.Text(sb, "Accessories", new Vector2(_slots[7].X, _slots[7].Y - 22), Theme.TextDim, 16f);
    }

    private void DrawRightPanel(SpriteBatch sb, Gfx gfx, InputState input, Rectangle panel)
    {
        gfx.Text(sb, _character.Name, new Vector2(panel.X + 24, panel.Y + 12), Theme.Text, 28f);
        // #21: money at the top right of the header, as gold / silver / copper coins.
        Hud.DrawMoney(sb, gfx, _character.Copper, panel.Right - 24, panel.Y + 14, 20f, Theme.Text);
        gfx.Text(sb, $"{_character.Race}  -  {_character.Distribution}", new Vector2(panel.X + 24, panel.Y + 50), Theme.Accent, 18f);

        gfx.Text(sb, $"Level {_character.Level}  -  XP {_character.XpIntoLevel}/{_character.XpForNextLevel}", new Vector2(panel.Right - 24 - gfx.Measure($"Level {_character.Level}  -  XP {_character.XpIntoLevel}/{_character.XpForNextLevel}", 18f).X, panel.Y + 50), Theme.Text, 18f);
        gfx.Text(sb, "Stats", new Vector2(panel.X + 24, panel.Y + 90), Theme.TextDim, 16f);
        string titleLine = _character.EquippedTitle == null ? "No title" : $"Title: {_character.EquippedTitle.Name}";
        gfx.Text(sb, titleLine, new Vector2(panel.X + 80, panel.Y + 90), _character.EquippedTitle == null ? Theme.TextDim * 0.7f : Theme.Positive, 16f);
        Button(sb, gfx, TitlesButton, "Titles (T)", true, input, 16f);
        for (int i = 0; i < StatTypes.Count; i++)
        {
            var stat = StatTypes.All[i];
            var row = _slots[StatBase + i];
            bool focus = _focus == StatBase + i;
            if (focus) { gfx.Rect(sb, row, Theme.ButtonHover * 0.8f); gfx.Border(sb, row, Theme.Accent, 2); }
            int added = NewPoints(i);
            gfx.Text(sb, stat.ToString(), new Vector2(row.X + 8, row.Y + 2), Theme.Text, 18f);
            int bonus = _character.TitleBonus(stat);
            var v = _character.EffectiveStats[stat].ToString();   // includes the worn title (#27)
            gfx.Text(sb, v, new Vector2(row.X + 126 - gfx.Measure(v, 18f).X, row.Y + 2), added > 0 ? Theme.Positive : bonus != 0 ? Theme.Positive : Theme.Accent, 18f);
            // #18: the stat's modifier in brackets, dimmer gold so it reads as secondary; recomputed every frame.
            var mod = $"({_character.Modifier(stat).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)})";
            gfx.Text(sb, mod, new Vector2(row.X + 130, row.Y + 5), Theme.Accent * 0.7f, 14f);
            if (bonus != 0)   // "+2" from the worn title, next to the modifier
                gfx.Text(sb, $"+{bonus}", new Vector2(row.X + 130 + gfx.Measure(mod, 14f).X + 6, row.Y + 5), Theme.Positive, 14f);

            // [-] only while there is a point from this menu session to take back; [+] while free points remain.
            if (added > 0) Button(sb, gfx, _minus[i], "-", true, input);
            Button(sb, gfx, _plus[i], "+", _character.FreeStatPoints > 0, input);
        }

        // Free points box (right of the stats).
        var fp = new Rectangle(panel.X + 330, panel.Y + 112, 246, 98);
        bool any = _character.FreeStatPoints > 0;
        gfx.Rect(sb, fp, Color.Black * 0.35f);
        gfx.Border(sb, fp, any ? Theme.Accent : Theme.PanelBorder, 2);
        gfx.TextCentered(sb, "Free points", new Rectangle(fp.X, fp.Y + 6, fp.Width, 24), Theme.TextDim, 16f);
        gfx.TextCentered(sb, _character.FreeStatPoints.ToString(), new Rectangle(fp.X, fp.Y + 28, fp.Width, 40), any ? Theme.Accent : Theme.TextDim, 34f);
        gfx.TextCentered(sb, any ? "Spend with + (Right / Enter)" : "Level up to earn more", new Rectangle(fp.X, fp.Y + 70, fp.Width, 22), Theme.TextDim, 14f);

        // Combat box (below Free points): recomputed every draw so stats and weapons update live.
        var cb = new Rectangle(fp.X, fp.Bottom + 10, fp.Width, 84);
        gfx.Rect(sb, cb, Color.Black * 0.35f);
        gfx.Border(sb, cb, Theme.PanelBorder, 2);
        gfx.TextCentered(sb, "Combat", new Rectangle(cb.X, cb.Y + 4, cb.Width, 22), Theme.TextDim, 16f);
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string phys = $"Physical Attack: {DamageCalculator.PhysicalDamage(_character)} (x{DamageCalculator.StrengthModifier(_character).ToString("0.0", inv)} Str)";
        string mag = $"Magic Attack: {DamageCalculator.MagicDamage(_character)} (x{DamageCalculator.IntelligenceModifier(_character).ToString("0.0", inv)} Int)";
        gfx.TextLeft(sb, phys, new Rectangle(cb.X, cb.Y + 28, cb.Width, 24), Theme.Text, 15f, 12);
        gfx.TextLeft(sb, mag, new Rectangle(cb.X, cb.Y + 54, cb.Width, 24), Theme.Text, 15f, 12);

        gfx.Text(sb, "Inventory", new Vector2(panel.X + 24, _slots[EquipCount].Y - 36), Theme.Accent, 22f);
    }

    private static void Button(SpriteBatch sb, Gfx gfx, Rectangle r, string label, bool enabled, InputState input, float size = 20f)
    {
        bool hover = enabled && r.Contains(input.MousePosition);
        gfx.Rect(sb, r, !enabled ? Theme.ButtonDisabled : hover ? Theme.ButtonHover : Theme.Button);
        gfx.Border(sb, r, enabled ? Theme.Accent : Theme.PanelBorder * 0.6f, 1);
        gfx.TextCentered(sb, label, r, enabled ? Theme.Text : Theme.TextDim * 0.5f, size);
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
        float fs = item == null ? 14f : 15f;
        while (fs > 10f && gfx.Measure(item ?? "Empty", fs).X > r.Width - 8) fs -= 1f;   // long names (Practice Staff) fit the slot
        gfx.TextCentered(sb, item ?? "Empty", body, item == null ? Theme.TextDim * 0.7f : Theme.Text, fs);
    }
}

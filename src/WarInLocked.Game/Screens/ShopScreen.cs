using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Items;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #28: the shop screen, opened from the forest when the Colonel's dialogue ends with the "Shop" action.
/// Buy and Sell tabs (Sell is greyed out for now), the Buy tab lists what the shop sells with its price in coins and
/// the details of the focused item on the right. Enter on an item opens a small Buy / Haggle choice (Haggle is greyed out for now).
/// W/S or Up/Down move, Enter or click choose, Esc backs out (closes the choice first, then the shop) and returns to the SAME
/// <see cref="ForestScreen"/>. Pure drawing and input: stock, prices and every rule (copper, room in the bag) come from Core
/// (<see cref="Shop.CanBuy"/> / <see cref="Shop.Buy"/>).
/// </summary>
public sealed class ShopScreen : Screen
{
    private const int RowHeight = 74;
    private static readonly Color SlotFill = new(26, 29, 40);

    private readonly Screen _back;
    private readonly Character _character;
    private readonly Shop _shop;
    private int _focus;
    private bool _choosing;      // the Buy / Haggle choice is open
    private int _action;         // 0 = Buy, 1 = Haggle
    private string? _message;
    private bool _messageGood;

    public ShopScreen(Screen back, Character character, Shop shop)
    {
        _back = back;
        _character = character;
        _shop = shop;
    }

    private static Rectangle BuyTab => new(30, 70, 150, 42);
    private static Rectangle SellTab => new(190, 70, 150, 42);
    private static Rectangle ListPanel => new(30, 124, 620, 500);
    private static Rectangle DetailPanel => new(670, 124, 580, 500);
    private static Rectangle RowRect(int i) => new(ListPanel.X + 12, ListPanel.Y + 12 + i * (RowHeight + 8), ListPanel.Width - 24, RowHeight);
    private static Rectangle ChoiceBox => new(DetailPanel.X + 40, DetailPanel.Bottom - 200, DetailPanel.Width - 80, 184);
    private static Rectangle BuyButton => new(ChoiceBox.X + 20, ChoiceBox.Y + 44, ChoiceBox.Width - 40, 44);
    private static Rectangle HaggleButton => new(ChoiceBox.X + 20, ChoiceBox.Y + 96, ChoiceBox.Width - 40, 44);

    private ShopEntry? Focused => _shop.Stock.Count == 0 ? null : _shop.Stock[Math.Clamp(_focus, 0, _shop.Stock.Count - 1)];

    public override void Update(GameTime time, InputState input)
    {
        if (input.Pressed(Keys.Escape))
        {
            if (_choosing) _choosing = false;
            else Screens.Replace(_back);
            return;
        }

        int count = _shop.Stock.Count;
        if (_focus >= count) _focus = Math.Max(0, count - 1);
        bool up = input.Pressed(Keys.Up) || input.Pressed(Keys.W);
        bool down = input.Pressed(Keys.Down) || input.Pressed(Keys.S);
        bool enter = input.Pressed(Keys.Enter) || input.Pressed(Keys.E);

        if (input.Clicked && SellTab.Contains(input.MousePosition))
        {
            input.ConsumeClick();
            Say(_shop.SellEnabled ? "" : "Selling is not available yet", false);
        }
        if (input.Pressed(Keys.Right) || input.Pressed(Keys.D) || input.Pressed(Keys.Tab))
            if (!_shop.SellEnabled && !_choosing) Say("Selling is not available yet", false);

        if (_choosing)
        {
            if (input.MouseMoved)
            {
                if (BuyButton.Contains(input.MousePosition)) _action = 0;
                else if (HaggleButton.Contains(input.MousePosition)) _action = 1;
            }
            if (input.Clicked)
            {
                if (BuyButton.Contains(input.MousePosition)) { _action = 0; enter = true; input.ConsumeClick(); }
                else if (HaggleButton.Contains(input.MousePosition)) { _action = 1; enter = true; input.ConsumeClick(); }
            }
            if (up) _action = 0;
            if (down) _action = 1;
            if (enter) RunAction();
            return;
        }

        if (input.MouseMoved)
            for (int i = 0; i < count; i++)
                if (RowRect(i).Contains(input.MousePosition)) _focus = i;
        if (input.Clicked)
            for (int i = 0; i < count; i++)
                if (RowRect(i).Contains(input.MousePosition)) { _focus = i; enter = true; input.ConsumeClick(); break; }

        if (up) _focus = Math.Max(0, _focus - 1);
        if (down) _focus = Math.Min(Math.Max(0, count - 1), _focus + 1);
        if (enter && Focused != null) { _choosing = true; _action = 0; _message = null; }
    }

    private void Say(string text, bool good)
    {
        _message = string.IsNullOrEmpty(text) ? null : text;
        _messageGood = good;
    }

    private void RunAction()
    {
        var entry = Focused;
        if (entry == null) return;
        if (_action == 1)
        {
            Say(_shop.HaggleEnabled ? "" : "Haggling is not available yet", false);
            return;
        }
        var result = _shop.Buy(_character, entry.Item.Id);   // Core checks copper and room, and counts the quest
        if (!result.Success)
        {
            Say(result.Reason ?? Shop.NotForSaleText, false);
            return;
        }
        string text = $"Bought {entry.Item.Name} for {entry.PriceText}";
        if (result.ReadyQuests.Count > 0)
            text += $"  -  {result.ReadyQuests[0].ShortTitle}: return to {result.ReadyQuests[0].TurnInNpcName}";
        Say(text, true);
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Theme.Background);
        gfx.Rect(sb, new Rectangle(0, 0, w, 56), Theme.Panel);
        gfx.TextLeft(sb, $"Shop  -  {_shop.Name}", new Rectangle(0, 0, w, 56), Theme.Accent, 26f, 30);
        gfx.TextLeft(sb, "W/S or mouse: select  -  Enter/click: choose  -  Esc: back", new Rectangle(w - 780, 0, 780, 56), Theme.TextDim, 16f);

        DrawTab(sb, gfx, BuyTab, Shop.BuyTabText, true, true);
        DrawTab(sb, gfx, SellTab, Shop.SellTabText, false, _shop.SellEnabled);

        // Player's copper, top right of the tab row.
        const string label = "You have";
        gfx.Text(sb, label, new Vector2(w - 30 - 330, 78), Theme.TextDim, 18f);
        Hud.DrawMoney(sb, gfx, _character.Copper, w - 30, 74, 24f, Theme.Text);

        foreach (var p in new[] { ListPanel, DetailPanel })
        {
            gfx.Rect(sb, p, Theme.Panel);
            gfx.Border(sb, p, Theme.PanelBorder, 2);
        }

        for (int i = 0; i < _shop.Stock.Count; i++) DrawRow(sb, gfx, i);
        if (_shop.Stock.Count == 0)
            gfx.TextCentered(sb, "Nothing for sale", ListPanel, Theme.TextDim, 20f);

        DrawDetails(sb, gfx, input);

        var bar = new Rectangle(30, 636, 1220, 54);
        gfx.Rect(sb, bar, Theme.Panel);
        gfx.Border(sb, bar, Theme.PanelBorder, 2);
        if (_message != null)
            gfx.TextLeft(sb, _message, bar, _messageGood ? Theme.Positive : Theme.Negative, 20f, 20);
        else
            gfx.TextLeft(sb, Focused == null ? "" : $"{Focused.Item.Name}  -  {Focused.PriceText}", bar, Theme.Text, 20f, 20);
    }

    private void DrawTab(SpriteBatch sb, Gfx gfx, Rectangle r, string text, bool active, bool enabled)
    {
        bool hover = enabled && r.Contains(Mouse.GetState().Position);
        gfx.Rect(sb, r, !enabled ? Theme.ButtonDisabled : active ? Theme.ButtonHover : hover ? Theme.ButtonHover : Theme.Button);
        gfx.Border(sb, r, active ? Theme.Accent : enabled ? Theme.PanelBorder : Theme.PanelBorder * 0.6f, 2);
        gfx.TextCentered(sb, text, r, enabled ? (active ? Theme.Accent : Theme.Text) : Theme.TextDim * 0.5f, 22f);
    }

    private void DrawRow(SpriteBatch sb, Gfx gfx, int i)
    {
        var entry = _shop.Stock[i];
        var r = RowRect(i);
        bool focus = i == _focus;
        gfx.Rect(sb, r, focus ? Theme.ButtonHover : SlotFill);
        gfx.Border(sb, r, focus ? Theme.Accent : Theme.PanelBorder, 2);

        float fs = 22f;
        while (fs > 14f && gfx.Measure(entry.Item.Name, fs).X > 340) fs -= 1f;
        gfx.Text(sb, entry.Item.Name, new Vector2(r.X + 16, r.Y + 10), Theme.Text, fs);
        int owned = _character.Inventory.CountOf(entry.Item.Id);
        gfx.Text(sb, $"Owned: {owned}", new Vector2(r.X + 16, r.Y + 44), Theme.TextDim, 15f);

        bool affordable = _character.CanAfford(entry.PriceCopper);
        Hud.DrawMoney(sb, gfx, entry.PriceCopper, r.Right - 16, r.Y + 22, 24f, affordable ? Theme.Text : Theme.Negative);
    }

    private void DrawDetails(SpriteBatch sb, Gfx gfx, InputState input)
    {
        var p = DetailPanel;
        var entry = Focused;
        if (entry == null)
        {
            gfx.TextCentered(sb, "Select an item to see its details.", p, Theme.TextDim, 18f);
            return;
        }

        var item = entry.Item;
        int x = p.X + 24, y = p.Y + 18;
        float fs = 28f;
        while (fs > 16f && gfx.Measure(item.Name, fs).X > p.Width - 48) fs -= 1f;
        gfx.Text(sb, item.Name, new Vector2(x, y), Theme.Text, fs);
        y += 46;
        string desc = string.IsNullOrWhiteSpace(item.Description) ? "No description." : item.Description;
        foreach (var line in gfx.Wrap(desc, p.Width - 48, 17f))
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.TextDim, 17f);
            y += 24;
        }
        y += 10;
        foreach (var line in item.DetailLines)
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.Accent, 19f);
            y += 28;
        }

        y += 10;
        gfx.Text(sb, "Price", new Vector2(x, y + 2), Theme.TextDim, 17f);
        Hud.DrawMoney(sb, gfx, entry.PriceCopper, x + 150, y, 22f, Theme.Text);
        if (!_choosing)
            gfx.Text(sb, "Enter: buy or haggle", new Vector2(x, p.Bottom - 40), Theme.TextDim, 16f);

        if (_choosing) DrawChoice(sb, gfx, input, entry);
    }

    private void DrawChoice(SpriteBatch sb, Gfx gfx, InputState input, ShopEntry entry)
    {
        var box = ChoiceBox;
        gfx.Rect(sb, box, new Color(14, 16, 24));
        gfx.Border(sb, box, Theme.Accent, 2);
        gfx.TextCentered(sb, entry.Item.Name, new Rectangle(box.X, box.Y + 6, box.Width, 32), Theme.Text, 20f);

        DrawChoiceButton(sb, gfx, BuyButton, $"{Shop.BuyActionText}  ({entry.PriceText})", true, _action == 0);
        DrawChoiceButton(sb, gfx, HaggleButton, Shop.HaggleActionText, _shop.HaggleEnabled, _action == 1);

        // The result of the last Buy shows in the bottom bar; the box keeps just the key hint.
        string status = "Enter: confirm  -  Esc: back";
        var color = Theme.TextDim;
        float fs = 15f;
        gfx.TextCentered(sb, status, new Rectangle(box.X, box.Bottom - 38, box.Width, 30), color, fs);
    }

    private static void DrawChoiceButton(SpriteBatch sb, Gfx gfx, Rectangle r, string text, bool enabled, bool focus)
    {
        gfx.Rect(sb, r, !enabled ? Theme.ButtonDisabled : focus ? Theme.ButtonHover : Theme.Button);
        gfx.Border(sb, r, focus ? Theme.Accent : enabled ? Theme.PanelBorder : Theme.PanelBorder * 0.6f, 2);
        gfx.TextCentered(sb, text, r, enabled ? Theme.Text : Theme.TextDim * 0.5f, 22f);
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Saving;

namespace WarInLocked.Game.Ui;

/// <summary>
/// #42: the save slot list. Load mode lists Auto save then Save 1-3 (empty or damaged rows can't be picked);
/// Save mode lists only the manual slots (saving over a used slot replaces it). Up/Down, Enter picks, Esc cancels.
/// </summary>
public sealed class SlotList
{
    public readonly bool SaveMode;
    private IReadOnlyList<SaveSlotInfo> _rows = Array.Empty<SaveSlotInfo>();
    private int _focus;

    public SlotList(bool saveMode) { SaveMode = saveMode; Refresh(); }

    public void Refresh()
    {
        var all = SaveSlots.List();
        var rows = new List<SaveSlotInfo>();
        foreach (var r in all) if (!SaveMode || !r.IsAuto) rows.Add(r);
        _rows = rows;
        if (!SaveMode) { _focus = 0; while (_focus < _rows.Count && !_rows[_focus].CanLoad) _focus++; if (_focus >= _rows.Count) _focus = 0; }
    }

    private bool Pickable(int i) => SaveMode || _rows[i].CanLoad;

    private Rectangle Panel => new((WarInLockedGame.ScreenWidth - 560) / 2, 150, 560, 110 + _rows.Count * 70);
    private Rectangle Row(int i) => new(Panel.X + 20, Panel.Y + 60 + i * 70, Panel.Width - 40, 60);

    /// <summary>Returns the picked slot, or null. <paramref name="cancelled"/> is set on Esc.</summary>
    public SaveSlot? Update(InputState input, out bool cancelled)
    {
        cancelled = input.Pressed(Keys.Escape);
        if (cancelled || _rows.Count == 0) return null;
        int step = input.Pressed(Keys.Down) || input.Pressed(Keys.S) ? 1 : input.Pressed(Keys.Up) || input.Pressed(Keys.W) ? -1 : 0;
        if (step != 0)
            for (int n = 0; n < _rows.Count; n++) { _focus = (_focus + step + _rows.Count) % _rows.Count; if (Pickable(_focus)) break; }
        for (int i = 0; i < _rows.Count; i++)
            if (input.MouseMoved && Row(i).Contains(input.MousePosition) && Pickable(i)) _focus = i;
        for (int i = 0; i < _rows.Count; i++)
            if (input.Clicked && Row(i).Contains(input.MousePosition) && Pickable(i)) return _rows[i].Slot;
        if ((input.Pressed(Keys.Enter) || input.Pressed(Keys.Space) || input.Pressed(Keys.E)) && Pickable(_focus)) return _rows[_focus].Slot;
        return null;
    }

    public void Draw(SpriteBatch sb, Gfx gfx)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * 0.55f);
        var p = Panel;
        gfx.Rect(sb, p, Theme.Panel);
        gfx.Border(sb, p, Theme.Accent, 2);
        gfx.TextCentered(sb, SaveMode ? "Save to which slot?" : "Load game", new Rectangle(p.X, p.Y + 10, p.Width, 40), Theme.Accent, 26f);
        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i]; var rr = Row(i); bool sel = i == _focus;
            gfx.Rect(sb, rr, sel ? Theme.Accent * 0.25f : Color.Black * 0.3f);
            gfx.Border(sb, rr, sel ? Theme.Accent : Theme.PanelBorder, sel ? 2 : 1);
            bool dim = !Pickable(i);
            gfx.TextLeft(sb, r.Label, new Rectangle(rr.X, rr.Y + 4, 160, 26), dim ? Theme.TextDim : Theme.Text, 20f, 12);
            string detail = r.Error != null ? r.Error
                : r.Save == null ? "Empty"
                : $"{r.Save.Name}  Lv {r.Save.Level}  {r.Save.MapId}  {r.Save.SavedAtUtc.ToLocalTime():d MMM HH:mm}";
            gfx.TextLeft(sb, detail, new Rectangle(rr.X, rr.Y + 30, rr.Width, 24), r.Error != null ? new Color(230, 110, 100) : Theme.TextDim, 16f, 12);
        }
        gfx.TextCentered(sb, "Up/Down to choose  -  Enter to " + (SaveMode ? "save" : "load") + "  -  Esc to cancel",
            new Rectangle(p.X, p.Bottom - 40, p.Width, 30), Theme.TextDim, 16f);
    }
}

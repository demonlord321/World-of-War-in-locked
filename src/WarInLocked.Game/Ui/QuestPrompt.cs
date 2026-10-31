using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Quests;

namespace WarInLocked.Game.Ui;

/// <summary>
/// The quest prompt panel (issue #8): title, description, progress, reward, failure condition and "Accept Yes/No".
/// Left/Right (or A/D) move between Yes and No, Enter confirms, Y/N answer directly, or click a button.
/// Esc does nothing.
/// </summary>
public sealed class QuestPrompt
{
    private static readonly Rectangle Panel = new(290, 130, 700, 400);
    private readonly Button _yes = new(new Rectangle(Panel.X + 190, Panel.Bottom - 80, 130, 50), "Yes");
    private readonly Button _no = new(new Rectangle(Panel.X + 380, Panel.Bottom - 80, 130, 50), "No");
    private bool _yesFocused = true;

    public QuestPrompt(Quest quest) { Quest = quest; }

    public Quest Quest { get; }

    /// <summary>Null while open, true for Yes, false for No.</summary>
    public bool? Update(InputState input)
    {
        if (input.Pressed(Keys.Left) || input.Pressed(Keys.A)) _yesFocused = true;
        if (input.Pressed(Keys.Right) || input.Pressed(Keys.D)) _yesFocused = false;
        if (input.MouseMoved)
        {
            if (_yes.IsHovered(input)) _yesFocused = true;
            if (_no.IsHovered(input)) _yesFocused = false;
        }

        if (_yes.WasClicked(input) || input.Pressed(Keys.Y)) return true;
        if (_no.WasClicked(input) || input.Pressed(Keys.N)) return false;
        if (input.Pressed(Keys.Enter) || input.Pressed(Keys.Space)) return _yesFocused;
        return null;
    }

    public void Draw(SpriteBatch sb, Gfx gfx, InputState input)
    {
        gfx.Rect(sb, new Rectangle(0, 0, WarInLockedGame.ScreenWidth, WarInLockedGame.ScreenHeight), Color.Black * 0.55f);
        gfx.Rect(sb, Panel, Theme.Panel);
        gfx.Border(sb, Panel, Theme.Accent, 3);

        int x = Panel.X + 36, y = Panel.Y + 24;
        gfx.Text(sb, Quest.Title, new Vector2(x, y), Theme.Accent, 30f);
        y += 56;
        foreach (string line in gfx.Wrap(Quest.Description, Panel.Width - 72, 22f))
        {
            gfx.Text(sb, line, new Vector2(x, y), Theme.Text, 22f);
            y += 30;
        }
        y += 14;
        gfx.Text(sb, Quest.ProgressText, new Vector2(x, y), Theme.Text, 22f);
        y += 40;
        gfx.Text(sb, "Reward: " + Quest.Reward.Text, new Vector2(x, y), Theme.Text, 22f);
        y += 32;
        gfx.Text(sb, "Failure condition: " + Quest.FailureCondition, new Vector2(x, y), Theme.Text, 22f);

        gfx.TextCentered(sb, "Accept", new Rectangle(Panel.X, Panel.Bottom - 120, Panel.Width, 30), Theme.TextDim, 20f);
        _yes.Draw(sb, gfx, input, _yesFocused);
        _no.Draw(sb, gfx, input, !_yesFocused);
    }
}

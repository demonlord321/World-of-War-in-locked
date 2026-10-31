using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>Placeholder shown after Create: the character's summary. Replace with the real game later.</summary>
public sealed class CharacterSummaryScreen : Screen
{
    private readonly Character _character;
    private readonly Button _menu = new(new Rectangle(60, 600, 280, 56), "Main Menu");

    public CharacterSummaryScreen(Character character) => _character = character;

    public override void Update(GameTime time, InputState input)
    {
        if (_menu.WasClicked(input) || input.Pressed(Keys.Enter) || input.Pressed(Keys.Escape) || input.Pressed(Keys.Space))
            Screens.Replace(new MainMenuScreen());
    }

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        gfx.Text(sb, "Character Created", new Vector2(60, 50), Theme.Accent, 40f);
        gfx.Text(sb, "(placeholder screen: the game itself comes later)", new Vector2(60, 104), Theme.TextDim, 18f);

        gfx.Text(sb, _character.Name, new Vector2(60, 160), Theme.Text, 34f);
        gfx.Text(sb, $"{_character.Race}  /  {_character.Distribution}", new Vector2(60, 206), Theme.TextDim, 24f);

        int y = 270;
        foreach (var stat in StatTypes.All)
        {
            gfx.Text(sb, stat.ToString(), new Vector2(60, y), Theme.Text, 24f);
            gfx.Text(sb, _character.Stats[stat].ToString(), new Vector2(260, y), Theme.Text, 24f);
            y += 38;
        }
        _menu.Draw(sb, gfx, input, true);
    }
}

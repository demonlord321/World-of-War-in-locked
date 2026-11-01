using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Interaction;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.World;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #5: the Forest of Beginnings. The player walks the map (arrows/WASD), placeholder
/// slimes wander about, and Colonel Powers stands at the N marker. No combat yet.
/// Issue #6/#7: next to an NPC a "Talk (E)" prompt appears; E or Enter starts the dialogue.
/// While talking, the player and the slimes are paused and Esc does nothing.
/// </summary>
public sealed class ForestScreen : Screen
{
    public const int TileSize = 32;
    private const float PlayerSpeed = 160f;     // pixels per second
    private const int PlayerSize = 22;          // smaller than a tile so corners don't snag

    private static readonly Color GrassA = new(58, 110, 52), GrassB = new(62, 116, 56);
    private static readonly Color PathColor = new(150, 120, 80), WaterColor = new(50, 100, 170);
    private static readonly Color TreeTrunk = new(80, 56, 34), TreeTop = new(30, 72, 34);
    private static readonly Color WallColor = new(28, 40, 28), SlimeColor = new(110, 210, 120);

    private readonly Character _character;
    private readonly TileMap _map;
    private readonly List<Wanderer> _slimes = new();
    private readonly Dictionary<Wanderer, Enemy> _enemies = new();   // combat state (Hp, flicker) of each slime
    private readonly Dictionary<Wanderer, Point> _spawnOf = new();   // where each slime came from (it respawns there)
    private readonly RespawnQueue _respawns = new();                 // defeated slimes come back (#19)
    private readonly Random _rng = new();
    private Vector2 _player;     // top-left, in pixels
    private Vector2 _camera;
    private readonly DialogueBox _dialogueBox = new();
    private DialogueRunner? _dialogue;
    private NpcSpot? _talkTarget;
    private string? _dialogueNpc;
    private QuestPrompt? _questPrompt;
    private string? _toast;
    private float _toastLeft;

    // #29 combat feedback: a red screen-edge flash, a player flash and floating "-N" numbers when the player is hit.
    private sealed class FloatText { public string Text = ""; public float X, Y, Age; }
    private const float HitFlashSeconds = 0.4f, FloatSeconds = 1.0f;
    private readonly List<FloatText> _floats = new();
    private const float XpFloatSeconds = 2.0f;
    private int _xpLost;          // XP lost at the last respawn, shown as a floating "-N XP"
    private float _xpLostAge = XpFloatSeconds;
    private float _hitFlash;
    private const float FadeInSeconds = 0.5f;
    private float _fadeIn;   // seconds of black fade-in left after a respawn

    private void OnDamaged(int amount)
    {
        _hitFlash = HitFlashSeconds;
        _floats.Add(new FloatText { Text = "-" + amount, X = _player.X + PlayerSize / 2f, Y = _player.Y - 4 });
    }

    /// <summary>Unsubscribes from the character's events; called when this forest is left for good (back to the main menu).</summary>
    private void Detach()
    {
        _character.Damaged -= OnDamaged;
        _character.Respawned -= OnRespawned;
    }

    /// <summary>#29: the goddess grants another chance. Moves the player to the bind point, calms the slimes and clears the combat feedback.</summary>
    private void OnRespawned(WarInLocked.Core.World.BindPoint bp)
    {
        if (bp.MapId == WarInLocked.Core.World.BindPoint.ForestOfBeginningsMapId)
            _player = new Vector2(bp.TileX * TileSize + (TileSize - PlayerSize) / 2f, bp.TileY * TileSize + (TileSize - PlayerSize) / 2f);
        // (A bind point on another map would need a map switch; there is only this map today.) The camera follows _player every frame.
        CombatController.CalmEnemies(_enemies.Values);
        _floats.Clear();
        _hitFlash = 0;
        _fadeIn = FadeInSeconds;
        _xpLost = _character.LastRespawnXpLost;   // set by Core before the event
        _xpLostAge = _xpLost > 0 ? 0 : XpFloatSeconds;
    }

    /// <summary>NPC tiles by id; "colonel-powers" is the first one on the map.</summary>
    public Dictionary<string, Point> NpcPositions { get; } = new();

    /// <summary>Combat state of a slime (Hp/MaxHp for a health bar, DamageFlash for the hit flicker); null once it is gone.</summary>
    public Enemy? EnemyOf(Wanderer slime) => _enemies.TryGetValue(slime, out var e) ? e : null;

    public ForestScreen(Character character)
    {
        _character = character;
        _character.Damaged += OnDamaged;
        _character.Respawned += OnRespawned;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "forest-of-beginnings.txt");
        _map = TileMap.Parse(File.ReadAllText(path));

        foreach (var s in _map.SlimeSpawns) AddSlime(s.X, s.Y);
        if (_map.Npcs.Count > 0) NpcPositions["colonel-powers"] = new Point(_map.Npcs[0].X, _map.Npcs[0].Y);

        var (sx, sy) = _map.PlayerStart;
        _player = new Vector2(sx * TileSize + (TileSize - PlayerSize) / 2f, sy * TileSize + (TileSize - PlayerSize) / 2f);
    }

    private void AddSlime(int x, int y)
    {
        var slime = new Wanderer(x, y, _rng);
        _slimes.Add(slime);
        _enemies[slime] = Enemy.NewRandomSlime(_rng);   // level 1-3, uniform (#24)
        _spawnOf[slime] = new Point(x, y);
    }

    public override void Update(GameTime time, InputState input)
    {
        if (_toastLeft > 0) _toastLeft -= (float)time.ElapsedGameTime.TotalSeconds;

        if (_questPrompt != null)
        {
            // The quest prompt is modal too: the world waits and Esc is ignored.
            bool? answer = _questPrompt.Update(input);
            if (answer == true)
            {
                ColonelPowersQuest.Accept(_character, out var notice); // gives + equips the Practice Sword once
                _questPrompt = null;
                if (notice != null) Toast(notice);
            }
            else if (answer == false)
            {
                _questPrompt = null; // nothing is given; talk to the Colonel again to be re-offered
            }
            return;
        }

        if (_dialogue != null)
        {
            // Dialogue is modal: no walking, slimes wait, and Esc is ignored.
            _dialogueBox.Update(_dialogue, input);
            if (_dialogue.IsEnded)
            {
                string? action = _dialogue.Action;
                _dialogue = null;
                // The Colonel's "Shop" option ends the dialogue with an open-shop action (#28): the shop screen takes over
                // and gives this same forest back on Esc.
                var shop = ShopLibrary.Get(DialogueActions.ShopIdOf(action) ?? "");
                if (shop != null)
                {
                    Screens.Replace(new ShopScreen(this, _character, shop));
                    return;
                }
                if (_dialogueNpc == "colonel-powers")
                {
                    var offer = ColonelPowersQuest.QuestToOfferAfterDialogue(_character, out var turnedIn); // turn-in gives the reward
                    if (turnedIn != null) Toast(turnedIn, 6f);
                    if (offer != null) _questPrompt = new QuestPrompt(offer);
                }
            }
            return;
        }

        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        var k = Keyboard.GetState();
        var dir = Vector2.Zero;
        if (k.IsKeyDown(Keys.Left) || k.IsKeyDown(Keys.A)) dir.X -= 1;
        if (k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D)) dir.X += 1;
        if (k.IsKeyDown(Keys.Up) || k.IsKeyDown(Keys.W)) dir.Y -= 1;
        if (k.IsKeyDown(Keys.Down) || k.IsKeyDown(Keys.S)) dir.Y += 1;
        if (_character.IsDead) dir = Vector2.Zero;   // a fallen player cannot move; the respawn countdown runs in Combat.Update
        if (dir != Vector2.Zero) dir.Normalize();

        // Move one axis at a time so the player slides along walls.
        TryMove(new Vector2(dir.X * PlayerSpeed * dt, 0));
        TryMove(new Vector2(0, dir.Y * PlayerSpeed * dt));

        var playerTile = new Point((int)((_player.X + PlayerSize / 2f) / TileSize), (int)((_player.Y + PlayerSize / 2f) / TileSize));
        foreach (var slime in _slimes)
            slime.Update(dt, _map, (x, y) =>
                (x == playerTile.X && y == playerTile.Y) || IsNpc(x, y) ||
                _slimes.Exists(o => o != slime && o.TileX == x && o.TileY == y));

        foreach (var slime in _slimes)
        {
            var enemy = _enemies[slime];
            enemy.TileX = slime.TileX;
            enemy.TileY = slime.TileY;
            enemy.Update(dt);
        }
        _character.Combat.Update(dt);   // cooldowns, energy regen, Slash onto the hotbar
        // Provoked slimes hit the player when next to them (they do not chase); Damaged fires from inside this call.
        _character.Combat.UpdateEnemies(_enemies.Values.ToList(), dt, playerTile.X, playerTile.Y);

        if (_hitFlash > 0) _hitFlash = Math.Max(0, _hitFlash - dt);
        if (_fadeIn > 0) _fadeIn = Math.Max(0, _fadeIn - dt);
        if (_xpLostAge < XpFloatSeconds) _xpLostAge += dt;
        foreach (var ft in _floats) ft.Age += dt;
        _floats.RemoveAll(ft => ft.Age >= FloatSeconds);

        // Defeated slimes come back at their spawn tile once it is free.
        foreach (var due in _respawns.Update(dt))
        {
            bool busy = (due.TileX == playerTile.X && due.TileY == playerTile.Y) || IsNpc(due.TileX, due.TileY) ||
                        _slimes.Exists(o => o.TileX == due.TileX && o.TileY == due.TileY);
            if (busy) _respawns.Schedule(due.TileX, due.TileY, 1.0);
            else AddSlime(due.TileX, due.TileY);
        }

        // Clamped follow camera.
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        _camera.X = MathHelper.Clamp(_player.X + PlayerSize / 2f - w / 2f, 0, Math.Max(0, _map.Width * TileSize - w));
        _camera.Y = MathHelper.Clamp(_player.Y + PlayerSize / 2f - h / 2f, 0, Math.Max(0, _map.Height * TileSize - h));

        _talkTarget = _character.IsDead ? null : TalkTarget.Find(playerTile.X, playerTile.Y, NpcSpots());
        if (_talkTarget != null && (input.Pressed(Keys.E) || input.Pressed(Keys.Enter)))
        {
            _dialogueNpc = _talkTarget.Id;
            _dialogue = _talkTarget.Id == "colonel-powers"
                ? ColonelPowersQuest.CreateDialogue(_character)   // intro first, then the quest offer / progress lines
                : new DialogueRunner(DialogueLibrary.Get(_talkTarget.DialogueId), _character);
            return;
        }

        if (!_character.IsDead && input.Pressed(Keys.Space)) Attack(playerTile, -1);
        for (int slot = 0; slot < Hotbar.SlotCount && !_character.IsDead; slot++)
            if (input.Pressed(Keys.D1 + slot) || input.Pressed(Keys.NumPad1 + slot)) Attack(playerTile, slot);

        // I opens the character menu; it gets this same screen back on close, so the position is kept.
        if (!_character.IsDead && input.Pressed(Keys.I)) { Screens.Replace(new CharacterMenuScreen(this, _character)); return; }

        // K opens the abilities screen (#20); it also gets this same screen back on close.
        if (!_character.IsDead && input.Pressed(Keys.K)) { Screens.Replace(new AbilitiesScreen(this, _character)); return; }

        if (input.Pressed(Keys.Escape)) { Detach(); Screens.Replace(new MainMenuScreen()); }
    }

    private void Toast(string text, float seconds = 3f)
    {
        _toast = text;
        _toastLeft = seconds;
    }

    /// <summary>Space (slot -1) is the basic attack, 1-5 use the hotbar. The rules live in Core (<see cref="CombatController"/>).</summary>
    private void Attack(Point playerTile, int slot)
    {
        var living = _slimes.Where(sl => !_enemies[sl].IsDead).ToList();
        var targets = living.Select(sl => _enemies[sl]).ToList();
        var combat = _character.Combat;
        var result = slot < 0
            ? combat.BasicAttack(playerTile.X, playerTile.Y, targets)
            : combat.UseHotbar(slot, playerTile.X, playerTile.Y, targets);

        switch (result.Outcome)
        {
            case CombatOutcome.NotEnoughEnergy:
                Toast("Not enough energy", 2f);
                break;
            case CombatOutcome.NeedsWeapon:
                Toast(AbilityDefinition.NeedsWeaponText, 2f);
                break;
            case CombatOutcome.ItemUsed:
                Toast($"{result.ItemResult!.Item.Name}: healed {result.ItemResult.Healed} HP", 2f);
                break;
            case CombatOutcome.ItemRefused:
                Toast(result.ItemResult!.Reason, 2f);   // "On cooldown", "Already at full health", "None left"
                break;
            case CombatOutcome.Killed:
                var dead = living[targets.IndexOf(result.Target!)];
                _slimes.Remove(dead);
                _enemies.Remove(dead);
                if (_spawnOf.Remove(dead, out var spawn)) _respawns.Schedule(spawn.X, spawn.Y);
                break;
        }

        if (result.Succeeded)
        {
            if (result.ReadyQuests.Count > 0)
                Toast($"{result.ReadyQuests[0].ShortTitle}: return to {result.ReadyQuests[0].TurnInNpcName}", 5f);
            if (result.AbilityLeveledUp && result.AbilityUsed != null)
                Toast($"{result.AbilityUsed.Name} reached level {result.AbilityUsed.Level}!", 4f);
        }
    }

    private IEnumerable<NpcSpot> NpcSpots()
    {
        foreach (var (id, p) in NpcPositions)
            yield return new NpcSpot(id, id, p.X, p.Y); // the NPC id doubles as its dialogue id
    }

    private bool IsNpc(int x, int y)
    {
        foreach (var p in NpcPositions.Values) if (p.X == x && p.Y == y) return true;
        return false;
    }

    private void TryMove(Vector2 delta)
    {
        if (delta == Vector2.Zero) return;
        var next = _player + delta;
        int left = (int)(next.X / TileSize), right = (int)((next.X + PlayerSize - 1) / TileSize);
        int top = (int)(next.Y / TileSize), bottom = (int)((next.Y + PlayerSize - 1) / TileSize);
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
                if (!_map.IsWalkable(x, y) || IsNpc(x, y)) return;
        _player = next;
    }

    private Rectangle Screen(float x, float y, int w, int h) => new((int)(x - _camera.X), (int)(y - _camera.Y), w, h);

    public override void Draw(SpriteBatch sb, Gfx gfx, InputState input, GameTime time)
    {
        int w = WarInLockedGame.ScreenWidth, h = WarInLockedGame.ScreenHeight;
        int x0 = Math.Max(0, (int)(_camera.X / TileSize)), y0 = Math.Max(0, (int)(_camera.Y / TileSize));
        int x1 = Math.Min(_map.Width - 1, (int)((_camera.X + w) / TileSize)), y1 = Math.Min(_map.Height - 1, (int)((_camera.Y + h) / TileSize));

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var r = Screen(x * TileSize, y * TileSize, TileSize, TileSize);
                var ground = ((x + y) & 1) == 0 ? GrassA : GrassB;
                switch (_map[x, y])
                {
                    case Tile.Path: gfx.Rect(sb, r, PathColor); break;
                    case Tile.Water: gfx.Rect(sb, r, WaterColor); break;
                    case Tile.Wall: gfx.Rect(sb, r, WallColor); break;
                    case Tile.Tree:
                        gfx.Rect(sb, r, ground);
                        gfx.Rect(sb, new Rectangle(r.X + 13, r.Y + 20, 6, 12), TreeTrunk);
                        gfx.Rect(sb, new Rectangle(r.X + 3, r.Y + 2, 26, 20), TreeTop);
                        break;
                    default: gfx.Rect(sb, r, ground); break;
                }
            }

        foreach (var s in _slimes)
        {
            var r = Screen(s.X * TileSize + 6, s.Y * TileSize + 12, 20, 16);
            var enemy = EnemyOf(s);
            if (enemy != null && enemy.AttackFlashFraction > 0) r.Inflate((int)(enemy.AttackFlashFraction * 4), (int)(enemy.AttackFlashFraction * 4));   // lunge
            gfx.Rect(sb, r, Hud.SlimeTint(enemy, SlimeColor, time));
            gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 4, 3, 3), Color.Black);
            gfx.Rect(sb, new Rectangle(r.X + 13, r.Y + 4, 3, 3), Color.Black);
            Hud.DrawSlimeBar(sb, gfx, enemy, r);
        }

        foreach (var (id, p) in NpcPositions)
        {
            var r = Screen(p.X * TileSize + 5, p.Y * TileSize + 2, 22, 28);
            gfx.Rect(sb, r, new Color(90, 100, 60));
            gfx.Border(sb, r, Color.Black, 2);
            gfx.TextCentered(sb, "Colonel Powers", Screen(p.X * TileSize - 60, p.Y * TileSize - 22, 152, 20), Theme.Text, 14f);
        }

        var pr = Screen(_player.X, _player.Y, PlayerSize, PlayerSize);
        bool hitBlink = _hitFlash > 0 && (int)(_hitFlash * 20) % 2 == 0;
        gfx.Rect(sb, pr, hitBlink ? new Color(235, 60, 60) : Theme.Accent);
        gfx.Border(sb, pr, Color.Black, 2);

        // Floating damage numbers rise from the player and fade.
        foreach (var ft in _floats)
        {
            float t = ft.Age / FloatSeconds;
            var box = Screen(ft.X - 40, ft.Y - 22 - t * 28, 80, 24);
            gfx.TextCentered(sb, ft.Text, new Rectangle(box.X + 1, box.Y + 1, box.Width, box.Height), Color.Black * (1 - t), 22f);
            gfx.TextCentered(sb, ft.Text, box, new Color(255, 80, 80) * (1 - t), 22f);
        }

        gfx.Rect(sb, new Rectangle(0, 0, w, 36), Color.Black * 0.55f);
        gfx.TextLeft(sb, $"The Forest of Beginnings  -  {_character.Name}", new Rectangle(0, 0, w, 36), Theme.Text, 18f);
        gfx.TextLeft(sb, "WASD move  -  E talk  -  Space attack  -  I character  -  K abilities  -  Esc menu", new Rectangle(w - 760, 0, 760, 36), Theme.TextDim, 16f);

        // Red edge flash while the player was just hit: four bars along the screen edges, fading out.
        if (_hitFlash > 0)
        {
            float a = 0.55f * _hitFlash / HitFlashSeconds;
            var red = new Color(220, 30, 30) * a;
            const int e = 36;
            gfx.Rect(sb, new Rectangle(0, 0, w, e), red);
            gfx.Rect(sb, new Rectangle(0, h - e, w, e), red);
            gfx.Rect(sb, new Rectangle(0, e, e, h - 2 * e), red);
            gfx.Rect(sb, new Rectangle(w - e, e, e, h - 2 * e), red);
        }

        Hud.Draw(sb, gfx, _character);   // player panel top-right, hotbar bottom-centre (#11, #12)

        // #29: while fallen, a dimmed overlay with Dyllan's text and a countdown to the automatic respawn at the bind point.
        if (_character.IsDead)
        {
            gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * 0.6f);
            gfx.TextCentered(sb, Character.FallenTitle, new Rectangle(0, h / 2 - 90, w, 70), new Color(220, 60, 60), 56f);
            gfx.TextCentered(sb, Character.FallenSubtitle, new Rectangle(0, h / 2 - 12, w, 36), Theme.Text, 24f);
            var bar = new Rectangle((w - 420) / 2, h / 2 + 46, 420, 16);
            gfx.Rect(sb, bar, new Color(30, 30, 40));
            gfx.Rect(sb, new Rectangle(bar.X, bar.Y, (int)(bar.Width * Math.Clamp(_character.RespawnProgress, 0, 1)), bar.Height), Theme.Accent);
            gfx.Border(sb, bar, Theme.PanelBorder, 2);
            if (_character.RespawnXpPenalty > 0)
                gfx.TextCentered(sb, $"You will lose up to {_character.RespawnXpPenalty} XP", new Rectangle(0, bar.Bottom + 38, w, 30), new Color(230, 150, 90), 20f);
            gfx.TextCentered(sb, $"Respawning in {(int)Math.Ceiling(_character.RespawnRemaining)}s", new Rectangle(0, bar.Bottom + 8, w, 30), Theme.TextDim, 20f);
        }

        // Quest tracker while a quest is active, e.g. "Slice and slime 0/5".
        var activeQuests = _character.Quests.Active;
        if (activeQuests.Count > 0)
        {
            var q = activeQuests[0];
            string tracker = q.IsReadyToTurnIn
                ? $"{q.ShortTitle}: return to {q.TurnInNpcName}"
                : q.Id == QuestLibrary.SliceAndSlimeId
                    ? $"{QuestLibrary.SliceAndSlimeShortTitle} {q.Objectives[0].Current}/{q.Objectives[0].Required}"
                    : q.ProgressText;
            var box = new Rectangle(10, 46, Math.Max(250, (int)gfx.Measure(tracker, 17f).X + 24), 30);
            gfx.Rect(sb, box, Color.Black * 0.6f);
            gfx.Border(sb, box, Theme.Accent, 1);
            gfx.TextLeft(sb, tracker, box, Theme.Text, 17f, 10);
        }

        if (_toastLeft > 0 && _toast != null)
        {
            var size = gfx.Measure(_toast, 20f);
            var box = new Rectangle((w - (int)size.X - 40) / 2, 90, (int)size.X + 40, 40);
            gfx.Rect(sb, box, Color.Black * 0.8f);
            gfx.Border(sb, box, Theme.Accent, 2);
            gfx.TextCentered(sb, _toast, box, Theme.Text, 20f);
        }

        if (_questPrompt != null)
        {
            _questPrompt.Draw(sb, gfx, input);
        }
        else if (_dialogue != null)
        {
            _dialogueBox.Draw(sb, gfx, _dialogue);
        }
        else if (_talkTarget != null && NpcPositions.TryGetValue(_talkTarget.Id, out var npcTile))
        {
            var prompt = Screen(npcTile.X * TileSize - 40, npcTile.Y * TileSize + TileSize + 6, 112, 30);
            gfx.Rect(sb, prompt, Color.Black * 0.75f);
            gfx.Border(sb, prompt, Theme.Accent, 2);
            gfx.TextCentered(sb, "Talk (E)", prompt, Theme.Accent, 18f);
        }

        // Fade in from black just after a respawn.
        if (_fadeIn > 0) gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * (_fadeIn / FadeInSeconds));

        // "-N XP" floats up from the player and fades over ~2 s after a respawn that cost XP.
        if (_xpLost > 0 && _xpLostAge < XpFloatSeconds)
        {
            float t = _xpLostAge / XpFloatSeconds;
            var xb = Screen(_player.X + PlayerSize + 4, _player.Y - 6 - t * 40, 100, 28);
            float a = 1 - t * t;
            string txt = $"-{_xpLost} XP";
            gfx.TextCentered(sb, txt, new Rectangle(xb.X + 1, xb.Y + 1, xb.Width, xb.Height), Color.Black * a, 24f);
            gfx.TextCentered(sb, txt, xb, new Color(200, 140, 255) * a, 24f);
        }
    }
}

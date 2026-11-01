using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarInLocked.Core.Characters;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Dialogue;
using WarInLocked.Core.Interaction;
using WarInLocked.Core.Items;
using WarInLocked.Core.Quests;
using WarInLocked.Core.Saving;
using WarInLocked.Core.World;
using WarInLocked.Game.Ui;

namespace WarInLocked.Game.Screens;

/// <summary>
/// Issue #53: this screen now shows whichever map the player is on (<see cref="MapId"/>): the Forest of Beginnings or the
/// Adventurers' Encampment. Edge doors (the &lt; &gt; ^ v map markers) switch maps; <see cref="MapDefs"/> says where they lead.
/// The class keeps its old name so the menus and shop that hand this screen back are unchanged.
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
    private TileMap _map = null!;
    private string _mapId = "";
    private string _mapTitle = "";
    private readonly List<Wanderer> _slimes = new();
    private readonly Dictionary<Wanderer, Enemy> _enemies = new();   // combat state (Hp, flicker) of each slime
    private readonly Dictionary<Wanderer, Point> _spawnOf = new();   // where each slime came from (it respawns there)
    private RespawnQueue _respawns = new();                 // defeated slimes come back (#19)
    private readonly Random _rng = new();
    private Vector2 _player;     // top-left, in pixels
    private Vector2 _camera;
    private readonly DialogueBox _dialogueBox = new();
    private DialogueRunner? _dialogue;
    private static bool _showControls = true;   // H toggles the bottom-left controls hint; stays as chosen for the session
    private NpcSpot? _talkTarget;
    private Point? _savePointTile;   // the save point the player can use right now (#54)
    private string? _dialogueNpc;
    private float _markerTime;
    private QuestPrompt? _questPrompt;
    private string? _toast;
    private float _toastLeft;

    // #29 combat feedback: a red screen-edge flash, a player flash and floating "-N" numbers when the player is hit.
    private sealed class FloatText { public string Text = ""; public float X, Y, Age; public Color Color = new(255, 80, 80); public bool JellyIcon; public float Life = FloatSeconds; }
    private const float HitFlashSeconds = 0.4f, FloatSeconds = 1.0f;
    private readonly List<FloatText> _floats = new();
    private const float XpFloatSeconds = 2.0f;
    private int _xpLost;          // XP lost at the last respawn, shown as a floating "-N XP"
    private float _xpLostAge = XpFloatSeconds;
    private float _hitFlash;
    private const float FadeInSeconds = 0.5f, FadeOutSeconds = 0.25f;
    private MapMarker? _leavingThrough;   // door being walked through: the screen fades to black, then the map switches
    private float _fadeOut;
    private float _fadeIn;   // seconds of black fade-in left after a respawn

    // #57: the blue night overlay eases in and out after Core's clock flips IsNight (no hours are hard-coded here).
    private const float NightFadeSeconds = 2.0f, NightMaxAlpha = 0.5f;
    private static readonly Color NightTint = new(8, 18, 70);
    private float _nightFade;   // 0 day .. 1 night, drawn

    // #55: resting or sleeping fades to black and back (about 1 s); the barracks action is applied at the darkest moment.
    private const float SleepFadeSeconds = 1.0f;
    private float _sleepT = -1;           // -1 = not sleeping, else seconds into the fade
    private string? _sleepAction;
    private bool _sleepApplied;

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
        // Died on another map than the bind point's: switch to the bind map first (it also gives fresh, calm slimes).
        if (bp.MapId != _mapId && MapDefs.Exists(bp.MapId)) LoadMap(bp.MapId);
        if (bp.MapId == _mapId) PlaceAtTile(bp.TileX, bp.TileY);
        // The camera follows _player every frame.
        _leavingThrough = null;
        _fadeOut = 0;
        CombatController.CalmEnemies(_enemies.Values);
        _floats.Clear();
        _hitFlash = 0;
        _fadeIn = FadeInSeconds;
        _xpLost = _character.LastRespawnXpLost;   // set by Core before the event
        _xpLostAge = _xpLost > 0 ? 0 : XpFloatSeconds;
        if (_character.DeathDebuffText is { } debuff) Toast(debuff, 4f);   // #35
    }

    /// <summary>NPC tiles by id; "colonel-powers" is the first one on the map.</summary>
    public Dictionary<string, Point> NpcPositions { get; } = new();
    /// <summary>Id of the map being shown, e.g. "forest-of-beginnings" or "adventurers-encampment".</summary>
    public string MapId => _mapId;

    private void PlaceAtTile(int tx, int ty)
    {
        (tx, ty) = NearestFreeTile(tx, ty);
        _player = new Vector2(tx * TileSize + (TileSize - PlayerSize) / 2f, ty * TileSize + (TileSize - PlayerSize) / 2f);
    }

    private bool IsFreeTile(int x, int y) =>
        _map.IsWalkable(x, y) && !IsNpc(x, y) && _map.DoorAt(x, y) == null && !TileOccupancy.IsBlocked(x, y, _enemies.Values);

    /// <summary>The tile itself if free, else the closest free tile around it (respawn/load/arrival onto a slime).</summary>
    private (int, int) NearestFreeTile(int tx, int ty)
    {
        if (IsFreeTile(tx, ty) || !TileOccupancy.IsBlocked(tx, ty, _enemies.Values)) return (tx, ty);
        for (int r = 1; r < 6; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && IsFreeTile(tx + dx, ty + dy)) return (tx + dx, ty + dy);
        return (tx, ty);
    }

    private static bool OverlapsTile(Vector2 pos, int x, int y) =>
        (int)(pos.X / TileSize) <= x && x <= (int)((pos.X + PlayerSize - 1) / TileSize) &&
        (int)(pos.Y / TileSize) <= y && y <= (int)((pos.Y + PlayerSize - 1) / TileSize);

    /// <summary>Loads a map by id: its tiles, NPCs and slimes (none in the camp). Slimes start fresh each time a map is entered.</summary>
    private void LoadMap(string mapId)
    {
        var def = MapDefs.Get(mapId);
        _map = TileMap.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", def.File)));
        _mapId = mapId;
        _mapTitle = def.Title;
        _slimes.Clear(); _enemies.Clear(); _spawnOf.Clear();
        _respawns = new RespawnQueue();
        NpcPositions.Clear();
        for (int i = 0; i < _map.Npcs.Count && i < def.NpcIds.Length; i++)
            NpcPositions[def.NpcIds[i]] = new Point(_map.Npcs[i].X, _map.Npcs[i].Y);
        foreach (var s in _map.SlimeSpawns) AddSlime(s.X, s.Y);
        var (sx, sy) = _map.PlayerStart;
        PlaceAtTile(sx, sy);
    }

    /// <summary>Walks through a door: loads the map it leads to and stands the player just inside the matching opposite door.</summary>
    private void SwitchThroughDoor(MapMarker door)
    {
        var dest = MapDefs.Destination(_mapId, door.Symbol);
        if (dest == null) return;
        LoadMap(dest);
        var (tx, ty) = MapDefs.ArrivalTile(_map, MapDefs.Opposite(door.Symbol)) ?? _map.PlayerStart;
        PlaceAtTile(tx, ty);
        _fadeIn = FadeInSeconds;
        Toast(MapTitleToast(dest), 2.5f);
    }

    /// <summary>Combat state of a slime (Hp/MaxHp for a health bar, DamageFlash for the hit flicker); null once it is gone.</summary>
    public Enemy? EnemyOf(Wanderer slime) => _enemies.TryGetValue(slime, out var e) ? e : null;

    /// <param name="startMapId">Map to open on (a loaded save, #54); null = the Forest of Beginnings at its start tile.</param>
    public ForestScreen(Character character, string? startMapId = null, int startTileX = 0, int startTileY = 0)
    {
        _character = character;
        _character.Damaged += OnDamaged;
        _character.Respawned += OnRespawned;
        bool loaded = startMapId != null && MapDefs.Exists(startMapId);
        LoadMap(loaded ? startMapId! : MapIds.Forest);
        if (loaded && _map.IsWalkable(startTileX, startTileY)) PlaceAtTile(startTileX, startTileY);
        _nightFade = _character.Clock.IsNight ? 1f : 0f;
        if (loaded) { _fadeIn = FadeInSeconds; Toast(MapTitleToast(_mapId), 2.5f); }
    }

    /// <summary>The map name for the toast on arrival, with "Safe zone" for non-combat maps (#57, <see cref="MapIds.IsSafeZone"/>).</summary>
    private static string MapTitleToast(string mapId) =>
        MapDefs.Get(mapId).Title + (MapIds.IsSafeZone(mapId) ? " - Safe zone" : "");

    /// <summary>A slime of this map's level range: level 1-3 in the forest (#24), 4-5 on the Path (levels come from <see cref="MapDefs"/>), +10 at night (#57).</summary>
    private Enemy NewSlimeForMap()
    {
        var (min, max) = MapDefs.Get(_mapId).SlimeLevels;
        return Enemy.NewRandomSlime(_rng, min, max, _character.Clock);   // night spawns and respawns get +10 levels (#57)
    }

    private void AddSlime(int x, int y)
    {
        var slime = new Wanderer(x, y, _rng);
        _slimes.Add(slime);
        _enemies[slime] = NewSlimeForMap();
        _spawnOf[slime] = new Point(x, y);
    }

    public override void Update(GameTime time, InputState input)
    {
        if (_toastLeft > 0) _toastLeft -= (float)time.ElapsedGameTime.TotalSeconds;
        {
            float step = (float)time.ElapsedGameTime.TotalSeconds / NightFadeSeconds;
            float target = _character.Clock.IsNight ? 1f : 0f;
            _nightFade = target > _nightFade ? Math.Min(target, _nightFade + step) : Math.Max(target, _nightFade - step);
        }

        if (_sleepT >= 0)
        {
            // Rest / sleep: the world waits while the screen goes black, the action lands at the darkest moment, then it fades back.
            _sleepT += (float)time.ElapsedGameTime.TotalSeconds;
            if (!_sleepApplied && _sleepT >= SleepFadeSeconds / 2)
            {
                _sleepApplied = true;
                ApplyBarracks(_sleepAction);
            }
            if (_sleepT >= SleepFadeSeconds) { _sleepT = -1; _sleepAction = null; }
            return;
        }

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
                if (_dialogueNpc == BarracksMaster.NpcId)
                {
                    // Rest and sleep fade out and back (the notice shows after); "Set as home" and "Leave" act at once.
                    if (action == BarracksActions.Rest || action == BarracksActions.Sleep)
                    {
                        _sleepAction = action; _sleepT = 0; _sleepApplied = false;
                        return;
                    }
                    ApplyBarracks(action);
                }
                if (_dialogueNpc == "whiplash" && WhiplashQuest.AfterDialogue(_character, out var whiplashNotice))
                    Toast(whiplashNotice, 6f);   // "Quest complete: Adventurers start! ..."
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
        if (_character.IsDead || _leavingThrough != null) dir = Vector2.Zero;   // a fallen player cannot move; the respawn countdown runs in Combat.Update
        if (dir != Vector2.Zero) dir.Normalize();

        // Move one axis at a time so the player slides along walls.
        TryMove(new Vector2(dir.X * PlayerSpeed * dt, 0));
        TryMove(new Vector2(0, dir.Y * PlayerSpeed * dt));

        var playerTile = new Point((int)((_player.X + PlayerSize / 2f) / TileSize), (int)((_player.Y + PlayerSize / 2f) / TileSize));

        // Stepping onto a door tile starts a short fade to black, then the map switches.
        if (_leavingThrough == null && !_character.IsDead && _map.DoorAt(playerTile.X, playerTile.Y) is { } door && MapDefs.Destination(_mapId, door.Symbol) != null)
        {
            _leavingThrough = door;
            _fadeOut = FadeOutSeconds;
        }
        if (_leavingThrough is { } leaving)
        {
            _fadeOut = Math.Max(0, _fadeOut - dt);
            if (_fadeOut <= 0)
            {
                _leavingThrough = null;
                SwitchThroughDoor(leaving);
                return;
            }
        }

        foreach (var slime in _slimes)
            if (_enemies[slime].IsAggro) slime.Glide(dt);   // angry slimes are stepped by Core's chase (#31)
            else slime.Update(dt, _map, (x, y) =>
                OverlapsTile(_player, x, y) || IsNpc(x, y) || _map.DoorAt(x, y) != null ||
                _slimes.Exists(o => o != slime && o.TileX == x && o.TileY == y));

        foreach (var slime in _slimes)
        {
            var enemy = _enemies[slime];
            enemy.TileX = slime.TileX;
            enemy.TileY = slime.TileY;
            enemy.Update(dt);
        }
        _character.Combat.Update(dt);   // cooldowns, energy regen, Slash onto the hotbar
        // Provoked slimes chase one tile at a time and hit when next to the player (#31); Damaged fires from inside this call.
        _character.Combat.UpdateEnemies(_enemies.Values.ToList(), dt, playerTile.X, playerTile.Y,
            (x, y) => _map.IsWalkable(x, y) && !IsNpc(x, y) && _map.DoorAt(x, y) == null && !OverlapsTile(_player, x, y));
        foreach (var slime in _slimes)
        {
            var enemy = _enemies[slime];
            if (enemy.IsAggro && (enemy.TileX != slime.TileX || enemy.TileY != slime.TileY)) slime.GlideTo(enemy.TileX, enemy.TileY);
        }

        _markerTime += dt;
        if (_hitFlash > 0) _hitFlash = Math.Max(0, _hitFlash - dt);
        if (_fadeIn > 0) _fadeIn = Math.Max(0, _fadeIn - dt);
        if (_xpLostAge < XpFloatSeconds) _xpLostAge += dt;
        foreach (var ft in _floats) ft.Age += dt;
        _floats.RemoveAll(ft => ft.Age >= ft.Life);

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
        // #54: on or next to a save point the "Save (E)" prompt shows. Where an NPC is in reach too, talking wins unless the player stands on the save tile.
        _savePointTile = null;
        if (!_character.IsDead && SavePoints.FindNear(_mapId, _map, playerTile.X, playerTile.Y) != null)
        {
            var sp = _map.SavePoints[0];
            bool onTile = playerTile.X == sp.X && playerTile.Y == sp.Y;
            if (_talkTarget == null || onTile) { _savePointTile = new Point(sp.X, sp.Y); _talkTarget = null; }
        }
        if (_savePointTile != null && (input.Pressed(Keys.E) || input.Pressed(Keys.Enter)))
        {
            SavePoints.Interact(_character, _mapId, playerTile.X, playerTile.Y, out var saveNotice);
            Toast(saveNotice, 3f);
            return;
        }
        if (_talkTarget != null && (input.Pressed(Keys.E) || input.Pressed(Keys.Enter)))
        {
            _dialogueNpc = _talkTarget.Id;
            _dialogue = _talkTarget.Id == "colonel-powers"
                ? ColonelPowersQuest.CreateDialogue(_character)   // intro first, then the quest offer / progress lines
                : _talkTarget.Id == "whiplash"
                    ? WhiplashQuest.CreateDialogue(_character)
                    : _talkTarget.Id == BarracksMaster.NpcId
                        ? BarracksMaster.CreateDialogue(_character)   // Rest / Set as home (Sleep until morning only at night)
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

        if (input.Pressed(Keys.H)) _showControls = !_showControls;   // hide or show the controls hint

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
                float kx = dead.X * TileSize + TileSize / 2f, ky = dead.Y * TileSize;
                if (result.XpBlocked)
                    _floats.Add(new FloatText { Text = "No XP while Death debuff 3", X = kx, Y = ky - 6, Color = new Color(200, 150, 255), Life = 2f });
                else if (result.XpGained > 0)
                    _floats.Add(new FloatText { Text = $"+{result.XpGained} XP", X = kx, Y = ky - 6, Color = new Color(255, 214, 40), Life = 1.8f });
                if (result.DroppedItem != null && !result.DropLost)
                    _floats.Add(new FloatText { Text = $"+1 {result.DroppedItem.Name}", X = kx, Y = ky + 18, Color = new Color(120, 230, 120), JellyIcon = true, Life = 2.2f });
                if (result.DropLost) Toast(CombatController.DropLostText, 3f);
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

    /// <summary>#55: does the picked Barracks Master option (Core decides what it does) and toasts the notice it gives.</summary>
    private void ApplyBarracks(string? action)
    {
        if (!NpcPositions.TryGetValue(BarracksMaster.NpcId, out var master)) return;
        var home = BarracksMaster.HomePointFor(_map, master.X, master.Y);
        BarracksMaster.Apply(_character, action ?? "", home, out var notice);
        if (notice != null) Toast(notice, 4f);
    }

    private IEnumerable<NpcSpot> NpcSpots()
    {
        foreach (var (id, p) in NpcPositions)
            if (MapDefs.HasDialogue(id))
                yield return new NpcSpot(id, id, p.X, p.Y); // the NPC id doubles as its dialogue id
    }

    private static readonly Color MarkerYellow = new(255, 214, 40), MarkerGrey = new(170, 170, 170);

    /// <summary>Quest marker over a quest giver: yellow ! available, grey ? in progress, yellow ? ready to hand in (Core QuestMarkers).</summary>
    private void DrawQuestMarker(SpriteBatch sb, Gfx gfx, string npcId, Point p)
    {
        var m = QuestMarkers.MarkerFor(npcId, _character);
        if (m == QuestMarker.None) return;
        string glyph = m == QuestMarker.Available ? "!" : "?";
        var color = m == QuestMarker.InProgress ? MarkerGrey : MarkerYellow;
        float bob = m == QuestMarker.InProgress ? 0 : (float)Math.Sin(_markerTime * 4) * 2f;
        var box = Screen(p.X * TileSize - 4, p.Y * TileSize - 50 + bob, 40, 28);
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            if (dx != 0 || dy != 0) gfx.TextCentered(sb, glyph, new Rectangle(box.X + dx * 2, box.Y + dy * 2, box.Width, box.Height), Color.Black, 28f);
        gfx.TextCentered(sb, glyph, box, color, 28f);
    }

    private bool IsNpc(int x, int y)
    {
        foreach (var p in NpcPositions.Values) if (p.X == x && p.Y == y) return true;
        return false;
    }

    /// <summary>True for a door that is shut for now: the forest's exit before the Colonel has sent the player off.</summary>
    private bool IsLockedDoor(int x, int y) => _mapId == MapDefs.ForestId && _map.DoorAt(x, y) != null && !_character.CanLeaveForest;

    private void TryMove(Vector2 delta)
    {
        if (delta == Vector2.Zero) return;
        var next = _player + delta;
        int left = (int)(next.X / TileSize), right = (int)((next.X + PlayerSize - 1) / TileSize);
        int top = (int)(next.Y / TileSize), bottom = (int)((next.Y + PlayerSize - 1) / TileSize);
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                if (IsLockedDoor(x, y)) { if (_toastLeft <= 0) Toast(Character.LeaveForestRefusal, 2.5f); return; }
                if (!_map.IsWalkable(x, y) || IsNpc(x, y)) return;
                // Living slimes block their tile (Core TileOccupancy). A tile the player already overlaps stays
                // passable, so a slime wandering half under the player can never trap them.
                if (TileOccupancy.IsBlocked(x, y, _enemies.Values) && !OverlapsTile(_player, x, y)) return;
            }
        _player = next;
    }

    private static readonly Color SandA = new(206, 182, 130), SandB = new(200, 176, 124);
    private static readonly Color FenceWood = new(122, 88, 52), FencePost = new(88, 62, 36);
    private static readonly Color BedFrame = new(96, 68, 40), BedBlanketA = new(150, 70, 70), BedBlanketB = new(70, 100, 140), BedPillow = new(235, 232, 220);
    private static readonly Color Canvas = new(214, 200, 160), CanvasShade = new(190, 174, 132), CanvasDark = new(120, 100, 70);

    /// <summary>A door is a brighter path tile with a bar along the edge it leads out through.</summary>
    private static void DrawDoorMark(SpriteBatch sb, Gfx gfx, Rectangle r, char symbol)
    {
        gfx.Rect(sb, new Rectangle(r.X, r.Y, r.Width, r.Height), new Color(184, 154, 104));
        var bar = symbol switch
        {
            '<' => new Rectangle(r.X, r.Y, 5, r.Height),
            '>' => new Rectangle(r.Right - 5, r.Y, 5, r.Height),
            '^' => new Rectangle(r.X, r.Y, r.Width, 5),
            _ => new Rectangle(r.X, r.Bottom - 5, r.Width, 5),
        };
        gfx.Rect(sb, bar, Theme.Accent);
    }

    private void DrawFence(SpriteBatch sb, Gfx gfx, Rectangle r, int x, int y, Color ground)
    {
        bool l = _map[x - 1, y] == Tile.Fence, rt = _map[x + 1, y] == Tile.Fence, u = _map[x, y - 1] == Tile.Fence, d = _map[x, y + 1] == Tile.Fence;
        gfx.Rect(sb, r, ((x + y) & 1) == 0 ? SandA : SandB);
        if (l || rt) gfx.Rect(sb, new Rectangle(l ? r.X : r.X + 13, r.Y + 12, (l ? r.Width : 19) - (rt ? 0 : 13), 7), FenceWood);
        if (u || d) gfx.Rect(sb, new Rectangle(r.X + 12, u ? r.Y : r.Y + 13, 7, (u ? r.Height : 19) - (d ? 0 : 13)), FenceWood);
        gfx.Rect(sb, new Rectangle(r.X + 10, r.Y + 10, 12, 12), FencePost);
    }

    /// <summary>Canvas tent: a roof band on its top row, a dark doorway at the centre of its bottom row.</summary>
    private void DrawTent(SpriteBatch sb, Gfx gfx, Rectangle r, int x, int y, Color ground)
    {
        gfx.Rect(sb, r, ground);
        bool top = _map[x, y - 1] != Tile.Tent, bottom = _map[x, y + 1] != Tile.Tent;
        gfx.Rect(sb, r, (x & 1) == 0 ? Canvas : CanvasShade);
        if (top) gfx.Rect(sb, new Rectangle(r.X, r.Y, r.Width, 5), CanvasDark);
        if (bottom) gfx.Rect(sb, new Rectangle(r.X, r.Bottom - 3, r.Width, 3), CanvasDark);
        if (_map[x - 1, y] != Tile.Tent) gfx.Rect(sb, new Rectangle(r.X, r.Y, 3, r.Height), CanvasDark);
        if (_map[x + 1, y] != Tile.Tent) gfx.Rect(sb, new Rectangle(r.Right - 3, r.Y, 3, r.Height), CanvasDark);
        if (bottom)
        {
            int left = 0, right = 0;
            while (_map[x - 1 - left, y] == Tile.Tent) left++;
            while (_map[x + 1 + right, y] == Tile.Tent) right++;
            if (left == right) gfx.Rect(sb, new Rectangle(r.X + 6, r.Y + 8, 20, r.Height - 8), new Color(46, 34, 24));
        }
    }

    /// <summary>Barracks bed: a wooden frame with a blanket, and a pillow at the head (left end of a row of beds).</summary>
    private void DrawBed(SpriteBatch sb, Gfx gfx, Rectangle r, int x, int y)
    {
        gfx.Rect(sb, r, ((x + y) & 1) == 0 ? SandA : SandB);
        gfx.Rect(sb, new Rectangle(r.X + 2, r.Y + 3, r.Width - 4, r.Height - 6), BedFrame);
        gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 5, r.Width - 8, r.Height - 10), (y & 1) == 0 ? BedBlanketA : BedBlanketB);
        if (_map[x - 1, y] != Tile.Bed) gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 5, 8, r.Height - 10), BedPillow);
    }

    /// <summary>Save point (#54, cosmetic for now): a pulsing blue-white glow with a bright core.</summary>
    private void DrawSavePoint(SpriteBatch sb, Gfx gfx, Rectangle r, Color ground, GameTime time)
    {
        gfx.Rect(sb, r, ground);
        float pulse = 0.5f + 0.5f * MathF.Sin((float)time.TotalGameTime.TotalSeconds * 3f);
        int grow = (int)(pulse * 4);
        gfx.Rect(sb, new Rectangle(r.X + 3 - grow, r.Y + 3 - grow, r.Width - 6 + grow * 2, r.Height - 6 + grow * 2), new Color(110, 190, 255) * (0.25f + 0.2f * pulse));
        gfx.Rect(sb, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), new Color(130, 210, 255) * 0.8f);
        gfx.Rect(sb, new Rectangle(r.X + 11, r.Y + 11, r.Width - 22, r.Height - 22), new Color(245, 252, 255));
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
                    case Tile.Path:
                        gfx.Rect(sb, r, PathColor);
                        if (_map.DoorAt(x, y) is { } dm) DrawDoorMark(sb, gfx, r, dm.Symbol);
                        break;
                    case Tile.Floor: gfx.Rect(sb, r, ((x + y) & 1) == 0 ? SandA : SandB); break;
                    case Tile.Fence: DrawFence(sb, gfx, r, x, y, ground); break;
                    case Tile.Tent: DrawTent(sb, gfx, r, x, y, ground); break;
                    case Tile.Bed: DrawBed(sb, gfx, r, x, y); break;
                    case Tile.SavePoint: DrawSavePoint(sb, gfx, r, ground, time); break;
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

        var slimeBars = new List<(Enemy? Enemy, Rectangle Body)>();
        foreach (var s in _slimes)
        {
            var r = Screen(s.X * TileSize + 6, s.Y * TileSize + 12, 20, 16);
            var enemy = EnemyOf(s);
            if (enemy != null && enemy.AttackFlashFraction > 0) r.Inflate((int)(enemy.AttackFlashFraction * 4), (int)(enemy.AttackFlashFraction * 4));   // lunge
            gfx.Rect(sb, r, Hud.SlimeTint(enemy, SlimeColor, time));
            gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 4, 3, 3), Color.Black);
            gfx.Rect(sb, new Rectangle(r.X + 13, r.Y + 4, 3, 3), Color.Black);
            slimeBars.Add((enemy, r));   // tags and bars are drawn after the night overlay so they stay readable
        }

        foreach (var (id, p) in NpcPositions)
        {
            var r = Screen(p.X * TileSize + 5, p.Y * TileSize + 2, 22, 28);
            gfx.Rect(sb, r, id == "whiplash" ? new Color(130, 60, 60) : id == "barracks-master" ? new Color(70, 85, 120) : new Color(90, 100, 60));
            gfx.Border(sb, r, Color.Black, 2);
            gfx.TextCentered(sb, MapDefs.NpcName(id), Screen(p.X * TileSize - 60, p.Y * TileSize - 22, 152, 20), Theme.Text, 14f);
            DrawQuestMarker(sb, gfx, id, p);
        }

        var pr = Screen(_player.X, _player.Y, PlayerSize, PlayerSize);
        bool hitBlink = _hitFlash > 0 && (int)(_hitFlash * 20) % 2 == 0;
        gfx.Rect(sb, pr, hitBlink ? new Color(235, 60, 60) : Theme.Accent);
        gfx.Border(sb, pr, Color.Black, 2);

        // #57: night is a blue tint over the world (under the HUD). Slime tags go on top so they stay readable.
        if (_nightFade > 0) gfx.Rect(sb, new Rectangle(0, 0, w, h), NightTint * (NightMaxAlpha * _nightFade));
        foreach (var (enemy, body) in slimeBars) Hud.DrawSlimeBar(sb, gfx, enemy, body);

        // Floating damage numbers rise from the player and fade.
        foreach (var ft in _floats)
        {
            float t = ft.Age / ft.Life;
            var box = Screen(ft.X - 140, ft.Y - 22 - t * 28, 280, 24);
            gfx.TextCentered(sb, ft.Text, new Rectangle(box.X + 1, box.Y + 1, box.Width, box.Height), Color.Black * (1 - t), 22f);
            gfx.TextCentered(sb, ft.Text, box, ft.Color * (1 - t), 22f);
            if (ft.JellyIcon)   // a little green blob left of the text (#36)
            {
                float tw = gfx.Measure(ft.Text, 22f).X;
                int ix = box.X + (int)(box.Width / 2f - tw / 2f) - 20, iy = box.Y + 5;
                gfx.Rect(sb, new Rectangle(ix + 2, iy, 12, 14), new Color(90, 200, 90) * (1 - t));
                gfx.Rect(sb, new Rectangle(ix, iy + 4, 16, 10), new Color(90, 200, 90) * (1 - t));
                gfx.Rect(sb, new Rectangle(ix + 4, iy + 3, 3, 3), Color.White * (1 - t));
            }
        }

        gfx.Rect(sb, new Rectangle(0, 0, w, 36), Color.Black * 0.55f);
        gfx.TextLeft(sb, $"{_mapTitle}  -  {_character.Name}", new Rectangle(0, 0, w, 36), Theme.Text, 18f);

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
        if (_showControls) Hud.DrawControlsHint(sb, gfx);   // bottom-left, H toggles

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
        else if (_savePointTile is { } saveTile)
        {
            var prompt = Screen(saveTile.X * TileSize - 40, saveTile.Y * TileSize + TileSize + 6, 112, 30);
            gfx.Rect(sb, prompt, Color.Black * 0.75f);
            gfx.Border(sb, prompt, new Color(130, 210, 255), 2);
            gfx.TextCentered(sb, "Save (E)", prompt, new Color(170, 225, 255), 18f);
        }
        else if (_talkTarget != null && NpcPositions.TryGetValue(_talkTarget.Id, out var npcTile))
        {
            var prompt = Screen(npcTile.X * TileSize - 40, npcTile.Y * TileSize + TileSize + 6, 112, 30);
            gfx.Rect(sb, prompt, Color.Black * 0.75f);
            gfx.Border(sb, prompt, Theme.Accent, 2);
            gfx.TextCentered(sb, "Talk (E)", prompt, Theme.Accent, 18f);
        }

        // Fade to black while walking through a door.
        if (_leavingThrough != null) gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * (1 - _fadeOut / FadeOutSeconds));

        // Resting or sleeping: black in the first half second, back out in the second.
        if (_sleepT >= 0) gfx.Rect(sb, new Rectangle(0, 0, w, h), Color.Black * (1 - Math.Abs(_sleepT / (SleepFadeSeconds / 2) - 1)));

        // Fade in from black just after a respawn or a map change.
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

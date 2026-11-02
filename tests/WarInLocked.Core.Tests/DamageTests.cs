using WarInLocked.Core.Characters;
using WarInLocked.Core.Quests;
using WarInLocked.Core.Combat;
using WarInLocked.Core.Items;

namespace WarInLocked.Core.Tests;

public class DamageTests
{
    private static Character Make(StatDistribution d) =>
        new CharacterCreation { Name = "Aragorn", Race = Race.Human, Distribution = d }.Create();

    private static Character WithStats(int str, int intel) =>
        new Character("T", Race.Human, StatDistribution.Warrior, new StatBlock(str, 8, 8, 8, intel, 8, 8, 8));

    private static Character Armed(Character c)
    {
        c.Equipment.AddAndEquip(ItemLibrary.PracticeSword);
        return c;
    }

    // ---- the modifier curve (#14, #46: a flat 0.1 per point, no milestone bonus)

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(8, 8)]
    [InlineData(9, 9)]      // 0.9
    [InlineData(10, 10)]    // 1.0: no milestone jump
    [InlineData(11, 11)]
    [InlineData(24, 24)]
    [InlineData(25, 25)]    // 2.5
    [InlineData(26, 26)]
    [InlineData(49, 49)]
    [InlineData(50, 50)]    // 5.0
    [InlineData(51, 51)]
    public void ModifierIsAFlatPointOneEachWithNoMilestoneBonus(int points, int tenths)
    {
        Assert.Equal(tenths, StatModifier.Tenths(points));
        Assert.Equal(tenths / 10.0, StatModifier.Value(points), 10);
    }

    [Fact]
    public void ModifierAtTheNamedPointsAsDecimals()
    {
        Assert.Equal(0.9, StatModifier.Value(9), 10);
        Assert.Equal(1.0, StatModifier.Value(10), 10);
        Assert.Equal(1.1, StatModifier.Value(11), 10);
        Assert.Equal(2.5, StatModifier.Value(25), 10);
        Assert.Equal(5.0, StatModifier.Value(50), 10);
        Assert.Equal(0, StatModifier.MilestoneTenths);   // #46: Dyllan "no milestone jump"; the thresholds stay for a later bonus and the title cap
    }

    [Fact]
    public void DefaultMilestonesAreTenTwentyFiveFifty()
    {
        Assert.Equal(new[] { 10, 25, 50 }, StatModifier.Milestones);
    }

    [Fact]
    public void MilestonesAreConfigurable()
    {
        Assert.Equal(9, StatModifier.Tenths(9, new int[0]));
        Assert.Equal(20, StatModifier.Tenths(20, new int[0]));          // no milestones: plain 0.1 per point
        Assert.Equal(14, StatModifier.Tenths(14, new[] { 5 }));         // milestones add no bonus (#46)
        Assert.Equal(20, StatModifier.Tenths(20, new[] { 5, 15 }));
        Assert.Equal(4, StatModifier.Tenths(4, new[] { 5 }));           // not reached yet
        Assert.Equal(10, StatModifier.Tenths(10, new[] { 10, 10 }));
    }

    [Fact]
    public void TheStaticMilestoneListCanBeChangedAndRestored()
    {
        var original = StatModifier.Milestones;
        try
        {
            StatModifier.Milestones = new[] { 5 };
            Assert.Equal(14, StatModifier.Tenths(14));
            Assert.Equal(0, StatModifier.NextMilestone(14));   // 5 is below 14: no milestone is left above it
        }
        finally { StatModifier.Milestones = original; }
        Assert.Equal(10, StatModifier.Tenths(10));
        Assert.Equal(25, StatModifier.NextMilestone(10));
    }

    // ---- physical damage (#14)

    [Fact]
    public void UnarmedIsTenTimesTheStrengthModifier()
    {
        Assert.Equal(9, DamageCalculator.UnarmedDamage(WithStats(9, 8)));     // 0.9 x 10
        Assert.Equal(10, DamageCalculator.UnarmedDamage(WithStats(10, 8)));   // 1.0 x 10
        Assert.Equal(25, DamageCalculator.UnarmedDamage(WithStats(25, 8)));
        Assert.Equal(50, DamageCalculator.UnarmedDamage(WithStats(50, 8)));
        Assert.Equal(13, DamageCalculator.PhysicalDamage(Make(StatDistribution.Warrior)));   // Str 13 -> 1.3
    }

    [Fact]
    public void WeaponBonusIsAddedToTheBaseBeforeMultiplying()
    {
        Assert.Equal(5, ItemLibrary.PracticeSwordPhysicalAttack);
        var c = Armed(WithStats(10, 8));
        Assert.Equal(5, DamageCalculator.PhysicalAttackBonus(c));
        Assert.Equal(15, DamageCalculator.PhysicalDamage(c));                      // (10 + 5) x 1.0
        Assert.Equal(10, DamageCalculator.UnarmedDamage(c));                       // unarmed ignores the sword
        Assert.Equal(20, DamageCalculator.PhysicalDamage(c, 10));                  // (10 + 10) x 1.0
    }

    [Fact]
    public void NoWeaponMeansNoBonus()
    {
        var c = WithStats(10, 8);
        Assert.Equal(0, DamageCalculator.PhysicalAttackBonus(c));
        Assert.Equal(DamageCalculator.UnarmedDamage(c), DamageCalculator.PhysicalDamage(c));
    }

    [Fact]
    public void DamageIsRoundedHalfUp()
    {
        // (10 + 5) x 0.7 = 10.5 -> 11, x 0.9 = 13.5 -> 14, x 1.1 = 16.5 -> 17, x 2.5 = 37.5 -> 38
        Assert.Equal(11, DamageCalculator.PhysicalDamage(WithStats(7, 8), 5));
        Assert.Equal(14, DamageCalculator.PhysicalDamage(WithStats(9, 8), 5));
        Assert.Equal(17, DamageCalculator.PhysicalDamage(WithStats(11, 8), 5));
        Assert.Equal(38, DamageCalculator.PhysicalDamage(WithStats(25, 8), 5));
        // below .5 goes down: 11 x 0.1 = 1.1 -> 1; above .5 goes up: 15 x 0.8 = 12 exactly
        Assert.Equal(1, DamageCalculator.PhysicalDamage(WithStats(1, 8), 1));
        Assert.Equal(1, DamageCalculator.PhysicalDamage(WithStats(1, 8), 0));
        Assert.Equal(12, DamageCalculator.PhysicalDamage(WithStats(8, 8), 5));
        Assert.Equal(14, DamageCalculator.PhysicalDamage(WithStats(8, 8), 7));    // 17 x 0.8 = 13.6 -> 14
    }

    [Fact]
    public void SlashIsTenPlusWeaponPlusPowerTimesStrengthRoundedOnce()
    {
        var c = Armed(WithStats(13, 8));   // sword: 15 x 1.3 = 19.5 -> 20 basic; Slash (15 + 5) x 1.3 = 26
        Assert.Equal(20, CombatController.BasicAttackDamage(c));
        Assert.Equal(26, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition));
        Assert.Equal(27, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition, 2));   // n 6: 21 x 1.3 = 27.3
        var d = WithStats(13, 8);   // unarmed: the formula still gives (10 + 0 + 5) x 1.3 = 19.5 -> 20, but Slash cannot be cast without a weapon
        Assert.Equal(20, CombatController.AbilityDamage(d, AbilityLibrary.SlashDefinition));
    }

    [Fact]
    public void EnergyBallIsMagicBasePlusPowerTimesIntelligenceAndTheStaffAddsToTheBase()
    {
        // magic base = 10 + main-hand Magical Attack; damage = (base + n) x Int modifier
        var sword = Armed(WithStats(8, 13));   // a sword has no Magical Attack: (10 + 5) x 1.3 = 19.5 -> 20
        Assert.Equal(20, CombatController.AbilityDamage(sword, AbilityLibrary.EnergyBallDefinition));
        Assert.Equal(20, CombatController.AbilityDamage(WithStats(8, 13), AbilityLibrary.EnergyBallDefinition));   // unarmed is allowed (placeholder)
        var staff = WithStats(8, 13);
        staff.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);   // (10 + 5 + 5) x 1.3 = 26
        Assert.Equal(26, CombatController.AbilityDamage(staff, AbilityLibrary.EnergyBallDefinition));
        Assert.Equal(27, CombatController.AbilityDamage(staff, AbilityLibrary.EnergyBallDefinition, 2));   // n 6: 21 x 1.3 = 27.3
        Assert.Equal(12, CombatController.AbilityDamage(WithStats(13, 8), AbilityLibrary.EnergyBallDefinition));   // Int 8: 15 x 0.8
        Assert.Equal(5, AbilityLibrary.EnergyBallDefinition.EnergyCost);
        Assert.Equal(5.0, AbilityLibrary.EnergyBallDefinition.CooldownSeconds);
        Assert.Equal(AbilityKind.Magic, AbilityLibrary.EnergyBallDefinition.Kind);
        Assert.False(AbilityLibrary.EnergyBallDefinition.RequiresWeapon);
        Assert.Equal("Conjure an orb of pure energy.", AbilityLibrary.EnergyBallDefinition.Description);
    }

    [Fact]
    public void BaseAttacksAreTenPlusTheMatchingWeaponBonus()
    {
        var none = WithStats(8, 8);
        Assert.Equal(10, DamageCalculator.PhysicalBaseAttack(none));
        Assert.Equal(10, DamageCalculator.MagicalBaseAttack(none));
        var sword = Armed(WithStats(8, 8));
        Assert.Equal(15, DamageCalculator.PhysicalBaseAttack(sword));
        Assert.Equal(10, DamageCalculator.MagicalBaseAttack(sword));
        Assert.Equal(0, DamageCalculator.MagicalAttackBonus(sword));
        var staff = WithStats(8, 8);
        staff.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);
        Assert.Equal(10, DamageCalculator.PhysicalBaseAttack(staff));   // a staff does not help physical attacks
        Assert.Equal(15, DamageCalculator.MagicalBaseAttack(staff));
        Assert.True(DamageCalculator.HasWeapon(staff));                // any weapon counts for Slash
        Assert.Equal(CombatController.BasicAttackDamage(WithStats(8, 8)), CombatController.BasicAttackDamage(staff));   // basic attack = unarmed with a staff
    }

    [Fact]
    public void CombatUsesTheNewFormula()
    {
        var c = Armed(Make(StatDistribution.Mystic));   // Str 7 -> 0.7: 15 x 0.7 = 10.5 -> 11
        Assert.Equal(11, CombatController.BasicAttackDamage(c));
        var s = new Enemy(Enemy.SlimeType, 30);   // plain target without resistances
        s.TileX = 6; s.TileY = 5;
        Assert.Equal(11, c.Combat.BasicAttack(5, 5, new[] { s }).Damage);
        Assert.Equal(19, s.Hp);
    }

    [Fact]
    public void AssigningStrengthPointsChangesDamageAcrossAMilestone()
    {
        var c = WithStats(9, 8);
        Assert.Equal(9, DamageCalculator.UnarmedDamage(c));
        c.GainXp(50);
        c.AssignPoint(StatType.Strength);   // Str 10: the milestone is reached
        Assert.Equal(10, DamageCalculator.UnarmedDamage(c));   // 1.0 x 10: no milestone jump (#46)
    }

    // ---- magical damage (#16)

    [Fact]
    public void MagicIsTenPlusSpellPowerTimesTheIntelligenceModifier()
    {
        var mystic = Make(StatDistribution.Mystic);   // Int 13 -> 1.3
        Assert.Equal(13, DamageCalculator.MagicDamage(mystic));
        Assert.Equal(13, DamageCalculator.MagicDamage(mystic, 0));
        Assert.Equal(20, DamageCalculator.MagicDamage(mystic, 5));       // 15 x 1.3 = 19.5 -> 20
        Assert.Equal(39, DamageCalculator.MagicDamage(mystic, 5, 2.0));  // a 2x spell: 39.0
        var warrior = Make(StatDistribution.Warrior);   // Int 7 -> 0.7
        Assert.Equal(7, DamageCalculator.MagicDamage(warrior));
        Assert.Equal(11, DamageCalculator.MagicDamage(warrior, 5));      // 10.5 -> 11
    }

    [Fact]
    public void MagicUsesTheSameCurveAndMilestonesAsPhysical()
    {
        Assert.Equal(9, DamageCalculator.MagicDamage(WithStats(8, 9)));
        Assert.Equal(10, DamageCalculator.MagicDamage(WithStats(8, 10)));
        Assert.Equal(25, DamageCalculator.MagicDamage(WithStats(8, 25)));
        Assert.Equal(50, DamageCalculator.MagicDamage(WithStats(8, 50)));
    }

    [Fact]
    public void MagicIgnoresStrengthAndPhysicalIgnoresIntelligence()
    {
        var c = WithStats(25, 9);
        Assert.Equal(9, DamageCalculator.MagicDamage(c));
        Assert.Equal(25, DamageCalculator.UnarmedDamage(c));
        Assert.Equal(0.9, DamageCalculator.IntelligenceModifier(c), 10);
        Assert.Equal(2.5, DamageCalculator.StrengthModifier(c), 10);
    }

    [Fact]
    public void NegativeBonusesAreRejected()
    {
        var c = WithStats(8, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => DamageCalculator.MagicDamage(c, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DamageCalculator.PhysicalDamage(c, -1));
    }

    // ---- how many hits does a level 1-3 slime take at the starting stats? (#24; standard damage, multiplier 1.0, per Dyllan on 1 Nov)

    // Slash with the sword (Physical Attack 5): (10 + 5 + 5) x Str. Energy Ball with the staff (Magical Attack 5): (10 + 5 + 5) x Int.
    [Theory]
    [InlineData(StatDistribution.Warrior, 26, 2, 3, 4, 14, 4, 6, 8)]    // Str 13 = 1.3: 26;  Int 7 = 0.7: 14
    [InlineData(StatDistribution.Mystic, 14, 4, 6, 8, 26, 2, 3, 4)]     // Str 7 = 0.7: 14;   Int 13 = 1.3: 26
    [InlineData(StatDistribution.Merchant, 16, 4, 5, 7, 18, 3, 5, 6)]   // Str 8 = 0.8: 16;           Int 9 = 0.9: 18
    public void HitsToKillSlimesWithSlashAndEnergyBall(StatDistribution d, int slash, int s1, int s2, int s3, int ball, int b1, int b2, int b3)
    {
        var sword = Armed(Make(d));
        var staff = Make(d);
        staff.Equipment.AddAndEquip(ItemLibrary.PracticeStaff);
        for (int level = 1; level <= 3; level++)
        {
            var slime = Enemy.NewSlime(level);
            Assert.Equal(slash, CombatController.AbilityDamage(sword, AbilityLibrary.SlashDefinition, 1, slime));
            Assert.Equal(ball, CombatController.AbilityDamage(staff, AbilityLibrary.EnergyBallDefinition, 1, slime));
            Assert.Equal(new[] { s1, s2, s3 }[level - 1], HitsToKill(slime, slash));
            Assert.Equal(new[] { b1, b2, b3 }[level - 1], HitsToKill(slime, ball));
        }
    }

    [Theory]
    [InlineData(StatDistribution.Warrior, 3, 4)]   // unarmed 13 vs L1 50 HP: 4 hits; sword basic 15 x 1.3 = 19.5 -> 20: 3 hits
    public void BasicAttackHitsAgainstALevelOneSlime(StatDistribution d, int unusedA, int unusedB)
    {
        var c = Make(d);
        var slime = Enemy.NewSlime(1);
        Assert.Equal(13, DamageCalculator.PhysicalDamage(c, slime.PhysicalMultiplier));
        Assert.Equal(4, HitsToKill(slime, 13));
        Assert.Equal(20, CombatController.BasicAttackDamage(Armed(Make(d)), slime));
        Assert.Equal(3, HitsToKill(slime, 20));
    }

    private static int HitsToKill(Enemy slime, int damage) => (slime.MaxHealth + damage - 1) / damage;
}

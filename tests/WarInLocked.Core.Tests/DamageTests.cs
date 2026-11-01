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

    // ---- the modifier curve (#14)

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(8, 8)]
    [InlineData(9, 9)]      // 0.9
    [InlineData(10, 20)]    // 1.0 + 1.0 = 2.0
    [InlineData(11, 21)]    // 2.1
    [InlineData(24, 34)]
    [InlineData(25, 45)]    // 2.5 + 2.0 = 4.5
    [InlineData(26, 46)]
    [InlineData(49, 69)]
    [InlineData(50, 80)]    // 5.0 + 3.0 = 8.0
    [InlineData(51, 81)]
    public void ModifierIsAPointOneEachPlusOneForEveryMilestoneReached(int points, int tenths)
    {
        Assert.Equal(tenths, StatModifier.Tenths(points));
        Assert.Equal(tenths / 10.0, StatModifier.Value(points), 10);
    }

    [Fact]
    public void ModifierAtTheNamedPointsAsDecimals()
    {
        Assert.Equal(0.9, StatModifier.Value(9), 10);
        Assert.Equal(2.0, StatModifier.Value(10), 10);
        Assert.Equal(2.1, StatModifier.Value(11), 10);
        Assert.Equal(4.5, StatModifier.Value(25), 10);
        Assert.Equal(8.0, StatModifier.Value(50), 10);
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
        Assert.Equal(14 + 10, StatModifier.Tenths(14, new[] { 5 }));    // one milestone at 5
        Assert.Equal(20 + 20, StatModifier.Tenths(20, new[] { 5, 15 }));
        Assert.Equal(4, StatModifier.Tenths(4, new[] { 5 }));           // not reached yet
        Assert.Equal(10 + 10, StatModifier.Tenths(10, new[] { 10, 10 })); // a duplicate counts once
    }

    [Fact]
    public void TheStaticMilestoneListCanBeChangedAndRestored()
    {
        var original = StatModifier.Milestones;
        try
        {
            StatModifier.Milestones = new[] { 5 };
            Assert.Equal(14 + 10, StatModifier.Tenths(14));
        }
        finally { StatModifier.Milestones = original; }
        Assert.Equal(20, StatModifier.Tenths(10));
    }

    // ---- physical damage (#14)

    [Fact]
    public void UnarmedIsTenTimesTheStrengthModifier()
    {
        Assert.Equal(9, DamageCalculator.UnarmedDamage(WithStats(9, 8)));     // 0.9 x 10
        Assert.Equal(20, DamageCalculator.UnarmedDamage(WithStats(10, 8)));   // 2.0 x 10
        Assert.Equal(45, DamageCalculator.UnarmedDamage(WithStats(25, 8)));
        Assert.Equal(80, DamageCalculator.UnarmedDamage(WithStats(50, 8)));
        Assert.Equal(23, DamageCalculator.PhysicalDamage(Make(StatDistribution.Warrior)));   // Str 13 -> 2.3
    }

    [Fact]
    public void WeaponBonusIsAddedToTheBaseBeforeMultiplying()
    {
        Assert.Equal(5, ItemLibrary.PracticeSwordBaseDamage);
        var c = Armed(WithStats(10, 8));
        Assert.Equal(5, DamageCalculator.WeaponBonus(c));
        Assert.Equal(30, DamageCalculator.PhysicalDamage(c));                      // (10 + 5) x 2.0
        Assert.Equal(20, DamageCalculator.UnarmedDamage(c));                       // unarmed ignores the sword
        Assert.Equal(40, DamageCalculator.PhysicalDamage(c, 10));                  // (10 + 10) x 2.0
    }

    [Fact]
    public void NoWeaponMeansNoBonus()
    {
        var c = WithStats(10, 8);
        Assert.Equal(0, DamageCalculator.WeaponBonus(c));
        Assert.Equal(DamageCalculator.UnarmedDamage(c), DamageCalculator.PhysicalDamage(c));
    }

    [Fact]
    public void DamageIsRoundedHalfUp()
    {
        // (10 + 5) x 0.7 = 10.5 -> 11, x 0.9 = 13.5 -> 14, x 2.1 = 31.5 -> 32, x 4.5 = 67.5 -> 68
        Assert.Equal(11, DamageCalculator.PhysicalDamage(WithStats(7, 8), 5));
        Assert.Equal(14, DamageCalculator.PhysicalDamage(WithStats(9, 8), 5));
        Assert.Equal(32, DamageCalculator.PhysicalDamage(WithStats(11, 8), 5));
        Assert.Equal(68, DamageCalculator.PhysicalDamage(WithStats(25, 8), 5));
        // below .5 goes down: 11 x 0.1 = 1.1 -> 1; above .5 goes up: 15 x 0.8 = 12 exactly
        Assert.Equal(1, DamageCalculator.PhysicalDamage(WithStats(1, 8), 1));
        Assert.Equal(1, DamageCalculator.PhysicalDamage(WithStats(1, 8), 0));
        Assert.Equal(12, DamageCalculator.PhysicalDamage(WithStats(8, 8), 5));
        Assert.Equal(14, DamageCalculator.PhysicalDamage(WithStats(8, 8), 7));    // 17 x 0.8 = 13.6 -> 14
    }

    [Fact]
    public void SlashIsTenPlusWeaponPlusPowerTimesStrengthRoundedOnce()
    {
        var c = Armed(WithStats(13, 8));   // sword: 15 x 2.3 = 34.5 -> 35 basic; Slash (15 + 5) x 2.3 = 46
        Assert.Equal(35, CombatController.BasicAttackDamage(c));
        Assert.Equal(46, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition));
        Assert.Equal(48, CombatController.AbilityDamage(c, AbilityLibrary.SlashDefinition, 2));   // n 6: 21 x 2.3 = 48.3
        var d = WithStats(13, 8);   // unarmed: the formula still gives (10 + 0 + 5) x 2.3 = 34.5 -> 35, but Slash cannot be cast without a weapon
        Assert.Equal(35, CombatController.AbilityDamage(d, AbilityLibrary.SlashDefinition));
    }

    [Fact]
    public void EnergyBallIsTenPlusPowerTimesIntelligenceAndIgnoresTheWeapon()
    {
        var c = Armed(WithStats(8, 13));   // (10 + 5) x 2.3 = 34.5 -> 35, the sword adds nothing
        Assert.Equal(35, CombatController.AbilityDamage(c, AbilityLibrary.EnergyBallDefinition));
        Assert.Equal(35, CombatController.AbilityDamage(WithStats(8, 13), AbilityLibrary.EnergyBallDefinition));
        Assert.Equal(37, CombatController.AbilityDamage(c, AbilityLibrary.EnergyBallDefinition, 2));   // 16 x 2.3 = 36.8
        Assert.Equal(12, CombatController.AbilityDamage(WithStats(13, 8), AbilityLibrary.EnergyBallDefinition));   // Int 8: 15 x 0.8
        Assert.Equal(15, AbilityLibrary.EnergyBallDefinition.EnergyCost);
        Assert.Equal(5.0, AbilityLibrary.EnergyBallDefinition.CooldownSeconds);
        Assert.Equal(AbilityKind.Magic, AbilityLibrary.EnergyBallDefinition.Kind);
    }

    [Fact]
    public void CombatUsesTheNewFormula()
    {
        var c = Armed(Make(StatDistribution.Mystic));   // Str 7 -> 0.7: 15 x 0.7 = 10.5 -> 11
        Assert.Equal(11, CombatController.BasicAttackDamage(c));
        var s = Enemy.NewSlime();
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
        Assert.Equal(20, DamageCalculator.UnarmedDamage(c));
    }

    // ---- magical damage (#16)

    [Fact]
    public void MagicIsTenPlusSpellPowerTimesTheIntelligenceModifier()
    {
        var mystic = Make(StatDistribution.Mystic);   // Int 13 -> 2.3
        Assert.Equal(23, DamageCalculator.MagicDamage(mystic));
        Assert.Equal(23, DamageCalculator.MagicDamage(mystic, 0));
        Assert.Equal(35, DamageCalculator.MagicDamage(mystic, 5));       // 15 x 2.3 = 34.5 -> 35
        Assert.Equal(69, DamageCalculator.MagicDamage(mystic, 5, 2.0));  // a 2x spell: 69.0
        var warrior = Make(StatDistribution.Warrior);   // Int 7 -> 0.7
        Assert.Equal(7, DamageCalculator.MagicDamage(warrior));
        Assert.Equal(11, DamageCalculator.MagicDamage(warrior, 5));      // 10.5 -> 11
    }

    [Fact]
    public void MagicUsesTheSameCurveAndMilestonesAsPhysical()
    {
        Assert.Equal(9, DamageCalculator.MagicDamage(WithStats(8, 9)));
        Assert.Equal(20, DamageCalculator.MagicDamage(WithStats(8, 10)));
        Assert.Equal(45, DamageCalculator.MagicDamage(WithStats(8, 25)));
        Assert.Equal(80, DamageCalculator.MagicDamage(WithStats(8, 50)));
    }

    [Fact]
    public void MagicIgnoresStrengthAndPhysicalIgnoresIntelligence()
    {
        var c = WithStats(25, 9);
        Assert.Equal(9, DamageCalculator.MagicDamage(c));
        Assert.Equal(45, DamageCalculator.UnarmedDamage(c));
        Assert.Equal(0.9, DamageCalculator.IntelligenceModifier(c), 10);
        Assert.Equal(4.5, DamageCalculator.StrengthModifier(c), 10);
    }

    [Fact]
    public void NegativeBonusesAreRejected()
    {
        var c = WithStats(8, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => DamageCalculator.MagicDamage(c, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DamageCalculator.PhysicalDamage(c, -1));
    }

    // ---- is a 30 HP slime still sensible at the start stats?

    [Theory]
    [InlineData(StatDistribution.Warrior, 2, 1, 1)]    // unarmed 23, sword 35, Slash 46
    [InlineData(StatDistribution.Mystic, 5, 3, 3)]     // unarmed 7,  sword 11, Slash 14 (20 x 0.7)
    [InlineData(StatDistribution.Merchant, 4, 3, 2)]   // unarmed 8,  sword 12, Slash 16
    public void HitsToKillASlimeAtTheStartStats(StatDistribution d, int unarmed, int sword, int slash)
    {
        var c = Make(d);
        Assert.Equal(unarmed, HitsToKill(DamageCalculator.UnarmedDamage(c)));
        var armed = Armed(Make(d));
        Assert.Equal(sword, HitsToKill(CombatController.BasicAttackDamage(armed)));
        Assert.Equal(slash, HitsToKill(CombatController.AbilityDamage(armed, AbilityLibrary.SlashDefinition)));
    }

    private static int HitsToKill(int damage) => (Enemy.SlimeMaxHp + damage - 1) / damage;
}

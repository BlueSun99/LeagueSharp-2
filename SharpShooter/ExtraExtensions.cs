using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter
{
    static class ExtraExtensions
    {
        internal static bool isReadyPerfectly(this Spell spell)
        {
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.Instance.State != SpellState.Cooldown && spell.Instance.State != SpellState.Disabled && spell.Instance.State != SpellState.NoMana && spell.Instance.State != SpellState.NotLearned && spell.Instance.State != SpellState.Surpressed && spell.Instance.State != SpellState.Unknown && spell.Instance.State == SpellState.Ready;
        }

        internal static bool isKillableAndValidTarget(this Obj_AI_Hero Target, double CalculatedDamage, float distance = float.MaxValue)
        {
            if (Target == null || !Target.IsValidTarget(distance) || Target.Health <= 0 || Target.HasBuffOfType(BuffType.SpellImmunity) || Target.HasBuffOfType(BuffType.SpellShield) || Target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                CalculatedDamage *= 0.6;

            if (Target.ChampionName == "Blitzcrank")
                if (!Target.HasBuff("manabarriercooldown"))
                    if (Target.Health + Target.HPRegenRate + Target.PhysicalShield + (Target.Mana / 2) + Target.PARRegenRate > CalculatedDamage)
                        return false;

            if (Target.HasBuff("FerociousHowl"))
                CalculatedDamage *= 0.3;

            return Target.Health + Target.HPRegenRate + Target.PhysicalShield < CalculatedDamage;
        }

        internal static bool isKillableAndValidTarget(this Obj_AI_Minion Target, double CalculatedDamage, float distance = float.MaxValue)
        {
            if (Target == null || !Target.IsValidTarget(distance) || Target.Health <= 0 || Target.HasBuffOfType(BuffType.SpellImmunity) || Target.HasBuffOfType(BuffType.SpellShield) || Target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                CalculatedDamage *= 0.6;

            BuffInstance dragonSlayerBuff = ObjectManager.Player.GetBuff("s5test_dragonslayerbuff");
            if (dragonSlayerBuff != null)
            {
                if (dragonSlayerBuff.Count >= 4)
                    CalculatedDamage += dragonSlayerBuff.Count == 5 ? CalculatedDamage * 0.30 : CalculatedDamage * 0.15;

                if (Target.CharData.BaseSkinName.ToLowerInvariant().Contains("dragon"))
                    CalculatedDamage *= 1 - (dragonSlayerBuff.Count * 0.07);
            }

            if (Target.CharData.BaseSkinName.ToLowerInvariant().Contains("baron") && ObjectManager.Player.HasBuff("barontarget"))
                CalculatedDamage *= 0.5;

            return Target.Health + Target.HPRegenRate + Target.PhysicalShield < CalculatedDamage;
        }

        internal static bool isKillableAndValidTarget(this Obj_AI_Base Target, double CalculatedDamage, float distance = float.MaxValue)
        {
            if (Target == null || !Target.IsValidTarget(distance) || Target.Health <= 0 || Target.HasBuffOfType(BuffType.SpellImmunity) || Target.HasBuffOfType(BuffType.SpellShield) || Target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                CalculatedDamage *= 0.6;

            if (Target.CharData.BaseSkinName == "Blitzcrank")
                if (!Target.HasBuff("manabarriercooldown"))
                    if (Target.Health + Target.HPRegenRate + Target.PhysicalShield + (Target.Mana / 2) + Target.PARRegenRate > CalculatedDamage)
                        return false;

            if (Target.HasBuff("FerociousHowl"))
                CalculatedDamage *= 0.3;

            BuffInstance dragonSlayerBuff = ObjectManager.Player.GetBuff("s5test_dragonslayerbuff");
            if (dragonSlayerBuff != null)
                if (Target.IsMinion)
                {
                    if (dragonSlayerBuff.Count >= 4)
                        CalculatedDamage += dragonSlayerBuff.Count == 5 ? CalculatedDamage * 0.30 : CalculatedDamage * 0.15;

                    if (Target.CharData.BaseSkinName.ToLowerInvariant().Contains("dragon"))
                        CalculatedDamage *= 1 - (dragonSlayerBuff.Count * 0.07);
                }

            if (Target.CharData.BaseSkinName.ToLowerInvariant().Contains("baron") && ObjectManager.Player.HasBuff("barontarget"))
                CalculatedDamage *= 0.5;

            return Target.Health + Target.HPRegenRate + Target.PhysicalShield < CalculatedDamage;
        }

        internal static bool isManaPercentOkay(this Obj_AI_Hero hero, int ManaPercent)
        {
            return hero.ManaPercent > ManaPercent;
        }

        internal static double isImmobileUntil(this Obj_AI_Hero unit)
        {
            var result =
                unit.Buffs.Where(
                    buff =>
                        buff.IsActive && Game.Time <= buff.EndTime &&
                        (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun ||
                         buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return (result - Game.Time);
        }

        internal static bool isWillDieByTristanaE(this Obj_AI_Base target)
        {
            if (ObjectManager.Player.ChampionName == "Tristana")
                if (target.HasBuff("tristanaecharge"))
                    if (target.isKillableAndValidTarget((float)(Damage.GetSpellDamage(ObjectManager.Player, target, SpellSlot.E) * (target.GetBuffCount("tristanaecharge") * 0.30)) + Damage.GetSpellDamage(ObjectManager.Player, target, SpellSlot.E)))
                        return true;
            return false;
        }

        internal static Spell.CastStates CastWithExtraTrapLogic(this Spell spell)
        {
            if (spell.isReadyPerfectly())
            {
                var Teleport = MinionManager.GetMinions(spell.Range).FirstOrDefault(x => x.HasBuff("teleport_target"));
                var Zhonya = HeroManager.Enemies.FirstOrDefault(x => ObjectManager.Player.Distance(x) <= spell.Range && x.HasBuff("zhonyasringshield"));

                if (Teleport != null)
                    return spell.Cast(Teleport);

                if (Zhonya != null)
                    return spell.Cast(Zhonya);

            }
            return Spell.CastStates.NotCasted;
        }

        internal static float GetRealAutoAttackRange(this AttackableUnit unit, AttackableUnit target, int AutoAttackRange)
        {
            float result = AutoAttackRange + unit.BoundingRadius;
            if (target.IsValidTarget())
                return result + target.BoundingRadius;
            return result;
        }
    }
}

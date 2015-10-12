using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Sivir
    {
        private Spell Q, W, E, R;

        public Sivir()
        {
            Q = new Spell(SpellSlot.Q, 1250f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 1000f);

            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ();
            MenuProvider.Champion.Laneclear.addUseW();
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addItem("Auto Q on immobile Target", true);
            MenuProvider.Champion.Misc.addItem("Auto E against targeted spells", true);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Console.WriteLine("Sharpshooter: Sivir Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Sivir</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(10))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnBestTarget();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnBestTarget();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var FarmLocation = Q.GetLineFarmLocation(MinionManager.GetMinions(Q.Range));
                                            if (FarmLocation.MinionsHit >= 4)
                                                Q.Cast(FarmLocation.Position);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600));
                                            if (Target != null)
                                                Q.Cast(Target);
                                        }
                                break;
                            }
                    }

                    if (MenuProvider.Champion.Misc.getBoolValue("Auto Q on immobile Target"))
                        if (Q.isReadyPerfectly())
                        {
                            var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Q.Range) && Q.GetPrediction(x).Hitchance >= HitChance.Immobile);
                            if (Target != null)
                                Q.Cast(Target);
                        }
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender != null)
                if (args.Target != null)
                    if (args.Target.IsMe)
                        if (MenuProvider.Champion.Misc.getBoolValue("Auto E against targeted spells"))
                            if (E.isReadyPerfectly())
                                if (sender is Obj_AI_Hero)
                                    if (sender.IsEnemy)
                                        if (!args.SData.IsAutoAttack())
                                            if (!args.SData.Name.Contains("summoner"))
                                                if (!args.SData.Name.Contains("TormentedSoil"))
                                                    E.Cast();

            if (sender.IsMe)
                if (args.Slot == SpellSlot.W)
                    Orbwalking.ResetAutoAttackTimer();
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit Target)
        {
            if (unit.IsMe)
                if (Orbwalking.InAutoAttackRange(Target))
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        W.Cast();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                if (MenuProvider.Champion.Laneclear.UseW)
                                    if (W.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                            if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)).Any(x => x.NetworkId == Target.NetworkId))
                                                W.Cast();

                                if (MenuProvider.Champion.Jungleclear.UseW)
                                    if (W.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                            if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.Neutral).Any(x => x.NetworkId == Target.NetworkId))
                                                W.Cast();
                                break;
                            }
                    }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0;

            if (!ObjectManager.Player.IsWindingUp)
            {
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);
            }

            if (Q.isReadyPerfectly())
            {
                damage += Q.GetDamage(enemy) * 1.4f;
            }

            if (W.isReadyPerfectly())
            {
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy);
            }

            return damage;
        }
    }
}
using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Varus
    {
        private Spell Q, W, E, R;
        private int ELastCastTime;

        public Varus()
        {
            Q = new Spell(SpellSlot.Q, 1600f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 925f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            R = new Spell(SpellSlot.R, 1200f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 70f, 1500f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.0f, 250f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 1200f, false, SkillshotType.SkillshotLine);

            Q.SetCharged("VarusQ", "VarusQ", 250, 1600, 1.2f);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addItem("Q Min Charge", new Slider(800, 0, 1600));
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addItem("Q Min Charge", new Slider(1600, 0, 1600));
            MenuProvider.Champion.Harass.addUseE(false);
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addUseE(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseE(false);
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Console.WriteLine("Sharpshooter: Varus Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Varus</font> Loaded.");
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.Slot == SpellSlot.E)
                    ELastCastTime = Environment.TickCount;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                    {
                                        var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Q.ChargedMaxRange) && x.isKillableAndValidTarget(Q.GetDamage(x), TargetSelector.DamageType.Physical, Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                        if (killableTarget != null)
                                        {
                                            if (Q.IsCharging)
                                            {
                                                if (killableTarget.IsValidTarget(Q.Range))
                                                    Q.Cast(killableTarget, false, true);
                                            }
                                            else
                                                Q.StartCharging();
                                        }
                                        else
                                        {
                                            if (W.Level > 0)
                                            {
                                                var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Q.ChargedMaxRange) && x.GetBuffCount("varuswdebuff") >= 3);
                                                if (Target != null)
                                                {
                                                    if (Q.IsCharging)
                                                    {
                                                        if (Q.Range >= MenuProvider.Champion.Combo.getSliderValue("Q Min Charge").Value)
                                                        {
                                                            if (Target.IsValidTarget(Q.Range))
                                                                Q.Cast(Target, false, true);
                                                        }
                                                    }
                                                    else
                                                    if (MenuProvider.Champion.Combo.UseE ? !E.isReadyPerfectly() : true)
                                                        if (ELastCastTime + 1500 < Environment.TickCount)
                                                            Q.StartCharging();
                                                }
                                                else
                                                if (Q.IsCharging)
                                                    if (Q.Range >= MenuProvider.Champion.Combo.getSliderValue("Q Min Charge").Value)
                                                        Q.CastOnBestTarget(0, false, true);
                                            }
                                            else
                                            {
                                                if (Q.IsCharging)
                                                {
                                                    if (Q.Range >= MenuProvider.Champion.Combo.getSliderValue("Q Min Charge").Value)
                                                        Q.CastOnBestTarget(0, false, true);
                                                }
                                                else
                                                if (TargetSelector.GetTarget(Q.ChargedMaxRange, Q.DamageType) != null)
                                                    Q.StartCharging();
                                            }
                                        }
                                    }

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                    {
                                        var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(E.GetDamage(x), TargetSelector.DamageType.Physical, E.Range));
                                        if (killableTarget != null)
                                            E.Cast(killableTarget, false, true);
                                        else
                                        {
                                            if (W.Level > 0)
                                            {
                                                var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.GetBuffCount("varuswdebuff") >= 3);
                                                if (Target != null)
                                                    E.Cast(Target, false, true);
                                                else
                                                    E.CastIfWillHit(E.GetTarget(), 3, false);
                                            }
                                            else
                                                E.CastOnBestTarget(0, false, true);
                                        }
                                    }

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        R.CastOnBestTarget(-500);

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                {
                                    if (Q.isReadyPerfectly())
                                    {
                                        if (Q.IsCharging)
                                        {
                                            if (Q.Range >= Q.ChargedMaxRange)
                                                Q.CastOnBestTarget(0f, false, true);
                                            else
                                            {
                                                var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), TargetSelector.DamageType.Physical, Q.Range));
                                                if (killableTarget != null)
                                                    Q.Cast(killableTarget, false, true);
                                            }
                                        }
                                        else
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            if (TargetSelector.GetTarget(Q.ChargedMaxRange, Q.DamageType) != null)
                                                Q.StartCharging();
                                    }
                                }

                                if (MenuProvider.Champion.Harass.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (E.isReadyPerfectly())
                                            E.CastOnBestTarget(0f, false, true);
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                    {
                                        var FarmLocation = Q.GetLineFarmLocation(MinionManager.GetMinions(Q.ChargedMaxRange));
                                        if (Q.IsCharging)
                                        {
                                            if (Q.Range >= Q.ChargedMaxRange)
                                                Q.Cast(FarmLocation.Position);
                                        }
                                        else
                                        if (FarmLocation.MinionsHit >= 4)
                                            if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                                Q.StartCharging();
                                    }

                                if (MenuProvider.Champion.Laneclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            var FarmLocation = E.GetCircularFarmLocation(MinionManager.GetMinions(E.Range));
                                            if (FarmLocation.MinionsHit >= 4)
                                                E.Cast(FarmLocation.Position);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                    {
                                        var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600));
                                        if (Target != null)
                                            if (Q.IsCharging)
                                            {
                                                if (Q.Range >= Q.ChargedMaxRange)
                                                    Q.Cast(Target);
                                            }
                                            else
                                            if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                                Q.StartCharging();
                                    }

                                if (MenuProvider.Champion.Jungleclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600) && x.GetBuffCount("varuswdebuff") >= 3);
                                            if (Target != null)
                                                E.Cast(Target);
                                        }
                                break;
                            }
                    }
                }
            }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
                args.Process = !Q.IsCharging;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (R.isReadyPerfectly())
                    {
                        if (gapcloser.Sender.IsValidTarget(R.Range))
                            R.Cast(gapcloser.Sender);
                    }
                    else
                    if (gapcloser.Sender.IsValidTarget(E.Range))
                        E.Cast(gapcloser.Sender, false, true);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.High)
                    if (sender.IsValidTarget(R.Range))
                        if (R.isReadyPerfectly())
                            R.CastOnUnit(sender);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

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
                damage += Q.GetDamage(enemy);
            }

            if (E.isReadyPerfectly())
            {
                damage += E.GetDamage(enemy);
            }

            if (R.isReadyPerfectly())
            {
                damage += R.GetDamage(enemy);
            }

            return damage;
        }
    }
}
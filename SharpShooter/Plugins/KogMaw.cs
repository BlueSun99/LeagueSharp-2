﻿using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class KogMaw
    {
        private Spell Q, W, E, R;

        public KogMaw()
        {
            Q = new Spell(SpellSlot.Q, 950f) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1260f) { MinHitChance = HitChance.High };
            R = new Spell(SpellSlot.R) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.50f, 120f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.5f, 225f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();
            MenuProvider.Champion.Combo.addItem("R Stacks Limit", new Slider(3, 1, 6));

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseE();
            MenuProvider.Champion.Harass.addUseR();
            MenuProvider.Champion.Harass.addItem("R Stacks Limit", new Slider(1, 1, 6));
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseE(false);
            MenuProvider.Champion.Laneclear.addUseR(false);
            MenuProvider.Champion.Laneclear.addItem("R Stacks Limit", new Slider(1, 1, 6));
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addUseR();
            MenuProvider.Champion.Jungleclear.addItem("R Stacks Limit", new Slider(1, 1, 6));
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: KogMaw Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">KogMaw</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            W.Range = 565 + 110 + (W.Level * 20) + 65;
            R.Range = 900 + R.Level * 300;

            if (!ObjectManager.Player.IsDead)
                if (Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                    {
                                        var Target = TargetSelector.GetTargetNoCollision(Q);
                                        if (Target != null)
                                            Q.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        if (HeroManager.Enemies.Any(x => x.IsValidTarget(W.Range)))
                                            W.Cast();

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        E.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        if (ObjectManager.Player.GetBuffCount("kogmawlivingartillerycost") < MenuProvider.Champion.Combo.getSliderValue("R Stacks Limit").Value)
                                            R.CastOnBestTarget(0, false, true);
                                        else
                                        {
                                            var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Magical, R.Range) && R.GetPrediction(x).Hitchance >= R.MinHitChance);
                                            if (killableTarget != null)
                                                R.Cast(killableTarget, false, true);
                                        }

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        {
                                            var Target = TargetSelector.GetTargetNoCollision(Q);
                                            if (Target != null)
                                                Q.Cast(Target);
                                        }

                                if (MenuProvider.Champion.Harass.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            E.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Harass.UseR)
                                    if (R.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            if (ObjectManager.Player.GetBuffCount("kogmawlivingartillerycost") < MenuProvider.Champion.Harass.getSliderValue("R Stacks Limit").Value)
                                                R.CastOnBestTarget(0, false, true);
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            var FarmLocation = E.GetLineFarmLocation(MinionManager.GetMinions(E.Range));
                                            if (FarmLocation.MinionsHit >= 4)
                                                E.Cast(FarmLocation.Position);
                                        }

                                if (MenuProvider.Champion.Laneclear.UseR)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (R.isReadyPerfectly())
                                            if (ObjectManager.Player.GetBuffCount("kogmawlivingartillerycost") < MenuProvider.Champion.Laneclear.getSliderValue("R Stacks Limit").Value)
                                            {
                                                var FarmLocation = R.GetCircularFarmLocation(MinionManager.GetMinions(R.Range));
                                                if (FarmLocation.MinionsHit >= 4)
                                                    R.Cast(FarmLocation.Position);
                                            }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault(x => x.IsValidTarget(600));
                                            if (Target != null)
                                                E.Cast(Target);
                                        }

                                if (MenuProvider.Champion.Jungleclear.UseR)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (R.isReadyPerfectly())
                                            if (ObjectManager.Player.GetBuffCount("kogmawlivingartillerycost") < MenuProvider.Champion.Jungleclear.getSliderValue("R Stacks Limit").Value)
                                            {
                                                var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault(x => x.IsValidTarget(600));
                                                if (Target != null)
                                                    R.Cast(Target);
                                            }

                                break;
                            }
                    }
                }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        if (W.isReadyPerfectly())
                            if (MenuProvider.Champion.Combo.UseW)
                                if (args.Target.IsValidTarget(W.Range))
                                    W.Cast();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        if (W.isReadyPerfectly())
                            if (MenuProvider.Champion.Jungleclear.UseW)
                                if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).Any(x => x.NetworkId == args.Target.NetworkId))
                                    W.Cast();
                        break;
                }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

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
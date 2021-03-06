﻿using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Graves
    {
        private Spell Q, W, E, R;

        public Graves()
        {
            Q = new Spell(SpellSlot.Q, 850f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 850f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1100f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 45f, 2000f, false, SkillshotType.SkillshotCone);
            W.SetSkillshot(0.25f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 100f, 2100f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addItem("Q Range", new Slider(550, 100, 850));
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();
            MenuProvider.Champion.Combo.addItem("Cast R if Will Hit >=", new Slider(3, 2, 5));

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addItem("Q Range", new Slider(550, 100, 850));
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseKillsteal();
            MenuProvider.Champion.Misc.addUseAntiGapcloser();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw R Killable", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Console.WriteLine("Sharpshooter: Graves Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Graves</font> Loaded.");
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
                                        var QTarget = TargetSelector.GetTarget(MenuProvider.Champion.Combo.getSliderValue("Q Range").Value, Q.DamageType);
                                        if (QTarget != null)
                                            Q.Cast(QTarget, false, true);
                                    }

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        W.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var RKillableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range));
                                        if (RKillableTarget != null)
                                            R.Cast(RKillableTarget, false, true);
                                        R.CastIfWillHit(TargetSelector.GetTarget(R.Range, R.DamageType), MenuProvider.Champion.Combo.getSliderValue("Cast R if Will Hit >=").Value);
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var QTarget = TargetSelector.GetTarget(MenuProvider.Champion.Harass.getSliderValue("Q Range").Value, Q.DamageType);
                                            if (QTarget != null)
                                                Q.Cast(QTarget, false, true);
                                        }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var QTarget = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                            if (QTarget != null)
                                                Q.Cast(QTarget);
                                        }
                                break;
                            }
                    }
                }

                if (MenuProvider.Champion.Misc.UseKillsteal)
                {
                    var QTarget = HeroManager.Enemies.OrderByDescending(x => Q.GetPrediction(x).Hitchance).FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), TargetSelector.DamageType.Physical, Q.Range));
                    if (QTarget != null)
                        Q.Cast(QTarget, false, true);

                    var RTarget = HeroManager.Enemies.OrderByDescending(x => R.GetPrediction(x).Hitchance).FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range));
                    if (RTarget != null)
                        R.Cast(RTarget, false, true);
                }
            }
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit Target)
        {
            if (unit.IsMe)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseE)
                                if (E.isReadyPerfectly())
                                    if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                        if (!Q.isReadyPerfectly())
                                            E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));
                                        else
                                           if (ObjectManager.Player.Mana - E.ManaCost >= Q.ManaCost)
                                            E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));
                            break;
                        }
                }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget())
                        if (gapcloser.Sender.ChampionName.ToLowerInvariant() != "masteryi")
                            if (E.isReadyPerfectly())
                                E.Cast(ObjectManager.Player.Position.Extend(gapcloser.Sender.Position, -E.Range));
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    if (MenuProvider.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, MenuProvider.Champion.Harass.getSliderValue("Q Range").Value, MenuProvider.Champion.Drawings.DrawQrange.Color);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, MenuProvider.Champion.Combo.getSliderValue("Q Range").Value, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);

                var DrawRKillable = MenuProvider.Champion.Drawings.getCircleValue("Draw R Killable");
                if (DrawRKillable.Active && R.Level > 0)
                    foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical)))
                    {
                        var TargetPos = Drawing.WorldToScreen(Target.Position);
                        Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawRKillable.Color);
                        Drawing.DrawText(TargetPos.X, TargetPos.Y - 20, DrawRKillable.Color, "R Killable");
                    }
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

            if (W.isReadyPerfectly())
            {
                damage += W.GetDamage(enemy);
            }

            if (R.isReadyPerfectly())
            {
                damage += R.GetDamage(enemy);
            }

            return damage;
        }
    }
}
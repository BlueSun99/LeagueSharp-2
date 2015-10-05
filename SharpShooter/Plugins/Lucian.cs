﻿using System;
using System.Linq;
using System.Collections.Generic;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace SharpShooter.Plugins
{
    public class Lucian
    {
        private Spell Q, QExtended, W, E, R;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675f, TargetSelector.DamageType.Physical);
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Physical);
            E = new Spell(SpellSlot.E, 475f);
            QExtended = new Spell(SpellSlot.Q, 1100f, TargetSelector.DamageType.Physical);

            QExtended.SetSkillshot(0.5f, 65f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 55f, 1600f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addIfMana();

            //MenuProvider.Champion.Laneclear.addUseQ(false);
            //MenuProvider.Champion.Laneclear.addUseW(false);
            //MenuProvider.Champion.Laneclear.addIfMana(60);

            //MenuProvider.Champion.Jungleclear.addUseQ();
            //MenuProvider.Champion.Jungleclear.addUseW();
            //MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;

            Console.WriteLine("Sharpshooter: Lucian Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                {
                                    var Target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                                    if (Target != null)
                                        Q.CastOnUnit(Target);
                                    else
                                    {
                                        var ExtendedTarget = TargetSelector.GetTarget(QExtended.Range, Q.DamageType);
                                        if (ExtendedTarget != null)
                                        {
                                            var Minion = Prediction.GetPrediction(ExtendedTarget, QExtended.Delay, QExtended.Width, QExtended.Speed, new CollisionableObjects[] { CollisionableObjects.Minions }).CollisionObjects.FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                            if (Minion != null)
                                                Q.CastOnUnit(Minion);
                                        }
                                    }
                                }

                            if (MenuProvider.Champion.Combo.UseW)
                                if (W.isReadyPerfectly())
                                    W.CastOnBestTarget();

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                    {
                                        var Target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                                        if (Target != null)
                                            Q.CastOnUnit(Target);
                                        else
                                        {
                                            var ExtendedTarget = TargetSelector.GetTarget(QExtended.Range, Q.DamageType);
                                            if (ExtendedTarget != null)
                                            {
                                                var Minion = Prediction.GetPrediction(ExtendedTarget, QExtended.Delay, QExtended.Width, QExtended.Speed, new CollisionableObjects[] { CollisionableObjects.Minions }).CollisionObjects.FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                                if (Minion != null)
                                                    Q.CastOnUnit(Minion);
                                            }
                                        }
                                    }

                            if (MenuProvider.Champion.Harass.UseW)
                                if (W.isReadyPerfectly())
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        W.CastOnBestTarget();

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            //Lane


                            //Jugnle


                            break;
                        }
                }
            }
        }

        private void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.SData.IsAutoAttack())
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.Position.Extend(Game.CursorPos, 450).CountEnemiesInRange(800) <= 1)
                                            E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 450));

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
            return Q.isReadyPerfectly() ? Q.GetDamage(enemy) : 0;
        }
    }
}
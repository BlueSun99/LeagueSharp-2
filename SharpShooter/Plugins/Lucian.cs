using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Lucian
    {
        private Spell Q, QExtended, W, WNoCollision, E, R;
        private bool HasPassive;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675f, TargetSelector.DamageType.Physical);
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1400f);
            QExtended = new Spell(SpellSlot.Q, 1100f, TargetSelector.DamageType.Physical);
            WNoCollision = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Physical);

            QExtended.SetSkillshot(0.5f, 65f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 55f, 1600f, true, SkillshotType.SkillshotLine);
            WNoCollision.SetSkillshot(0.30f, 55f, 1600f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addIfMana();

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: Lucian Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (!ObjectManager.Player.IsDashing())
                                        if (HasPassive == false)
                                        {
                                            var Target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                                            if (Target != null)
                                                Q.CastOnUnit(Target);
                                            else
                                            {
                                                var ExtendedTarget = TargetSelector.GetTarget(QExtended.Range, Q.DamageType);
                                                if (ExtendedTarget != null)
                                                {
                                                    var Minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                                                    foreach (var Minion in Minions)
                                                    {
                                                        var BOX = new Geometry.Polygon.Rectangle(ObjectManager.Player.ServerPosition, ObjectManager.Player.ServerPosition.Extend(Minion.ServerPosition, QExtended.Range), QExtended.Width);
                                                        if (BOX.IsInside(QExtended.GetPrediction(ExtendedTarget).UnitPosition))
                                                        {
                                                            Q.CastOnUnit(Minion);
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), Q.Range));
                                            if (killableTarget != null)
                                                Q.CastOnUnit(killableTarget);
                                        }

                            if (MenuProvider.Champion.Combo.UseW)
                                if (W.isReadyPerfectly())
                                    if (!ObjectManager.Player.IsDashing())
                                        if (HasPassive == false)
                                        {
                                            if (HeroManager.Enemies.Any(x => Orbwalking.InAutoAttackRange(x)))
                                                WNoCollision.CastOnBestTarget();
                                            else
                                            {
                                                var Target = TargetSelector.GetTargetNoCollision(W);
                                                if (Target != null)
                                                    W.Cast(Target);
                                            }
                                        }
                                        else
                                        {
                                            var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(W.GetDamage(x), W.Range) && W.GetPrediction(x).Hitchance >= HitChance.High);
                                            if (killableTarget != null)
                                                W.Cast(killableTarget);
                                        }

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (HasPassive == false)
                                    if (Q.isReadyPerfectly())
                                        if (!ObjectManager.Player.IsDashing())
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
                                                        var Minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                                                        foreach (var Minion in Minions)
                                                        {
                                                            var BOX = new Geometry.Polygon.Rectangle(ObjectManager.Player.ServerPosition, ObjectManager.Player.ServerPosition.Extend(Minion.ServerPosition, QExtended.Range), QExtended.Width);
                                                            if (BOX.IsInside(QExtended.GetPrediction(ExtendedTarget).UnitPosition))
                                                            {
                                                                Q.CastOnUnit(Minion);
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                            if (MenuProvider.Champion.Harass.UseW)
                                if (W.isReadyPerfectly())
                                    if (!ObjectManager.Player.IsDashing())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            if (HasPassive == false)
                                            {
                                                if (HeroManager.Enemies.Any(x => Orbwalking.InAutoAttackRange(x)))
                                                    WNoCollision.CastOnBestTarget();
                                                else
                                                {
                                                    var Target = TargetSelector.GetTargetNoCollision(W);
                                                    if (Target != null)
                                                        W.Cast(Target);
                                                }
                                            }
                                            else
                                            {
                                                var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(W.GetDamage(x), W.Range) && W.GetPrediction(x).Hitchance >= HitChance.High);
                                                if (killableTarget != null)
                                                    W.Cast(killableTarget);
                                            }

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            //Laneclear
                            if (MenuProvider.Champion.Laneclear.UseQ)
                                if (HasPassive == false)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                            if (!ObjectManager.Player.IsDashing())
                                            {
                                                var Minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
                                                foreach (var Minion in Minions)
                                                {
                                                    var BOX = new Geometry.Polygon.Rectangle(ObjectManager.Player.ServerPosition, ObjectManager.Player.ServerPosition.Extend(Minion.ServerPosition, QExtended.Range), QExtended.Width);
                                                    if (Minions.Count(x => BOX.IsInside(x.ServerPosition)) >= 3)
                                                    {
                                                        Q.CastOnUnit(Minion);
                                                        break;
                                                    }
                                                }
                                            }

                            //Jungleclear
                            if (MenuProvider.Champion.Jungleclear.UseQ)
                                if (HasPassive == false)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                            if (!ObjectManager.Player.IsDashing())
                                            {
                                                var Target = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                                if (Target != null)
                                                    Q.CastOnUnit(Target);
                                            }

                            if (MenuProvider.Champion.Jungleclear.UseW)
                                if (HasPassive == false)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (W.isReadyPerfectly())
                                            if (!ObjectManager.Player.IsDashing())
                                            {
                                                var Target = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(W.Range));
                                                if (Target != null)
                                                    W.Cast(Target);
                                            }
                            break;
                        }
                }
            }
        }

        private void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.IsAutoAttack())
                {
                    HasPassive = false;

                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                            E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 700));
                                break;
                            }
                    }
                }

                if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.E)
                {
                    //do you know it? lucian can do autoattack cancel like riven
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    Utility.DelayAction.Add(Game.Ping * 2 + 10, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsDead)
                if (sender.IsMe)
                {
                    if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E || args.Slot == SpellSlot.R)
                        HasPassive = true;
                }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
                if (ObjectManager.Player.HasBuff("LucianR"))
                    args.Process = false;
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
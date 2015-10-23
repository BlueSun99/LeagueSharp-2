using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Caitlyn
    {
        private Spell Q, W, E, R;

        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1250f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 820f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E, 800f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            R = new Spell(SpellSlot.R, 2000f);

            Q.SetSkillshot(0.625f, 90f, 2200f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(1.00f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.125f, 80f, 2000f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addItem("Don't Use Q in AutoAttack Range", false);
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addItem("Use EQ", true);
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto R on Killable Target", false);
            MenuProvider.Champion.Misc.addItem("Auto W on Immobile Target", true);
            MenuProvider.Champion.Misc.addItem("Dash to Cursor Position (With E)", new KeyBind('G', KeyBindType.Press));
            MenuProvider.Champion.Misc.addItem("EQ Combo", new KeyBind('T', KeyBindType.Press));

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw R Killable", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Console.WriteLine("Sharpshooter: Caitlyn Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Caitlyn</font> Loaded.");
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                if (args.SData.IsAutoAttack())
                    if (sender.Type == GameObjectType.obj_AI_Hero)
                        if (sender.IsMelee)
                            if (MenuProvider.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        E.Cast(sender);
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            R.Range = 1500 + (500 * R.Level);

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
                                        if (MenuProvider.Champion.Combo.getBoolValue("Don't Use Q in AutoAttack Range") ? HeroManager.Enemies.Count(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x)) == 0 : true)
                                            Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                    {
                                        if (W.CastOnBestTarget() != Spell.CastStates.SuccessfullyCasted)
                                            W.CastWithExtraTrapLogic();
                                    }

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                    {
                                        var Target = HeroManager.Enemies.FirstOrDefault(x => !Orbwalking.InAutoAttackRange(x) && x.isKillableAndValidTarget(E.GetDamage(x), TargetSelector.DamageType.Physical, E.Range) && E.GetPrediction(x).Hitchance >= HitChance.High);
                                        if (Target != null)
                                            E.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var Target = HeroManager.Enemies.FirstOrDefault(x => !Orbwalking.InAutoAttackRange(x) && x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range));
                                        if (Target != null)
                                            if (ObjectManager.Player.GetEnemiesInRange(1500f).Count(x => Target.NetworkId != Target.NetworkId) == 0)
                                            {
                                                var collision = LeagueSharp.Common.Collision.GetCollision(new System.Collections.Generic.List<SharpDX.Vector3> { Target.ServerPosition }, new PredictionInput { UseBoundingRadius = true, Unit = ObjectManager.Player, Delay = 0.5f, Speed = 1500f, Radius = 200f, CollisionObjects = new CollisionableObjects[] { CollisionableObjects.Heroes } }).Any(x => x.NetworkId != Target.NetworkId);
                                                if (!collision)
                                                    R.CastOnUnit(Target);
                                            }
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (Q.isReadyPerfectly())
                                            Q.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Harass.getBoolValue("Use EQ"))
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (E.isReadyPerfectly())
                                            if (Q.isReadyPerfectly())
                                            {
                                                var Target = TargetSelector.GetTargetNoCollision(E);
                                                if (Target != null)
                                                    if (E.Cast(Target) == Spell.CastStates.SuccessfullyCasted)
                                                        Q.Cast(Target);
                                            }
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

                    if (MenuProvider.Champion.Misc.getBoolValue("Auto R on Killable Target"))
                        if (R.isReadyPerfectly())
                        {
                            var Target = HeroManager.Enemies.FirstOrDefault(x => !Orbwalking.InAutoAttackRange(x) && x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range));
                            if (Target != null)
                                if (ObjectManager.Player.GetEnemiesInRange(1500f).Count(x => Target.NetworkId != Target.NetworkId) == 0)
                                {
                                    var collision = LeagueSharp.Common.Collision.GetCollision(new System.Collections.Generic.List<SharpDX.Vector3> { Target.ServerPosition }, new PredictionInput { Unit = ObjectManager.Player, Delay = 0.5f, Speed = 1500f, Radius = 200f, CollisionObjects = new CollisionableObjects[] { CollisionableObjects.Heroes } }).Any(x => x.NetworkId != Target.NetworkId);
                                    if (!collision)
                                        R.CastOnUnit(Target);
                                }
                        }

                    if (MenuProvider.Champion.Misc.getBoolValue("Auto W on Immobile Target"))
                        if (W.isReadyPerfectly())
                        {
                            var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(W.Range) && x.isImmobileUntil() > 0.5f);
                            if (Target != null)
                                W.Cast(Target);
                        }
                }

                if (MenuProvider.Champion.Misc.getKeyBindValue("Dash to Cursor Position (With E)").Active)
                    if (E.isReadyPerfectly())
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, -(E.Range / 2)));

                if (MenuProvider.Champion.Misc.getKeyBindValue("EQ Combo").Active)
                {
                    if (E.isReadyPerfectly())
                        if (Q.isReadyPerfectly())
                        {
                            var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && x.IsValidTarget(200, true, Game.CursorPos) && E.GetPrediction(x).Hitchance >= E.MinHitChance).OrderByDescending(x => TargetSelector.GetPriority(x)).FirstOrDefault();
                            if (Target != null)
                            {
                                if (E.Cast(Target) == Spell.CastStates.SuccessfullyCasted)
                                    Q.Cast(Target, false, true);
                            }
                        }

                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget(E.Range))
                        if (E.isReadyPerfectly())
                            E.Cast(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.Medium)
                    if (sender.IsValidTarget(W.Range))
                        if (W.isReadyPerfectly())
                            W.CastOnUnit(sender);
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

                var DrawRKillable = MenuProvider.Champion.Drawings.getCircleValue("Draw R Killable");
                if (DrawRKillable.Active && R.Level > 0)
                    foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical)))
                    {
                        var TargetPos = Drawing.WorldToScreen(Target.Position);
                        Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawRKillable.Color);
                        Drawing.DrawText(TargetPos.X, TargetPos.Y - 20, DrawRKillable.Color, "R Killable");
                    }

                if (MenuProvider.Champion.Misc.getKeyBindValue("Dash to Cursor Position (With E)").Active)
                {
                    var CursorPos = Drawing.WorldToScreen(Game.CursorPos);

                    if (E.isReadyPerfectly())
                    {
                        Drawing.DrawText(CursorPos.X, CursorPos.Y - 40, DrawRKillable.Color, "Dash");
                        Render.Circle.DrawCircle(Game.CursorPos, 50, System.Drawing.Color.GreenYellow, 3);
                    }
                    else
                    {
                        Drawing.DrawText(CursorPos.X, CursorPos.Y - 40, DrawRKillable.Color, "Dash is Not Ready");
                    }

                }

                if (MenuProvider.Champion.Misc.getKeyBindValue("EQ Combo").Active)
                {
                    var CursorPos = Drawing.WorldToScreen(Game.CursorPos);
                    if (E.isReadyPerfectly() && Q.isReadyPerfectly())
                    {
                        Drawing.DrawText(CursorPos.X, CursorPos.Y - 50, DrawRKillable.Color, "EQ Combo");
                        Render.Circle.DrawCircle(Game.CursorPos, 200, System.Drawing.Color.GreenYellow, 3);
                    }
                    else
                    {
                        Drawing.DrawText(CursorPos.X, CursorPos.Y - 40, DrawRKillable.Color, "EQ Combo is Not Ready");
                    }
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

            if (R.isReadyPerfectly())
            {
                damage += R.GetDamage(enemy);
            }

            return damage;
        }
    }
}
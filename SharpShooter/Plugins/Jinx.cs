using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Jinx
    {
        private Spell Q, W, E, R;
        const int defaultRange = 525;
        private int GetQRange { get { return defaultRange + (25 * Q.Level); } }
        private bool isQActive { get { return ObjectManager.Player.HasBuff("JinxQ"); } }
        private int WCastTime;

        public Jinx()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1450f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.VeryHigh };
            E = new Spell(SpellSlot.E, 900f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            R = new Spell(SpellSlot.R, 2500f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };

            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            //MenuProvider.Champion.Combo.addItem("Switch to FISHBONES If will hit enemy Number >=", new Slider(3, 2, 6));
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addAutoHarass();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Lasthit.addUseQ();
            MenuProvider.Champion.Lasthit.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ();
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto E on Immobile Target", true);
            MenuProvider.Champion.Misc.addItem("Auto R on Killable Target", true);
            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw Rocket explosion range on AutoAttack Target", true);
            MenuProvider.Champion.Drawings.addItem("Draw R Killable", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: Jinx Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Jinx</font> Loaded.");
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Mixed:
                        if (MenuProvider.Champion.Harass.UseQ)
                            if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                            {
                                if (args.Target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(args.Target, defaultRange)))
                                    if (isQActive)
                                    {
                                        QSwitch(false);
                                        args.Process = false;
                                    }
                            }
                            else
                                QSwitch(false);
                        else
                            QSwitch(false);

                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        if (MinionManager.GetMinions(float.MaxValue).Any(x => x.NetworkId == args.Target.NetworkId))
                        {
                            if (MenuProvider.Champion.Laneclear.UseQ)
                            {
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                {
                                    if (MinionManager.GetMinions(float.MaxValue).Count(x => x.IsValidTarget(200, true, args.Target.Position) && (x.Health > ObjectManager.Player.GetAutoAttackDamage(x) * 2 || x.Health <= ObjectManager.Player.GetAutoAttackDamage(x) + Q.GetDamage(x))) >= 3)
                                        QSwitch(true);
                                    else
                                        QSwitch(false);
                                }
                                else
                                    QSwitch(false);
                            }
                            else
                                QSwitch(false);
                        }
                        else
                        if (MinionManager.GetMinions(float.MaxValue, MinionTypes.All, MinionTeam.Neutral).Any(x => x.NetworkId == args.Target.NetworkId))
                        {
                            if (MenuProvider.Champion.Jungleclear.UseQ)
                            {
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                {
                                    if (MinionManager.GetMinions(float.MaxValue, MinionTypes.All, MinionTeam.Neutral).Count(x => x.IsValidTarget(200, true, args.Target.Position)) >= 2)
                                        QSwitch(true);
                                    else
                                        QSwitch(false);
                                }
                                else
                                    QSwitch(false);
                            }
                            else
                                QSwitch(false);
                        }
                        else
                        {
                            QSwitch(false);
                        }
                        break;
                }
            }
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
                                {
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.CountEnemiesInRange(2000f) > 0)
                                        {
                                            var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(x, GetQRange))).OrderByDescending(a => TargetSelector.GetPriority(a)).FirstOrDefault();
                                            QSwitch(!Target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(Target, defaultRange)));
                                        }
                                        else
                                            QSwitch(false);
                                }
                                else
                                    QSwitch(false);

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                    {
                                        var Target = TargetSelector.GetTargetNoCollision(W);
                                        if (Target.IsValidTarget(W.Range))
                                            W.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                    {
                                        var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && E.GetPrediction(x).Hitchance >= E.MinHitChance && !x.IsFacing(ObjectManager.Player) && x.IsMoving).OrderBy(x => x.Distance(ObjectManager.Player)).FirstOrDefault();
                                        if (Target != null)
                                            E.Cast(Target, false, true);
                                        else
                                            E.CastWithExtraTrapLogic();
                                    }

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        if (WCastTime + 1060 <= Environment.TickCount)
                                        {
                                            var Target = HeroManager.Enemies.FirstOrDefault(x => !x.IsZombie && x.CountAlliesInRange(500) < 2 && HealthPrediction.GetHealthPrediction(x, 5000) > 0 && ObjectManager.Player.Distance(x) >= GetQRange && x.isKillableAndValidTarget(GetRDamage(x), R.Range) && R.GetPrediction(x).Hitchance >= HitChance.High);
                                            if (Target != null)
                                            {
                                                var prediction = R.GetPrediction(Target);
                                                var collision = LeagueSharp.Common.Collision.GetCollision(new System.Collections.Generic.List<SharpDX.Vector3> { prediction.UnitPosition }, new PredictionInput { UseBoundingRadius = true, Unit = ObjectManager.Player, Delay = R.Delay, Speed = R.Speed, Radius = 200, CollisionObjects = new CollisionableObjects[] { CollisionableObjects.Heroes } }).Any(x => x.NetworkId != Target.NetworkId);
                                                if (!collision)
                                                    R.Cast(Target);
                                            }
                                        }

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                {
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            if (ObjectManager.Player.CountEnemiesInRange(2000f) > 0)
                                            {
                                                var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(x, GetQRange))).OrderByDescending(a => TargetSelector.GetPriority(a)).FirstOrDefault();
                                                QSwitch(!Target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(Target, defaultRange)));
                                            }
                                            else
                                                QSwitch(false);
                                        else
                                            QSwitch(false);
                                }
                                else
                                    QSwitch(false);

                                if (MenuProvider.Champion.Harass.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var Target = TargetSelector.GetTargetNoCollision(W);
                                            if (Target.IsValidTarget(W.Range))
                                                W.Cast(Target);
                                        }

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                {
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        {

                                        }
                                        else
                                            QSwitch(false);
                                }
                                else
                                    QSwitch(false);

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600) && W.GetPrediction(x).Hitchance >= W.MinHitChance);
                                            if (Target != null)
                                                W.Cast(Target);
                                        }

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LastHit:
                            {
                                if (MenuProvider.Champion.Lasthit.UseQ)
                                {
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Lasthit.IfMana))
                                        {
                                            var Target = MinionManager.GetMinions(float.MaxValue).FirstOrDefault(x => x.isKillableAndValidTarget(ObjectManager.Player.GetAutoAttackDamage(x, false) + Q.GetDamage(x)) && x.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(x, GetQRange)) && !x.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(x, defaultRange)));
                                            if (Target != null)
                                            {
                                                QSwitch(true);

                                                if (Orbwalking.InAutoAttackRange(Target))
                                                    MenuProvider.Orbwalker.ForceTarget(Target);
                                            }
                                            else
                                                QSwitch(false);
                                        }
                                        else
                                            QSwitch(false);
                                }
                                else
                                    QSwitch(false);

                                break;
                            }
                    }

                    if (MenuProvider.Champion.Misc.getBoolValue("Auto R on Killable Target"))
                    {
                        if (R.isReadyPerfectly())
                            if (WCastTime + 1060 <= Environment.TickCount)
                            {
                                var Target = HeroManager.Enemies.FirstOrDefault(x => !x.IsZombie && x.CountAlliesInRange(500) < 2 && HealthPrediction.GetHealthPrediction(x, 5000) > 0 && ObjectManager.Player.Distance(x) >= GetQRange && x.isKillableAndValidTarget(GetRDamage(x), R.Range) && R.GetPrediction(x).Hitchance >= HitChance.High);
                                if (Target != null)
                                {
                                    var prediction = R.GetPrediction(Target);
                                    var collision = LeagueSharp.Common.Collision.GetCollision(new System.Collections.Generic.List<SharpDX.Vector3> { prediction.UnitPosition }, new PredictionInput { Unit = ObjectManager.Player, Delay = R.Delay, Speed = R.Speed, Radius = R.Width, CollisionObjects = new CollisionableObjects[] { CollisionableObjects.Heroes } }).Any(x => x.NetworkId != Target.NetworkId);
                                    if (!collision)
                                        R.Cast(Target);
                                }
                            }
                    }

                    if (MenuProvider.Champion.Misc.getBoolValue("Auto E on Immobile Target"))
                        if (E.isReadyPerfectly())
                        {
                            var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.isImmobileUntil() > 0.5f);
                            if (Target != null)
                                E.Cast(Target, false, true);
                        }

                    if (MenuProvider.Champion.Harass.AutoHarass)
                        if (!ObjectManager.Player.IsRecalling())
                            if (MenuProvider.Champion.Harass.UseW)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                {
                                    var Target = TargetSelector.GetTargetNoCollision(W);
                                    if (Target != null)
                                        if (ObjectManager.Player.UnderTurret(true) ? !Target.UnderTurret(true) : true)
                                            W.Cast(Target);
                                }
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (E.isReadyPerfectly())
                    if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                        E.Cast(gapcloser.End);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (E.isReadyPerfectly())
                    if (args.DangerLevel >= Interrupter2.DangerLevel.Medium)
                        E.Cast(sender);
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.Slot == SpellSlot.W)
                    WCastTime = Environment.TickCount;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly() && !isQActive)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, GetQRange + 115, MenuProvider.Champion.Drawings.DrawQrange.Color);

            if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

            if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

            if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);

            if (MenuProvider.Champion.Drawings.getBoolValue("Draw Rocket explosion range on AutoAttack Target"))
                if (isQActive)
                {
                    var AATarget = MenuProvider.Orbwalker.GetTarget();
                    if (AATarget != null)
                        Render.Circle.DrawCircle(AATarget.Position, 200, System.Drawing.Color.Red, 4, true);
                }

            var DrawRKillable = MenuProvider.Champion.Drawings.getCircleValue("Draw R Killable");
            if (DrawRKillable.Active && R.Level > 0)
                foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(GetRDamage(x))))
                {
                    var TargetPos = Drawing.WorldToScreen(Target.Position);
                    Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawRKillable.Color);
                    Drawing.DrawText(TargetPos.X, TargetPos.Y - 20, DrawRKillable.Color, "R Killable");
                }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0;

            if (!ObjectManager.Player.IsWindingUp)
            {
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);
            }

            if (W.isReadyPerfectly())
            {
                damage += W.GetDamage(enemy);
            }

            if (R.isReadyPerfectly())
            {
                damage += (float)GetRDamage(enemy);
            }

            return damage;
        }

        private void QSwitch(bool activate)
        {
            if (Q.isReadyPerfectly())
                if (!ObjectManager.Player.IsWindingUp)
                    switch (activate)
                    {
                        case true:
                            if (!ObjectManager.Player.HasBuff("JinxQ"))
                                Q.Cast();
                            break;
                        case false:
                            if (ObjectManager.Player.HasBuff("JinxQ"))
                                Q.Cast();
                            break;
                    }
        }

        private double GetRDamage(Obj_AI_Base Target)
        {
            return ObjectManager.Player.CalcDamage(Target, Damage.DamageType.Physical, new double[] { 0, 25, 30, 35 }[R.Level] / 100 * (Target.MaxHealth - Target.Health) + ((new double[] { 0, 25, 35, 45 }[R.Level] + 0.1 * ObjectManager.Player.FlatPhysicalDamageMod) * Math.Min((1 + ObjectManager.Player.Distance(Target.ServerPosition) / 15 * 0.09d), 10)));
        }
    }
}
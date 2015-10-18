using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Blitzcrank
    {
        private Spell Q, W, E, R;

        public Blitzcrank()
        {
            Q = new Spell(SpellSlot.Q, 925f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 175f, TargetSelector.DamageType.Physical);
            R = new Spell(SpellSlot.R, 550f, TargetSelector.DamageType.Magical);

            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseE();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw Q Target", new Circle(true, System.Drawing.Color.DeepSkyBlue));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Console.WriteLine("Sharpshooter: Blitzcrank Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Blitzcrank</font> Loaded.");
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.Slot == SpellSlot.E)
                    Orbwalking.ResetAutoAttackTimer();
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
                if (target.Type == GameObjectType.obj_AI_Hero)
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            if (MenuProvider.Champion.Combo.UseE)
                                if (E.isReadyPerfectly())
                                    E.Cast();
                            break;
                        case Orbwalking.OrbwalkingMode.Mixed:
                            if (MenuProvider.Champion.Harass.UseE)
                                if (E.isReadyPerfectly())
                                    E.Cast();
                            break;
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
                                    if (Q.isReadyPerfectly())
                                    {
                                        var Target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                                        if (Target != null)
                                            Q.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        foreach (var Target in HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range)))
                                        {
                                            //R Logics
                                            if (Target.isKillableAndValidTarget(R.GetDamage(Target), R.Range))
                                                R.Cast(Target);

                                            if (Target.HasBuff("rocketgrab2"))
                                            {
                                                R.Cast(Target);
                                                if (MenuProvider.Champion.Combo.UseE)
                                                    E.Cast();
                                            }
                                        }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnBestTarget();

                                break;
                            }
                    }
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (R.isReadyPerfectly())
                    if (gapcloser.Sender.IsValidTarget(R.Range))
                        R.Cast();
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.High)
                    if (sender.IsValidTarget(R.Range))
                        if (R.isReadyPerfectly())
                            R.Cast();
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);

                var DrawQTarget = MenuProvider.Champion.Drawings.getCircleValue("Draw Q Target");
                if (DrawQTarget.Active)
                {
                    var Target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                    if (Target != null)
                        Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawQTarget.Color, 3, true);
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
using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Ezreal
    {
        private Spell Q, W, E, R;

        public Ezreal()
        {
            Q = new Spell(SpellSlot.Q, 1150f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E, 475f, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 3000f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.0f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseR();
            MenuProvider.Champion.Combo.addItem("Cast R if Will Hit >=", new Slider(3, 2, 5));

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW(false);
            MenuProvider.Champion.Harass.addAutoHarass();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ();
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Console.WriteLine("Sharpshooter: Ezreal Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Ezreal</font> Loaded.");
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
                if (target.Type == GameObjectType.obj_AI_Hero)
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            if (Q.isReadyPerfectly())
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (target.IsValidTarget(Q.Range))
                                        Q.Cast(target as Obj_AI_Base);
                            }
                            else
                            if (W.isReadyPerfectly())
                                if (MenuProvider.Champion.Combo.UseW)
                                    if (target.IsValidTarget(W.Range))
                                        W.Cast(target as Obj_AI_Base);
                            break;
                        case Orbwalking.OrbwalkingMode.Mixed:
                            if (Q.isReadyPerfectly())
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (target.IsValidTarget(Q.Range))
                                        Q.Cast(target as Obj_AI_Base);
                            }
                            else
                            if (W.isReadyPerfectly())
                                if (MenuProvider.Champion.Harass.UseW)
                                    if (target.IsValidTarget(W.Range))
                                        W.Cast(target as Obj_AI_Base);
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
                                        var Target = TargetSelector.GetTargetNoCollision(Q);
                                        if (Target.IsValidTarget(Q.Range))
                                            Q.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        W.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var KillableTarget = HeroManager.Enemies.FirstOrDefault(x => !Orbwalking.InAutoAttackRange(x) && x.isKillableAndValidTarget(R.GetDamage(x), R.Range));
                                        if (KillableTarget != null)
                                            R.Cast(KillableTarget, false, true);
                                        R.CastIfWillHit(TargetSelector.GetTarget(R.Range, R.DamageType), 3);
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = TargetSelector.GetTargetNoCollision(Q);
                                            if (Target.IsValidTarget(Q.Range))
                                                Q.Cast(Target);
                                        }

                                if (MenuProvider.Champion.Harass.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (W.isReadyPerfectly())
                                            W.CastOnBestTarget(0, false, true);
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(Q.Range).OrderBy(x => x.Health).FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                            if (Target != null)
                                                Q.Cast(Target);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                            if (Target != null)
                                                Q.Cast(Target);
                                        }
                                break;
                            }
                    }

                    if (MenuProvider.Champion.Harass.AutoHarass)
                    {
                        if (Q.isReadyPerfectly())
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                    Q.CastOnBestTarget();

                        if (W.isReadyPerfectly())
                            if (MenuProvider.Champion.Harass.UseW)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                    W.CastOnBestTarget();
                    }
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
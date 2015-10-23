using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Ryze
    {
        private Spell Q, W, E, R, QNoCollision;

        public Ryze()
        {
            Q = new Spell(SpellSlot.Q, 900f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            QNoCollision = new Spell(SpellSlot.Q, 900f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.Low };
            W = new Spell(SpellSlot.W, 600f, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 600f, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 50f, 1400f, true, SkillshotType.SkillshotLine);
            QNoCollision.SetSkillshot(0.25f, 50f, 1400f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();
            MenuProvider.Champion.Combo.addItem("Ignore Collision", true);

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addUseE();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addUseW(false);
            MenuProvider.Champion.Laneclear.addUseE(false);
            MenuProvider.Champion.Laneclear.addItem("Use Burst Laneclear if R is Activated", true);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto keep passive stacks", new KeyBind('T', KeyBindType.Toggle, true));
            MenuProvider.Champion.Misc.addItem("^ Min Mana", new Slider(70, 0, 100));

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: Ryze Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Ryze</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                if (ObjectManager.Player.HasBuff("ryzepassivecharged") ? true : Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (MenuProvider.Champion.Combo.getBoolValue("Ignore Collision"))
                                            QNoCollision.CastOnBestTarget();
                                        else
                                            Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        W.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        E.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        if (!Q.isReadyPerfectly())
                                            if (!W.isReadyPerfectly())
                                                if (!E.isReadyPerfectly())
                                                    if (ObjectManager.Player.CountEnemiesInRange(W.Range) >= 1)
                                                        R.Cast();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Harass.UseW)
                                    if (W.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            W.CastOnBestTarget();

                                if (MenuProvider.Champion.Harass.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            E.CastOnBestTarget();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            if (MenuProvider.Champion.Laneclear.getBoolValue("Use Burst Laneclear if R is Activated") && ObjectManager.Player.HasBuff("ryzepassivecharged"))
                                            {
                                                var Target = MinionManager.GetMinions(Q.Range).FirstOrDefault(x => x.IsValidTarget(Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                                if (Target != null)
                                                    Q.Cast(Target);
                                            }
                                            else
                                            {
                                                var Target = MinionManager.GetMinions(Q.Range).FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), TargetSelector.DamageType.Magical, Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                                if (Target != null)
                                                    Q.Cast(Target);
                                            }
                                        }

                                if (MenuProvider.Champion.Laneclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            if (MenuProvider.Champion.Laneclear.getBoolValue("Use Burst Laneclear if R is Activated") && ObjectManager.Player.HasBuff("ryzepassivecharged"))
                                            {
                                                var Target = MinionManager.GetMinions(W.Range).FirstOrDefault(x => x.IsValidTarget(W.Range));
                                                if (Target != null)
                                                    W.CastOnUnit(Target);
                                            }
                                            else
                                            {
                                                var Target = MinionManager.GetMinions(W.Range).FirstOrDefault(x => x.isKillableAndValidTarget(W.GetDamage(x), TargetSelector.DamageType.Magical, W.Range));
                                                if (Target != null)
                                                    W.CastOnUnit(Target);
                                            }
                                        }

                                if (MenuProvider.Champion.Laneclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            if (MenuProvider.Champion.Laneclear.getBoolValue("Use Burst Laneclear if R is Activated") && ObjectManager.Player.HasBuff("ryzepassivecharged"))
                                            {
                                                var Target = MinionManager.GetMinions(E.Range).FirstOrDefault(x => x.IsValidTarget(E.Range));
                                                if (Target != null)
                                                    E.CastOnUnit(Target);
                                            }
                                            else
                                            {
                                                var Target = MinionManager.GetMinions(E.Range).FirstOrDefault(x => x.isKillableAndValidTarget(E.GetDamage(x), TargetSelector.DamageType.Magical, E.Range));
                                                if (Target != null)
                                                    E.CastOnUnit(Target);
                                            }
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

                                if (MenuProvider.Champion.Jungleclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600));
                                            if (Target != null)
                                                W.CastOnUnit(Target);
                                        }

                                if (MenuProvider.Champion.Jungleclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (E.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600));
                                            if (Target != null)
                                                E.CastOnUnit(Target);
                                        }
                                break;
                            }
                    }
                }

                if (MenuProvider.Champion.Misc.getKeyBindValue("Auto keep passive stacks").Active)
                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Misc.getSliderValue("^ Min Mana").Value))
                        if (!ObjectManager.Player.IsRecalling())
                            if (Q.Level > 0)
                                if (W.Level > 0)
                                    if (E.Level > 0)
                                        if (Q.isReadyPerfectly())
                                            if (!ObjectManager.Player.HasBuff("ryzepassivecharged"))
                                            {
                                                var passive = ObjectManager.Player.GetBuff("ryzepassivestack");
                                                if (passive != null)
                                                {
                                                    if (passive.Count < 4)
                                                        if (passive.EndTime - Game.ClockTime <= 0.5)
                                                            Q.Cast(Game.CursorPos);
                                                }
                                                else
                                                    Q.Cast(Game.CursorPos);
                                            }
            }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (W.isReadyPerfectly())
                                args.Process = false;
                            break;
                        }
                }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (W.isReadyPerfectly())
                    if (gapcloser.Sender.IsValidTarget(W.Range))
                        W.CastOnUnit(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (W.isReadyPerfectly())
                    if (sender.IsValidTarget(W.Range))
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
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0;

            if (!ObjectManager.Player.IsWindingUp)
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);

            if (Q.isReadyPerfectly())
                damage += Q.GetDamage(enemy);

            if (W.isReadyPerfectly())
                damage += W.GetDamage(enemy);

            if (E.isReadyPerfectly())
                damage += E.GetDamage(enemy);

            return damage;
        }
    }
}
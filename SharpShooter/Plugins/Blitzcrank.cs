using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Blitzcrank
    {
        private Spell Q, W, E, R;
        private bool DontAutoAttack;

        public Blitzcrank()
        {
            Q = new Spell(SpellSlot.Q, 925f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 175f, TargetSelector.DamageType.Physical);
            R = new Spell(SpellSlot.R, 550f, TargetSelector.DamageType.Magical);

            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();

            MenuProvider.MenuInstance.SubMenu("Champion").SubMenu("Combo").AddSubMenu(new Menu("Q WhiteList", "Q WhiteList"));
            foreach (var enemy in HeroManager.Enemies)
                MenuProvider.ChampionMenuInstance.SubMenu("Combo").SubMenu("Q WhiteList").AddItem(new MenuItem("Combo.Q WhiteList." + enemy.ChampionName, enemy.ChampionName, true)).SetValue(true);

            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseE();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw Q Target Mark", new Circle(true, System.Drawing.Color.DeepSkyBlue));
            MenuProvider.Champion.Drawings.addItem("Draw Whitelisted Target Mark", new Circle(true, System.Drawing.Color.White));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;

            Console.WriteLine("Sharpshooter: Blitzcrank Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Blitzcrank</font> Loaded.");
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
                                        var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance && MenuProvider.MenuInstance.Item("Combo.Q WhiteList." + x.ChampionName, true).GetValue<bool>()).OrderByDescending(x => TargetSelector.GetPriority(x)).FirstOrDefault();
                                        if (Target != null)
                                            Q.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (HeroManager.Enemies.Any(x => x.HasBuff("rocketgrab2")))
                                        W.Cast();

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        foreach (var Target in HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range)))
                                        {
                                            //R Logics
                                            if (Target.isKillableAndValidTarget(R.GetDamage(Target), R.Range))
                                                R.Cast(Target);

                                            if (Target.HasBuff("rocketgrab2"))
                                            {
                                                if (MenuProvider.Champion.Combo.UseE)
                                                    if (E.isReadyPerfectly())
                                                        E.Cast();

                                                R.Cast(Target);
                                            }

                                            if (Target.isImmobileUntil() > 0f)
                                                if (Target.isImmobileUntil() <= 0.25f)
                                                    R.Cast();
                                        }

                                        if (ObjectManager.Player.CountEnemiesInRange(R.Range) >= 2)
                                            R.Cast();
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

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.Slot == SpellSlot.E)
                    Orbwalking.ResetAutoAttackTimer();
        }

        private void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation == "Spell1" || args.Animation == "Spell4")
                    DontAutoAttack = true;
                else
                    DontAutoAttack = false;
            }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
            {
                if (DontAutoAttack)
                {
                    args.Process = false;
                    return;
                }

                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        if (MenuProvider.Champion.Combo.UseW)
                            if (args.Target.Type == GameObjectType.obj_AI_Hero)
                                if (W.isReadyPerfectly())
                                    W.Cast();
                        break;
                }
            }
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        if (target.Type == GameObjectType.obj_AI_Hero)
                        {
                            if (MenuProvider.Champion.Combo.UseE)
                                if (E.isReadyPerfectly())
                                    E.Cast();
                        }
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        if (target.Type == GameObjectType.obj_AI_Hero)
                        {
                            if (MenuProvider.Champion.Harass.UseE)
                                if (E.isReadyPerfectly())
                                    E.Cast();
                        }
                        break;
                }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);

                var DrawQTargetMark = MenuProvider.Champion.Drawings.getCircleValue("Draw Q Target Mark");
                if (DrawQTargetMark.Active)
                    if (Q.isReadyPerfectly())
                    {
                        var Target = HeroManager.Enemies.Where(x => x.IsValidTarget(Q.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance && MenuProvider.MenuInstance.Item("Combo.Q WhiteList." + x.ChampionName, true).GetValue<bool>()).OrderByDescending(x => TargetSelector.GetPriority(x)).FirstOrDefault();
                        if (Target != null)
                            Render.Circle.DrawCircle(Target.Position, 70, DrawQTargetMark.Color, 3, false);
                    }

                var DrawWhitelistedTargetMark = MenuProvider.Champion.Drawings.getCircleValue("Draw Whitelisted Target Mark");
                if (DrawWhitelistedTargetMark.Active)
                {
                    if (Q.isReadyPerfectly())
                    {
                        foreach (var Target in HeroManager.Enemies.Where(x => x.IsValidTarget() && MenuProvider.MenuInstance.Item("Combo.Q WhiteList." + x.ChampionName, true).GetValue<bool>()))
                        {
                            Render.Circle.DrawCircle(Target.Position, 30, DrawWhitelistedTargetMark.Color, 5, false);
                        }
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
using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Karthus
    {
        private Spell Q, W, E, R;
        private int LastPingTime;

        public Karthus()
        {
            Q = new Spell(SpellSlot.Q, 875f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.VeryHigh };
            E = new Spell(SpellSlot.E, 520f);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(1.0f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addItem("Disable AutoAttack", true);

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW(false);
            MenuProvider.Champion.Harass.addUseE(false);
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addQHitchanceSelector(HitChance.VeryHigh);
            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addItem("Ping Notify on R Killable Targets", true);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw R Killable Mark", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: Karthus Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Karthus</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            Q.MinHitChance = MenuProvider.Champion.Misc.QSelectedHitchance;

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
                                        Q.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        W.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        ESwitch(ObjectManager.Player.CountEnemiesInRange(E.Range) > 0);
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Harass.UseW)
                                    if (W.isReadyPerfectly())
                                        W.CastOnBestTarget(0, false, true);

                                if (MenuProvider.Champion.Harass.UseE)
                                    if (E.isReadyPerfectly())
                                        ESwitch(ObjectManager.Player.CountEnemiesInRange(E.Range) > 0);
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var FarmLocation = Q.GetCircularFarmLocation(MinionManager.GetMinions(Q.Range));
                                            if (FarmLocation.MinionsHit >= 1)
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
                }

                if (MenuProvider.Champion.Misc.getBoolValue("Ping Notify on R Killable Targets"))
                    if (R.isReadyPerfectly())
                    {
                        if (Environment.TickCount - LastPingTime >= 1000)
                        {
                            foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(R.GetDamage(x), R.DamageType)))
                                Game.ShowPing(PingCategory.Normal, Target.Position, true);

                            LastPingTime = Environment.TickCount;
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
                        {
                            if (MenuProvider.Champion.Combo.getBoolValue("Disable AutoAttack"))
                                args.Process = false;
                            else
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                    args.Process = false;
                            break;
                        }
                }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget())
                        if (W.isReadyPerfectly())
                            W.Cast(gapcloser.Sender);
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

                var DrawRKillableMark = MenuProvider.Champion.Drawings.getCircleValue("Draw R Killable Mark");
                if (DrawRKillableMark.Active)
                    if (R.isReadyPerfectly())
                    {
                        foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(R.GetDamage(x), R.DamageType)))
                        {
                            var TargetPos = Drawing.WorldToScreen(Target.Position);
                            Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawRKillableMark.Color);
                            Drawing.DrawText(TargetPos.X, TargetPos.Y - 20, DrawRKillableMark.Color, "R Killable");
                        }
                    }
            }
        }

        private void ESwitch(bool activate)
        {
            if (activate)
            {
                if (E.Instance.ToggleState == 1)
                    E.Cast();
            }
            else
            {
                if (E.Instance.ToggleState != 1)
                    E.Cast();
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
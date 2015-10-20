using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace SharpShooter.Plugins
{
    public class Vayne
    {
        private Spell Q, W, E, R;

        public Vayne()
        {
            Q = new Spell(SpellSlot.Q, 915f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 650f) { Width = 1f, MinHitChance = HitChance.VeryHigh };
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.14f, 1700f);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Harass.addUseQ(false);
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto Q when using R", true);
            MenuProvider.Champion.Misc.addItem("Q Stealth duration (ms)", new Slider(1000, 0, 1000));

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addItem("Draw E Crash Prediction", new Circle(false, System.Drawing.Color.YellowGreen));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Console.WriteLine("Sharpshooter: Vayne Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Vayne</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
                if (Orbwalking.CanMove(100))
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range)))
                                        {
                                            var Prediction = E.GetPrediction(enemy);
                                            if (Prediction.Hitchance >= HitChance.High)
                                            {
                                                var FinalPosition = Prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -400).To3D();
                                                for (int i = 1; i < 400; i += 50)
                                                {
                                                    Vector3 loc3 = Prediction.UnitPosition.Extend(ObjectManager.Player.ServerPosition, -i);
                                                    if (FinalPosition.IsWall() || loc3.IsWall())
                                                    {
                                                        E.CastOnUnit(enemy);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                break;
                            }
                    }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.R)
                    if (MenuProvider.Champion.Misc.getBoolValue("Auto Q when using R"))
                        if (Q.isReadyPerfectly())
                            Q.Cast(Game.CursorPos);

                if (args.Slot == SpellSlot.Q)
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
            }
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit Target)
        {
            if (unit.IsMe)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                        Q.Cast(Game.CursorPos);
                            break;
                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                            Q.Cast(Game.CursorPos);
                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            //Lane
                            if (MenuProvider.Champion.Laneclear.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                            if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)).Any(x => x.NetworkId == Target.NetworkId))
                                                Q.Cast(Game.CursorPos);

                            //Jugnle
                            if (MenuProvider.Champion.Jungleclear.UseQ)
                                if (Q.isReadyPerfectly())
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.Neutral).Any(x => x.NetworkId == Target.NetworkId))
                                            Q.Cast(Game.CursorPos);
                            break;
                        }
                }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
            {
                var buff = ObjectManager.Player.GetBuff("vaynetumblefade");
                if (buff != null)
                    if (buff.IsValidBuff())
                        if (buff.EndTime - Game.Time > (buff.EndTime - buff.StartTime) - (MenuProvider.Champion.Misc.getSliderValue("Q Stealth duration (ms)").Value / 1000))
                            args.Process = false;
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget(E.Range))
                        if (E.isReadyPerfectly())
                            E.CastOnUnit(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.High)
                    if (sender.IsValidTarget(E.Range))
                        if (E.isReadyPerfectly())
                            E.CastOnUnit(sender);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active && Q.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                var DrawECrashPrediction = MenuProvider.Champion.Drawings.getCircleValue("Draw E Crash Prediction");
                if (DrawECrashPrediction.Active)
                {
                    foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range)))
                    {
                        var Prediction = E.GetPrediction(enemy);

                        var FinalPosition = Prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -400).To3D();
                        for (int i = 1; i < 400; i += 50)
                        {
                            Vector3 loc3 = Prediction.UnitPosition.Extend(ObjectManager.Player.ServerPosition, -i);
                            if (FinalPosition.IsWall() || loc3.IsWall())
                            {
                                Render.Circle.DrawCircle(loc3, 50, DrawECrashPrediction.Color, 3, true);
                            }
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

            var buff = enemy.GetBuff("vaynesilvereddebuff");

            if (buff != null)
                if (buff.Caster.IsMe)
                    if (buff.Count == 2)
                        damage += W.GetDamage(enemy) + (float)ObjectManager.Player.GetAutoAttackDamage(enemy);

            if (Q.isReadyPerfectly())
            {
                damage += Q.GetDamage(enemy) + (float)ObjectManager.Player.GetAutoAttackDamage(enemy);
            }

            if (ObjectManager.Player.HasBuff("vaynetumblebonus"))
            {
                damage += Q.GetDamage(enemy) + (float)ObjectManager.Player.GetAutoAttackDamage(enemy);
            }

            return 0;
        }
    }
}
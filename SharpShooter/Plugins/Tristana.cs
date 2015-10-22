using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Tristana
    {
        private Spell Q, W, E, R;

        public Tristana()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1170f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            W.SetSkillshot(0.5f, 270f, 1500f, false, SkillshotType.SkillshotCircle);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();
            
            MenuProvider.Champion.Harass.addUseE();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseKillsteal();
            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto E on Turret", true);

            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addItem("Draw E Killable", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addItem("Draw R Killable", new Circle(true, System.Drawing.Color.GreenYellow));
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Console.WriteLine("Sharpshooter: Tristana Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Tristana</font> Loaded.");
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit Target)
        {
            if (unit.IsMe)
                if (Target != null)
                    if (Target.Type == GameObjectType.obj_AI_Turret || Target.Type == GameObjectType.obj_Turret)
                        if (MenuProvider.Champion.Misc.getBoolValue("Auto E on Turret"))
                            E.CastOnUnit(Target as Obj_AI_Base);
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            E.Range = Orbwalking.GetRealAutoAttackRange(null) + 65;
            R.Range = Orbwalking.GetRealAutoAttackRange(null) + 65;

            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        E.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var Target = HeroManager.Enemies.OrderByDescending(x => x.Health).FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range) && !x.isWillDieByTristanaE());
                                        if (Target != null)
                                            R.CastOnUnit(Target);
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (E.isReadyPerfectly())
                                            E.CastOnBestTarget();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Jungleclear
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

                if (MenuProvider.Champion.Misc.UseKillsteal)
                {
                    var Target = HeroManager.Enemies.OrderByDescending(x => x.Health).FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Physical, R.Range) && !x.isWillDieByTristanaE());
                    if (Target != null)
                        R.CastOnUnit(Target);
                }
            }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
                if (Orbwalking.InAutoAttackRange(args.Target))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.Cast();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.Neutral).Any(x => x.NetworkId == args.Target.NetworkId))
                                            Q.Cast();
                                break;
                            }
                    }

                }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget(R.Range))
                        if (R.isReadyPerfectly())
                            R.CastOnUnit(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.High)
                    if (sender.IsValidTarget(R.Range))
                        if (R.isReadyPerfectly())
                            R.CastOnUnit(sender);
        }


        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);

                var DrawEKillable = MenuProvider.Champion.Drawings.getCircleValue("Draw E Killable");
                if (DrawEKillable.Active)
                    foreach (var Target in HeroManager.Enemies.Where(x => x.IsValidTarget() && x.isWillDieByTristanaE()))
                    {

                        var TargetPos = Drawing.WorldToScreen(Target.Position);
                        Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawEKillable.Color);
                        Drawing.DrawText(TargetPos.X, TargetPos.Y - 50, DrawEKillable.Color, "will die by E");
                    }

                var DrawRKillable = MenuProvider.Champion.Drawings.getCircleValue("Draw R Killable");
                if (DrawRKillable.Active && R.isReadyPerfectly())
                    foreach (var Target in HeroManager.Enemies.Where(x => x.isKillableAndValidTarget(R.GetDamage(x), TargetSelector.DamageType.Magical)))
                    {
                        var TargetPos = Drawing.WorldToScreen(Target.Position);
                        Render.Circle.DrawCircle(Target.Position, Target.BoundingRadius, DrawRKillable.Color);
                        Drawing.DrawText(TargetPos.X, TargetPos.Y - 20, DrawRKillable.Color, "R Killable");
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

            if (enemy.HasBuff("tristanaecharge"))
            {
                damage += (float)(E.GetDamage(enemy) * (enemy.GetBuffCount("tristanaecharge") * 0.30)) + E.GetDamage(enemy);
            }

            if (R.isReadyPerfectly())
            {
                damage += R.GetDamage(enemy);
            }

            return damage;
        }
    }
}
using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Teemo
    {
        private Spell Q, W, E, R;

        public Teemo()
        {
            Q = new Spell(SpellSlot.Q, 680f, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 300f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };

            R.SetSkillshot(1.5f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ();
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Console.WriteLine("Sharpshooter: Teemo Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Teemo</font> Loaded.");
        }

        private void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
                if (target.Type == GameObjectType.obj_AI_Hero)
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.LastHit:
                            break;
                        case Orbwalking.OrbwalkingMode.Mixed:
                            break;
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            break;
                        case Orbwalking.OrbwalkingMode.Combo:
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (target.IsValidTarget(Q.Range))
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnUnit(target as Obj_AI_Base);
                            break;
                        case Orbwalking.OrbwalkingMode.None:
                            if (MenuProvider.Champion.Harass.UseQ)
                                if (target.IsValidTarget(Q.Range))
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnUnit(target as Obj_AI_Base);
                            break;
                        default:
                            break;
                    }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            R.Range = R.Level * 300;

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
                                        if (!HeroManager.Enemies.Any(x => Orbwalking.InAutoAttackRange(x)))
                                            Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                        if (ObjectManager.Player.CountEnemiesInRange(1000f) >= 1)
                                            W.Cast();

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var Target = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(R.Range) && !x.IsFacing(ObjectManager.Player) && !x.HasBuff("bantamtraptarget") && R.GetPrediction(x).Hitchance >= R.MinHitChance);
                                        if (Target != null)
                                            R.Cast(Target, false, true);
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (!HeroManager.Enemies.Any(x => Orbwalking.InAutoAttackRange(x)))
                                            Q.CastOnBestTarget();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(Q.Range).FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), TargetSelector.DamageType.Magical, Q.Range) && (x.CharData.BaseSkinName.Contains("siege") || x.CharData.BaseSkinName.Contains("super")));
                                            if (Target != null)
                                                Q.CastOnUnit(Target);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault(x => x.IsValidTarget(600));
                                            if (Target != null)
                                                Q.CastOnUnit(Target);
                                        }
                                break;
                            }
                    }
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
            {
                if (gapcloser.Sender.IsValidTarget(Q.Range))
                    if (Q.isReadyPerfectly())
                        Q.CastOnUnit(gapcloser.Sender);

                if (W.isReadyPerfectly())
                    W.Cast();

                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 300)
                    if (R.isReadyPerfectly())
                        R.Cast(ObjectManager.Player.Position);
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

            return damage;
        }
    }
}
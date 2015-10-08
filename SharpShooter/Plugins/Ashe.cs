using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Ashe
    {
        private Spell Q, W, E, R;

        public Ashe()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 2500f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };

            W.SetSkillshot(0.25f, 60f, 1500f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 130f, 1600f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseW(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseW();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();
            MenuProvider.Champion.Misc.addUseInterrupter();
            MenuProvider.Champion.Misc.addItem("Auto R on immobile targets", true);

            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack; ;

            Console.WriteLine("Sharpshooter: Ashe Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(10))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseW)
                                    if (W.isReadyPerfectly())
                                    {
                                        var Target = TargetSelector.GetTargetNoCollision(W);
                                        if (Target.IsValidTarget(W.Range))
                                            W.Cast(Target);
                                    }

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                        foreach (var Target in HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range) && R.GetPrediction(x).Hitchance >= HitChance.High))
                                        {
                                            //R Logics
                                            if (Target.isKillableAndValidTarget(Damage.GetSpellDamage(ObjectManager.Player, Target, SpellSlot.R), R.Range) && !Orbwalking.InAutoAttackRange(Target))
                                                R.Cast(Target);//killable

                                            if (Target.IsValidTarget(600f))
                                                R.Cast(Target);//too close

                                            if (Target.isImmobileUntil() > Target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) / R.Speed)
                                                R.Cast(Target);//immobile
                                        }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
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
                                //Laneclear
                                if (MenuProvider.Champion.Laneclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var FarmLocation = W.GetLineFarmLocation(MinionManager.GetMinions(W.Range));
                                            if (FarmLocation.MinionsHit >= 1)
                                                W.Cast(FarmLocation.Position);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(600) && W.GetPrediction(x).Hitchance >= HitChance.High);
                                            if (Target != null)
                                                W.Cast(Target);
                                        }
                                break;
                            }
                    }
                }

                if (MenuProvider.Champion.Misc.getBoolValue("Auto R on immobile targets"))
                    if (R.isReadyPerfectly())
                    {
                        var RTarget = HeroManager.Enemies.FirstOrDefault(x => R.GetPrediction(x).Hitchance >= HitChance.High && x.IsValidTarget(R.Range) && x.isImmobileUntil() > x.Distance(ObjectManager.Player.ServerPosition) / R.Speed);
                        if (RTarget != null)
                            R.Cast(RTarget);
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
                                    if (ObjectManager.Player.HasBuff("asheqcastready"))
                                        if (Q.isReadyPerfectly())
                                            Q.Cast();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.HasBuff("asheqcastready"))
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
                    if (gapcloser.Sender.IsValidTarget())
                        if (R.isReadyPerfectly())
                            R.Cast(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuProvider.Champion.Misc.UseInterrupter)
                if (args.DangerLevel >= Interrupter2.DangerLevel.High)
                    if (sender.IsValidTarget(R.Range))
                        if (R.isReadyPerfectly())
                            R.Cast(sender);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active && R.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            return (W.isReadyPerfectly() ? R.GetDamage(enemy) : 0) + (R.isReadyPerfectly() ? R.GetDamage(enemy) : 0);
        }
    }
}
using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Corki
    {
        private Spell Q, W, E, R;

        public Corki()
        {
            Q = new Spell(SpellSlot.Q, 825f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 800f, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 600f, TargetSelector.DamageType.Physical);
            R = new Spell(SpellSlot.R, 1250f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.3f, 250f, 1000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0f, 45f, float.MaxValue, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.20f, 40f, 2000f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseR();
            MenuProvider.Champion.Harass.addIfMana(60);

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseR();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Console.WriteLine("Sharpshooter: Corki Loaded.");
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
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        E.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseR)
                                    if (R.isReadyPerfectly())
                                    {
                                        var Target = TargetSelector.GetTargetNoCollision(R);
                                        if (Target != null)
                                            R.Cast(Target);
                                    }
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (Q.isReadyPerfectly())
                                            Q.CastOnBestTarget();

                                if (MenuProvider.Champion.Harass.UseR)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (R.isReadyPerfectly())
                                        {
                                            var Target = TargetSelector.GetTargetNoCollision(R);
                                            if (Target != null)
                                                R.Cast(Target);
                                        }
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
                                            if (FarmLocation.MinionsHit >= 4)
                                                Q.Cast(FarmLocation.Position);
                                        }

                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (Q.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(Q.Range));
                                            if (Target != null)
                                                Q.Cast(Target);
                                        }

                                if (MenuProvider.Champion.Jungleclear.UseR)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (R.isReadyPerfectly())
                                        {
                                            var Target = MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(R.Range));
                                            if (Target != null)
                                                R.Cast(Target);
                                        }
                                break;
                            }
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
            return R.isReadyPerfectly() ? (ObjectManager.Player.HasBuff("corkimissilebarragecounterbig") ? R.GetDamage(enemy, 1) : R.GetDamage(enemy)) : 0;
        }
    }
}
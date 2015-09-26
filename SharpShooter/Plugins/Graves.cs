using System.Linq;
using System.Collections.Generic;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace SharpShooter.Plugins
{
    public class Graves
    {
        private Spell Q, W, E, R;

        public Graves()
        {
            Q = new Spell(SpellSlot.Q) { DamageType = TargetSelector.DamageType.Physical, MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 850f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1000f) { MinHitChance = HitChance.High };

            Q.SetSkillshot(0.25f, 30f, 2000f, false, SkillshotType.SkillshotCone);
            W.SetSkillshot(0.25f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 100f, 2100f, false, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addItem("Q Range", new Slider(425, 100, 850));
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseR();

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addIfMana(60);

            MenuProvider.Champion.Misc.addUseAntiGapcloser();

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void Game_OnUpdate(System.EventArgs args)
        {
            Q.Range = MenuProvider.Champion.Combo.getSliderValue("Q Range").Value;

            if (!ObjectManager.Player.IsDead)
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseQ)
                                if (Q.isReadyPerfectly())
                                    Q.CastOnBestTarget();

                            if (MenuProvider.Champion.Combo.UseW)
                                if (W.isReadyPerfectly())
                                    W.CastOnBestTarget();

                            if (MenuProvider.Champion.Combo.UseR)
                                if (R.isReadyPerfectly())
                                {
                                    var RKillableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(R.GetDamage(x), R.Range));
                                    if (RKillableTarget != null)
                                        R.Cast(RKillableTarget);
                                    R.CastIfWillHit(TargetSelector.GetTarget(R.Range, R.DamageType), 3);
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
                                        if (FarmLocation.MinionsHit >= 3)
                                            Q.Cast(FarmLocation.Position);
                                    }

                            //Jungleclear
                            if (MenuProvider.Champion.Jungleclear.UseQ)
                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                    if (Q.isReadyPerfectly())
                                    {
                                        var FarmLocation = Q.GetCircularFarmLocation(MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral));
                                        if (FarmLocation.MinionsHit >= 3)
                                            Q.Cast(FarmLocation.Position);
                                    }
                            break;
                        }
                }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuProvider.Champion.Misc.UseAntiGapcloser)
                if (gapcloser.End.Distance(ObjectManager.Player.Position) <= 200)
                    if (gapcloser.Sender.IsValidTarget())
                        if (E.isReadyPerfectly())
                            E.Cast(ObjectManager.Player.Position.Extend(gapcloser.Sender.Position, - E.Range));
        }

        private void Drawing_OnDraw(System.EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawWrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                if (MenuProvider.Champion.Drawings.DrawRrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, MenuProvider.Champion.Drawings.DrawRrange.Color);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            return R.IsReady() ? R.GetDamage(enemy) : 0;
        }
    }
}
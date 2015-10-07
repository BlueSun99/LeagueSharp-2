using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter.Plugins
{
    public class Twitch
    {
        private Spell Q, W, E, Recall;

        public Twitch()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 900f, TargetSelector.DamageType.True) { MinHitChance = HitChance.High };
            E = new Spell(SpellSlot.E, 1200f);

            Recall = new Spell(SpellSlot.Recall);

            W.SetSkillshot(0.25f, 100f, 1400f, false, SkillshotType.SkillshotCircle);

            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Jungleclear.addUseW(false);
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addUseKillsteal();
            MenuProvider.Champion.Misc.addItem("Stealth Recall", new KeyBind('T', KeyBindType.Press));

            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addItem("R Pierce Line", true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Console.WriteLine("Sharpshooter: Twitch Loaded.");
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
                                        W.CastOnBestTarget();

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        if (HeroManager.Enemies.Any(x => x.IsValidTarget(E.Range) && (x.GetBuffCount("twitchdeadlyvenom") >= 6 || x.isKillableAndValidTarget(E.GetDamage(x)))))
                                            E.Cast();
                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Jungleclear
                                if (MenuProvider.Champion.Jungleclear.UseW)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (W.isReadyPerfectly())
                                        {
                                            var FarmLocation = W.GetCircularFarmLocation(MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral));
                                            if (FarmLocation.MinionsHit >= 1)
                                                W.Cast(FarmLocation.Position);
                                        }

                                if (MenuProvider.Champion.Jungleclear.UseE)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        if (E.isReadyPerfectly())
                                            if (MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral).Any(x => x.IsValidTarget(E.Range) && (x.GetBuffCount("TwitchHideInShadows") >= 6 || x.isKillableAndValidTarget(E.GetDamage(x)))))
                                                E.Cast();
                                break;
                            }
                    }
                }

                if (MenuProvider.Champion.Misc.UseKillsteal)
                    if (HeroManager.Enemies.Any(x => x.isKillableAndValidTarget(E.GetDamage(x), E.Range)))
                        E.Cast();

                if (MenuProvider.Champion.Misc.getKeyBindValue("Stealth Recall").Active)
                    if (Q.isReadyPerfectly())
                        if (Recall.isReadyPerfectly())
                        {
                            Q.Cast();
                            Recall.Cast();
                        }
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawWrange.Active && W.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, MenuProvider.Champion.Drawings.DrawWrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active && E.isReadyPerfectly())
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);

                if (MenuProvider.Champion.Drawings.getBoolValue("R Pierce Line"))
                    if (ObjectManager.Player.HasBuff("TwitchFullAutomatic"))
                    {
                        var Target = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), TargetSelector.DamageType.Physical);
                        if (Target.IsValidTarget())
                        {
                            var from = Drawing.WorldToScreen(ObjectManager.Player.Position);
                            var dis = (Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 300) - ObjectManager.Player.Distance(Target, false);
                            var to = Drawing.WorldToScreen(dis > 0 ? Target.ServerPosition.Extend(ObjectManager.Player.Position, -dis) : Target.ServerPosition);
                            Drawing.DrawLine(from[0], from[1], to[0], to[1], 10, System.Drawing.Color.FromArgb(200, System.Drawing.Color.GreenYellow));
                        }
                    }

            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            return E.isReadyPerfectly() ? E.GetDamage(enemy) : 0;
        }
    }
}
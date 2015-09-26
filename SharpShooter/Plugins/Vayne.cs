using System.Linq;
using System.Collections.Generic;

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
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.14f, 1200f);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addIfMana();

            MenuProvider.Champion.Laneclear.addUseQ();
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(60);

            MenuProvider.Champion.Misc.addItem("auto Q when using R");

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;
        }

        private void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.SData.IsAutoAttack())
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        Q.Cast(Game.CursorPos);

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                            Q.Cast(Game.CursorPos);

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Lane
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                            if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)).Any())
                                                Q.Cast(Game.CursorPos);

                                //Jugnle
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                            if (MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.Neutral).Any())
                                                Q.Cast(Game.CursorPos);

                                break;
                            }
                    }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                if (args.Slot == SpellSlot.R)
                    if (MenuProvider.Champion.Misc.getBoolValue("auto Q when using R"))
                        if (Q.isReadyPerfectly())
                            Q.Cast(Game.CursorPos);
        }

        private void Game_OnUpdate(System.EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {
                            if (MenuProvider.Champion.Combo.UseE)
                                if (E.isReadyPerfectly())
                                    foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range)))
                                    {
                                        var Prediction = E.GetPrediction(enemy);
                                        var FinalPosition = Prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), 350).To3D();

                                        for (int i = 1; i <= 350; i += (int)enemy.BoundingRadius)
                                        {
                                            Vector3 loc3 = Prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -i).To3D();

                                            if (loc3.IsWall())
                                                E.CastOnUnit(enemy);
                                        }

                                    }

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            break;
                        }
                }
            }
        }

        private void Drawing_OnDraw(System.EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (MenuProvider.Champion.Drawings.DrawQrange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, MenuProvider.Champion.Drawings.DrawQrange.Color);

                if (MenuProvider.Champion.Drawings.DrawErange.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, MenuProvider.Champion.Drawings.DrawErange.Color);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            return Q.IsReady() ? Q.GetDamage(enemy) + (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true) : 0;
        }
    }
}
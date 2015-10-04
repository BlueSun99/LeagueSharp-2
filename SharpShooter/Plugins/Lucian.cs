using System;
using System.Linq;
using System.Collections.Generic;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace SharpShooter.Plugins
{
    public class Lucian
    {
        private Spell Q, W, E, R;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675f, TargetSelector.DamageType.Physical);
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Physical);
            E = new Spell(SpellSlot.E, 475f);

            Q.SetSkillshot(0.25f, 65f, 1100f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            
            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseW();
            MenuProvider.Champion.Combo.addUseE();

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addUseW();
            MenuProvider.Champion.Harass.addIfMana();

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addUseW(false);
            MenuProvider.Champion.Laneclear.addIfMana(60);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Console.WriteLine("Sharpshooter: Lucian Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        {

                            break;

                        }
                    case Orbwalking.OrbwalkingMode.Mixed:
                        {
                            

                            break;
                        }
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        {
                            //Lane
                            

                            //Jugnle
                            

                            break;
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
            return Q.isReadyPerfectly() ? Q.GetDamage(enemy) : 0;
        }
    }
}
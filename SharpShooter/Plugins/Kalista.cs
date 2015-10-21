using System;
using System.Linq;
using System.Collections.Generic;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace SharpShooter.Plugins
{
    public class Kalista
    {
        private Spell Q, W, E, R;
        private int ELastCastTime;
        private Vector3 BaronLocation;
        private Vector3 DragonLocation;

        public Kalista()
        {
            Q = new Spell(SpellSlot.Q, 1150f, TargetSelector.DamageType.Physical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 5000f);
            E = new Spell(SpellSlot.E, 950f);
            R = new Spell(SpellSlot.R, 1500f);

            Q.SetSkillshot(0.25f, 40f, 1200f, true, SkillshotType.SkillshotLine);

            MenuProvider.Champion.Combo.addUseQ();
            MenuProvider.Champion.Combo.addUseE();
            MenuProvider.Champion.Combo.addItem("Attack Minion For Chasing", false);

            MenuProvider.Champion.Harass.addUseQ();
            MenuProvider.Champion.Harass.addIfMana();

            MenuProvider.Champion.Laneclear.addUseQ(false);
            MenuProvider.Champion.Laneclear.addItem("Cast Q if Killable Minion Number >=", new Slider(3, 1, 7));
            MenuProvider.Champion.Laneclear.addUseE();
            MenuProvider.Champion.Laneclear.addItem("Cast E if Killable Minion Number >=", new Slider(2, 1, 5));
            MenuProvider.Champion.Laneclear.addIfMana(20);

            MenuProvider.Champion.Jungleclear.addUseQ();
            MenuProvider.Champion.Jungleclear.addUseE();
            MenuProvider.Champion.Jungleclear.addIfMana(20);

            MenuProvider.Champion.Misc.addItem("Use Killsteal (With E)", true);
            MenuProvider.Champion.Misc.addItem("Use Mobsteal (With E)", true);
            MenuProvider.Champion.Misc.addItem("Use Lasthit Assist (With E)", true);
            MenuProvider.Champion.Misc.addItem("Use Soulbound Saver (With R)", true);
            MenuProvider.Champion.Misc.addItem("Auto Balista Combo (With R)", true);
            MenuProvider.Champion.Misc.addItem("Auto Steal Siege minion & Super minion (With E)", true);
            MenuProvider.Champion.Misc.addItem("Auto E Harass (With E)", true);
            MenuProvider.Champion.Misc.addItem("^ Don't do this in ComboMode", true);
            MenuProvider.Champion.Misc.addItem("Auto W on Dragon or Baron (With W)", false);
            MenuProvider.Champion.Misc.addItem("Cast W on Dragon", new KeyBind('J', KeyBindType.Press));
            MenuProvider.Champion.Misc.addItem("Cast W on Baron", new KeyBind('K', KeyBindType.Press));

            MenuProvider.Champion.Drawings.addDrawQrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addDrawWrange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawErange(System.Drawing.Color.DeepSkyBlue, false);
            MenuProvider.Champion.Drawings.addDrawRrange(System.Drawing.Color.DeepSkyBlue, true);
            MenuProvider.Champion.Drawings.addItem("Draw E Damage Percent", false);
            MenuProvider.Champion.Drawings.addDamageIndicator(GetComboDamage);
            MenuProvider.Champion.Drawings.addDamageIndicatorForJungle(GetJungleDamage);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            BaronLocation = new Vector3(5064f, 10568f, -71f);
            DragonLocation = new Vector3(9796f, 4432f, -71f);

            Console.WriteLine("Sharpshooter: Kalista Loaded.");
            Game.PrintChat("<font color = \"#00D8FF\"><b>SharpShooter Reworked:</b></font> <font color = \"#FF007F\">Kalista</font> Loaded.");
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (UnderClocking.NeedtoUnderClocking())
                return;

            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                if (MenuProvider.Champion.Combo.getBoolValue("Attack Minion For Chasing"))
                                    if (!HeroManager.Enemies.Any(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x)))
                                    {
                                        var Minion = MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(null) + 65, MinionTypes.All, MinionTeam.NotAlly).OrderBy(x => x.Distance(ObjectManager.Player)).FirstOrDefault();
                                        if (Minion != null)
                                            Orbwalking.Orbwalk(Minion, Game.CursorPos, 0f);
                                    }

                                if (MenuProvider.Champion.Combo.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (!ObjectManager.Player.IsDashing())
                                            if (!ObjectManager.Player.IsWindingUp)
                                            {
                                                var Target = TargetSelector.GetTargetNoCollision(Q);
                                                if (Target != null)
                                                    if (ObjectManager.Player.Mana - Q.ManaCost >= E.ManaCost)
                                                        Q.Cast(Target);
                                                    else
                                                    {
                                                        var killableTarget = HeroManager.Enemies.FirstOrDefault(x => x.isKillableAndValidTarget(Q.GetDamage(x), E.Range) && Q.GetPrediction(x).Hitchance >= Q.MinHitChance);
                                                        if (killableTarget != null)
                                                            Q.Cast(killableTarget);
                                                    }
                                            }

                                if (MenuProvider.Champion.Combo.UseE)
                                    if (E.isReadyPerfectly())
                                        if (HeroManager.Enemies.Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x), E.Range)))
                                            E.Cast();

                                break;

                            }
                        case Orbwalking.OrbwalkingMode.Mixed:
                            {
                                if (MenuProvider.Champion.Harass.UseQ)
                                    if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Harass.IfMana))
                                        if (!ObjectManager.Player.IsDashing())
                                            if (!ObjectManager.Player.IsWindingUp)
                                                if (Q.isReadyPerfectly())
                                                    Q.CastOnBestTarget();

                                break;
                            }
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            {
                                //Lane
                                if (MenuProvider.Champion.Laneclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (!ObjectManager.Player.IsDashing())
                                            if (!ObjectManager.Player.IsWindingUp)
                                                if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                                {
                                                    foreach (var KillableMinion in MinionManager.GetMinions(Q.Range).Where(x => Q.GetPrediction(x).Hitchance >= Q.MinHitChance && x.isKillableAndValidTarget(Damage.GetSpellDamage(ObjectManager.Player, x, SpellSlot.Q), Q.Range)))
                                                    {
                                                        int killableNumber = 0;

                                                        var CollisionMinions =
                                                        LeagueSharp.Common.Collision.GetCollision(new List<Vector3> { ObjectManager.Player.ServerPosition.Extend(KillableMinion.ServerPosition, Q.Range) },
                                                            new PredictionInput
                                                            {
                                                                Unit = ObjectManager.Player,
                                                                Delay = Q.Delay,
                                                                Speed = Q.Speed,
                                                                Radius = Q.Width,
                                                                Range = Q.Range,
                                                                CollisionObjects = new CollisionableObjects[] { CollisionableObjects.Minions },
                                                                UseBoundingRadius = false
                                                            }
                                                        ).OrderBy(x => x.Distance(ObjectManager.Player));

                                                        foreach (Obj_AI_Minion CollisionMinion in CollisionMinions)
                                                        {
                                                            if (CollisionMinion.isKillableAndValidTarget(Damage.GetSpellDamage(ObjectManager.Player, CollisionMinion, SpellSlot.Q), Q.Range))
                                                                killableNumber++;
                                                            else
                                                                break;
                                                        }

                                                        if (killableNumber >= MenuProvider.Champion.Laneclear.getSliderValue("Cast Q if Killable Minion Number >=").Value)
                                                        {
                                                            if (!ObjectManager.Player.IsWindingUp)
                                                            {
                                                                Q.Cast(KillableMinion.ServerPosition);
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                if (MenuProvider.Champion.Laneclear.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Laneclear.IfMana))
                                            if (MinionManager.GetMinions(E.Range).Count(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x))) >= MenuProvider.Champion.Laneclear.getSliderValue("Cast E if Killable Minion Number >=").Value)
                                                E.Cast();

                                //Jugnle
                                if (MenuProvider.Champion.Jungleclear.UseQ)
                                    if (Q.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                        {
                                            var QTarget = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(x => x.IsValidTarget(Q.Range) && Q.GetPrediction(x).Hitchance >= HitChance.High);

                                            if (QTarget != null)
                                                Q.Cast(QTarget);
                                        }

                                if (MenuProvider.Champion.Jungleclear.UseE)
                                    if (E.isReadyPerfectly())
                                        if (ObjectManager.Player.isManaPercentOkay(MenuProvider.Champion.Jungleclear.IfMana))
                                            if (MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral).Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x))))
                                                E.Cast();

                                break;
                            }
                    }
                }

                if (MenuProvider.Champion.Misc.getBoolValue("Use Killsteal (With E)"))
                    if (E.isReadyPerfectly())
                        if (HeroManager.Enemies.Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x), E.Range)))
                            E.Cast();

                if (MenuProvider.Champion.Misc.getBoolValue("Use Mobsteal (With E)"))
                {
                    if (E.isReadyPerfectly())
                        if (MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral).Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x))))
                            E.Cast();
                }

                if (MenuProvider.Champion.Misc.getBoolValue("Auto Steal Siege minion & Super minion (With E)"))
                {
                    if (E.isReadyPerfectly())
                        if (MinionManager.GetMinions(E.Range).Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x)) && (x.CharData.BaseSkinName.ToLower().Contains("siege") || x.CharData.BaseSkinName.ToLower().Contains("super"))))
                            E.Cast();
                }

                if (MenuProvider.Champion.Misc.getBoolValue("Auto Balista Combo (With R)"))
                    if (R.isReadyPerfectly())
                    {
                        var MyBlitzcrank = HeroManager.Allies.FirstOrDefault(x => !x.IsDead && x.HasBuff("kalistacoopstrikeally") && x.ChampionName == "Blitzcrank");
                        if (MyBlitzcrank != null)
                        {
                            var GrabTarget = HeroManager.Enemies.FirstOrDefault(x => !x.IsDead && x.HasBuff("rocketgrab2"));
                            if (GrabTarget != null)
                                if (ObjectManager.Player.Distance(GrabTarget) > MyBlitzcrank.Distance(GrabTarget))
                                    R.Cast();
                        }
                    }

                if (MenuProvider.Champion.Misc.getBoolValue("Auto E Harass (With E)"))
                    if (E.isReadyPerfectly())
                        if (!(MenuProvider.Champion.Misc.getBoolValue("^ Don't do this in ComboMode") && MenuProvider.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo))
                            if (ObjectManager.Player.Mana - E.ManaCost >= E.ManaCost)
                                if (HeroManager.Enemies.Any(x => x.IsValidTarget(E.Range) && E.GetDamage(x) > 10))
                                    if (MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly).Any(x => HealthPrediction.GetHealthPrediction(x, 500) > 0 && x.isKillableAndValidTarget(E.GetDamage(x), E.Range)))
                                        E.Cast();

                if (MenuProvider.Champion.Misc.getBoolValue("Auto W on Dragon or Baron (With W)"))
                    if (!ObjectManager.Player.IsRecalling())
                        if (ObjectManager.Player.Position.CountEnemiesInRange(1500f) <= 0)
                            if (MenuProvider.Orbwalker.GetTarget() == null)
                            {
                                if (W.isReadyPerfectly())
                                    if (ObjectManager.Player.Distance(BaronLocation) <= W.Range)
                                        W.Cast(BaronLocation);

                                if (W.isReadyPerfectly())
                                    if (ObjectManager.Player.Distance(DragonLocation) <= W.Range)
                                        W.Cast(DragonLocation);
                            }

                if (MenuProvider.Champion.Misc.getKeyBindValue("Cast W on Dragon").Active)
                    if (W.isReadyPerfectly())
                        if (ObjectManager.Player.Distance(DragonLocation) <= W.Range)
                            W.Cast(DragonLocation);

                if (MenuProvider.Champion.Misc.getKeyBindValue("Cast W on Baron").Active)
                    if (W.isReadyPerfectly())
                        if (ObjectManager.Player.Distance(BaronLocation) <= W.Range)
                            W.Cast(BaronLocation);
            }
        }

        private void Orbwalking_OnNonKillableMinion(AttackableUnit minion)
        {
            if (!ObjectManager.Player.IsDead)
            {
                Obj_AI_Minion Minion = minion as Obj_AI_Minion;
                if (MenuProvider.Champion.Misc.getBoolValue("Use Lasthit Assist (With E)"))
                    if (E.isReadyPerfectly())
                        if (Minion.isKillableAndValidTarget(E.GetDamage(Minion)))
                            if (HealthPrediction.GetHealthPrediction(Minion, 500) > 0)
                                if (!HeroManager.Enemies.Any(x => Orbwalking.InAutoAttackRange(x)))
                                    E.Cast();
            }
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
                if (sender.Owner.IsMe)
                    if (args.Slot == SpellSlot.E)
                        if (ELastCastTime > Utils.TickCount - 700)
                            args.Process = false;
                        else
                            ELastCastTime = Utils.TickCount;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender != null)
                if (sender.IsEnemy)
                    if (sender.Type == GameObjectType.obj_AI_Hero)
                        if (MenuProvider.Champion.Misc.getBoolValue("Use Soulbound Saver (With R)"))
                            if (R.isReadyPerfectly())
                            {
                                var soulbound = HeroManager.Allies.FirstOrDefault(x => !x.IsDead && x.HasBuff("kalistacoopstrikeally"));
                                if (soulbound != null)
                                    if (args.Target.NetworkId == soulbound.NetworkId || args.End.Distance(soulbound.Position) <= 200)
                                        if (soulbound.HealthPercent < 20)
                                            R.Cast();
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

                if (MenuProvider.Champion.Drawings.getBoolValue("Draw E Damage Percent"))
                {
                    foreach (var Target in HeroManager.Enemies.Where(x => !x.IsDead && x.IsVisible))
                    {
                        var TargetPos = Drawing.WorldToScreen(Target.Position);
                        var DamagePercent = (E.GetDamage(Target) / Target.Health + Target.PhysicalShield) * 100;

                        if (DamagePercent > 0)
                            Drawing.DrawText(TargetPos.X, TargetPos.Y - 100, DamagePercent >= 100 ? System.Drawing.Color.Red : System.Drawing.Color.GreenYellow, DamagePercent.ToString("0.0"));
                    }
                }
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = E.GetDamage(enemy);

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                damage *= 0.6f;

            if (enemy.HasBuff("FerociousHowl"))
                damage *= 0.3f;

            return E.isReadyPerfectly() ? damage : 0;
        }

        private float GetJungleDamage(Obj_AI_Minion enemy)
        {
            float damage = E.GetDamage(enemy);
            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                damage *= 0.6f;

            BuffInstance dragonSlayerBuff = ObjectManager.Player.GetBuff("s5test_dragonslayerbuff");
            if (dragonSlayerBuff != null)
            {
                if (dragonSlayerBuff.Count >= 4)
                    damage += dragonSlayerBuff.Count == 5 ? damage * 0.30f : damage * 0.15f;

                if (enemy.CharData.BaseSkinName.ToLowerInvariant().Contains("dragon"))
                    damage *= 1 - (dragonSlayerBuff.Count * 0.07f);
            }

            if (enemy.CharData.BaseSkinName.ToLowerInvariant().Contains("baron") && ObjectManager.Player.HasBuff("barontarget"))
                damage *= 0.5f;

            return E.isReadyPerfectly() ? damage : 0;
        }
    }
}
using System.Net.Configuration;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using Aimtec.SDK.Events;

namespace Garen_By_Demavex
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.Util;
    using ZLib;


    using Spell = Aimtec.SDK.Spell;

    internal class Garen
    {
        public static Menu Menu = new Menu("Garen_By_Demavex", "Garen_By_Demavex", true);

        public static Orbwalker Orbwalker = new Orbwalker();

        public static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        public static Spell Q, W, E, R, Flash, Ignite;

        public void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 400);

            if (Player.SpellBook.GetSpell(SpellSlot.Summoner1).SpellData.Name == "SummonerFlash")
                Flash = new Spell(SpellSlot.Summoner1, 425);
            if (Player.SpellBook.GetSpell(SpellSlot.Summoner2).SpellData.Name == "SummonerFlash")
                Flash = new Spell(SpellSlot.Summoner2, 425);
            if (Player.SpellBook.GetSpell(SpellSlot.Summoner1).SpellData.Name == "SummonerDot")
                Ignite = new Spell(SpellSlot.Summoner1, 600);
            if (Player.SpellBook.GetSpell(SpellSlot.Summoner2).SpellData.Name == "SummonerDot")
                Ignite = new Spell(SpellSlot.Summoner2, 600);
        }

        public Garen()
        {
            Orbwalker.Attach(Menu);
            var ComboMenu = new Menu("combo", "Combo");
            {
                ComboMenu.Add(new MenuBool("useq", "Use Q in Combo"));
             //   ComboMenu.Add(new MenuBool("qaa", "Use Q AA Reset"));
             //   ComboMenu.Add(new MenuKeyBind("QAA", "Q AA Toggle", KeyCode.T, KeybindType.Toggle));
                ComboMenu.Add(new MenuBool("usee", "Use E in Combo"));
                ComboMenu.Add(new MenuBool("items", "Use Items"));
            }
            Menu.Add(ComboMenu);
            var HarassMenu = new Menu("harass", "Harass");
            {

                HarassMenu.Add(new MenuBool("useq", "Use Q in Harass"));
             //   HarassMenu.Add(new MenuBool("qaa", "Use Q AA Reset"));
                HarassMenu.Add(new MenuBool("usee", "Use E in Harass"));
            }
            Menu.Add(HarassMenu);
            var FarmMenu = new Menu("farming", "Farming");
            {

                FarmMenu.Add(new MenuBool("useq", "Use Q to Farm"));
                FarmMenu.Add(new MenuBool("usee", "Use E to Farm"));
                FarmMenu.Add(new MenuSlider("hite", "^- If Hits X", 1, 1, 4));
            }
            Menu.Add(FarmMenu);

            var KSMenu = new Menu("killsteal", "Killsteal");
            {
                KSMenu.Add(new MenuBool("ksq", "Killsteal with Q"));
                KSMenu.Add(new MenuBool("kse", "Killsteal with E"));
                KSMenu.Add(new MenuBool("ksr", "Killsteal with R"));
            }
            Menu.Add(KSMenu);

            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
                DrawMenu.Add(new MenuBool("drawtoggle", "Draw Toggle"));
            }
            Menu.Add(DrawMenu);

            var m = new Menu("zlibtest", "ZLibtest", true);
            ZLib.Attach(m);
            m.Attach();



            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            //Orbwalker.PostAttack += OnPostAttack;
            LoadSpells();
            Console.WriteLine("Garen by Demavex - Loaded");
        }


        private void Game_OnUpdate()
        {

            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }
            
            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    OnCombo();
                    break;
                case OrbwalkingMode.Mixed:
                    OnHarass();
                    break;
                case OrbwalkingMode.Laneclear:
                    Clearing();
                    Jungle();
                    break;

            }

            Killsteal();

        }

        /*public void OnPostAttack(object sender, PostAttackEventArgs args)
        {
            var heroTarget = args.Target as Obj_AI_Hero;
            if (Orbwalker.Mode.Equals(OrbwalkingMode.Combo))
            {
                if (!Menu["combo"]["QAA"].Enabled)
                {
                    return;
                }
                if (!Menu["combo"]["qaa"].Enabled)
                {
                    return;
                }
                Obj_AI_Hero hero = args.Target as Obj_AI_Hero;
                if (hero == null || !hero.IsValid || !hero.IsEnemy)
                {
                    return;
                }
                if (Q.Cast())
                {
                    Orbwalker.ResetAutoAttackTimer();
                }


            }


            if (Orbwalker.Mode.Equals(OrbwalkingMode.Mixed))
            {
                if (!Menu["combo"]["QAA"].Enabled)
                {
                    return;
                }
                if (!Menu["harass"]["qaa"].Enabled)
                {
                    return;
                }
                Obj_AI_Hero hero = args.Target as Obj_AI_Hero;
                if (hero == null || !hero.IsValid || !hero.IsEnemy)
                {
                    return;
                }
                if (Q.Cast())
                {
                    Orbwalker.ResetAutoAttackTimer();
                }



            }

    }*/
        private void Render_OnPresent()
            {
                Vector2 maybeworks;
                var heropos = Render.WorldToScreen(Player.Position, out maybeworks);
                var xaOffset = (int)maybeworks.X;
                var yaOffset = (int)maybeworks.Y;

                if (Menu["drawings"]["drawe"].Enabled)
                {
                    Render.Circle(Player.Position, E.Range, 40, Color.CornflowerBlue);
                }
                if (Menu["drawings"]["drawr"].Enabled)
                {
                    Render.Circle(Player.Position, R.Range, 40, Color.Crimson);
                }

               /* if (Menu["drawings"]["drawtoggle"].Enabled)
                {

                    if (Menu["combo"]["QAA"].Enabled)
                    {
                        Render.Text(xaOffset - 50, yaOffset + 50, Color.LimeGreen, "Q AA : ON",
                            RenderTextFlags.VerticalCenter);
                    }
                    if (!Menu["combo"]["QAA"].Enabled)
                    {
                        Render.Text(xaOffset - 50, yaOffset + 50, Color.Red, "Q AA : OFF",
                            RenderTextFlags.VerticalCenter);



                    }
                }*/


            }
        

            public static List<Obj_AI_Minion> GetAllGenericMinionsTargets()
            {
                return GetAllGenericMinionsTargetsInRange(float.MaxValue);
            }

            public static List<Obj_AI_Minion> GetAllGenericMinionsTargetsInRange(float range)
            {
                return GetEnemyLaneMinionsTargetsInRange(range).Concat(GetGenericJungleMinionsTargetsInRange(range))
                    .ToList();
            }

            public static List<Obj_AI_Base> GetAllGenericUnitTargets()
            {
                return GetAllGenericUnitTargetsInRange(float.MaxValue);
            }

            public static List<Obj_AI_Base> GetAllGenericUnitTargetsInRange(float range)
            {
                return GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(range))
                    .Concat<Obj_AI_Base>(GetAllGenericMinionsTargetsInRange(range)).ToList();
            }


            public static List<Obj_AI_Minion> GetEnemyLaneMinionsTargets()
            {
                return GetEnemyLaneMinionsTargetsInRange(float.MaxValue);
            }

            public static List<Obj_AI_Minion> GetEnemyLaneMinionsTargetsInRange(float range)
            {
                return GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range)).ToList();
            }


            private void Clearing()
            {
                bool useQ = Menu["farming"]["useq"].Enabled;
                bool useE = Menu["farming"]["usee"].Enabled;
                float hits = Menu["farming"]["hite"].As<MenuSlider>().Value;


                foreach (var minion in GetEnemyLaneMinionsTargetsInRange(E.Range))
                {

                    if (useE && minion.IsValidTarget(E.Range) && GameObjects.EnemyMinions.Count(h => h.IsValidTarget(
                                325, false, false,
                                minion.ServerPosition)) >= hits)
                    {
                    if (Player.HasBuff("Judgment") == false)
                    { 
                        E.Cast();
                    }
                }
                    if (useQ && minion.IsValidTarget(150) && (Player.GetSpellDamage(minion, SpellSlot.Q)) > minion.Health)
                    {
                        if (Q.Cast())
                        {
                        Orbwalker.ForceTarget(minion);
                        }
                    }

                }
            }


            public static List<Obj_AI_Minion> GetGenericJungleMinionsTargets()
            {
                return GetGenericJungleMinionsTargetsInRange(float.MaxValue);
            }

            public static List<Obj_AI_Minion> GetGenericJungleMinionsTargetsInRange(float range)
            {
                return GameObjects.Jungle.Where(m => !GameObjects.JungleSmall.Contains(m) && m.IsValidTarget(range))
                    .ToList();
            }

            private void Jungle()
            {
                foreach (var jungleTarget in GameObjects.Jungle.Where(m => m.IsValidTarget(Q.Range)).ToList())
                {
                    if (!jungleTarget.IsValidTarget() || !jungleTarget.IsValidSpellTarget())
                    {
                        return;
                    }
                    bool useQ = Menu["farming"]["useq"].Enabled;
                    bool useE = Menu["farming"]["usee"].Enabled;
                    float hits = Menu["farming"]["hitq"].As<MenuSlider>().Value;

                if (useQ && jungleTarget.IsValidTarget(Q.Range) && GameObjects.Jungle.Count(
                        h => h.IsValidTarget(300, false, false,
                            jungleTarget.ServerPosition)) >= hits)
                {
                    if (Q.Cast())
                    {
                        Orbwalker.ForceTarget(jungleTarget);
                    }
                    }
                    if (useE && jungleTarget.IsValidTarget(E.Range))
                    {
                    if (Player.HasBuff("Judgment") == false)
                    {
                        E.Cast();
                    }
                }


                }
            }


        public static Obj_AI_Hero GetBestKillableHero(Spell spell, DamageType damageType = DamageType.True,
            bool ignoreShields = false)
        {
            return TargetSelector.Implementation.GetOrderedTargets(spell.Range).FirstOrDefault(t => t.IsValidTarget());
        }


        private void Killsteal()
        {
            if (Q.Ready &&
                Menu["killsteal"]["ksq"].Enabled)
            {
                var bestTarget = GetBestKillableHero(Q, DamageType.Physical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.Q) >= bestTarget.Health &&
                    bestTarget.IsValidTarget(Q.Range))
                {
                    if (Q.Cast())
                    {
                        Orbwalker.ForceTarget(bestTarget);
                    }
                }
            }
            if (E.Ready &&
                Menu["killsteal"]["kse"].Enabled)
            {
                var bestTarget = GetBestKillableHero(E, DamageType.Physical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.E) >= bestTarget.Health &&
                    bestTarget.IsValidTarget(E.Range))
                {
                    if (Player.HasBuff("Judgment") == false)
                    {
                        E.Cast();
                    }
                }
            }
            if (R.Ready &&
                Menu["killsteal"]["ksr"].Enabled)
            {
                var bestTarget = GetBestKillableHero(R, DamageType.Physical, false);
                if (bestTarget != null &&
                    Player.GetSpellDamage(bestTarget, SpellSlot.R) >= bestTarget.Health &&
                    bestTarget.IsValidTarget(R.Range))
                {
                    R.CastOnUnit(bestTarget);
                }
            }
        }


        public static Obj_AI_Hero GetBestEnemyHeroTarget()
            {
                return GetBestEnemyHeroTargetInRange(float.MaxValue);
            }

            public static Obj_AI_Hero GetBestEnemyHeroTargetInRange(float range)
            {
                var ts = TargetSelector.Implementation;
                var target = ts.GetTarget(range);
                if (target != null && target.IsValidTarget() && !Invulnerable.Check(target))
                {
                    return target;
                }

                var firstTarget = ts.GetOrderedTargets(range)
                    .FirstOrDefault(t => t.IsValidTarget() && !Invulnerable.Check(t));
                if (firstTarget != null)
                {
                    return firstTarget;
                }

                return null;
            }
            private void OnCombo()
            {

                bool useQ = Menu["combo"]["useq"].Enabled;
                bool useE = Menu["combo"]["usee"].Enabled;
               
                var target = GetBestEnemyHeroTargetInRange(1200);


                if (!target.IsValidTarget())
                {
                    return;
                }

                if (useQ && target.IsValidTarget(600) && target != null)
                {
                    if (Q.Cast())
                    {
                    Orbwalker.ForceTarget(target);
                    }   
            }
                if (useE && target.IsValidTarget(E.Range) && target != null)
                {
                    if (Player.HasBuff("Judgment") == false)
                    {
                        E.Cast();
                    }
            }
            
                                
        }



        private void OnHarass()
        {

            bool useQ = Menu["harass"]["useq"].Enabled;
            bool useE = Menu["harass"]["usee"].Enabled;
            var target = GetBestEnemyHeroTargetInRange(Q.Range);
           
            if (!target.IsValidTarget())
                {
                    return;
                }

                if (useQ && target != null)
                {
                    if (target.IsValidTarget(500))
                    {
                    if (Q.Cast())
                    {
                        Orbwalker.ForceTarget(target);
                    }
                }
                }
                if (useE && target != null)
                {
                    if (target.IsValidTarget(E.Range))
                    {
                    if (Player.HasBuff("Judgment") == false)
                    {
                        E.Cast();
                    }
                }
                }

            }
        }
    }

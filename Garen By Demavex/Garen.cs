﻿using System.Net.Configuration;
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
    using ZLib.Base;
    using ZLib.Handlers;

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
                ComboMenu.Add(new MenuBool("usee", "Use E in Combo"));
               // ComboMenu.Add(new MenuBool("items", "Use Items"));
            }
            Menu.Add(ComboMenu);
            var HarassMenu = new Menu("harass", "Harass");
            {

                HarassMenu.Add(new MenuBool("useq", "Use Q in Harass"));
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

            var WMenu = new Menu("wsettings", "W Settings");
            {
                WMenu.Add(new MenuBool("usew", "Enable W usage"));
                WMenu.Add(new MenuSlider("wdmg", "^- If Incoming Damage is X% of Max Health", 10, 0, 90));
 
            }
            Menu.Add(WMenu);

            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawe", "Draw E Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
                DrawMenu.Add(new MenuBool("drawtoggle", "Draw Toggle"));
            }
            Menu.Add(DrawMenu);

           
            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            LoadSpells();
            Console.WriteLine("Garen by Demavex - Loaded");

            var m = new Menu("zlibtest", "ZLibtest", true);
            ZLib.Attach(m);
            m.Attach();

            ZLib.OnPredictDamage += ZLib_OnPredictDamage;
        }

        private static void ZLib_OnPredictDamage(Unit unit, PredictDamageEventArgs args)
        {
            bool useW = Menu["wsettings"]["usew"].Enabled;

            if (!unit.Instance.IsMe || !useW)
            {
                return;
            }


            if (unit.Instance.HasBuffOfType(BuffType.Invulnerability))
            {
                args.NoProcess = true;
            }


            var objShop = ObjectManager.Get<GameObject>()
                .FirstOrDefault(x => x.Type == GameObjectType.obj_Shop && x.Team == unit.Instance.Team);

            if (objShop != null
                && objShop.Distance(unit.Instance.ServerPosition) <= 1250)
            {
                args.NoProcess = true;
            }

            var incomingDamagePercent = unit.IncomeDamage / unit.Instance.MaxHealth * 100;
            float whp = Menu["wsettings"]["wdmg"].As<MenuSlider>().Value;

             
            if (unit.IncomeDamage >= unit.Instance.Health || incomingDamagePercent >= whp || unit.Events.Contains(EventType.CrowdControl) || unit.Events.Contains(EventType.Ultimate))
                {
                if (unit.Instance.IsMe)
                {
                    W.Cast();
                }

            }
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

                    if (E.Ready && useE && minion.IsValidTarget(E.Range) && (GetEnemyLaneMinionsTargetsInRange(E.Range).Count >= hits))
                    {
                    if(!Player.HasBuff("GarenE") && !Player.HasBuff("GarenQ"))
                    {
                        E.Cast();
                    }
                }
                    if (Q.Ready && useQ && minion.IsValidTarget(250) && (Player.GetSpellDamage(minion, SpellSlot.Q)) > minion.Health)
                    {
                    if (!Player.HasBuff("GarenE"))
                    {
                          if ( Player.GetAutoAttackDamage(minion) <= minion.Health || !Orbwalker.CanAttack() )
                        {
                            Q.Cast();                                               
                        }
                        
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
            foreach (var jungleTarget in GameObjects.Jungle.Where(m => m.IsValidTarget(E.Range)).ToList())
            {
                if (!jungleTarget.IsValidTarget() || !jungleTarget.IsValidSpellTarget())
                {
                    return;
                }
                bool useQ = Menu["farming"]["useq"].Enabled;
                bool useE = Menu["farming"]["usee"].Enabled;
               // float hits = Menu["farming"]["hite"].As<MenuSlider>().Value;
                
                if (E.Ready && useE && jungleTarget.IsValidTarget(E.Range))
                {
                    if (!Player.HasBuff("GarenE") && !Player.HasBuff("GarenQ"))
                    {
                        E.Cast();
                    }
                }
                if (Q.Ready && useQ && jungleTarget.IsValidTarget(100))
                {
                    if (!Player.HasBuff("GarenE"))
                    {
                        Q.Cast();
                                            }
                }

            }
    
            }


        public static Obj_AI_Hero GetBestKillableHero(Spell spell, DamageType damageType = DamageType.True,
            bool ignoreShields = false)
        {
            return TargetSelector.Implementation.GetOrderedTargets(spell.Range).FirstOrDefault(t => t.IsValidTarget());
        }

        private static float RDamage(Obj_AI_Base target)
        {
            if (target.HasBuff("garenpassiveenemytarget"))
            {
                return (float)Player.CalculateDamage(target, DamageType.True, Damage.GetSpellDamage(Player, target, SpellSlot.R));
            }

            return (float)Player.CalculateDamage(target, DamageType.Magical, Damage.GetSpellDamage(Player, target, SpellSlot.R));
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
                    Q.Cast();                    
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
                    if (!Player.HasBuff("GarenE"))
                    {
                        E.Cast();
                    }
                }
            }
            if (R.Ready &&
                Menu["killsteal"]["ksr"].Enabled)
            {
                var bestTarget = GetBestKillableHero(R, DamageType.Magical, false);
                if (bestTarget != null &&
                    RDamage(bestTarget) >= bestTarget.Health &&
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

                if (Q.Ready && useQ && target.IsValidTarget(800) && target != null)
                {
                Q.Cast();
                    
            }
                if (E.Ready && useE && target.IsValidTarget(E.Range) && target != null)
                {
                if (!Player.HasBuff("GarenE") && !Player.HasBuff("GarenQ"))
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

            if (Q.Ready && useQ && target.IsValidTarget(500) && target != null)
            {
                Q.Cast();
                
            }
            if (E.Ready && useE && target.IsValidTarget(E.Range) && target != null)
            {
                if (!Player.HasBuff("GarenE") && !Player.HasBuff("GarenQ"))
                {
                    E.Cast();
                }
            }

        }


    }


    }


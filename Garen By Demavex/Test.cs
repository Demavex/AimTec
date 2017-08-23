using System.Net.Configuration;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using Aimtec.SDK.Events;

namespace Master_Yi_By_Kornis
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


    using Spell = Aimtec.SDK.Spell;

    internal class MasterYi
    {
        public static Menu Menu = new Menu("Master Y By Kornis", "Master Yi by Kornis", true);

        public static Orbwalker Orbwalker = new Orbwalker();

        public static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        public static Spell Q, W, E, R, Smites;

        public void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 300);
            R = new Spell(SpellSlot.R, 0);

            var smiteSlot = Player.SpellBook.Spells.FirstOrDefault(x => x.SpellData.Name.ToLower().Contains("smite"));
            if (smiteSlot != null)
            {

                Smites = new Spell(smiteSlot.Slot, 700);

            }
        }

        public MasterYi()
        {
            Orbwalker.Attach(Menu);
            var ComboMenu = new Menu("combo", "Combo");
            {
                ComboMenu.Add(new MenuBool("useq", "Use Q in Combo"));
                ComboMenu.Add(new MenuList("qmode", "Q Mode", new[] { "Always", "Smart" }, 1));
                ComboMenu.Add(new MenuBool("waa", "Use W AA Reset"));
                ComboMenu.Add(new MenuKeyBind("WAA", "W AA Toggle", KeyCode.T, KeybindType.Toggle));
                ComboMenu.Add(new MenuBool("usee", "Use E in Combo"));
                ComboMenu.Add(new MenuBool("user", "Use R in Combo"));
                ComboMenu.Add(new MenuBool("items", "Use Items"));
            }
            Menu.Add(ComboMenu);
            var HarassMenu = new Menu("harass", "Harass");
            {
                HarassMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                HarassMenu.Add(new MenuBool("useq", "Use Q in Harass"));
                HarassMenu.Add(new MenuBool("waa", "Use W AA Reset"));
                HarassMenu.Add(new MenuBool("usee", "Use E in Harass"));
            }
            Menu.Add(HarassMenu);
            var FarmMenu = new Menu("farming", "Farming");
            {
                FarmMenu.Add(new MenuSlider("mana", "Mana Manager", 50));
                FarmMenu.Add(new MenuBool("useq", "Use Q to Farm"));
                FarmMenu.Add(new MenuSlider("hitq", "^- If Hits X", 1, 1, 4));
                FarmMenu.Add(new MenuBool("usee", "Use E to Farm"));
            }
            Menu.Add(FarmMenu);
            var SmiteMenu = new Menu("smite", "Smite Settings");
            {
                SmiteMenu.Add(new MenuBool("SmiteUse", "Use Smite on Monsters"));
                SmiteMenu.Add(new MenuBool("SmiteUseHeroes", "Use Smite on Champions"));
                SmiteMenu.Add(new MenuKeyBind("smitekey", "Smite Toggle", KeyCode.Z, KeybindType.Toggle));
            }
            Menu.Add(SmiteMenu);
            var DrawMenu = new Menu("drawings", "Drawings");
            {
                DrawMenu.Add(new MenuBool("drawq", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("drawr", "Draw R Range"));
                DrawMenu.Add(new MenuBool("drawsmite", "Draw Smite"));
                DrawMenu.Add(new MenuBool("drawtoggle", "Draw Toggle"));
                DrawMenu.Add(new MenuBool("drawdamage", "Draw Damage"));
                DrawMenu.Add(new MenuSlider("aa", "^- Include X AA Damage", 3, 1, 10));
            }
            Menu.Add(DrawMenu);
            var MenuSupportedSpells = new Menu("spells", "Q Dodge Spells");
            MenuSupportedSpells.Add(new MenuBool("enable", "Enabled"));
            MenuSupportedSpells.Add(new MenuBool("targeted", "Enable Targeted Spells"));
            MenuSupportedSpells.Add(new MenuSlider("hp", "If my HP <=", 80, 1, 100));
            foreach (var xEnemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                Obj_AI_Hero enemy = xEnemy;
                foreach (var ccList in SpellData.Spells.Where(xList => xList.charName == enemy.ChampionName))
                {
                    MenuSupportedSpells.Add(new MenuBool(ccList.spellName, ccList.charName + " : " + ccList.name));

                }

            }
            Menu.Add(MenuSupportedSpells);

            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.PostAttack += OnPostAttack;
            LoadSpells();
            Console.WriteLine("Master Yi by Kornis - Loaded");
        }

        public static readonly List<string> SpecialChampions = new List<string> { "Annie", "Jhin" };

        private static readonly string[] SmiteMobs =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon_Water", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air",
            "SRU_Dragon_Elder", "SRU_Baron", "SRU_RiftHerald"
        };



        private void Game_OnUpdate()
        {

            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }
            SmiteUse();
            if (Player.HasBuff("Meditate") && Menu["combo"]["waa"].Enabled &&
                (Orbwalker.Mode.Equals(OrbwalkingMode.Combo) || Orbwalker.Mode.Equals(OrbwalkingMode.Mixed)))
            {
                var target = GetBestEnemyHeroTarget();


                if (!target.IsValidTarget())
                {
                    return;
                }

                if (target.Distance(Player) < 300)
                {
                    Player.IssueOrder(OrderType.AttackTo, target);
                }
                if (target.Distance(Player) > 300)
                {
                    Player.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                }


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


        }

        public void OnPostAttack(object sender, PostAttackEventArgs args)
        {
            var heroTarget = args.Target as Obj_AI_Hero;
            if (Orbwalker.Mode.Equals(OrbwalkingMode.Combo))
            {
                if (!Menu["combo"]["WAA"].Enabled)
                {
                    return;
                }
                if (!Menu["combo"]["waa"].Enabled)
                {
                    return;
                }
                Obj_AI_Hero hero = args.Target as Obj_AI_Hero;
                if (hero == null || !hero.IsValid || !hero.IsEnemy)
                {
                    return;
                }
                if (W.Cast())
                {
                    Orbwalker.ResetAutoAttackTimer();
                }

                if (Menu["combo"]["items"].Enabled)
                {

                    if (Player.HasBuff("doublestrike"))
                    {

                        if (Player.HasItem(ItemId.TitanicHydra) || Player.HasItem(ItemId.Tiamat) ||
                            Player.HasItem(ItemId.RavenousHydra))
                        {
                            var items = new[] { ItemId.TitanicHydra, ItemId.Tiamat, ItemId.RavenousHydra };
                            var slot = Player.Inventory.Slots.First(s => items.Contains(s.ItemId));
                            if (slot != null)
                            {
                                var spellslot = slot.SpellSlot;
                                if (spellslot != SpellSlot.Unknown &&
                                    Player.SpellBook.GetSpell(spellslot).State == SpellState.Ready)
                                {
                                    Player.SpellBook.CastSpell(spellslot);
                                }
                            }
                        }
                    }
                }
            }


            if (Orbwalker.Mode.Equals(OrbwalkingMode.Mixed))
            {
                if (!Menu["combo"]["WAA"].Enabled)
                {
                    return;
                }
                if (!Menu["harass"]["waa"].Enabled)
                {
                    return;
                }
                Obj_AI_Hero hero = args.Target as Obj_AI_Hero;
                if (hero == null || !hero.IsValid || !hero.IsEnemy)
                {
                    return;
                }
                if (W.Cast())
                {
                    Orbwalker.ResetAutoAttackTimer();
                }


                if (Player.HasBuff("doublestrike"))
                {
                    if (Menu["combo"]["items"].Enabled)
                    {
                        if (Player.HasItem(ItemId.TitanicHydra) || Player.HasItem(ItemId.Tiamat) ||
                            Player.HasItem(ItemId.RavenousHydra))
                        {
                            var items = new[] { ItemId.TitanicHydra, ItemId.Tiamat, ItemId.RavenousHydra };
                            var slot = Player.Inventory.Slots.First(s => items.Contains(s.ItemId));
                            if (slot != null)
                            {
                                var spellslot = slot.SpellSlot;
                                if (spellslot != SpellSlot.Unknown &&
                                    Player.SpellBook.GetSpell(spellslot).State == SpellState.Ready)
                                {
                                    Player.SpellBook.CastSpell(spellslot);
                                }
                            }
                        }
                    }
                }

            }
        }



        private static int SmiteDamages
        {
            get
            {
                int[] Hello = new int[]
                    {390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000};

                return Hello[Player.Level - 1];
            }
        }

        public static void SmiteUse()
        {

            if (Menu["smite"]["smitekey"].Enabled)
            {
                if (Smites != null)
                {
                    if (Menu["smite"]["SmiteUse"].Enabled)
                    {
                        var minion = GameObjects.Jungle.Where(x => x.IsValidTarget(Smites.Range));
                        foreach (var m in minion)
                        {
                            if (m != null && SmiteMobs.Contains(m.UnitSkinName))
                            {

                                if (m.Distance(Player) <= Smites.Range)
                                {


                                    if (SmiteDamages >= m.Health)
                                    {

                                        Smites.Cast(m);

                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        public static int SxOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 1 : 10;
        }

        public static int SyOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 3 : 20;
        }

        private void Render_OnPresent()
        {
            Vector2 maybeworks;
            var heropos = Render.WorldToScreen(Player.Position, out maybeworks);
            var xaOffset = (int)maybeworks.X;
            var yaOffset = (int)maybeworks.Y;

            if (Menu["drawings"]["drawq"].Enabled)
            {
                Render.Circle(Player.Position, Q.Range, 40, Color.CornflowerBlue);
            }
            if (Menu["drawings"]["drawr"].Enabled)
            {
                Render.Circle(Player.Position, 1200, 40, Color.Crimson);
            }
            if (Menu["drawings"]["drawsmite"].Enabled)
            {

                if (Menu["smite"]["smitekey"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 10, Color.LimeGreen, "Smite: ON",
                        RenderTextFlags.VerticalCenter);
                }
                if (!Menu["smite"]["smitekey"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 10, Color.Red, "Smite:  OFF",
                        RenderTextFlags.VerticalCenter);



                }
            }
            if (Menu["drawings"]["drawtoggle"].Enabled)
            {

                if (Menu["combo"]["WAA"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 50, Color.LimeGreen, "W AA : ON",
                        RenderTextFlags.VerticalCenter);
                }
                if (!Menu["combo"]["WAA"].Enabled)
                {
                    Render.Text(xaOffset - 50, yaOffset + 50, Color.Red, "W AA : OFF",
                        RenderTextFlags.VerticalCenter);



                }
            }
            if (Menu["drawings"]["drawdamage"].Enabled)
            {

                ObjectManager.Get<Obj_AI_Base>()
                    .Where(h => h is Obj_AI_Hero && h.IsValidTarget() && h.IsValidTarget(Q.Range * 2))
                    .ToList()
                    .ForEach(
                        unit =>
                        {

                            var heroUnit = unit as Obj_AI_Hero;
                            int width = 103;
                            int height = 8;
                            int xOffset = SxOffset(heroUnit);
                            int yOffset = SyOffset(heroUnit);
                            var barPos = unit.FloatingHealthBarPosition;
                            barPos.X += xOffset;
                            barPos.Y += yOffset;
                            var drawEndXPos = barPos.X + width * (unit.HealthPercent() / 100);
                            var drawStartXPos =
                                (float)(barPos.X + (unit.Health >
                                                     Player.GetSpellDamage(unit, SpellSlot.Q) +
                                                     Player.GetAutoAttackDamage(unit) *
                                                     Menu["drawings"]["aa"].As<MenuSlider>().Value
                                             ? width * ((unit.Health - (Player.GetSpellDamage(unit, SpellSlot.Q) +
                                                                        Player.GetAutoAttackDamage(unit) *
                                                                        Menu["drawings"]["aa"].As<MenuSlider>()
                                                                            .Value)) /
                                                        unit.MaxHealth * 100 / 100)
                                             : 0));

                            Render.Line(drawStartXPos, barPos.Y, drawEndXPos, barPos.Y, 8, true,
                                unit.Health < Player.GetSpellDamage(unit, SpellSlot.Q) +
                                Player.GetAutoAttackDamage(unit) * Menu["drawings"]["aa"].As<MenuSlider>().Value
                                    ? Color.GreenYellow
                                    : Color.Orange);

                        });
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
            float hits = Menu["farming"]["hitq"].As<MenuSlider>().Value;
            float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;

            foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
            {


                if (manapercent < Player.ManaPercent())
                {
                    if (useQ && minion.IsValidTarget(Q.Range) && GameObjects.EnemyMinions.Count(h => h.IsValidTarget(
                            600, false, false,
                            minion.ServerPosition)) >= hits)
                    {
                        Q.CastOnUnit(minion);
                    }
                    if (useE && minion.IsValidTarget(300))
                    {
                        E.Cast();
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
                float manapercent = Menu["farming"]["mana"].As<MenuSlider>().Value;
                if (manapercent < Player.ManaPercent())
                {
                    if (useQ && jungleTarget.IsValidTarget(Q.Range) && GameObjects.Jungle.Count(
                            h => h.IsValidTarget(600, false, false,
                                jungleTarget.ServerPosition)) >= hits)
                    {
                        Q.CastOnUnit(jungleTarget);
                    }
                    if (useE && jungleTarget.IsValidTarget(300))
                    {
                        E.Cast();
                    }
                }

            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs args)
        {
            if (Menu["spells"]["enable"].Enabled && Menu["spells"]["hp"].As<MenuSlider>().Value >= Player.HealthPercent())
            {
                if (sender.IsEnemy && sender.IsHero)
                {

                    if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy)
                    {
                        if (Menu["spells"]["targeted"].Enabled)
                        {
                            if (args.Target == Player && !args.SpellData.Name.Contains("BasicAttack") && !args.Sender.IsUnderEnemyTurret())
                            {
                                Console.WriteLine("SkillShotType.Targeted");
                                if (Q.Ready)
                                {
                                    var t = TargetSelector.GetTarget(Q.Range);
                                    if (t.IsValidTarget())
                                    {
                                        Q.CastOnUnit(t);

                                    }
                                    else

                                        foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                                        {
                                            if (minion != null)
                                            {
                                                Q.CastOnUnit(minion);
                                            }
                                        }

                                }
                            }
                        }
                        foreach (var spell in SpellData.Spells.Where(xList => xList.charName == sender.UnitSkinName))
                        {

                            if (args.SpellData.Name == spell.spellName && Menu["spells"][args.SpellData.Name].Enabled)
                            {

                                switch (spell.spellType)
                                {

                                    case SpellType.Circular:
                                        if (Player.Distance(args.End) <= 300f)
                                        {

                                            Console.WriteLine("SkillShotType.SkillshotCircle");
                                            if (Q.Ready)
                                            {
                                                var t = TargetSelector.GetTarget(Q.Range);
                                                if (t.IsValidTarget())
                                                {
                                                    Q.CastOnUnit(t);

                                                }
                                                else

                                                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                                                    {
                                                        if (minion != null)
                                                        {
                                                            Q.CastOnUnit(minion);
                                                        }
                                                    }

                                            }

                                        }
                                        break;
                                    case SpellType.Line:
                                        if (Player.Distance(args.End) <= 150f)
                                        {
                                            Console.WriteLine("SkillShotType.SkillshotLine");
                                            if (Q.Ready)
                                            {
                                                var t = TargetSelector.GetTarget(Q.Range);
                                                if (t.IsValidTarget())
                                                {
                                                    Q.CastOnUnit(t);
                                                }
                                                else

                                                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                                                    {
                                                        if (minion != null)
                                                        {
                                                            Q.CastOnUnit(minion);
                                                        }
                                                    }

                                            }
                                        }
                                        break;
                                    case SpellType.Cone:
                                        if (Player.Distance(args.End) <= 300f)
                                        {
                                            Console.WriteLine("SkillShotType.SkillshotCone");
                                            if (Q.Ready)
                                            {
                                                var t = TargetSelector.GetTarget(Q.Range);
                                                if (t.IsValidTarget())
                                                {
                                                    Q.CastOnUnit(t);
                                                }
                                                else

                                                    foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                                                    {
                                                        if (minion != null)
                                                        {
                                                            Q.CastOnUnit(minion);
                                                        }
                                                    }

                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
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
            bool useR = Menu["combo"]["user"].Enabled;
            var target = GetBestEnemyHeroTargetInRange(1200);


            if (!target.IsValidTarget())
            {
                return;
            }
            if (Menu["smite"]["smitekey"].Enabled)
            {
                if (Smites != null)
                {
                    if (Menu["smite"]["SmiteUseHeroes"].Enabled)
                    {

                        if (target.IsValidTarget(Smites.Range) && target != null)
                        {
                            Smites.CastOnUnit(target);
                        }
                    }
                }
            }
            var items = new[] { ItemId.BladeoftheRuinedKing, ItemId.BilgewaterCutlass };
            if (Player.HasItem(ItemId.BladeoftheRuinedKing) || Player.HasItem(ItemId.BilgewaterCutlass))
            {
                var slot = Player.Inventory.Slots.First(s => items.Contains(s.ItemId));
                if (slot != null)
                {
                    var spellslot = slot.SpellSlot;
                    if (spellslot != SpellSlot.Unknown &&
                        Player.SpellBook.GetSpell(spellslot).State == SpellState.Ready)
                    {
                        Player.SpellBook.CastSpell(spellslot, target);
                    }
                }
            }
            switch (Menu["combo"]["qmode"].As<MenuList>().Value)
            {
                case 0:
                    if (useQ && target.IsValidTarget(Q.Range) && target != null)
                    {
                        Q.CastOnUnit(target);
                    }
                    if (useE && target != null)
                    {
                        if (target.IsValidTarget(300))
                        {
                            E.Cast();
                        }
                    }
                    if (useR && target != null && target.IsValidTarget(1200) && target.Distance(Player) > Q.Range)
                    {
                        R.Cast();
                    }
                    break;
                case 1:
                    if (useQ && target.IsValidTarget(Q.Range) && target != null)
                    {
                        if (target.IsDashing())
                        {
                            Q.CastOnUnit(target);
                        }
                        if (target.HealthPercent() <= 30)
                        {
                            Q.CastOnUnit(target);
                        }
                        if (Player.HealthPercent() <= 30)
                        {
                            Q.CastOnUnit(target);
                        }
                        if (Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health)
                        {
                            Q.CastOnUnit(target);
                        }
                        if (target.Distance(Player) > 400 && target.Distance(Player) < Q.Range)
                        {
                            Q.CastOnUnit(target);
                        }
                    }
                    if (useE && target != null)
                    {
                        if (target.IsValidTarget(300))
                        {
                            E.Cast();
                        }
                    }
                    if (useR && target != null && target.IsValidTarget(1200) && target.Distance(Player) > Q.Range)
                    {
                        R.Cast();
                    }
                    break;
            }
        }



        private void OnHarass()
        {

            bool useQ = Menu["harass"]["useq"].Enabled;
            bool useE = Menu["harass"]["usee"].Enabled;
            var target = GetBestEnemyHeroTargetInRange(Q.Range);
            float manapercent = Menu["harass"]["mana"].As<MenuSlider>().Value;
            if (manapercent < Player.ManaPercent())
            {
                if (!target.IsValidTarget())
                {
                    return;
                }

                if (useQ && target != null)
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target);
                    }
                }
                if (useE && target != null)
                {
                    if (target.IsValidTarget(300))
                    {
                        E.Cast();
                    }
                }

            }
        }
    }
}
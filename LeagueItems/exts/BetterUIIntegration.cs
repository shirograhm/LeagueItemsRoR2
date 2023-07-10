using RoR2;
using BepInEx.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace LeagueItems
{
    internal static class BetterUIIntegration
    {
        internal static void Init()
        {
            RoR2Application.onLoad += BetterUIItemStats.RegisterItemStats;
        }

        public static class BetterUIItemStats
        {
            public static void RegisterItemStats()
            {
                // Bloodthirster
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Bloodthirster.itemDef,
                    nameToken: "ITEMSTATS_BT_LIFESTEAL",
                    Bloodthirster.bonusLifestealPercent,
                    Bloodthirster.bonusLifestealPercent,
                    
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Healing
                );
                // BotRK
                BetterUI.ItemStats.RegisterStat(
                    itemDef: BladeOfTheRuinedKing.itemDef,
                    "ITEMSTATS_BOTRK_PERCENT",
                    BladeOfTheRuinedKing.onHitDamagePercent,
                    BladeOfTheRuinedKing.onHitDamagePercent,
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // Dead Man's Plate
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeadMansPlate.itemDef,
                    "ITEMSTATS_DEADMANS_DAMAGE",
                    DeadMansPlate.bonusDamagePerItemStackPercent,
                    DeadMansPlate.bonusDamagePerItemStackPercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // NashorsTooth
                BetterUI.ItemStats.RegisterStat(
                    itemDef: NashorsTooth.itemDef,
                    nameToken: "ITEMSTATS_NASHORS_DAMAGE",
                    NashorsTooth.onHitDamageAmount,
                    NashorsTooth.onHitDamageAmount,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Damage,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // SpearOfShojin
                BetterUI.ItemStats.RegisterStat(
                    itemDef: SpearOfShojin.itemDef,
                    nameToken: "ITEMSTATS_SHOJIN_COOLDOWN",
                    -SpearOfShojin.cdrFromDamagePercent,
                    -SpearOfShojin.cdrFromDamagePercent,
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.SkillCooldown
                );
                // TitanicHydra
                BetterUI.ItemStats.RegisterStat(
                    itemDef: TitanicHydra.itemDef,
                    "Bonus Base Damage",
                    TitanicHydra.firstStackBonusPercent,
                    TitanicHydra.extraStackBonusPercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                // WarmogsArmor
                BetterUI.ItemStats.RegisterStat(
                    itemDef: WarmogsArmor.itemDef,
                    "ITEMSTATS_WARMOGS_HEALTH",
                    WarmogsArmor.bonusHealthIncreasePercent,
                    WarmogsArmor.bonusHealthIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
            }
        }
    }
}
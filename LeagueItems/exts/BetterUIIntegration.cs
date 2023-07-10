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
                    "Lifesteal On Hit",
                    Bloodthirster.bonusLifestealPercent,
                    Bloodthirster.bonusLifestealPercent,
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Healing
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Bloodthirster.itemDef,
                    "Total Healing Done",
                    1f,
                    1f,
                    statFormatter: BloodthirsterTotalFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Healing
                );
                // BotRK
                BetterUI.ItemStats.RegisterStat(
                    itemDef: BladeOfTheRuinedKing.itemDef,
                    "Current Health Damage",
                    BladeOfTheRuinedKing.onHitDamagePercent,
                    BladeOfTheRuinedKing.onHitDamagePercent,
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: BladeOfTheRuinedKing.itemDef,
                    "Total Damage Dealt",
                    1f,
                    1f,
                    statFormatter: BotrkTotalFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // Dead Man's Plate
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeadMansPlate.itemDef,
                    "Proc Damage",
                    DeadMansPlate.bonusDamagePerItemStackPercent,
                    DeadMansPlate.bonusDamagePerItemStackPercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeadMansPlate.itemDef,
                    "Total Damage Dealt",
                    1f,
                    1f,
                    statFormatter: DeadMansTotalFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // Nashor's Tooth
                BetterUI.ItemStats.RegisterStat(
                    itemDef: NashorsTooth.itemDef,
                    "On-Hit Damage",
                    1f,
                    1f,
                    statFormatter: NashorsFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: NashorsTooth.itemDef,
                    "Total Damage Dealt",
                    1f,
                    1f,
                    statFormatter: NashorsTotalFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                // Spear Of Shojin
                BetterUI.ItemStats.RegisterStat(
                    itemDef: SpearOfShojin.itemDef,
                    "Bonus Cooldown Reduction",
                    1f,
                    1f,
                    statFormatter: SpearOfShojinFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.SkillCooldown
                );
                // Titanic Hydra
                BetterUI.ItemStats.RegisterStat(
                    itemDef: TitanicHydra.itemDef,
                    "Bonus Base Damage",
                    1f,
                    1f,
                    statFormatter: TitanicDamageFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                // Warmog's Armor
                BetterUI.ItemStats.RegisterStat(
                    itemDef: WarmogsArmor.itemDef,
                    "Bonus Base Health",
                    1f,
                    1f,
                    statFormatter: WarmogsHealthFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
            }

            public static BetterUI.ItemStats.StatFormatter BloodthirsterTotalFormatter = new BetterUI.ItemStats.StatFormatter()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float totalHealing = Bloodthirster.totalHealingDone.TryGetValue(master.netId, out float _) ? Bloodthirster.totalHealingDone[master.netId] : 0f;
                    
                    string valueHealingText = totalHealing == 0 ? "0" : String.Format("{0:#}", totalHealing);

                    sb.AppendFormat(valueHealingText);
                }
            };
            
            public static BetterUI.ItemStats.StatFormatter BotrkTotalFormatter = new BetterUI.ItemStats.StatFormatter()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    var totalDamage = BladeOfTheRuinedKing.totalDamageDone.TryGetValue(master.netId, out float _) ? BladeOfTheRuinedKing.totalDamageDone[master.netId] : 0f;

                    string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                    sb.AppendFormat(valueDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter DeadMansTotalFormatter = new BetterUI.ItemStats.StatFormatter()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    var totalDamage = DeadMansPlate.totalDamageDealt.TryGetValue(master.netId, out float _) ? DeadMansPlate.totalDamageDealt[master.netId] : 0f;

                    string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                    sb.AppendFormat(valueDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter NashorsFormatter = new BetterUI.ItemStats.StatFormatter()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float damageOnHit = NashorsTooth.onHitDamageAmount * master.GetBody().level * value;

                    string valueDamageOnHitText = damageOnHit == 0 ? "0" : String.Format("{0:#}", damageOnHit);

                    sb.AppendFormat(valueDamageOnHitText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter NashorsTotalFormatter = new BetterUI.ItemStats.StatFormatter()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float totalDamage = NashorsTooth.totalDamageDone.TryGetValue(master.netId, out float _) ? NashorsTooth.totalDamageDone[master.netId] : 0f;

                    string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                    sb.AppendFormat(valueDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter SpearOfShojinFormatter = new BetterUI.ItemStats.StatFormatter()
            {

                suffix = "%",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float hyperbolicPercentage = 1 - (1 / (1 + (LeagueItems.SpearOfShojin.cdrFromDamagePercent * value)));
                    float bonusCDR = hyperbolicPercentage * master.GetBody().damage;
                    bonusCDR = bonusCDR > SpearOfShojin.MAX_BONUS_CDR ? SpearOfShojin.MAX_BONUS_CDR : bonusCDR;

                    string valueBonusCDRText = bonusCDR == 0 ? "0" : String.Format("{0:#}", bonusCDR);

                    sb.AppendFormat(valueBonusCDRText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter TitanicDamageFormatter = new BetterUI.ItemStats.StatFormatter()
            {

                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float bonusBaseDamage = (TitanicHydra.firstStackBonusPercent + TitanicHydra.extraStackBonusPercent * (value - 1)) * master.GetBody().maxHealth;

                    string valueBaseDamageText = bonusBaseDamage == 0 ? "0" : String.Format("{0:#}", bonusBaseDamage);

                    sb.AppendFormat(valueBaseDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter WarmogsHealthFormatter = new BetterUI.ItemStats.StatFormatter()
            {

                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    float warmogsBonusMultiplier = value * WarmogsArmor.bonusHealthIncreasePercent;
                    float bonusHealth = (master.GetBody().baseMaxHealth + (master.GetBody().level - 1) * master.GetBody().levelMaxHealth) * warmogsBonusMultiplier;
                    float warmogsPercentBonus = value * WarmogsArmor.bonusHealthIncreaseNumber;
                    
                    string valueBonusHealthText = bonusHealth == 0 ? "0" : String.Format("{0:#}", bonusHealth);
                    string valueWarmogsPercentText = warmogsPercentBonus == 0 ? "0" : String.Format("{0:#}", warmogsPercentBonus);

                    sb.AppendFormat("" + valueBonusHealthText + " (+" + valueWarmogsPercentText + "%)");
                }
            };
        }
    }
}
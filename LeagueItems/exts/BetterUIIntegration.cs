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
                // Heartsteel
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Heartsteel.itemDef,
                    "Health Stolen From Elite Enemies: ",
                    Heartsteel.firstStackIncreasePercent,
                    Heartsteel.extraStackIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Heartsteel.itemDef,
                    "Total Bonus Health",
                    1f,
                    1f,
                    statFormatter: HeartsteelFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                // Nashor's Tooth
                BetterUI.ItemStats.RegisterStat(
                    itemDef: NashorsTooth.itemDef,
                    "On-Hit Damage",
                    1f,
                    1f,
                    statFormatter: NashorsOnHitFormatter,
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
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
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

            public static BetterUI.ItemStats.StatFormatter BloodthirsterTotalFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Healing,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<Bloodthirster.BloodthirsterStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#}", component.TotalHealingDone);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter BotrkTotalFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<BladeOfTheRuinedKing.BladeStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#}", component.TotalDamageDealt);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter DeadMansTotalFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<DeadMansPlate.DeadMansStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#}", component.TotalDamageDealt);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter HeartsteelFormatter = new()
            {
                suffix = " HP",
                style = BetterUI.ItemStats.Styles.Health,
                statFormatter = (sb, value, master) =>
                {
                    if(!master.inventory) return;

                    var component = master.inventory.GetComponent<Heartsteel.HeartsteelStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#}", component.TotalBonusHealth);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter NashorsOnHitFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.hasBody) return;

                    float onHitDamage = NashorsTooth.CalculateDamageOnHit(master.GetBody(), value);
                    string valueDamageText = onHitDamage == 0 ? "0" : String.Format("{0:#}", onHitDamage);

                    sb.AppendFormat(valueDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter NashorsTotalFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<NashorsTooth.NashorsStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#}", component.TotalDamageDealt);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter SpearOfShojinFormatter = new()
            {
                suffix = "%",
                style = BetterUI.ItemStats.Styles.Artifact,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.hasBody) return;

                    float bonusCDR = SpearOfShojin.CalculateBonusCooldownReduction(master.GetBody(), value);
                    string valueBonusCDRText = bonusCDR == 0 ? "0" : String.Format("{0:#.#}", bonusCDR);

                    sb.AppendFormat(valueBonusCDRText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter TitanicDamageFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.hasBody) return;

                    float bonusBaseDamage = TitanicHydra.CalculateBonusBaseDamage(master.GetBody(), value);
                    string valueBonusHealthText = bonusBaseDamage == 0 ? "0" : String.Format("{0:#.#}", bonusBaseDamage);

                    sb.AppendFormat(valueBonusHealthText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter WarmogsHealthFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Health,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.hasBody) return;

                    float bonusHealth = WarmogsArmor.CalculateHealthIncrease(master.GetBody(), value);
                    string valueBonusHealthText = bonusHealth == 0 ? "0" : String.Format("{0:#}", bonusHealth);

                    float warmogsPercentBonus = WarmogsArmor.CalculateHealthIncreasePercent(master.GetBody(), value) * 100f;
                    string valueWarmogsPercentText = warmogsPercentBonus == 0 ? "0" : String.Format(format: "({0:#}%)", warmogsPercentBonus);

                    sb.AppendFormat("" + valueBonusHealthText + " " + valueWarmogsPercentText + " HP");
                }
            };
        }
    }
}
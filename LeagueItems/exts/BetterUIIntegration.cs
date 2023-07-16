using RoR2;
using BepInEx.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using BetterUI;

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
                    statFormatter: BotrkTotalFormatter
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
                    statFormatter: DeadMansTotalFormatter
                );
                // Death's Dance
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeathsDance.itemDef,
                    "Damage Reduction",
                    DeathsDance.damageReductionIncreasePercent,
                    DeathsDance.damageReductionIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.HyperbolicStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeathsDance.itemDef,
                    "Total Damage Taken",
                    1f,
                    1f,
                    statFormatter: DeathsDanceTakenFormatter
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: DeathsDance.itemDef,
                    "Total Damage Cleansed",
                    1f,
                    1f,
                    statFormatter: DeathsDanceCleansedFormatter
                );
                // Guinsoo's Rageblade
                BetterUI.ItemStats.RegisterStat(
                    itemDef: GuinsoosRageblade.itemDef,
                    "On-Hit Damage",
                    1f,
                    1f,
                    statFormatter: GuinsoosOnHitFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.Damage
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: GuinsoosRageblade.itemDef,
                    "Total Damage Dealt",
                    1f,
                    1f,
                    statFormatter: GuinsoosTotalFormatter
                );
                // Heartsteel
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Heartsteel.itemDef,
                    "Health Gain On Proc",
                    Heartsteel.firstStackIncreasePercent,
                    Heartsteel.extraStackIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: Heartsteel.itemDef,
                    "Bonus Base Health",
                    1f,
                    1f,
                    statFormatter: HeartsteelTotalFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                // InfinityEdge
                BetterUI.ItemStats.RegisterStat(
                    itemDef: InfinityEdge.itemDef,
                    "Bonus Crit Chance",
                    InfinityEdge.critChanceIncreasePercent,
                    InfinityEdge.critChanceIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Luck
                );
                BetterUI.ItemStats.RegisterStat(
                    itemDef: InfinityEdge.itemDef,
                    "Bonus Crit Damage",
                    InfinityEdge.critDamageIncreasePercent,
                    InfinityEdge.critDamageIncreasePercent,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Percent,
                    itemTag: BetterUI.ItemStats.ItemTag.Luck
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
                    statFormatter: NashorsTotalFormatter
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
                    "Bonus Damage",
                    1f,
                    1f,
                    statFormatter: TitanicDamageFormatter
                );
                // Warmog's Armor
                BetterUI.ItemStats.RegisterStat(
                    itemDef: WarmogsArmor.itemDef,
                    "Bonus Health",
                    1f,
                    1f,
                    statFormatter: WarmogsHealthFormatter,
                    itemTag: BetterUI.ItemStats.ItemTag.MaxHealth
                );
                // Wit's End
                BetterUI.ItemStats.RegisterStat(
                    itemDef: WitsEnd.itemDef,
                    "Fray Stack Duration",
                    WitsEnd.frayDurationPerStack,
                    WitsEnd.frayDurationPerStack,
                    stackingFormula: BetterUI.ItemStats.LinearStacking,
                    statFormatter: BetterUI.ItemStats.StatFormatter.Seconds
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

            public static BetterUI.ItemStats.StatFormatter DeathsDanceTakenFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<DeathsDance.DeathsDanceStatistics>();

                    if (component)
                    {
                        string tempTaken = String.Format("{0:#}", component.TotalDamageTaken);
                        tempTaken = tempTaken == "" ? "0" : tempTaken;

                        sb.AppendFormat(tempTaken);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter DeathsDanceCleansedFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Healing,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<DeathsDance.DeathsDanceStatistics>();

                    if (component)
                    {
                        string tempCleansed = String.Format("{0:#}", component.TotalDamageCleansed);
                        tempCleansed = tempCleansed == "" ? "0" : tempCleansed;

                        sb.AppendFormat(tempCleansed);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter GuinsoosOnHitFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.hasBody) return;

                    float onHitDamage = GuinsoosRageblade.CalculateDamageOnHit(master.GetBody(), value);
                    string valueDamageText = onHitDamage == 0 ? "0" : String.Format("{0:#.#}", onHitDamage);

                    sb.AppendFormat(valueDamageText);
                }
            };

            public static BetterUI.ItemStats.StatFormatter GuinsoosTotalFormatter = new()
            {
                suffix = "",
                style = BetterUI.ItemStats.Styles.Damage,
                statFormatter = (sb, value, master) =>
                {
                    if (!master.inventory) return;

                    var component = master.inventory.GetComponent<GuinsoosRageblade.GuinsoosStatistics>();

                    if (component)
                    {
                        string temp = String.Format("{0:#.#}", component.TotalDamageDealt);
                        temp = temp == "" ? "0" : temp;

                        sb.AppendFormat(temp);
                    }
                    else
                    {
                        sb.Append("0");
                    }
                }
            };

            public static BetterUI.ItemStats.StatFormatter HeartsteelTotalFormatter = new()
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

                    sb.Append("/" + String.Format("{0:#}", Heartsteel.CalculateMaxStackableHealth(value)));
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
                    string valueDamageText = onHitDamage == 0 ? "0" : String.Format("{0:#.#}", onHitDamage);

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
                        string temp = String.Format("{0:#.#}", component.TotalDamageDealt);
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
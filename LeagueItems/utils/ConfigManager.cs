using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;


namespace LeagueItems
{
    internal static class ConfigManager
    {
        public static ConfigFile ConfigFileItemScaling;
        
        public static ConfigEntry<float> BladeScaling_PercentDamagePerItem;
        
        public static ConfigEntry<float> BloodthirsterScaling_HealingPerItem;
        
        public static ConfigEntry<float> DeadMansScaling_MovespeedPerItem;
        public static ConfigEntry<float> DeadMansScaling_DamagePerItem;

        public static ConfigEntry<float> DeathsDanceScaling_MaxHealthBurn;
        public static ConfigEntry<float> DeathsDanceScaling_DamageReductionPerItem;

        public static ConfigEntry<float> DuskbladeScaling_FirstItem;
        public static ConfigEntry<float> DuskbladeScaling_ExtraItem;

        public static ConfigEntry<float> GuinsoosScaling_FirstItem;
        public static ConfigEntry<float> GuinsoosScaling_ExtraItem;

        public static ConfigEntry<float> HeartsteelScaling_FirstItem;
        public static ConfigEntry<float> HeartsteelScaling_ExtraItem;
        public static ConfigEntry<float> HeartsteelScaling_HealthCapPerItem;

        public static ConfigEntry<float> InfinityEdgeScaling_CritChancePerItem;
        public static ConfigEntry<float> InfinityEdgeScaling_CritDamagePerItem;
        
        public static ConfigEntry<float> NashorsScaling_FirstItem;
        public static ConfigEntry<float> NashorsScaling_ExtraItem;
        public static ConfigEntry<float> NashorsScaling_PerLevel;
        
        public static ConfigEntry<float> SpearOfShojinScaling_MaxCDRBonus;
        public static ConfigEntry<float> SpearOfShojinScaling_PercentDamagePerItem;
        
        public static ConfigEntry<float> TitanicScaling_FirstItem;
        public static ConfigEntry<float> TitanicScaling_ExtraItem;
        
        public static ConfigEntry<float> WarmogsScaling_FirstItem;
        public static ConfigEntry<float> WarmogsScaling_ExtraItem;
        
        public static ConfigEntry<float> WitsEndScaling_FrayDurationPerItem;
        public static ConfigEntry<float> WitsEndScaling_BonusStatPercentPerItem;
        
        static ConfigManager()
        {
            ConfigFileItemScaling = new ConfigFile(Paths.ConfigPath + "\\LeagueItems_ItemScaling.cfg", true);

            BindScalings();            
        }

        private static BindScalings()
        {
            BladeScaling_PercentDamagePerItem = Bind(
                ConfigFileItemScaling, 
                "Blade of the Ruined King Scaling", 
                "% Damage Per Stack", 
                BladeOfTheRuinedKing.onHitDamageNumber, 
                "Percent of enemy current health dealt as on-hit damage (hyperbolic)."
            );

            BloodthirsterScaling_HealingPerItem = Bind(
                ConfigFileItemScaling, 
                "Bloodthirster Scaling",
                "Healing Per Stack", 
                Bloodthirster.bonusLifestealNumber, 
                "Percent of damage dealt received as healing (hyperbolic)."
            );

            DeadMansScaling_DamagePerItem = Bind(
                ConfigFileItemScaling, 
                "Dead Man's Plate Scaling", 
                "Damage Per Stack", 
                DeadMansPlate.bonusDamagePerItemStack, 
                "Percent of damage dealt on consumption of stacks (linear)."
            );

            DeadMansScaling_MovespeedPerItem = Bind(
                ConfigFileItemScaling, 
                "Dead Man's Plate Scaling", 
                "Movement Speed Per Stack", 
                DeadMansPlate.movementSpeedPerStack, 
                "Percent movement speed gained per stack (linear)."
            );
            
            DeathsDanceScaling_MaxHealthBurn = Bind(
                ConfigFileItemScaling, 
                "Death's Dance Scaling", 
                "Max Health Burn", 
                DeathsDance.MAX_DAMAGE_PER_SECOND, 
                "Percent of max health lost while you have stacks."
            );

            DeathsDanceScaling_DamageReductionPerItem = Bind(
                ConfigFileItemScaling, 
                "Death's Dance Scaling", 
                "Damage Reduction Per Stack", 
                DeathsDance.damageReductionIncreaseNumber, 
                "Percent of incoming damage reduced per stack (hyperbolic)."
            );

            DuskbladeScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Duskblade Scaling",
                "Armor Ignored On First Stack",
                DuskbladeOfDraktharr.armorIgnoredFirstStackNumber,
                "Amount of armor ignored on first stack."
            );

            DuskbladeScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Duskblade Scaling",
                "Armor Ignored Per Additional Stack",
                DuskbladeOfDraktharr.armorIgnoredExtraStackNumber,
                "Amount of armor ignored for every additional stack (linear)."
            );

            GuinsoosScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Guinsoo's Rageblade Scaling",
                "Base On-Hit Damage Per 1% Crit",
                GuinsoosRageblade.firstStackDamagePerCrit,
                "Base on-hit damage for every 1% of crit chance."
            );

            GuinsoosScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Guinsoo's Rageblade Scaling",
                "Bonus On-Hit Damage Per 1% Crit",
                GuinsoosRageblade.extraStackDamagePerCrit,
                "Additional on-hit damage per stack for every 1% of crit chance (linear)."
            );

            HeartsteelScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Heartsteel Scaling",
                "Base Percent Health Gained",
                Heartsteel.firstStackIncreaseNumber,
                "Base percent max health gained when killing an elite enemy."
            );

            HeartsteelScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Heartsteel Scaling",
                "Bonus Percent Health Gained",
                Heartsteel.extraStackIncreaseNumber,
                "Bonus percent max health gained per stack when killing an elite enemy (linear)."
            );

            HeartsteelScaling_HealthCapPerItem = Bind(
                ConfigFileItemScaling,
                "Heartsteel Scaling",
                "Health Cap Per Stack",
                Heartsteel.MAX_HEALTH_BONUS_PER_STACK,
                "Max health bonus allowed per item stack (linear)."
            );

            InfinityEdgeScaling_CritChancePerItem = Bind(
                ConfigFileItemScaling,
                "Infinity Edge Scaling",
                "Crit Chance Per Stack",
                InfinityEdge.critChanceIncreaseNumber,
                "Crit chance gained per item stack (linear)."
            );

            InfinityEdgeScaling_CritDamagePerItem = Bind(
                ConfigFileItemScaling,
                "Infinity Edge Scaling",
                "Crit Damage Per Stack",
                InfinityEdge.critDamageIncreaseNumber,
                "Crit damage gained per item stack (linear)."
            );
            
            NashorsScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Nashor's Tooth Scaling",
                "Base On-Hit Damage",
                NashorsTooth.firstStackMultiplier,
                "Initial on-hit damage for the first stack of Nashor's Tooth."
            );

            NashorsScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Nashor's Tooth Scaling",
                "Bonus On-Hit Damage Per Stack",
                NashorsTooth.extraStacksMultiplier,
                "Bonus on-hit damage for each additional stack of Nashor's Tooth (linear)."
            );

            NashorsScaling_PerLevel = Bind(
                ConfigFileItemScaling,
                "Nashor's Tooth Scaling",
                "Bonus On-Hit Damage Per Player Level",
                NashorsTooth.levelDamageMultiplier,
                "Bonus on-hit damage per player level (linear)."
            );

            SpearOfShojinScaling_MaxCDRBonus = Bind(
                ConfigFileItemScaling,
                "Spear Of Shojin Scaling",
                "Maximum Possible Cooldown Reduction",
                SpearOfShojin.MAX_BONUS_CDR,
                "Maximum amount of cooldown reduction that can be gained from base health."
            );

            SpearOfShojinScaling_PercentDamagePerItem = Bind(
                ConfigFileItemScaling,
                "Spear Of Shojin Scaling",
                "Percent of Base Damage Converted",
                SpearOfShojin.cdrFromDamageNumber,
                "Percent of base damage converted into cooldown reduction for every stack of Spear Of Shojin (hyperbolic)."
            );

            TitanicScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Titanic Hydra Scaling",
                "Base Percent of Max Health Converted",
                TitanicHydra.firstStackBonusNumber,
                "Percentage of max health converted into bonus damage for the first stack of Titanic Hydra."
            );

            TitanicScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Titanic Hydra Scaling",
                "Bonus Percent of Max Health Converted",
                TitanicHydra.extraStackBonusNumber,
                "Percentage of max health converted into bonus damage for each additional stack of Titanic Hydra (linear)."
            );

            WarmogsScaling_FirstItem = Bind(
                ConfigFileItemScaling,
                "Warmog's Armor Scaling",
                "Base Percent Bonus Health",
                WarmogsArmor.firstStackIncreaseNumber,
                "Percentage of max health converted into bonus health for the first stack of Warmog's Armor."
            );

            WarmogsScaling_ExtraItem = Bind(
                ConfigFileItemScaling,
                "Warmog's Armor Scaling",
                "Additional Percent Bonus Health Per Stack",
                WarmogsArmor.extraStackIncreaseNumber,
                "Percentage of max health converted into bonus health for each additional stack of Warmog's Armor (linear)."
            );

            WitsEndScaling_FrayDurationPerItem = Bind(
                ConfigFileItemScaling,
                "Wit's End Scaling",
                "Fray Buff Duration",
                WitsEnd.frayDurationPerStack,
                "Fray buff duration for each stack of Wit's End (linear)."
            );

            WitsEndScaling_BonusStatPercentPerItem = Bind(
                ConfigFileItemScaling,
                "Wit's End Scaling",
                "Bonus Attack and Movement Speed",
                WitsEnd.statPerStackNumber,
                "Amount of bonus attack and movement speed for each stack of Wit's End (linear)."
            );
        }

        /// <summary>
        /// Automatically transfers the value of old config entries to new config entries based on
        /// <see cref="previousEntryMap"/>. Old config entries are deleted after their value has
        /// been transferred.<br/>
        /// Wraps <see cref="ConfigFile.Bind{T}(string, string, T, string)"/>.
        /// </summary>
        /// <param name="config">File containing the config entry..</param>
        /// <param name="section">Section/category/group of the setting. Settings are grouped by this.</param>
        /// <param name="key">Name of the setting.</param>
        /// <param name="defaultValue">Value of the setting if the setting was not created yet.</param>
        /// <param name="description">Simple description of the setting shown to the user.</param>
        /// <typeparam name="T">Type of the value contained in this setting.</typeparam>
        static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, string description)
        {
            var currentEntry = config.Bind(section, key, defaultValue, description);

            if (previousEntryMap.ContainsKey((section, key)))
            {
                var previousTuple = previousEntryMap[(section, key)];
                var previousDefinition = new ConfigDefinition(previousTuple.Item1, previousTuple.Item2);
                // ConfigFile has to Bind to a Section + Key before it knows that it's in the file.
                var previousEntry = config.Bind(previousDefinition, "If you ever set your config option to this, it's your own fault", new ConfigDescription(description));
                if(previousEntry.Value != "If you ever set your config option to this, it's your own fault")
                {
                    // Let ConfigBaseEntry deal with type parsing.
                    currentEntry.SetSerializedValue(previousEntry.GetSerializedValue());
                }
                config.Remove(previousDefinition);
            }

            return currentEntry;
        }
    }
}
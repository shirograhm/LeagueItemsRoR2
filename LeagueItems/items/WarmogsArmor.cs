using System;
using System.Collections.Generic;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace LeagueItems
{
    internal class WarmogsArmor
    {
        public static ItemDef itemDef;

        // Gain 50% (+25% per stack) of your base health as bonus health.
        public static float firstStackIncreaseNumber = 50f;
        public static float firstStackIncreasePercent = firstStackIncreaseNumber / 100f;
        public static float extraStackIncreaseNumber = 25f;
        public static float extraStackIncreasePercent = extraStackIncreaseNumber / 100f;

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "WA";
            itemDef.nameToken = "WAToken";
            itemDef.pickupToken = "WAPickup";
            itemDef.descriptionToken = "WADesc";
            itemDef.loreToken = "WALore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("WarmogsArmor.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateHealthIncreasePercent(CharacterBody sender, float itemCount)
        {
            return firstStackIncreasePercent + (itemCount - 1) * extraStackIncreasePercent;
        }

        public static float CalculateHealthIncrease(CharacterBody sender, float itemCount)
        {
            float bonusesFromOtherItems = 0;
            // Add all base health bonuses from other items here
            if (sender.master.inventory && sender.master.inventory.GetItemCount(Heartsteel.itemDef) > 0)
            {
                // If the player has a Heartsteel in their inventory, we add the heartsteel bonus to the Warmog's calculation
                var itemStats = sender.inventory.GetComponent<Heartsteel.HeartsteelStatistics>();
                bonusesFromOtherItems += (itemStats.TotalBonusHealth);
            }

            // Calculate current base health before other base health bonuses
            float baseMaxHealth = sender.baseMaxHealth + (sender.level - 1) * sender.levelMaxHealth;
            return (baseMaxHealth + bonusesFromOtherItems) * CalculateHealthIncreasePercent(sender, itemCount);
        }

        private static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        float warmogsBonusMultiplier = CalculateHealthIncreasePercent(sender, itemCount);
                        args.healthMultAdd += warmogsBonusMultiplier;
                    }
                }
            };

            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (Integrations.itemStatsEnabled)
                {
                    // Let other mods handle stat tracking if installed
                    return;
                }

                if (self.itemInventoryDisplay && characterMaster)
                {
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                    {
                        int itemCount = self.itemInventoryDisplay.inventory.GetItemCount(itemDef);
                        
                        float bonusHealthAmount = CalculateHealthIncrease(characterMaster.GetBody(), itemCount);
                        float warmogsPercentageBonus = CalculateHealthIncreasePercent(characterMaster.GetBody(), itemCount) * 100f;

                        string valueBonusHealthText = String.Format("{0:#}", bonusHealthAmount);
                        string valueWarmogsPercentText = String.Format("({0:#}%)", warmogsPercentageBonus);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Total Bonus Health: " + valueBonusHealthText + " " + valueWarmogsPercentText + " HP";
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("WA", "Warmog's Armor");

            // Name Token
            LanguageAPI.Add("WAToken", "Warmog's Armor");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("WAPickup", "Gain max health.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("WADesc", 
                "Gain <style=cIsHealth>" + firstStackIncreaseNumber + "%</style> " +
                "<style=cStack>(+" + extraStackIncreaseNumber + "% per stack)</style> of your base health as bonus health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("WALore", "The living armor protected the greatest troll warrior in the entire realm during the bloodiest and most devastating battles of the Rune Wars. " +
                                "Deep within the dark woods of Crystone the living armor waits to protect its next owner. A word of warning required before seeking out Warmog... " +
                                "it will protect you for a time, but when it grows tired of you, who will protect you from it?");
        }
    }
}

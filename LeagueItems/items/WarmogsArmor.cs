﻿using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    internal class WarmogsArmor
    {
        public static ItemDef itemDef;

        // Gain 80% (+80% per stack) of your base health as bonus health.
        public static float bonusHealthIncreaseNumber = 80f;
        public static float bonusHealthIncreasePercent = bonusHealthIncreaseNumber / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalBonusHealth = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateHealthIncrease(CharacterBody sender, float itemCount)
        {
            float bonusesFromOtherItems = 0;
            // Add all base health bonuses from other items here
            if (sender.master.inventory.GetItemCount(Heartsteel.itemDef) > 0)
            {
                // If the player has a Heartsteel in their inventory, we add the heartsteel bonus to the Warmog's calculation
                bonusesFromOtherItems += (Heartsteel.totalHealthGained.TryGetValue(sender.master.netId, out float _) ? Heartsteel.totalHealthGained[sender.master.netId] : 0f);
            }

            // Calculate current base health before other base health bonuses
            float baseMaxHealth = sender.baseMaxHealth + (sender.level - 1) * sender.levelMaxHealth;
            return (baseMaxHealth + bonusesFromOtherItems) * itemCount * bonusHealthIncreasePercent;
        }

        private static void Hooks()
        {
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (!self || !self.inventory)
                {
                    return;
                }

                int itemCount = self.inventory.GetItemCount(itemDef);
                if (itemCount > 0)
                {
                    float healthIncrease = CalculateHealthIncrease(self, itemCount);
                    Utilities.SetValueInDictionary(ref totalBonusHealth, self.master, healthIncrease);
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        float healthIncrease = CalculateHealthIncrease(sender, itemCount);
                        Utilities.SetValueInDictionary(ref totalBonusHealth, sender.master, healthIncrease);

                        float warmogsBonusMultiplier = itemCount * bonusHealthIncreasePercent;
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
                        float bonusHealth = totalBonusHealth.TryGetValue(characterMaster.netId, out float _) ? totalBonusHealth[characterMaster.netId] : 0f;
                        float warmogsTotalBonus = self.itemInventoryDisplay.inventory.GetItemCount(itemDef) * bonusHealthIncreaseNumber;

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Total Bonus Health: " + String.Format("{0:#} ({1:#}%)", bonusHealth, warmogsTotalBonus);
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
            LanguageAPI.Add("WADesc", "Gain <style=cIsHealth>" + bonusHealthIncreaseNumber + "%</style> <style=cStack>(+" + bonusHealthIncreaseNumber + "% per stack)</style> of your base health as bonus health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("WALore", "The living armor protected the greatest troll warrior in the entire realm during the bloodiest and most devastating battles of the Rune Wars. " +
                                "Deep within the dark woods of Crystone the living armor waits to protect its next owner. A word of warning required before seeking out Warmog... " +
                                "it will protect you for a time, but when it grows tired of you, who will protect you from it?");
        }
    }
}

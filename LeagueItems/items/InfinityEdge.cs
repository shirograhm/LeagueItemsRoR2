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
    internal class InfinityEdge
    {
        public static ItemDef itemDef;

        // Gain 30% (+30% per stack) crit chance and 60% (+60% per stack) crit damage.
        public static float critChanceIncreaseNumber = 30f;
        public static float critChanceIncreasePercent = critChanceIncreaseNumber / 100f;
        public static float critDamageIncreaseNumber = 60f;
        public static float critDamageIncreasePercent = critDamageIncreaseNumber / 100f;

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
            itemDef.name = "IE";
            itemDef.nameToken = "IEToken";
            itemDef.pickupToken = "IEPickup";
            itemDef.descriptionToken = "IEDesc";
            itemDef.loreToken = "IELore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("InfinityEdge.png");
            itemDef.pickupModelPrefab = LeagueItemsPlugin.MainAssets.LoadAsset<GameObject>("InfinityEdge.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateTotalCritChancePercent(float itemCount)
        {
            return critChanceIncreasePercent * itemCount;
        }

        public static float CalculateTotalCritDamagePercent(float itemCount)
        {
            return critDamageIncreasePercent * itemCount;
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
                        float critChanceIncrease = CalculateTotalCritChancePercent(itemCount);
                        float critDamageIncrease = CalculateTotalCritDamagePercent(itemCount);

                        args.critAdd += critChanceIncrease * 100f;
                        args.critDamageMultAdd += critDamageIncrease;
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

                        float critChanceBonus = CalculateTotalCritChancePercent(itemCount) * 100f;
                        string critChanceBonusText = String.Format("{0:#}", critChanceBonus);

                        float critDamageBonus = CalculateTotalCritDamagePercent(itemCount) * 100f;
                        string critDamageBonusText = String.Format("{0:#}", critDamageBonus);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Bonus Crit Chance: " + critChanceBonusText
                                + "<br>Bonus Crit Damage: " + critDamageBonusText;
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
        }


        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("IE", "Infinity Edge");

            // Name Token
            LanguageAPI.Add("IEToken", "Infinity Edge");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("IEPickup", "Gain crit chance and crit damage.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("IEDesc",
                "Gain <style=cIsUtility>" + critChanceIncreaseNumber + "%</style> <style=cStack>(+" + critChanceIncreaseNumber + "% per stack)</style> crit chance " +
                "and <style=cIsUtility>" + critDamageIncreaseNumber + "%</style> <style=cStack>(+" + critDamageIncreaseNumber + "% per stack)</style> crit damage.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("IELore", "Infinity edge lore.");
        }
    }
}

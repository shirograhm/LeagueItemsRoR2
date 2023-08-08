using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    internal class TitanicHydra
    {
        public static ItemDef itemDef;

        // Increase damage by 2.5% (+1% per stack) of max health.
        public static ConfigurableValue<float> firstStackBonusNumber = new(
            "Item: Titanic Hydra",
            "Max Health Coverted (First Stack)",
            2.5f,
            "Percentage of maximum health gained as bonus damage for the first stack of Titanic Hydra.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static float firstStackBonusPercent = firstStackBonusNumber / 100f;

        public static ConfigurableValue<float> extraStackBonusNumber = new(
            "Item: Titanic Hydra",
            "Max Health Coverted (Extra Stack)",
            1f,
            "Percentage of maximum health gained as bonus damage for each additional stack of Titanic Hydra.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static float extraStackBonusPercent = extraStackBonusNumber / 100f;

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
            itemDef.name = "TH";
            itemDef.nameToken = "THToken";
            itemDef.pickupToken = "THPickup";
            itemDef.descriptionToken = "THDesc";
            itemDef.loreToken = "THLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("TitanicHydra.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateBonusBaseDamage(CharacterBody sender, float itemCount)
        {
            return (firstStackBonusPercent + extraStackBonusPercent * (itemCount - 1)) * sender.healthComponent.fullHealth;
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
                        float bonusBaseDamage = CalculateBonusBaseDamage(sender, itemCount);
                        args.baseDamageAdd += bonusBaseDamage;
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

                        float bonusBaseDamage = CalculateBonusBaseDamage(characterMaster.GetBody(), itemCount);

                        string valueBaseDamageText = bonusBaseDamage == 0 ? "0" : String.Format("{0:#.#}", bonusBaseDamage);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Total Bonus Base Damage: " + valueBaseDamageText;
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
            LanguageAPI.Add("TH", "Titanic Hydra");

            // Name Token
            LanguageAPI.Add("THToken", "Titanic Hydra");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("THPickup", "Increase damage by a percentage of your max health.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("THDesc",
                "Increase damage by <style=cIsHealth>" + firstStackBonusNumber + "%</style> " +
                "<style=cStack>(+" + extraStackBonusNumber + "% per stack)</style> of max health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("THLore", "");
        }
    }
}

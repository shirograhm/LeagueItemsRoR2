using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    class DuskbladeOfDraktharr
    {
        public static ItemDef itemDef;

        // All damage dealt ignores 60 (+40 per stack) flat armor.
        public static ConfigurableValue<float> armorIgnoredFirstStackNumber = new(
            "Item: Duskblade of Draktharr",
            "Armor Penetration (First Stack)",
            60f,
            "Armor penetration for the first stack of Duskblade.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_DUSKBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> armorIgnoredExtraStackNumber = new(
            "Item: Duskblade of Draktharr",
            "Armor Penetration (Extra Stack)",
            40f,
            "Armor penetration for each additional stack of Duskblade.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_DUSKBLADE_DESC"
            }
        );

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
            itemDef.name = "DoD";
            itemDef.nameToken = "DoDToken";
            itemDef.pickupToken = "DoDPickup";
            itemDef.descriptionToken = "DoDDesc";
            itemDef.loreToken = "DoDLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DuskbladeOfDraktharr.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateArmorReduction(int itemCount)
        {
            return armorIgnoredFirstStackNumber + (itemCount - 1) * armorIgnoredExtraStackNumber;
        }

        private static void Hooks()
        {
            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                if (attackerInfo.inventory)
                {
                    int itemCount = attackerInfo.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        float armorToReduce = CalculateArmorReduction(itemCount);

                        victimInfo.body.armor -= armorToReduce;
                    }
                }
            };
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("DoD", "Duskblade of Draktharr");

            // Name Token
            LanguageAPI.Add("DoDToken", "Duskblade of Draktharr");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("DoDPickup", "All damage dealt ignores a flat amount of armor.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("DoDDesc",
                "All damage dealt ignores <style=cIsUtility>" + armorIgnoredFirstStackNumber + "</style> " +
                "<style=cStack>(+" + armorIgnoredExtraStackNumber + " per stack)</style> flat armor.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("DoDLore", "Duskblade lore.");
        }
    }
}

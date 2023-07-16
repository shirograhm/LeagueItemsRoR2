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
    internal class WitsEnd
    {
        public static ItemDef itemDef;
        public static BuffDef frayBuff;

        // Gain a stack of Fray on-hit.
        // Each stack of Fray grants 2% (+2% per stack) bonus attack and movement speed and lasts for 2 (+2 per stack) seconds.
        public static float frayDurationPerStack = 2f;

        public static float statPerStackNumber = 2f;
        public static float statPerStackPercent = statPerStackNumber / 100f;
        
        internal static void Init()
        {
            GenerateItem();
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(frayBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "WE";
            itemDef.nameToken = "WEToken";
            itemDef.pickupToken = "WEPickup";
            itemDef.descriptionToken = "WEDesc";
            itemDef.loreToken = "WELore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("WitsEnd.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            frayBuff = ScriptableObject.CreateInstance<BuffDef>();

            frayBuff.name = "Fray";
            frayBuff.iconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("WitsEndBuff.png");
            frayBuff.canStack = true;
        }

        public static float CalculateStatIncreasePercent(int itemCount)
        {
            return statPerStackPercent * itemCount;
        }

        public static float CalculateFrayDuration(int itemCount)
        {
            return frayDurationPerStack * itemCount;
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
                        int frayStackCount = sender.GetBuffCount(frayBuff);

                        args.attackSpeedMultAdd += frayStackCount * CalculateStatIncreasePercent(itemCount);
                        args.moveSpeedMultAdd += frayStackCount * CalculateStatIncreasePercent(itemCount);
                    }
                }
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active) return;

                if (damageInfo.attacker == null || victim == null)
                {
                    return;
                }

                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef.itemIndex);

                    if (itemCount > 0)
                    {
                        attackerBody.AddTimedBuff(frayBuff, CalculateFrayDuration(itemCount));
                    }
                }
            };
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("WE", "Wit's End");

            // Name Token
            LanguageAPI.Add("WEToken", "Wit's End");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("WEPickup", "Gain temporary attack speed and movement speed on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("WEDesc", "Gain a stack of Fray on-hit. " +
                "Each stack of Fray grants <style=cIsUtility>" + statPerStackNumber + "%</style> <style=cStack>(+" + statPerStackNumber + "% per stack)</style> bonus attack and movement speed " +
                "and lasts for <style=cIsUtility>" + frayDurationPerStack + "</style> <style=cStack>(+" + frayDurationPerStack + " per stack)</style> seconds.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("WELore", "You are at your wit's end.");
        }
    }
}

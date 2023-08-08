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
    class MejaisSoulstealer
    {
        public static ItemDef itemDef;
        public static BuffDef gloryBuff;

        // Gain a stack of Glory after killing an enemy. Each Glory stack grants 3.0 permanent bonus damage. Glory stacks persist until the next stage and are lost upon death.
        public static ConfigurableValue<int> maximumGloryStacksPerItem = new(
            "Item: Mejais Soulstealer",
            "Max Glory Stacks",
            20,
            "Maximum amount of Glory stacks allowed per item.",
            new List<string>()
            {
                "ITEM_MEJAIS_DESC"
            }
        );
        public static ConfigurableValue<float> bonusDamagePerStack = new(
            "Item: Mejais Soulstealer",
            "Bonus Damage Per Stack",
            1.5f,
            "Bonus damage gained from each stack of Glory.",
            new List<string>()
            {
                "ITEM_MEJAIS_DESC"
            }
        );

        internal static void Init()
        {
            GenerateItem();
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(gloryBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "Mejais";
            itemDef.nameToken = "MejaisToken";
            itemDef.pickupToken = "MejaisPickup";
            itemDef.descriptionToken = "MejaisDesc";
            itemDef.loreToken = "MejaisLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("MejaisSoulstealer.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            gloryBuff = ScriptableObject.CreateInstance<BuffDef>();

            gloryBuff.name = "Glory";
            gloryBuff.iconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("GloryBuff.png");
            gloryBuff.canStack = true;
        }

        public static float CalculateMejaisBonusDamage(CharacterBody sender, float itemCount)
        {
            float buffCount = Mathf.Clamp(sender.GetBuffCount(gloryBuff), 0, maximumGloryStacksPerItem * itemCount);

            return bonusDamagePerStack * buffCount;
        }

        public static float CalculateMejaisTotalDamage(float itemCount)
        {
            return bonusDamagePerStack * maximumGloryStacksPerItem * itemCount;
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
                        float bonusBaseDamage = CalculateMejaisBonusDamage(sender, itemCount);
                        args.baseDamageAdd += bonusBaseDamage;
                    }
                }
            };

            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, eventManager, damageReport) =>
            {
                orig(eventManager, damageReport);

                if (!NetworkServer.active || !damageReport.victim || !damageReport.attackerBody) return;

                if (damageReport.attackerBody.inventory)
                {
                    int itemCount = damageReport.attackerBody.inventory.GetItemCount(itemDef);
                    int buffCount = damageReport.attackerBody.GetBuffCount(gloryBuff);

                    if (itemCount > 0 && buffCount < maximumGloryStacksPerItem * itemCount)
                    {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        damageReport.attackerBody.SetBuffCount(gloryBuff.buffIndex, buffCount + 1);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    }
                }

                // If you die, lose all stacks
                if (damageReport.victimBody.inventory)
                {
                    int itemCount = damageReport.victimBody.inventory.GetItemCount(itemDef);
                    int buffCount = damageReport.victimBody.GetBuffCount(gloryBuff);

                    if (itemCount > 0 && buffCount > 0)
                    {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        damageReport.victimBody.SetBuffCount(gloryBuff.buffIndex, 0);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    }
                }
            };
        }

        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("Mejais", "Mejai's Soulstealer");

            // Name Token
            LanguageAPI.Add("MejaisToken", "Mejai's Soulstealer");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("MejaisPickup", "Convert a portion of damage taken into damage taken over time. Cleanse remaining damage when killing an elite enemy.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("MejaisDesc",
                " Gain a stack of Glory after killing an enemy, up to a maximum of " +
                "<style=cIsUtility>" + maximumGloryStacksPerItem + "</style> <style=cStack>(+" + maximumGloryStacksPerItem + " per stack)</style>. " +
                "Each Glory stack grants <style=cIsDamage>" + bonusDamagePerStack + "</style> permanent bonus damage. " +
                "Glory stacks persist until the next stage and are lost upon death.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("MejaisLore", "");
        }
    }

}

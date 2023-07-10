using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    internal class Bloodthirster
    {
        public static ItemDef itemDef;

        // Heal for 20% of damage dealt. Does not apply to on-hit effects.
        public static float bonusLifestealNumber = 20f;
        public static float bonusLifestealPercent = bonusLifestealNumber / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalHealingDone = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
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
            itemDef.name = "BT";
            itemDef.nameToken = "BTToken";
            itemDef.pickupToken = "BTPickup";
            itemDef.descriptionToken = "BTDesc";
            itemDef.loreToken = "BTLore";

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Assets.loadedIcons ? Assets.icons.LoadAsset<Sprite>("Assets/LeagueItems/Bloodthirster") : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);
                if (!damageInfo.attacker)
                {
                    return;
                }

                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef.itemIndex);
                    // Calculate scaled lifesteal percentage based on hyperbolic curve
                    float hyperbolicPercentage = 1 - (1 / (1 + (bonusLifestealPercent * itemCount)));

                    if (itemCount > 0)
                    {
                        float healAmount = damageInfo.damage * hyperbolicPercentage;

                        attackerBody.healthComponent.Heal(healAmount, damageInfo.procChainMask);
                        Utilities.AddValueInDictionary(ref totalHealingDone, attackerBody.master, healAmount);
                    }
                }
            };

            /*
            On.RoR2.UI.HUD.Update += (orig, self) =>
            {
                orig(self);
                LeagueItemsPlugin.logger.LogMessage("Called On.RoR2.UI.HUD.Update");
                if (self.itemInventoryDisplay && self.targetMaster)
                {
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                    {
                        if (item.itemIndex == itemDef.itemIndex && totalHealingDone.TryGetValue(self.targetMaster.netId, out float totalHealing))
                        {
                            LeagueItemsPlugin.logger.LogMessage("Attempting to overwrite text");
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Total Healing Done: " + String.Format("{0:#}", totalHealing);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
            */

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
                        float totalHealing = totalHealingDone.TryGetValue(characterMaster.netId, out float _) ? totalHealingDone[characterMaster.netId] : 0f;

                        string valueHealingText = totalHealing == 0 ? "0" : String.Format("{0:#}", totalHealing);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            if (Integrations.itemStatsEnabled)
                            {
                                itemDef.descriptionToken += "<br><br>Total Healing Done: " + valueHealingText;
                            }
                            else
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Healing Done: " + valueHealingText;
                            }
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
            LanguageAPI.Add("BT", "Bloodthirster");

            // Name Token
            LanguageAPI.Add("BTToken", "Bloodthirster");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BTPickup", "Heal for a percentage of damage dealt.");
            
            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BTDesc", "Heal for <style=cIsHealing>" + bonusLifestealNumber + "%</style> <style=cStack>(+" + bonusLifestealNumber + "%)</style> of the damage dealt. Does not apply to on-hit damage.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BTLore", "A large sword.");
        }
    }
}

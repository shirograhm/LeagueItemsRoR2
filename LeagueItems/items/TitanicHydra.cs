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

        // Increase base damage by 3% (+1.5% per stack) of max health.
        public static float firstStackBonusNumber = 3f;
        public static float firstStackBonusPercent = firstStackBonusNumber / 100f;
        public static float extraStackBonusNumber = 1.5f;
        public static float extraStackBonusPercent = extraStackBonusNumber / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> currentBonusBaseDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Assets.loadedIcons ? Assets.icons.LoadAsset<Sprite>("Assets/LeagueItems/TitanicHydra") : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void Hooks()
        {
            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);
                if (Integrations.itemStatsEnabled)
                {
                    // Let other mods handle stat tracking if installed
                    return;
                }

                if (!self.inventory)
                {
                    return;
                }

                int itemCount = self.inventory.GetItemCount(itemDef);
                int extraCount = itemCount - 1;

                if (itemCount > 0)
                {
                    float bonusBaseDamage = (firstStackBonusPercent + extraStackBonusPercent * extraCount) * self.maxHealth;
                    Utilities.SetValueInDictionary(ref currentBonusBaseDamage, self.master, bonusBaseDamage);
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);
                    int extraCount = itemCount - 1;

                    if (itemCount > 0)
                    {
                        float bonusBaseDamage = (firstStackBonusPercent + extraStackBonusPercent * extraCount) * sender.maxHealth;
                        Utilities.SetValueInDictionary(ref currentBonusBaseDamage, sender.master, bonusBaseDamage);

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
                        float bonusBaseDamage = currentBonusBaseDamage.TryGetValue(characterMaster.netId, out float _) ? currentBonusBaseDamage[characterMaster.netId] : 0f;

                        string valueBaseDamageText = bonusBaseDamage == 0 ? "0" : String.Format("{0:#}", bonusBaseDamage);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            if (Integrations.itemStatsEnabled)
                            {
                                itemDef.descriptionToken += "<br><br>Total Bonus Base Damage: " + valueBaseDamageText;
                            }
                            else
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Bonus Base Damage: " + valueBaseDamageText;
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
            LanguageAPI.Add("TH", "Titanic Hydra");

            // Name Token
            LanguageAPI.Add("THToken", "Titanic Hydra");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("THPickup", "Increase base damage by a percentage of your max health.");
            
            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("THDesc", "Increase base damage by <style=cIsHealth>" + firstStackBonusNumber + "%</style> <style=cStack>(+" + extraStackBonusNumber + "%)</style> of max health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("THLore", "A large weapon.");
        }
    }
}

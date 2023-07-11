using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    internal class SpearOfShojin
    {
        public static ItemDef itemDef;

        // Gain 50% (+50% per stack) of your base damage as bonus cooldown reduction, up to a maximum bonus of 50% CDR.
        public const float MAX_BONUS_CDR = 50.0f;

        public static float cdrFromDamageNumber = 50.0f;
        public static float cdrFromDamagePercent = cdrFromDamageNumber / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusCooldownReduction = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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
            itemDef.name = "SoS";
            itemDef.nameToken = "SoSToken";
            itemDef.pickupToken = "SoSPickup";
            itemDef.descriptionToken = "SoSDesc";
            itemDef.loreToken = "SoSLore";

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("SpearOfShojin.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateBonusCooldownReduction(CharacterBody sender, float itemCount)
        {
            float hyperbolicPercentage = 1 - (1 / (1 + (cdrFromDamagePercent * itemCount)));
            float bonusCDR = hyperbolicPercentage * sender.damage;
            // Cap cooldown reduction
            return bonusCDR > MAX_BONUS_CDR ? MAX_BONUS_CDR : bonusCDR;
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
                    float bonusCDR = CalculateBonusCooldownReduction(self, itemCount);
                    Utilities.SetValueInDictionary(ref bonusCooldownReduction, self.master, bonusCDR);
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        float bonusCDR = CalculateBonusCooldownReduction(sender, itemCount);
                        Utilities.SetValueInDictionary(ref bonusCooldownReduction, sender.master, bonusCDR);

                        args.cooldownMultAdd -= bonusCDR / 100f;
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
                        float bonusCDR = bonusCooldownReduction.TryGetValue(characterMaster.netId, out float _) ? bonusCooldownReduction[characterMaster.netId] : 0f;

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Bonus Cooldown Reduction: " + String.Format("{0:#.#}%", bonusCDR);
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
            LanguageAPI.Add("SoS", "Spear of Shojin");

            // Name Token
            LanguageAPI.Add("SoSToken", "Spear of Shojin");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("SoSPickup", "Gain a percentage of your damage as bonus cooldown reduction.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("SoSDesc", "Gain <style=cIsUtility>" + cdrFromDamageNumber + "%</style> <style=cStack>(+" + cdrFromDamageNumber + "% per stack)</style> "
                                        + "of your base damage as bonus cooldown reduction, up to a maximum bonus of <style=cIsUtility>" + MAX_BONUS_CDR + "%</style> CDR.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("SoSLore", "A jade spear.");
        }
    }
}

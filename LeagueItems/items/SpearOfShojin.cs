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
        public static BuffDef exigencyBuff;

        // Gain 40% (+40% per stack) of your damage as bonus cooldown reduction, up to a maximum bonus of 40%.
        // Gain up to 15% bonus movement speed based on missing health.
        public const float MAX_BONUS_CDR = 40.0f;
        public const float MAX_BONUS_MOVESPEED = 15.0f;

        public static float cdrFromDamageNumber = 40.0f;
        public static float cdrFromDamagePercent = cdrFromDamageNumber / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusCooldownReduction = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        internal static void Init()
        {
            GenerateItem();
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(exigencyBuff);

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
            itemDef.pickupIconSprite = Assets.loadedIcons ? Assets.icons.LoadAsset<Sprite>("Assets/LeagueItems/SpearOfShojin") : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            exigencyBuff = ScriptableObject.CreateInstance<BuffDef>();

            exigencyBuff.name = "Exigency";
            exigencyBuff.buffColor = new Color32(106, 230, 112, 255);
            exigencyBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            exigencyBuff.canStack = true;
        }

        private static void Hooks()
        {
            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);

                if (self.inventory.GetItemCount(itemDef) > 0)
                {
                    float missingHealth = ((self.maxHealth - self.healthComponent.health) / self.maxHealth) * 100f;
                    // Scaled 66% missing health to 15% bonus movespeed. 4.4% missing health per 1% bonus movespeed.
                    int stacksToApply = (int) Math.Floor(missingHealth / 4.4f);
                    // Cap stacks at 15.
                    stacksToApply = stacksToApply > 15 ? 15 : stacksToApply;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    self.SetBuffCount(exigencyBuff.buffIndex, stacksToApply);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    float hyperbolicPercentage = 1 - (1 / (1 + (cdrFromDamagePercent * itemCount)));

                    if (itemCount > 0)
                    {
                        float bonusCDR = hyperbolicPercentage * sender.damage;
                        // Cap at 40% bonus cooldown reduction
                        bonusCDR = bonusCDR > 40f ? 40f : bonusCDR;

                        args.cooldownMultAdd -= bonusCDR / 100f;
                        Utilities.SetValueInDictionary(ref bonusCooldownReduction, sender.master, bonusCDR);

                        int movespeedStacks = sender.GetBuffCount(exigencyBuff);
                        // 1% bonus movespeed per stack.
                        args.moveSpeedMultAdd += movespeedStacks * 0.01f;
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
                            if (Integrations.itemStatsEnabled)
                            {
                                itemDef.descriptionToken += "<br><br>Bonus Cooldown Reduction: " + String.Format("{0:#}%", bonusCDR);
                            }
                            else
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Bonus Cooldown Reduction: " + String.Format("{0:#}%", bonusCDR);
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
            LanguageAPI.Add("SoS", "Spear of Shojin");

            // Name Token
            LanguageAPI.Add("SoSToken", "Spear of Shojin");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("SoSPickup", "Gain a percentage of your damage as bonus cooldown reduction.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("SoSDesc", "Gain <style=cIsUtility>" + cdrFromDamageNumber + "%</style> <style=cStack>(+" + cdrFromDamageNumber + "%)</style> "
                                        + "of your damage as bonus cooldown reduction, up to a maximum bonus of 40%. "
                                        + "Gain up to 15% bonus movement speed based on missing health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("SoSLore", "A jade spear that once belonged to the Shojin Order of Ionia.");
        }
    }
}

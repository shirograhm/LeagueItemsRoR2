using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    class Heartsteel
    {
        public static ItemDef itemDef;

        // Upon killing an elite enemy, gain 3% (+3% per stack) of their max health as permanent base health.
        public static float heartsteelValuePerStack = 3f;
        public static float heartsteelValuePerStackPercentage = heartsteelValuePerStack / 100f;

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalHealthGained = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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
            itemDef.name = "heartsteel";
            itemDef.nameToken = "heartsteelToken";
            itemDef.pickupToken = "heartsteelPickup";
            itemDef.descriptionToken = "heartsteelDesc";
            itemDef.loreToken = "heartsteelLore";

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("Heartsteel.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, eventManager, damageReport) =>
            {
                orig(eventManager, damageReport);

                if (!damageReport.victim || !damageReport.attacker)
                {
                    return;
                }

                if (damageReport.attackerBody.inventory)
                {
                    int itemCount = damageReport.attackerBody.inventory.GetItemCount(itemDef);

                    if (itemCount > 0 && damageReport.victimIsElite)
                    {
                        float healthToGain = (damageReport.victimBody.healthComponent.fullHealth) * heartsteelValuePerStackPercentage;
                        Utilities.AddValueInDictionary(ref totalHealthGained, damageReport.attackerMaster, healthToGain);
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        float healthIncrease = totalHealthGained.TryGetValue(sender.master.netId, out float _) ? totalHealthGained[sender.master.netId] : 0f;
                        args.baseHealthAdd += healthIncrease;
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
                        float healthGain = totalHealthGained.TryGetValue(characterMaster.netId, out float _) ? totalHealthGained[characterMaster.netId] : 0f;

                        string valueHealthGainText = healthGain == 0 ? "0" : String.Format("{0:#.#}", healthGain);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Total Bonus Health: " + valueHealthGainText;
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
        }

        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("heartsteel", "Heartsteel");

            // Name Token
            LanguageAPI.Add("heartsteelToken", "Heartsteel");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("heartsteelPickup", "Gain stacking movement speed over time. Expend max stacks to deal bonus damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("heartsteelDesc", "Upon killing an elite enemy, gain <style=cIsHealth>" + heartsteelValuePerStack + "%</style> <style=cStack>(+" + heartsteelValuePerStack + "% per stack)</style>"
                                               + " of their max health as permanent base health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("heartsteelLore", "Heartsteel lore.");
        }
    }
}

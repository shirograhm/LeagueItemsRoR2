using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;



namespace LeagueItems
{
    internal class BladeOfTheRuinedKing
    {
        public static ItemDef itemDef;

        // Deals 2% (+2% per stack) current health damage on-hit.
        public static float onHitDamageNumber = 2.0f;
        public static float onHitDamagePercent = onHitDamageNumber / 100f;

        private static DamageAPI.ModdedDamageType botrkDamageType;
        public static DamageColorIndex botrkDamageColor = DamageColorAPI.RegisterDamageColor(new Color32(40, 179, 191, 255));

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalDamageDone = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            botrkDamageType = DamageAPI.ReserveDamageType();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "Botrk";
            itemDef.nameToken = "BotrkToken";
            itemDef.pickupToken = "BotrkPickup";
            itemDef.descriptionToken = "BotrkDesc";
            itemDef.loreToken = "BotrkLore";

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Assets.loadedIcons ? Assets.icons.LoadAsset<Sprite>("Assets/LeagueItems/BotRK") : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
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
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef.itemIndex);
                    // Calculate scaled percentage value based on hyperbolic curve
                    float hyperbolicPercentage = 1 - (1 / (1 + (onHitDamagePercent * itemCount)));

                    if (itemCount > 0 && damageInfo.procCoefficient > 0)
                    {
                        float tempDamage = victimBody.healthComponent.health * damageInfo.procCoefficient * hyperbolicPercentage;
                        // Damage is capped at minimum of 1.0f
                        float botrkDamage = tempDamage > 1 ? tempDamage : 1;

                        DamageInfo botrkProc = new DamageInfo();
                        botrkProc.damage = botrkDamage;
                        botrkProc.attacker = damageInfo.attacker;
                        botrkProc.inflictor = damageInfo.attacker;
                        botrkProc.procCoefficient = 0f;
                        botrkProc.position = damageInfo.position;
                        botrkProc.crit = false;
                        botrkProc.damageColorIndex = botrkDamageColor;
                        botrkProc.procChainMask = damageInfo.procChainMask;
                        botrkProc.damageType = DamageType.Silent;
                        DamageAPI.AddModdedDamageType(botrkProc, botrkDamageType);

                        victimBody.healthComponent.TakeDamage(botrkProc);
                        Utilities.AddValueInDictionary(ref totalDamageDone, attackerBody.master, botrkDamage);
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
                        float totalDamage = totalDamageDone.TryGetValue(characterMaster.netId, out float _) ? totalDamageDone[characterMaster.netId] : 0f;

                        string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            if (Integrations.itemStatsEnabled)
                            {
                                itemDef.descriptionToken += "<br><br>Total Damage Done: " + valueDamageText;
                            }
                            else
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Damage Done: " + valueDamageText;
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
            LanguageAPI.Add("Botrk", "Blade of the Ruined King");

            // Name Token
            LanguageAPI.Add("BotrkToken", "Blade of the Ruined King");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BotrkPickup", "Deal a percentage of enemies current health as bonus damage on-hit.");
            
            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BotrkDesc", "Deal <style=cIsDamage>" + onHitDamageNumber + "%</style> <style=cStack>(+" + onHitDamageNumber + "%)</style> of enemy current health as bonus damage on-hit. Deals a minimum of 1 damage on-hit.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BotrkLore", "A sword belonging to the Shadow Isles.");
        }
    }
}

// Styles
// <style=cIsHealth>" + exampleValue + "</style>
// <style=cIsDamage>" + exampleValue + "</style>
// <style=cIsHealing>" + exampleValue + "</style>
// <style=cIsUtility>" + exampleValue + "</style>
// <style=cIsVoid>" + exampleValue + "</style>
// <style=cHumanObjective>" + exampleValue + "</style>
// <style=cLunarObjective>" + exampleValue + "</style>
// <style=cStack>" + exampleValue + "</style>
// <style=cWorldEvent>" + exampleValue + "</style>
// <style=cArtifact>" + exampleValue + "</style>
// <style=cUserSetting>" + exampleValue + "</style>
// <style=cDeath>" + exampleValue + "</style>
// <style=cSub>" + exampleValue + "</style>
// <style=cMono>" + exampleValue + "</style>
// <style=cShrine>" + exampleValue + "</style>
// <style=cEvent>" + exampleValue + "</style>
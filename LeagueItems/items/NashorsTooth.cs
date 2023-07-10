using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LeagueItems
{
    internal class NashorsTooth
    {
        public static ItemDef itemDef;

        // Deals 2 (+2 per stack) damage per level on-hit.
        public static float onHitDamageAmount = 2f;

        public static DamageAPI.ModdedDamageType nashorsDamageType;
        public static DamageColorIndex nashorsDamageColor = DamageColorAPI.RegisterDamageColor(new Color32(176, 26, 94, 255));

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> cumOnHitDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalDamageDone = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            nashorsDamageType = DamageAPI.ReserveDamageType();
            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "NT";
            itemDef.nameToken = "NTToken";
            itemDef.pickupToken = "NTPickup";
            itemDef.descriptionToken = "NTDesc";
            itemDef.loreToken = "NTLore";

            // Tier2 (green) item
#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Assets.loadedIcons ? Assets.icons.LoadAsset<Sprite>("Assets/LeagueItems/NashorsTooth") : Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
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

                if (itemCount > 0)
                {
                    float currentOnHitDamageCalculated = itemCount * onHitDamageAmount * self.level;
                    Utilities.SetValueInDictionary(ref cumOnHitDamage, self.master, currentOnHitDamageCalculated);
                }
            };

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

                    if (itemCount > 0 && damageInfo.procCoefficient > 0)
                    {
                        float nashorsDamage = damageInfo.procCoefficient * itemCount * onHitDamageAmount * attackerBody.level;

                        DamageInfo nashorsProc = new DamageInfo();
                        nashorsProc.damage = nashorsDamage;
                        nashorsProc.attacker = damageInfo.attacker;
                        nashorsProc.inflictor = damageInfo.attacker;
                        nashorsProc.procCoefficient = 0f;
                        nashorsProc.position = damageInfo.position;
                        nashorsProc.crit = false;
                        nashorsProc.damageColorIndex = nashorsDamageColor;
                        nashorsProc.procChainMask = damageInfo.procChainMask;
                        nashorsProc.damageType = DamageType.Silent;
                        DamageAPI.AddModdedDamageType(nashorsProc, nashorsDamageType);

                        victimBody.healthComponent.TakeDamage(nashorsProc);
                        Utilities.AddValueInDictionary(ref totalDamageDone, attackerBody.master, nashorsDamage);
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
                        float currentOnHit = cumOnHitDamage.TryGetValue(characterMaster.netId, out float _) ? cumOnHitDamage[characterMaster.netId] : 0f;
                        float totalDamage = totalDamageDone.TryGetValue(characterMaster.netId, out float _) ? totalDamageDone[characterMaster.netId] : 0f;

                        string valueOnHit = currentOnHit == 0 ? "0" : String.Format("{0:#}", currentOnHit);
                        string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            if (Integrations.itemStatsEnabled)
                            {
                                itemDef.descriptionToken += "<br><br>On Hit Damage: " + valueOnHit + "<br>Total Damage Done: " + valueDamageText;
                            }
                            else
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>On Hit Damage: " + valueOnHit
                                    + "<br>Total Damage Done: " + valueDamageText;
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
            LanguageAPI.Add("NT", "Nashor's Tooth");

            // Name Token
            LanguageAPI.Add("NTToken", "Nashor's Tooth");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("NTPickup", "Deal flat damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("NTDesc", "Deal <style=cIsDamage>" + onHitDamageAmount + "</style> <style=cStack>(+" + onHitDamageAmount + ")</style> damage per level on-hit.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("NTLore", "A sword belonging to the Shadow Isles.");
        }
    }
}


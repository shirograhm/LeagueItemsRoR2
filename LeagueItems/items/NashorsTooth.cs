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
    internal class NashorsTooth
    {
        public static ItemDef itemDef;

        public static Color32 nashorsColor = new Color32(191, 10, 137, 255);

        // Deals 6 ((+3 per stack) +1.5 per level) damage on-hit.
        public static float firstStackMultiplier = 6f;
        public static float extraStacksMultiplier = 3f;
        public static float levelDamageMultiplier = 1.5f;

        public static DamageAPI.ModdedDamageType nashorsDamageType;
        public static DamageColorIndex nashorsDamageColor = DamageColorAPI.RegisterDamageColor(nashorsColor);

        public class NashorsStatistics : MonoBehaviour
        {
            private float _totalDamageDealt;
            public float TotalDamageDealt
            {
                get { return _totalDamageDealt; }
                set
                {
                    _totalDamageDealt = value;
                    if (NetworkServer.active)
                    {
                        new NashorsSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class NashorsSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageDealt;

                public NashorsSync()
                {
                }

                public NashorsSync(NetworkInstanceId objId, float totalDamage)
                {
                    this.objId = objId;
                    this.totalDamageDealt = totalDamage;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalDamageDealt = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        NashorsStatistics component = obj.GetComponent<NashorsStatistics>();
                        if (component) component.TotalDamageDealt = totalDamageDealt;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalDamageDealt);
                }
            }
        }

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<NashorsStatistics.NashorsSync>();

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

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("NashorsTooth.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateDamageOnHit(CharacterBody sender, float itemCount)
        {
            return firstStackMultiplier + extraStacksMultiplier * (itemCount - 1) + levelDamageMultiplier * sender.level;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<NashorsStatistics>();
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
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if (attackerBody && attackerBody.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef);
                    // If the item is in the inventory and the on-hit multiplier is greater than 0
                    if (itemCount > 0 && damageInfo.procCoefficient > 0)
                    {
                        float damageOnHit = CalculateDamageOnHit(attackerBody, itemCount);
                        float nashorsDamage = damageInfo.procCoefficient * damageOnHit;

                        DamageInfo nashorsProc = new()
                        {
                            damage = nashorsDamage,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.attacker,
                            procCoefficient = 0f,
                            position = damageInfo.position,
                            crit = false,
                            damageColorIndex = nashorsDamageColor,
                            procChainMask = damageInfo.procChainMask,
                            damageType = DamageType.Silent
                        };
                        DamageAPI.AddModdedDamageType(nashorsProc, nashorsDamageType);

                        victimBody.healthComponent.TakeDamage(nashorsProc);

                        var itemStats = attackerBody.inventory.GetComponent<NashorsStatistics>();
                        itemStats.TotalDamageDealt += nashorsDamage;
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
                    var itemStats = self.itemInventoryDisplay.inventory.GetComponent<NashorsStatistics>();

                    if (itemStats)
                    {
                        self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                        {
                            int itemCount = self.itemInventoryDisplay.inventory.GetItemCount(itemDef);

                            float damageOnHit = CalculateDamageOnHit(characterMaster.GetBody(), itemCount);

                            string valueOnHit = String.Format("{0:#.#}", damageOnHit);
                            string valueDamageText = String.Format("{0:#}", itemStats.TotalDamageDealt);

                            if (item.itemIndex == itemDef.itemIndex)
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>On Hit Damage: " + valueOnHit
                                    + "<br>Total Damage Done: " + valueDamageText;
                            }
                        });
                    }
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
            LanguageAPI.Add("NTDesc", "Deal <style=cIsDamage>" + firstStackMultiplier + "</style> (<style=cStack>(+" + extraStacksMultiplier + " per stack)</style> "
                                       + "+<style=cStack>" + levelDamageMultiplier + "</style> per level) damage on-hit.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("NTLore", "A sword belonging to the Shadow Isles.");
        }
    }
}


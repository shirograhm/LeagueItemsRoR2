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
    internal class GuinsoosRageblade
    {
        public static ItemDef itemDef;

        public static Color32 guinsoosColor = new(224, 163, 20, 255);

        // Convert every 1% of crit chance to 0.75 + (0.75 per stack) damage on-hit.
        public static ConfigurableValue<float> firstStackDamagePerCrit = new(
            "Item: Guinsoos Rageblade",
            "On-Hit Damage (First Stack)",
            0.75f,
            "On-hit damage for every 1% of crit chance.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> extraStackDamagePerCrit = new(
            "Item: Guinsoos Rageblade",
            "On-Hit Damage (Extra Stack)",
            0.75f,
            "Additional damage on-hit for every 1% of crit chance for every additional stack of Guinsoo's Rageblade.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );

        public static DamageAPI.ModdedDamageType guinsoosDamageType;
        public static DamageColorIndex guinsoosDamageColor = DamageColorAPI.RegisterDamageColor(guinsoosColor);

        public class GuinsoosStatistics : MonoBehaviour
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
                        new GuinsoosSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class GuinsoosSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageDealt;

                public GuinsoosSync()
                {
                }

                public GuinsoosSync(NetworkInstanceId objId, float totalDamage)
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
                        GuinsoosStatistics component = obj.GetComponent<GuinsoosStatistics>();
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

            NetworkingAPI.RegisterMessageType<GuinsoosStatistics.GuinsoosSync>();

            guinsoosDamageType = DamageAPI.ReserveDamageType();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "GR";
            itemDef.nameToken = "GRToken";
            itemDef.pickupToken = "GRPickup";
            itemDef.descriptionToken = "GRDesc";
            itemDef.loreToken = "GRLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.VoidTier3;
            });
            // Requires Void DLC to use item
            itemDef.requiredExpansion = LeagueItemsPlugin.voidDLC;

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("GuinsoosRageblade_Void.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateDamageOnHit(CharacterBody sender, float itemCount)
        {
            return sender.crit * (firstStackDamagePerCrit + (itemCount - 1) * extraStackDamagePerCrit);
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<GuinsoosStatistics>();
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                if (attackerInfo.inventory)
                {
                    int itemCount = attackerInfo.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        damageInfo.crit = false;
                    }
                }
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active || damageInfo.attacker == null || victim == null)
                {
                    return;
                }

                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if (attackerBody && attackerBody.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef);
                    // If the item is in the inventory and the on-hit chance is greater than 0
                    if (itemCount > 0 && damageInfo.procCoefficient > 0)
                    {
                        float guinsoosDamage = CalculateDamageOnHit(attackerBody, itemCount);

                        DamageInfo guinsoosProc = new()
                        {
                            damage = guinsoosDamage,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.attacker,
                            procCoefficient = 0f,
                            position = damageInfo.position,
                            crit = false,
                            damageColorIndex = guinsoosDamageColor,
                            procChainMask = damageInfo.procChainMask,
                            damageType = DamageType.Silent
                        };
                        DamageAPI.AddModdedDamageType(guinsoosProc, guinsoosDamageType);

                        victimBody.healthComponent.TakeDamage(guinsoosProc);

                        var itemStats = attackerBody.inventory.GetComponent<GuinsoosStatistics>();
                        itemStats.TotalDamageDealt += guinsoosDamage;
                    }
                }
            };
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("GR", "Guinsoo's Rageblade");

            // Name Token
            LanguageAPI.Add("GRToken", "Guinsoo's Rageblade");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("GRPickup", "Convert your crit chance into flat damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("GRDesc", 
                "Convert every 1% of your crit chance into <style=cIsDamage>" + firstStackDamagePerCrit + "</style> " +
                "<style=cStack>(+" + extraStackDamagePerCrit + " per stack) damage on-hit.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("GRLore", "A very angry blade.");
        }
    }
}

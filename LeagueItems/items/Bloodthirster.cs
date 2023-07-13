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
    internal class Bloodthirster
    {
        public static ItemDef itemDef;

        // Heal for 10% of damage dealt on-hit.
        public static float bonusLifestealNumber = 10f;
        public static float bonusLifestealPercent = bonusLifestealNumber / 100f;

        public class BloodthirsterStatistics : MonoBehaviour
        {
            private float _totalHealingDone;
            public float TotalHealingDone
            {
                get { return _totalHealingDone; }
                set
                {
                    _totalHealingDone = value;
                    if (NetworkServer.active)
                    {
                        new BloodthirsterSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class BloodthirsterSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalHealingDone;

                public BloodthirsterSync()
                {
                }

                public BloodthirsterSync(NetworkInstanceId objId, float totalHealing)
                {
                    this.objId = objId;
                    this.totalHealingDone = totalHealing;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalHealingDone = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        BloodthirsterStatistics component = obj.GetComponent<BloodthirsterStatistics>();
                        if (component) component.TotalHealingDone = totalHealingDone;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalHealingDone);
                }
            }
        }

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<BloodthirsterStatistics.BloodthirsterSync>();

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
            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("Bloodthirster.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<BloodthirsterStatistics>();
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

                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef.itemIndex);
                    // Calculate scaled lifesteal percentage based on hyperbolic curve
                    float hyperbolicPercentage = 1 - (1 / (1 + (bonusLifestealPercent * itemCount)));

                    if (itemCount > 0)
                    {
                        float healAmount = damageInfo.damage * hyperbolicPercentage;

                        attackerBody.healthComponent.Heal(healAmount, damageInfo.procChainMask);

                        // Store healing for total health count
                        var itemStats = attackerBody.inventory.GetComponent<BloodthirsterStatistics>();
                        itemStats.TotalHealingDone += healAmount;
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
                    var itemStats = self.itemInventoryDisplay.inventory.GetComponent<BloodthirsterStatistics>();

                    if (itemStats)
                    {
                        self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                        {
                            string valueHealingText = String.Format("{0:#}", itemStats.TotalHealingDone);

                            if (item.itemIndex == itemDef.itemIndex)
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Healing Done: " + valueHealingText;
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
            LanguageAPI.Add("BT", "Bloodthirster");

            // Name Token
            LanguageAPI.Add("BTToken", "Bloodthirster");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BTPickup", "Heal for a percentage of damage dealt.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BTDesc", "Heal for <style=cIsHealing>" + bonusLifestealNumber + "%</style> <style=cStack>(+" + bonusLifestealNumber + "% per stack)</style> of the damage dealt on-hit.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BTLore", "A large sword.");
        }
    }
}

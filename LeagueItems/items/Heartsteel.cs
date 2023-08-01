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
    class Heartsteel
    {
        public static ItemDef itemDef;

        // Upon killing an elite enemy, gain 3% (+1.5% per stack) of your max health as permanent base health, up to a max of 600 (+600 per stack) bonus base health.
        public static ConfigurableValue<float> maxHealthBonusPerStack = new(
            "Item: Heartsteel",
            "Max Permanent Health",
            600f,
            "Maximum permanent health for each stack of Heartsteel.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_HEARTSTEEL_DESC"
            }
        );

        public static ConfigurableValue<float> firstStackIncreaseNumber = new(
            "Item: Heartsteel",
            "Health Gain (First Stack)",
            3f,
            "Percentage of max health gained as permanent base health when killing an elite enemy.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_HEARTSTEEL_DESC"
            }
        );
        public static float firstStackIncreasePercent = firstStackIncreaseNumber / 100f;

        public static ConfigurableValue<float> extraStackIncreaseNumber = new(
            "Item: Heartsteel",
            "Health Gain (Extra Stack)",
            1.5f,
            "Percentage of max health gained as permanent base health when killing an elite enemy for each additional stack of Heartsteel.",
            new System.Collections.Generic.List<string>()
            {
                "ITEM_HEARTSTEEL_DESC"
            }
        );
        public static float extraStackIncreasePercent = extraStackIncreaseNumber / 100f;

        public class HeartsteelStatistics : MonoBehaviour
        {
            private float _totalBonusHealth;
            public float TotalBonusHealth
            {
                get { return _totalBonusHealth; }
                set
                {
                    _totalBonusHealth = value;
                    if (NetworkServer.active)
                    {
                        new HeartsteelSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class HeartsteelSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalBonusHealth;

                public HeartsteelSync()
                {
                }

                public HeartsteelSync(NetworkInstanceId objId, float totalBonusHP)
                {
                    this.objId = objId;
                    this.totalBonusHealth = totalBonusHP;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalBonusHealth = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        HeartsteelStatistics component = obj.GetComponent<HeartsteelStatistics>();
                        if (component) component.TotalBonusHealth = totalBonusHealth;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalBonusHealth);
                }
            }
        }

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<HeartsteelStatistics.HeartsteelSync>();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "HS";
            itemDef.nameToken = "HSToken";
            itemDef.pickupToken = "HSPickup";
            itemDef.descriptionToken = "HSDesc";
            itemDef.loreToken = "HSLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("Heartsteel_Small.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        public static float CalculateHealthIncreasePercent(float itemCount)
        {
            return firstStackIncreasePercent + (itemCount - 1) * extraStackIncreasePercent;
        }

        public static float CalculateMaxStackableHealth(float itemCount)
        {
            return itemCount * maxHealthBonusPerStack;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<HeartsteelStatistics>();
            };

            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, eventManager, damageReport) =>
            {
                orig(eventManager, damageReport);

                if (!NetworkServer.active) return;

                if (!damageReport.victim || !damageReport.attacker)
                {
                    return;
                }

                if (damageReport.attackerBody.inventory)
                {
                    int itemCount = damageReport.attackerBody.inventory.GetItemCount(itemDef);

                    if (itemCount > 0 && damageReport.victimIsElite)
                    {
                        var itemStats = damageReport.attackerBody.inventory.GetComponent<HeartsteelStatistics>();

                        float healthToGain = (damageReport.attackerBody.healthComponent.fullHealth) * CalculateHealthIncreasePercent(itemCount);

                        itemStats.TotalBonusHealth += healthToGain;
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);
                    var itemStats = sender.inventory.GetComponent<HeartsteelStatistics>();

                    if (itemCount > 0 && itemStats)
                    {
                        // Cap max heartsteel health increase based on stacks
                        itemStats.TotalBonusHealth = Mathf.Clamp(itemStats.TotalBonusHealth, 0, CalculateMaxStackableHealth(itemCount));

                        args.baseHealthAdd += itemStats.TotalBonusHealth;
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
                    var itemStats = self.itemInventoryDisplay.inventory.GetComponent<HeartsteelStatistics>();

                    if (itemStats)
                    {
                        self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                        {
                            string valueHealthGainText = String.Format("{0:#.#}", itemStats.TotalBonusHealth);

                            if (item.itemIndex == itemDef.itemIndex)
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Bonus Health: " + valueHealthGainText + " HP";
                            }
                        });
                    }
#pragma warning restore Publicizer001
                }
            };
        }

        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("HS", "Heartsteel");

            // Name Token
            LanguageAPI.Add("HSToken", "Heartsteel");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("HSPickup", "Gain stacking movement speed over time. Expend max stacks to deal bonus damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("HSDesc",
                "Upon killing an elite enemy, gain <style=cIsHealth>" + firstStackIncreaseNumber + "%</style> " +
                "<style=cStack>(+" + extraStackIncreaseNumber + "% per stack)</style> of your max health as permanent base health, up to a max of " +
                "<style=cIsHealth>" + maxHealthBonusPerStack + "</style> <style=cStack>(+" + maxHealthBonusPerStack + " per stack)</style> bonus health.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("HSLore", "Heartsteel lore.");
        }
    }
}

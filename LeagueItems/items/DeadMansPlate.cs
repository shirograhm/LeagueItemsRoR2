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
    class DeadMansPlate
    {
        public static ItemDef itemDef;
        public static BuffDef momentumBuff;

        public static Color32 deadmansColor = new Color32(230, 92, 0, 255);

        // Gain a stack of Momentum every second, up to 10 stacks. Each stack gives 3% movement speed.
        // Once fully stacked, expend all stacks to deal 300% (+300% per item stack) bonus on-hit damage.
        public const int MAX_MOMENTUM_STACKS = 10;

        public static float movementSpeedPerStack = 3.0f;
        public static float movementSpeedPerStackPercent = movementSpeedPerStack / 100f;

        public static float bonusDamagePerItemStack = 300.0f;
        public static float bonusDamagePerItemStackPercent = bonusDamagePerItemStack / 100f;

        public static float timeOfLastStack = 0f;

        private static DamageAPI.ModdedDamageType deadMansDamageType;
        public static DamageColorIndex deadMansDamageColor = DamageColorAPI.RegisterDamageColor(deadmansColor);

        public class DeadMansStatistics : MonoBehaviour
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
                        new DeadMansSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class DeadMansSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageDealt;

                public DeadMansSync()
                {
                }

                public DeadMansSync(NetworkInstanceId objId, float totalDamage)
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
                        DeadMansStatistics component = obj.GetComponent<DeadMansStatistics>();
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
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(momentumBuff);

            NetworkingAPI.RegisterMessageType<DeadMansStatistics.DeadMansSync>();

            deadMansDamageType = DamageAPI.ReserveDamageType();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "DMP";
            itemDef.nameToken = "DMPToken";
            itemDef.pickupToken = "DMPPickup";
            itemDef.descriptionToken = "DMPDesc";
            itemDef.loreToken = "DMPLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DeadMansPlate.png");
            itemDef.pickupModelPrefab = LeagueItemsPlugin.MainAssets.LoadAsset<GameObject>("DeadMansPlate.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            momentumBuff = ScriptableObject.CreateInstance<BuffDef>();

            momentumBuff.name = "Momentum";
            momentumBuff.iconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DeadMansBuff.png");
            momentumBuff.canStack = true;
        }

        public static float CalculateDamageProc(CharacterBody sender, float itemCount)
        {
            return sender.damage * bonusDamagePerItemStackPercent * itemCount;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<DeadMansStatistics>();
            };

            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);

                if (!self || !self.inventory)
                {
                    return;
                }

                if (self.inventory.GetItemCount(itemDef) > 0)
                {
                    if (!self.HasBuff(momentumBuff) && timeOfLastStack + 1.0f <= Time.time)
                    {
                        self.AddBuff(momentumBuff);
                        timeOfLastStack = Time.time;
                    }
                    else
                    {
                        if (self.GetBuffCount(momentumBuff) < MAX_MOMENTUM_STACKS && timeOfLastStack + 1.0f <= Time.time)
                        {
                            self.AddBuff(momentumBuff);
                            timeOfLastStack = Time.time;
                        }
                        else
                        {
                            // Max stacks achieved
                        }
                    }
                }
                else
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    self.SetBuffCount(momentumBuff.buffIndex, 0);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        int numStacks = sender.GetBuffCount(momentumBuff);
                        args.moveSpeedMultAdd += numStacks * movementSpeedPerStackPercent;
                    }
                }
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

                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef.itemIndex);
                    // If the item is in the inventory and the on-hit chance is greater than 0
                    if (itemCount > 0 && damageInfo.procCoefficient > 0 &&
                        attackerBody.GetBuffCount(momentumBuff) == MAX_MOMENTUM_STACKS)
                    {
                        float damageProc = CalculateDamageProc(attackerBody, itemCount);
                        float deadMansDamage = damageProc;

                        DamageInfo deadMansProc = new()
                        {
                            damage = deadMansDamage,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.attacker,
                            procCoefficient = 0f,
                            position = damageInfo.position,
                            crit = false,
                            damageColorIndex = deadMansDamageColor,
                            procChainMask = damageInfo.procChainMask,
                            damageType = DamageType.Silent
                        };
                        DamageAPI.AddModdedDamageType(deadMansProc, deadMansDamageType);

                        victimBody.healthComponent.TakeDamage(deadMansProc);

                        var itemStats = attackerBody.inventory.GetComponent<DeadMansStatistics>();
                        itemStats.TotalDamageDealt += deadMansDamage;

                        // Reset stacks to zero, reset stack time
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        attackerBody.SetBuffCount(momentumBuff.buffIndex, 0);
                        timeOfLastStack = Time.time;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
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
                    var itemStats = self.itemInventoryDisplay.inventory.GetComponent<DeadMansStatistics>();

                    if (itemStats)
                    {
                        self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                        {
                            int itemCount = self.itemInventoryDisplay.inventory.GetItemCount(itemDef);
                            float damageProc = CalculateDamageProc(characterMaster.GetBody(), itemCount);

                            string valueProcText = String.Format("{0:#}", damageProc);
                            string valueDamageText = String.Format("{0:#}", itemStats.TotalDamageDealt);

                            if (item.itemIndex == itemDef.itemIndex)
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Proc Damage: " + valueProcText
                                    + "<br>Total Damage Dealt: " + valueDamageText;
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
            LanguageAPI.Add("DMP", "Dead Man's Plate");

            // Name Token
            LanguageAPI.Add("DMPToken", "Dead Man's Plate");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("DMPPickup", "Gain stacking movement speed over time. Expend max stacks to deal bonus damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("DMPDesc",
                "Gain a stack of Momentum every second, up to a maximum of <style=cIsUtility>" + MAX_MOMENTUM_STACKS + "</style>. " +
                "Each stack gives <style=cIsUtility>" + movementSpeedPerStack + "%</style> movement speed. " +
                "Once fully stacked, expend all stacks to deal <style=cIsDamage>" + bonusDamagePerItemStack + "%</style> " +
                "<style=cStack>(+" + bonusDamagePerItemStack + "% per stack)</style> bonus on-hit damage.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("DMPLore", "The plate armor of a dead man.");
        }
    }
}

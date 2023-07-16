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
    class DeathsDance
    {
        public static ItemDef itemDef;
        public static BuffDef defianceBuff;

        public static Color32 deathsDanceColor = new Color32(153, 22, 11, 255);

        // Convert 25% (+25% per stack) of damage taken into stacks of Defiance (each stack = 1 damage).
        // Defiance stacks are consumed as damage over time, but damage from Defiance cannot exceed 2% of your max health per second.
        // Cleanse all remaining stacks when killing an elite enemy.
        public const float MAX_DAMAGE_PER_SECOND = 2f;
        public const float MAX_DAMAGE_PER_SECOND_PERCENT = MAX_DAMAGE_PER_SECOND / 100f;

        public static float damageReductionIncreaseNumber = 30f;
        public static float damageReductionIncreasePercent = damageReductionIncreaseNumber / 100f;

        public static float timeOfLastDefianceProc = 0f;

        private static DamageAPI.ModdedDamageType deathsDanceDamageType;
        public static DamageColorIndex deathsDanceDamageColor = DamageColorAPI.RegisterDamageColor(deathsDanceColor);

        public class DeathsDanceStatistics : MonoBehaviour
        {
            private float _totalDamageTaken;
            public float TotalDamageTaken
            {
                get { return _totalDamageTaken; }
                set
                {
                    _totalDamageTaken = value;
                    if (NetworkServer.active)
                    {
                        new DamageTakenSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }
            private float _totalDamageCleansed;
            public float TotalDamageCleansed
            {
                get { return _totalDamageCleansed; }
                set
                {
                    _totalDamageCleansed = value;
                    if (NetworkServer.active)
                    {
                        new DamageCleansedSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class DamageTakenSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageTaken;

                public DamageTakenSync() { }
                
                public DamageTakenSync(NetworkInstanceId objId, float totalTaken)
                {
                    this.objId = objId;
                    this.totalDamageTaken = totalTaken;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalDamageTaken = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        DeathsDanceStatistics component = obj.GetComponent<DeathsDanceStatistics>();
                        if (component) component.TotalDamageTaken = totalDamageTaken;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalDamageTaken);
                }
            }

            public class DamageCleansedSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageCleansed;

                public DamageCleansedSync()
                {
                }

                public DamageCleansedSync(NetworkInstanceId objId, float totalDamage)
                {
                    this.objId = objId;
                    this.totalDamageCleansed = totalDamage;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalDamageCleansed = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        DeathsDanceStatistics component = obj.GetComponent<DeathsDanceStatistics>();
                        if (component) component.TotalDamageCleansed = totalDamageCleansed;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalDamageCleansed);
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
            ContentAddition.AddBuffDef(defianceBuff);

            NetworkingAPI.RegisterMessageType<DeathsDanceStatistics.DamageTakenSync>();
            NetworkingAPI.RegisterMessageType<DeathsDanceStatistics.DamageCleansedSync>();

            deathsDanceDamageType = DamageAPI.ReserveDamageType();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "DD";
            itemDef.nameToken = "DDToken";
            itemDef.pickupToken = "DDPickup";
            itemDef.descriptionToken = "DDDesc";
            itemDef.loreToken = "DDLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DeathsDance.png");
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            defianceBuff = ScriptableObject.CreateInstance<BuffDef>();

            defianceBuff.name = "Defiance";
            defianceBuff.iconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DeathsDanceBuff.png");
            defianceBuff.canStack = true;
        }

        public static float CalculateDamageReductionPercentage(int itemCount)
        {
            return 1 - (1 / (1 + (damageReductionIncreasePercent * itemCount)));
        }

        public static float CalculateMaximumDamagePerSecond(float healthAmount)
        {
            return healthAmount * MAX_DAMAGE_PER_SECOND_PERCENT;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<DeathsDanceStatistics>();
            };

            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);

                if (!self || !self.inventory)
                {
                    return;
                }

                if (self.HasBuff(defianceBuff) && timeOfLastDefianceProc + 1.0f <= Time.time)
                {
                    int defianceStackCount = self.GetBuffCount(defianceBuff);
                    // Clamp damageToTake
                    int damageToTake = (int)Mathf.Clamp(defianceStackCount, 0, CalculateMaximumDamagePerSecond(self.healthComponent.fullHealth)) * 2;

                    DamageInfo info = new()
                    {
                        damage = damageToTake,
                        attacker = self.gameObject,
                        procCoefficient = 0f,
                        position = self.corePosition,
                        crit = false,
                        damageColorIndex = deathsDanceDamageColor,
                        damageType = DamageType.BypassBlock | DamageType.BypassArmor | DamageType.BypassOneShotProtection   // Damage ignores armor stat and flat damage reduction
                    };
                    DamageAPI.AddModdedDamageType(info, deathsDanceDamageType);

                    self.healthComponent.TakeDamage(info);

                    var itemStats = self.inventory.GetComponent<DeathsDanceStatistics>();
                    itemStats.TotalDamageTaken += damageToTake;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    self.SetBuffCount(defianceBuff.buffIndex, defianceStackCount - damageToTake);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    timeOfLastDefianceProc = Time.time;
                }
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
                        int stackCount = damageReport.attackerBody.GetBuffCount(defianceBuff);

                        if (stackCount > 0)
                        {
                            var itemStats = damageReport.attackerBody.inventory.GetComponent<DeathsDanceStatistics>();
                            itemStats.TotalDamageCleansed += stackCount;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            damageReport.attackerBody.SetBuffCount(defianceBuff.buffIndex, 0);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                        }
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                if (victimInfo.inventory)
                {
                    int itemCount = victimInfo.inventory.GetItemCount(itemDef);

                    if (itemCount > 0 && !damageInfo.HasModdedDamageType(deathsDanceDamageType))
                    {
                        // Reduce damage before damage application
                        float damageTurnedIntoDefiance = damageInfo.damage * CalculateDamageReductionPercentage(itemCount);
                        damageInfo.damage -= damageTurnedIntoDefiance;

                        int oldDefiance = victimInfo.body.GetBuffCount(defianceBuff);
                        int newDefiance = oldDefiance + (int)damageTurnedIntoDefiance;
                        // Set stacks equal to total stacks plus stacks gained
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        victimInfo.body.SetBuffCount(defianceBuff.buffIndex, newDefiance);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                        if (oldDefiance == 0)
                        {
                            timeOfLastDefianceProc = Time.time;
                        }
                    }
                }
            };
        }

        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("DD", "Death's Dance");

            // Name Token
            LanguageAPI.Add("DDToken", "Death's Dance");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("DDPickup", "Convert a portion of damage taken into damage taken over time. Cleanse remaining damage when killing an elite enemy.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("DDDesc", "Convert <style=cIsUtility>" + damageReductionIncreaseNumber + "%</style> <style=cStack>(+" + damageReductionIncreaseNumber + "% per stack)</style> of damage taken into stacks of Defiance (1 damage per stack)."
                            + " Defiance stacks are consumed as damage over time, but damage from Defiance stacks cannot exceed <style=cIsDamage>" + MAX_DAMAGE_PER_SECOND + "%</style> of your max health per second."
                            + " When you kill an elite enemy, cleanse all remaining stacks.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("DDLore", "Death's Dance lore.");
        }
    }
}

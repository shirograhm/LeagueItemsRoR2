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
        public static BuffDef dissentBuff;

        public static Color32 deathsDanceColor = new Color32(153, 22, 11, 255);

        // Convert 30% (+30% per stack) of damage taken into stacks of Dissent (each stack = 1 damage).
        // Dissent stacks are consumed as damage over time, but damage from Dissent cannot exceed 1% of your max health per second.
        public const float MAX_DAMAGE_PER_SECOND = 1f;
        public const float MAX_DAMAGE_PER_SECOND_PERCENT = MAX_DAMAGE_PER_SECOND / 100f;

        public static float damageReductionIncreaseNumber = 30f;
        public static float damageReductionIncreasePercent = damageReductionIncreaseNumber / 100f;

        public static float timeOfLastDefianceProc = 0f;

        private static DamageAPI.ModdedDamageType deathsDanceDamageType;
        public static DamageColorIndex deathsDanceDamageColor = DamageColorAPI.RegisterDamageColor(deathsDanceColor);

        internal static void Init()
        {
            GenerateItem();
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(dissentBuff);

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
            dissentBuff = ScriptableObject.CreateInstance<BuffDef>();

            dissentBuff.name = "Dissent";
            dissentBuff.buffColor = deathsDanceColor;
            dissentBuff.iconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("DeathsDanceBuff.png");
            dissentBuff.canStack = true;
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
            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);

                if (!self || !self.inventory)
                {
                    return;
                }

                if (self.HasBuff(dissentBuff) && timeOfLastDefianceProc + 1.0f <= Time.time)
                {
                    int dissentStackCount = self.GetBuffCount(dissentBuff);
                    // Clamp damageToTake
                    int damageToTake = (int) Mathf.Clamp(dissentStackCount, 0, CalculateMaximumDamagePerSecond(self.healthComponent.fullHealth));

                    DamageInfo info = new()
                    {
                        damage = damageToTake * 2,   // Why is this x2? Because it works for some reason. Without this the damage is halved by something.
                        attacker = self.gameObject,
                        procCoefficient = 0f,
                        position = self.corePosition,
                        crit = false,
                        damageColorIndex = deathsDanceDamageColor,
                        damageType = DamageType.Silent
                    };
                    DamageAPI.AddModdedDamageType(info, deathsDanceDamageType);

                    self.healthComponent.TakeDamage(info);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    self.SetBuffCount(dissentBuff.buffIndex, dissentStackCount - damageToTake);
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
                        if (damageReport.attackerBody.GetBuffCount(dissentBuff) > 0)
                        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            damageReport.attackerBody.SetBuffCount(dissentBuff.buffIndex, 0);
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

                        int oldDefiance = victimInfo.body.GetBuffCount(dissentBuff);
                        int newDefiance = oldDefiance + (int)damageTurnedIntoDefiance;
                        // Set stacks equal to total stacks plus stacks gained
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        victimInfo.body.SetBuffCount(dissentBuff.buffIndex, newDefiance);
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
            LanguageAPI.Add("DDPickup", "Gain damage reduction, but enemies deal additional damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("DDDesc", "Convert <style=cIsDamage>" + damageReductionIncreaseNumber + "%</style> <style=cStack>(+" + damageReductionIncreaseNumber + "% per stack)</style> of damage taken into stacks of Defiance (1 damage per stack)."
                            + " Defiance stacks are consumed as damage over time, but damage from Defiance stacks cannot exceed <style=cIsUtility>" + MAX_DAMAGE_PER_SECOND + "%</style> of your max health per second."
                            + " On elite enemy kill, cleanse all remaining Defiance stacks.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("DDLore", "Death's Dance lore.");
        }
    }
}

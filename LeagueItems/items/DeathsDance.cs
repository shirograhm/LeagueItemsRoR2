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

        // Convert 25% (+25% per stack) of damage taken into stacks of Defiance (1 damage per stack).
        // Defiance stacks are consumed as damage over time, but damage from Defiance stacks cannot exceed 5% of your max health per second.
        // On elite enemy kill, cleanse all remaining Defiance stacks.
        public const float MAX_DAMAGE_PER_SECOND = 5f;

        public static float damageReductionIncreaseNumber = 25f;
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
            ContentAddition.AddBuffDef(defianceBuff);

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
            defianceBuff.buffColor = deathsDanceColor;
            defianceBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            defianceBuff.canStack = true;
        }

        public static float CalculateDamageReductionPercentage(int itemCount)
        {
            return 1 - (1 / (1 + (damageReductionIncreasePercent * itemCount)));
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

                if (self.HasBuff(defianceBuff) && timeOfLastDefianceProc + 1.0f <= Time.time)
                {
                    int defianceStackCount = self.GetBuffCount(defianceBuff);
                    LeagueItemsPlugin.logger.LogDebug("Player currently has " + defianceStackCount + " stacks.");

                    // Clamp damageToTake at a max of 5% max health
                    int damageToTake = (int)Mathf.Clamp(defianceStackCount, 0, self.healthComponent.fullHealth * (MAX_DAMAGE_PER_SECOND / 100f));

                    LeagueItemsPlugin.logger.LogDebug("Player tries to take " + damageToTake + " stacks of damage.");
                    LeagueItemsPlugin.logger.LogDebug("Player can only take up to " + self.healthComponent.fullHealth * (MAX_DAMAGE_PER_SECOND / 100f) + " stacks of damage.");

                    DamageInfo info = new()
                    {
                        damage = damageToTake,
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
                        if (damageReport.attackerBody.GetBuffCount(defianceBuff) > 0)
                        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            damageReport.attackerBody.SetBuffCount(defianceBuff.buffIndex, 0);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                        }
                    }
                }
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active) return;

                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if (victimBody && victimBody.inventory)
                {
                    int itemCount = victimBody.inventory.GetItemCount(itemDef);

                    if (itemCount > 0 && !damageInfo.HasModdedDamageType(deathsDanceDamageType)) // TODO Fix deaths dance reducing its own damage
                    {
                        float damageTurnedIntoDefiance = damageInfo.damage * CalculateDamageReductionPercentage(itemCount);
                        damageInfo.damage -= damageTurnedIntoDefiance;

                        int currentDefiance = victimBody.GetBuffCount(defianceBuff);
                        currentDefiance += (int)damageTurnedIntoDefiance;
                        // Set stacks equal to total stacks plus stacks gained
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        victimBody.SetBuffCount(defianceBuff.buffIndex, currentDefiance);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                        timeOfLastDefianceProc = Time.time;
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

using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;


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

        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> currentDamageProc = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        internal static void Init()
        {
            GenerateItem();
            GenerateBuff();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));
            ContentAddition.AddBuffDef(momentumBuff);

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

#pragma warning disable Publicizer001
            itemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void GenerateBuff()
        {
            momentumBuff = ScriptableObject.CreateInstance<BuffDef>();

            momentumBuff.name = "Momentum";
            momentumBuff.buffColor = deadmansColor;
            momentumBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            momentumBuff.canStack = true;
        }

        public static void CalculateDamageProc(CharacterBody sender, float itemCount)
        {
            float damageProc = sender.damage * bonusDamagePerItemStackPercent * itemCount;

            Utilities.SetValueInDictionary(ref currentDamageProc, sender.master, damageProc);
        }

        private static void Hooks()
        {
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
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
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.master)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);

                    if (itemCount > 0)
                    {
                        CalculateDamageProc(sender, itemCount);

                        int numStacks = sender.GetBuffCount(momentumBuff);
                        args.moveSpeedMultAdd += numStacks * movementSpeedPerStackPercent;
                    }
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

                    if (itemCount > 0 && attackerBody.GetBuffCount(momentumBuff) == MAX_MOMENTUM_STACKS)
                    {
                        float damageProc = currentDamageProc.TryGetValue(attackerBody.master.netId, out float _) ? currentDamageProc[attackerBody.master.netId] : 0f;
                        float deadMansDamage = damageInfo.procCoefficient * damageProc;

                        DamageInfo deadMansProc = new DamageInfo();
                        deadMansProc.damage = deadMansDamage;
                        deadMansProc.attacker = damageInfo.attacker;
                        deadMansProc.inflictor = damageInfo.attacker;
                        deadMansProc.procCoefficient = 0f;
                        deadMansProc.position = damageInfo.position;
                        deadMansProc.crit = false;
                        deadMansProc.damageColorIndex = deadMansDamageColor;
                        deadMansProc.procChainMask = damageInfo.procChainMask;
                        deadMansProc.damageType = DamageType.Silent;
                        DamageAPI.AddModdedDamageType(deadMansProc, deadMansDamageType);

                        victimBody.healthComponent.TakeDamage(deadMansProc);
                        Utilities.AddValueInDictionary(ref totalDamageDealt, attackerBody.master, deadMansDamage);

                        // Reset stack to zero, reset stack time
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
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                    {
                        float damageProc = currentDamageProc.TryGetValue(characterMaster.netId, out float _) ? currentDamageProc[characterMaster.netId] : 0f;
                        float totalDamage = totalDamageDealt.TryGetValue(characterMaster.netId, out float _) ? totalDamageDealt[characterMaster.netId] : 0f;

                        string valueProcText = damageProc == 0 ? "0" : String.Format("{0:#}", damageProc);
                        string valueDamageText = totalDamage == 0 ? "0" : String.Format("{0:#}", totalDamage);

                        if (item.itemIndex == itemDef.itemIndex)
                        {
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(itemDef.descriptionToken)
                                + "<br><br>Proc Damage: " + valueProcText
                                + "<br>Total Damage Dealt: " + valueDamageText;
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
            LanguageAPI.Add("DMP", "Dead Man's Plate");

            // Name Token
            LanguageAPI.Add("DMPToken", "Dead Man's Plate");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("DMPPickup", "Gain stacking movement speed over time. Expend max stacks to deal bonus damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("DMPDesc", "Gain a stack of Momentum every second, up to a maximum of <style=cIsUtility>" + MAX_MOMENTUM_STACKS + "</style>. Each stack gives <style=cIsUtility>" + movementSpeedPerStack + "%</style> movement speed. "
                                        + "Once fully stacked, expend all stacks to deal <style=cIsDamage>" + bonusDamagePerItemStack + "%</style> <style=cStack>(+" + bonusDamagePerItemStack + "% per stack)</style> bonus on-hit damage.");

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("DMPLore", "The plate armor of a dead man.");
        }
    }
}

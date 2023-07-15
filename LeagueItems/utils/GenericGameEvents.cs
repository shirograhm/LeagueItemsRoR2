﻿using RoR2;
using UnityEngine;
using System;

using static LeagueItems.LeagueItemsPlugin;

namespace LeagueItems
{
    class GenericGameEvents
    {
        public struct OnTakeDamageEventInfo
        {
            public float damageTaken;
        }

        public delegate void DamageAttackerVictimEventHandler(DamageInfo damageInfo, GenericCharacterInfo attackerInfo, GenericCharacterInfo victimInfo);
        public delegate void DamageReportEventHandler(DamageReport damageReport);

        public static event DamageAttackerVictimEventHandler OnHitEnemy;
        public static event DamageAttackerVictimEventHandler BeforeTakeDamage;
        public static event DamageReportEventHandler OnTakeDamage;

        internal static void Init()
        {
            On.RoR2.HealthComponent.Awake += (orig, self) =>
            {
                self.gameObject.AddComponent<GenericDamageEvent>();
                orig(self);
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);
                if (damageInfo.attacker)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;

                    GenericCharacterInfo attackerInfo = new GenericCharacterInfo(attackerBody);
                    GenericCharacterInfo victimInfo = new GenericCharacterInfo(victimBody);
                    if (OnHitEnemy != null) OnHitEnemy(damageInfo, attackerInfo, victimInfo);
                }
            };
        }

        public class GenericDamageEvent : MonoBehaviour, IOnIncomingDamageServerReceiver, IOnTakeDamageServerReceiver
        {
            public HealthComponent healthComponent;
            public CharacterBody victimBody;

            public void Start()
            {
                healthComponent = GetComponent<HealthComponent>();
                if (!healthComponent)
                {
                    Destroy(this);
                    return;
                }
                victimBody = healthComponent.body;
            }

            public void OnIncomingDamageServer(DamageInfo damageInfo)
            {
                GenericCharacterInfo attackerInfo = new GenericCharacterInfo();
                if (damageInfo.attacker) attackerInfo = new GenericCharacterInfo(damageInfo.attacker.GetComponent<CharacterBody>());
                GenericCharacterInfo victimInfo = new GenericCharacterInfo(victimBody);
                if (BeforeTakeDamage != null) BeforeTakeDamage(damageInfo, attackerInfo, victimInfo);
            }

            public void OnTakeDamageServer(DamageReport damageReport)
            {
                if (victimBody && OnTakeDamage != null) OnTakeDamage(damageReport);
            }
        }
    }
}
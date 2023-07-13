using RoR2;
using UnityEngine;
using System.Collections.Generic;

namespace LeagueItems
{
    class DamageColorAPI
    {
        public static List<DamageColorIndex> registeredColorIndexList = new List<DamageColorIndex>();

        internal static void Init()
        {
            hooks();
        }

        private static void hooks()
        {
            On.RoR2.DamageColor.FindColor += (orig, colorIndex) =>
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (registeredColorIndexList.Contains(colorIndex))
                {
                    return DamageColor.colors[(int)colorIndex];
                }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                return orig(colorIndex);
            };
        }

        public static DamageColorIndex RegisterDamageColor(Color color)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            int nextColorIndex = DamageColor.colors.Length;
            DamageColorIndex newDamageColorIndex = (DamageColorIndex)nextColorIndex;

            HG.ArrayUtils.ArrayAppend(ref DamageColor.colors, color);
            registeredColorIndexList.Add(newDamageColorIndex);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            return newDamageColorIndex;
        }
    }
}

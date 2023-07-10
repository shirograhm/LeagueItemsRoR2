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
                if (registeredColorIndexList.Contains(colorIndex))
                {
                    return DamageColor.colors[(int)colorIndex];
                }

                return orig(colorIndex);
            };
        }

        public static DamageColorIndex RegisterDamageColor(Color color)
        {
            int nextColorIndex = DamageColor.colors.Length;
            DamageColorIndex newDamageColorIndex = (DamageColorIndex)nextColorIndex;

            HG.ArrayUtils.ArrayAppend(ref DamageColor.colors, color);
            registeredColorIndexList.Add(newDamageColorIndex);

            return newDamageColorIndex;
        }
    }
}

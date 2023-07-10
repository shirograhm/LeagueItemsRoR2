using System.Collections.Generic;
using RoR2;

namespace LeagueItems
{
    public class Utilities
    {
        public static void AddValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value)
        {
            UnityEngine.Networking.NetworkInstanceId id = CheckForMinionOwner(characterMaster);
            if (myDictionary.ContainsKey(id))
            {
                myDictionary[id] += value;
            }
            else
            {
                myDictionary.Add(id, value);
            }
        }

        public static void SetValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value)
        {
            UnityEngine.Networking.NetworkInstanceId id = CheckForMinionOwner(characterMaster);
            if (myDictionary.ContainsKey(id))
            {
                myDictionary[id] = value;
            }
            else
            {
                myDictionary.Add(id, value);
            }
        }

        private static UnityEngine.Networking.NetworkInstanceId CheckForMinionOwner(CharacterMaster characterMaster)
        {
            return characterMaster?.minionOwnership?.ownerMaster?.netId != null ? characterMaster.minionOwnership.ownerMaster.netId : characterMaster.netId;
        }
    }
}
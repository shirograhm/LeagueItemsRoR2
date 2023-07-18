using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeagueItems
{
    public class ConfigManager
    {
        private static bool reloadLogbook = false;

        internal static void Init()
        {
            On.RoR2.UI.LogBook.LogBookController.Awake += LogBookController_Awake;
            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
        }

        private static void LogBookController_Awake(On.RoR2.UI.LogBook.LogBookController.orig_Awake orig, RoR2.UI.LogBook.LogBookController self)
        {
            orig(self);
            if (reloadLogbook)
            {
                reloadLogbook = false;
                RoR2.UI.LogBook.LogBookController.BuildStaticData();
            }
        }

        private static string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, RoR2.Language self, string token)
        {
            var result = orig(self, token);
            foreach (var configurableValue in ConfigurableValue.instancesList.FindAll(x => x.stringsToAffect.Contains(token)))
            {
                result = result.Replace("{" + configurableValue.key + "}", configurableValue.ToString());
            }
            return result;
        }
    }
}
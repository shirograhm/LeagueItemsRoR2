using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Reflection;

namespace LeagueItems
{
    // Dependencies
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(DamageAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class LeagueItemsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.shirograhm.leagueitems";
        public const string PluginName = "LeagueItems";
        public const string PluginVersion = "0.1.1";

        public static PluginInfo PInfo { get; private set; }

        public static AssetBundle MainAssets;

        internal static BepInEx.Logging.ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            PInfo = Info;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeagueItems.assetbundle"))
            {
                if (stream != null)
                {
                    MainAssets = AssetBundle.LoadFromStream(stream);

                    logger.LogMessage("Successfully loaded assets.");
                }
                else
                {
                    logger.LogError("ERROR: Assets failed to load.");
                }
            }

            DamageColorAPI.Init();

            RoR2.ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            BladeOfTheRuinedKing.Init();
            Bloodthirster.Init();
            DeadMansPlate.Init();
            Heartsteel.Init();
            InfinityEdge.Init();
            NashorsTooth.Init();
            SpearOfShojin.Init();
            TitanicHydra.Init();
            WarmogsArmor.Init();

            logger.LogMessage(nameof(Awake) + " done.");
        }
       
        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                logger.LogMessage($"Player pressed F2. Spawning our custom item at coordinates {transform.position}.");

                // Red Item Array
                ItemIndex[] redItems = new ItemIndex[]
                {
                    BladeOfTheRuinedKing.itemDef.itemIndex,
                    Bloodthirster.itemDef.itemIndex,
                    DeadMansPlate.itemDef.itemIndex,
                    Heartsteel.itemDef.itemIndex,
                    NashorsTooth.itemDef.itemIndex,
                    SpearOfShojin.itemDef.itemIndex,
                    TitanicHydra.itemDef.itemIndex,
                    WarmogsArmor.itemDef.itemIndex
                };
                // Green Item Array
                ItemIndex[] greenItems = new ItemIndex[]
                {
                    InfinityEdge.itemDef.itemIndex
                };

                var random = new System.Random();
                int redArrayIdx = random.Next(0, redItems.Length);
                int greenArrayIdx = random.Next(0, greenItems.Length);

                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(redItems[redArrayIdx]), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(greenItems[greenArrayIdx]), transform.position, transform.forward * 20f);
            }
        }
    }
}

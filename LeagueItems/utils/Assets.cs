using UnityEngine;

public static class Assets
{
    public static bool loadedIcons = false;
    public static bool loadedPrefabs = false;

    public static AssetBundle icons;
    public static AssetBundle prefabs;
    public const string iconsLocation = "icons";
    public const string prefabsLocation = "prefabs";

    //The direct path to your AssetBundle
    public static string IconAssetBundlePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(LeagueItems.LeagueItemsPlugin.pInfo.Location), iconsLocation);
    public static string PrefabAssetBundlePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(LeagueItems.LeagueItemsPlugin.pInfo.Location), prefabsLocation);

    public static void Init()
    {
        //Loads the assetBundle from the Path, and stores it in the static field.
        icons = AssetBundle.LoadFromFile(IconAssetBundlePath);
        if (icons == null)
        {
            LeagueItems.LeagueItemsPlugin.logger.LogError("ERROR: Failed to load asset bundle for icons.");
        }
        else
        {
            loadedIcons = true;
        }
        prefabs = AssetBundle.LoadFromFile(PrefabAssetBundlePath);
        if (icons == null)
        {
            LeagueItems.LeagueItemsPlugin.logger.LogError("ERROR: Failed to load asset bundle for prefabs.");
        }
        else
        {
            loadedPrefabs = true;
        }
    }
}
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  public class ModConfig {
    public string EnableScrapRecipeDesc = "Enable additional recipe for the Magnetic Compass. Uses Metal Scraps instead of Magnetite.";
    public bool EnableScrapRecipe = true;
    public string EnableOriginRecipeDesc = "Allow the Origin Compass to be crafted. <REQUIRED TO CRAFT THE RELATIVE COMPASS>";
    public bool EnableOriginRecipe = true;
    public string EnableRelativeRecipeDesc = "Allow the Relative Compass to be crafted.";
    public bool EnableRelativeRecipe = true;
    public string OriginCompassGearsDesc = "Number of Temporal Gears required to craft the Origin Compass. Min: 1, Max: 8";
    public int OriginCompassGears = 2;
    public string RelativeCompassGearsDesc = "Number of Temporal Gears required to craft the Relative Compass. Min: 1, Max: 8";
    public int RelativeCompassGears = 2;
    public string AllowCompassesInOffhandDesc = "Allow a player to place a compass in their offhand slot.";
    public bool AllowCompassesInOffhand = true;

    // static helper methods
    public static string filename = "CompassMod.json";
    public static ModConfig Load(ICoreAPI api) {
      ModConfig config = null;
      try {
        for(int attempts = 1; attempts < 4; attempts++) {
          try {
            config = api.LoadModConfig<ModConfig>(filename);
          } catch (JsonReaderException e) {
            var badLineNum = e.LineNumber;
            api.Logger.Error($"[CompassMod Error] Unable to parse config JSON. Attempt {attempts} to salvage the file...");
            var configFilepath = Path.Combine(GamePaths.ModConfig, filename);
            var badConfigFilepath = Path.Combine(GamePaths.Logs, "ERROR_" + filename);
            var compassLogFilepath = Path.Combine(GamePaths.Logs, "compass-mod-logs.txt");
            if (attempts == 1) {
              if (File.Exists(badConfigFilepath)) File.Delete(badConfigFilepath);
              File.Copy(configFilepath, badConfigFilepath);
              File.WriteAllText(compassLogFilepath, e.ToString());
            }
            if (attempts != 3) {
              var lines = new List<string>(File.ReadAllLines(configFilepath));
              lines.RemoveAt(badLineNum - 1);
              File.WriteAllText(configFilepath, string.Join("\n", lines.ToArray()));
            }
          }
        }
        try {
          config = api.LoadModConfig<ModConfig>(filename);
        } catch (JsonReaderException) {
          api.Logger.Error("[CompassMod Error] Unable to salvage config.");
        }
      } catch (System.Exception e) {
        api.Logger.Error("[CompassMod Error] Something went really wrong with reading the config file.");
        File.WriteAllText(Path.Combine(GamePaths.Logs, "compass-mod-logs.txt"), e.ToString());
      }

      if (config == null) {
        api.Logger.Warning("[CompassMod Warning] Unable to load valid config file. Generating default config.");
        config = new ModConfig();
      }
      config.OriginCompassGears = GameMath.Clamp(config.OriginCompassGears, 1, 8);
      config.RelativeCompassGears = GameMath.Clamp(config.RelativeCompassGears, 1, 8);
      Save(api, config);
      return config;
    }
    public static void Save(ICoreAPI api, ModConfig config) {
      api.StoreModConfig(config, filename);
    }

    public static void ApplyConfigs(ICoreAPI api, ModConfig config) {
      if (api.Side == EnumAppSide.Server) {
        var sapi = (ICoreServerAPI)api;
        sapi.Event.ServerRunPhase(EnumServerRunPhase.GameReady, () => {
          var scrapRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-magnetic-from-scrap.json");
          var originRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-origin.json");
          var relativeRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-relative.json");
          var compassModGridRecipes = sapi.World.GridRecipes.FindAll(gr => gr.Name.Domain == "compass");
          var scrap = compassModGridRecipes.Find(gr => gr.Name.Path == scrapRecipeAssetLoc.Path);
          var origin = compassModGridRecipes.Find(gr => gr.Name.Path == originRecipeAssetLoc.Path);
          var relative = compassModGridRecipes.Find(gr => gr.Name.Path == relativeRecipeAssetLoc.Path);

          if (!config.EnableScrapRecipe) sapi.World.GridRecipes.Remove(scrap);

          if (!config.EnableOriginRecipe) sapi.World.GridRecipes.Remove(origin);
          else {
            origin.IngredientPattern = "C".PadRight(GameMath.Clamp(config.OriginCompassGears, 1, 8) + 1, 'G').PadRight(9, '_');
            origin.ResolveIngredients(sapi.World);
          }

          if (!config.EnableRelativeRecipe) sapi.World.GridRecipes.Remove(relative);
          else {
            relative.IngredientPattern = "C".PadRight(GameMath.Clamp(config.RelativeCompassGears, 1, 8) + 1, 'G').PadRight(9, '_');
            relative.ResolveIngredients(sapi.World);
          }

          if (config.AllowCompassesInOffhand) {
            var allCompassModBlockAssetLocs = sapi.World.Collectibles.FindAll(c => c.Code.Domain.Equals("compass") && c.Code.ToShortString().Contains("compass"));
            foreach (var collectible in allCompassModBlockAssetLocs) {
              collectible.StorageFlags = collectible.StorageFlags | EnumItemStorageFlags.Offhand;
            }
          }
        });
      }
    }
  }
}

using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass.Common {
  public class Config {
    public const string DEFAULT_FILENAME = "Compass2 Config.json";

    public string EnableMagneticRecipeDesc = "Enable crafting a Magnetic Compass with a Magnetite Nugget.";
    public bool EnableMagneticRecipe = true;
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

    public static Config LoadOrCreateDefault(ICoreAPI api, string filename = DEFAULT_FILENAME) {
      Config config = TryLoadModConfig(api, filename);

      if (config == null) {
        api.Logger.Notification("[Compass2] Unable to load valid config file. Generating {0} with defaults.", filename);
        config = new Config();
      }

      // Save before clamp-correcting to preserve user's chosen values, even if invalid.
      // Valid config values might be detected as invalid due to coding errors.
      Save(api, config, filename);
      Clamp(api, config);
      return config;
    }

    // Throws exception if the config file exists, but had parsing errors.
    // Returns null if no config file exists.
    private static Config TryLoadModConfig(ICoreAPI api, string filename) {
      Config config = null;
      try {
        config = api.LoadModConfig<Config>(filename);
      }
      catch (JsonReaderException e) {
        api.Logger.Error("[Compass2] Unable to parse configuration file, {0}. Correct syntax errors and retry, or delete.", filename);
        throw e;
      }
      catch (Exception e) {
        api.Logger.Error("[Compass2] I don't know what happened. Delete {0} in the mod config folder and try again.", filename);
        throw e;
      }

      return config;
    }

    public static void Clamp(ICoreAPI api, Config config) {
      if (config == null) { return; }

      var temp = config.OriginCompassGears;
      config.OriginCompassGears = GameMath.Clamp(config.OriginCompassGears, 1, 8);
      if (config.OriginCompassGears != temp) {
        api.Logger.Warning("[Compass2] Config \"OriginCompassGears\" value {0} is out of bounds. Using {1}", temp, config.OriginCompassGears);
      }

      temp = config.RelativeCompassGears;
      config.RelativeCompassGears = GameMath.Clamp(config.RelativeCompassGears, 1, 8);
      if (config.RelativeCompassGears != temp) {
        api.Logger.Warning("[Compass2] Config \"RelativeCompassGears\" value {0} is out of bounds. Using {1}", temp, config.OriginCompassGears);
      }
    }

    public static void Save(ICoreAPI api, Config config, string filename) {
      api.StoreModConfig(config, filename);
    }
  }
}

using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Compass.Common {
  public class Config {
    public const string DEFAULT_FILENAME = "Compass2 Config.json";

    public static Config LoadOrCreateDefault(ICoreAPI api, string filename = DEFAULT_FILENAME) {
      Config config = TryLoadModConfig(api, filename);

      if (config == null) {
        api.Logger.Notification("[Compass2] Unable to load valid config file. Generating {0} with defaults.", filename);
        config = new Config();
      }

      // Save before clamp-correcting to preserve user's chosen values, even if invalid.
      // Valid config values might be detected as invalid due to coding errors.
      Save(api, config, filename);
      Clamp(config);
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

    public static void Clamp(Config config) {
      if (config == null) { return; }
    }

    public static void Save(ICoreAPI api, Config config, string filename) {
      api.StoreModConfig(config, filename);
    }
  }
}

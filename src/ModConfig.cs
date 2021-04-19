using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public class ModConfig {

    public bool EnableScrapRecipe = true;
    public bool EnableOriginRecipe = true;
    public bool EnableRelativeRecipe = true;
    public int OriginCompassGears = 2;
    public int RelativeCompassGears = 2;

    // static helper methods
    public static string filename = "CompassMod.json";
    public static ModConfig Load(ICoreAPI api) {
      var config = api.LoadModConfig<ModConfig>(filename);
      if (config == null) {
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
  }
}

using Vintagestory.API.Common;

namespace Compass {
  public class CompassMod : ModSystem {
    private ModConfig config;

    public override void Start(ICoreAPI api) {
      api.Logger.Debug("[Compass] Start");
      base.Start(api);

      config = ModConfig.Load(api);

      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityCompass));

      ModConfig.ApplyConfigs(api, config);
    }
  }
}

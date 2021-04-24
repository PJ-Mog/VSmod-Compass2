using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[assembly: ModInfo("Compass")]

namespace Compass {
  public class CompassMod : ModSystem {
    private ModConfig config;

    public override void Start(ICoreAPI api) {
      api.Logger.Debug("[Compass] Start");
      base.Start(api);
      
      config = ModConfig.Load(api);

      api.RegisterItemClass("ItemMagneticCompass", typeof(ItemMagneticCompass));
      api.RegisterItemClass("ItemRelativeCompass", typeof(ItemRelativeCompass));
      api.RegisterItemClass("ItemOriginCompass", typeof(ItemOriginCompass));

      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityCompass));

      ModConfig.ApplyConfigs(api, config);
    }
  }
}

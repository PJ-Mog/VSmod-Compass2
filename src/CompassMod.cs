using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Compass {
  public class CompassMod : ModSystem {
    public static readonly string ModId = "compass2";
    public static readonly string Domain = "compass";
    public static readonly string HarmonyId = "japanhasrice." + ModId;
    private ICoreClientAPI Capi;

    public override void Start(ICoreAPI api) {
      base.Start(api);

      RegisterModClasses(api);
    }

    public void RegisterModClasses(ICoreAPI api) {
      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));
      api.RegisterBlockClass("BlockPlayerCompass", typeof(BlockPlayerCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityXZTracker));
    }
  }
}

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    private static AssetLocation update = new AssetLocation("compass:recipes/grid/update.json");
    public override float GetNeedleAngleRadians(BlockPos fromPos, ItemStack compass) {
      return GetAngleRadians(fromPos, GetCompassCraftedPos(compass));
    }
  }
}

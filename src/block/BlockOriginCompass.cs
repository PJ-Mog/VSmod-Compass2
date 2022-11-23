using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockOriginCompass : BlockCompass {
    public override float GetNeedleYawToTargetRadians(BlockPos fromPos, ItemStack compassStack) {
      return GetYawRadians(fromPos, GetTargetPos(compassStack));
    }

    public override BlockPos GetTargetPos(ItemStack compassStack) {
      return api.World.DefaultSpawnPosition.AsBlockPos;
    }
  }
}

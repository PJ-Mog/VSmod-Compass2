using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockOriginCompass : BlockCompass {
    protected override BlockPos GetTargetPos(ItemStack compassStack) {
      return api.World.DefaultSpawnPosition.AsBlockPos;
    }
  }
}

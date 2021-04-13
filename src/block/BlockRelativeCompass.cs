using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    public override float GetNeedleAngleRadians(BlockPos fromPos, ItemStack compass) {
      return GetAngleRadians(fromPos, GetCompassCraftedPos(compass));
    }
  }
}
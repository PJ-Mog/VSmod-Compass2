using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockRelativeCompass : BlockBaseCompass {
    private BlockPos targetPos;

    public override float GetNeedleAngleRadians(BlockPos fromPos) {
      if (targetPos == null) return 0;
      return GetAngleRadians(fromPos, this.targetPos);
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null) {
      this.targetPos = GetCompassCraftedPos(byItemStack);
      base.OnBlockPlaced(world, blockPos, byItemStack);
    }
  }
}
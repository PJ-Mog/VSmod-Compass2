using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    public override float GetNeedle2DAngleRadians(BlockPos fromPos, ItemStack compassStack) {
      return Get2DAngleRadians(fromPos, GetTargetPos(compassStack));
    }

    public override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCompassCraftedPos(compassStack);
    }
  }
}

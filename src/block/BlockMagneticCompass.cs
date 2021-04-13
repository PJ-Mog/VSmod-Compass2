using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockMagneticCompass : BlockCompass {
    public override float GetNeedleAngleRadians(BlockPos fromPos, ItemStack compass) {
      return 0.0f;
    }
  }
}

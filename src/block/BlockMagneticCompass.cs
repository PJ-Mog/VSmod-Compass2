using Vintagestory.API.MathTools;

namespace Compass {
  class BlockMagneticCompass : BlockBaseCompass {
    public override float GetNeedleAngleRadians(BlockPos fromPos) {
      return 0.0f;
    }
  }
}

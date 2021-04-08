using Vintagestory.API.MathTools;

namespace Compass {
  class BlockMagneticCompass : BlockCompass {
    public override float GetNeedleAngleRadians(BlockPos fromPos) {
      return 0.0f;
    }
  }
}

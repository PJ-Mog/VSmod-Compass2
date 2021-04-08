using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockOriginCompass : BlockBaseCompass {
    public override float GetNeedleAngleRadians(BlockPos fromPos) {
      var originPos = api.World.DefaultSpawnPosition.AsBlockPos;
      return GetAngleRadians(fromPos, originPos);
    }
  }
}
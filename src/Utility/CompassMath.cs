using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass.Utility {
  public static class CompassMath {
    //  Summary:
    //    The XZ-plane angle in radians between two positions.
    //    Null if the angle cannot be calculated.
    public static float? YawRadians(BlockPos fromPos, BlockPos toPos) {
      if (fromPos == null || toPos == null) {
        return null;
      }
      return (float)Math.Atan2(fromPos.X - toPos.X, fromPos.Z - toPos.Z);
    }

    public static int? XZManhattanDistance(BlockPos fromPos, BlockPos toPos) {
      if (fromPos == null || toPos == null) { return null; }
      return Math.Abs(fromPos.X - toPos.X) + Math.Abs(fromPos.Z - toPos.Z);
    }

    public static float GetWildSpinAngleRadians(ICoreAPI api) {
      float milli = api.World.ElapsedMilliseconds;
      return (float)((milli / 500) + (GameMath.FastSin(milli / 150)) + (GameMath.FastSin(milli / 432)) * 3);
    }
  }
}

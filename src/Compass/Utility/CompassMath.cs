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

    public delegate int? DistanceCalculator(BlockPos fromPos, BlockPos toPos);
    public static int? XZManhattanDistance(BlockPos fromPos, BlockPos toPos) {
      if (fromPos == null || toPos == null) { return null; }
      return Math.Abs(fromPos.X - toPos.X) + Math.Abs(fromPos.Z - toPos.Z);
    }
    public static int? XZDistanceSquared(BlockPos fromPos, BlockPos toPos) {
      if (fromPos == null || toPos == null) { return null; }
      var x = (fromPos.X - toPos.X);
      var z = (fromPos.Z - toPos.Z);
      return (x * x) + (z * z);
    }

    public static float GetIdleWobble(ICoreAPI api) {
      return GameMath.FastSin(api.World.ElapsedMilliseconds * 0.0025f) * 0.03f;
    }

    public static float GetFastSpinAngleRadians(ICoreAPI api) {
      return api.World.ElapsedMilliseconds * 0.0314159f;
    }

    public static float GetWildSpinAngleRadians(ICoreAPI api) {
      float milli = api.World.ElapsedMilliseconds;
      return (float)((milli / 500) + (GameMath.FastSin(milli / 150)) + (GameMath.FastSin(milli / 432)) * 3);
    }

    public static float GetStormInterferenceRadians(ICoreAPI api, float percentSpeed = 1f) {
      float milli = api.World.ElapsedMilliseconds;
      float adjust = GameMath.Clamp(percentSpeed, 0.1f, 1f);
      float wave1 = 0.33f * GameMath.FastSin(milli / 25 * adjust);
      float wave2 = GameMath.FastSin(milli / 222 * adjust);
      float wave3 = GameMath.FastSin(milli / 131 * adjust);
      return milli / 777 + wave1 + wave2 + wave3;
    }
  }
}

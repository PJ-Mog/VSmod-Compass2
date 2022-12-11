using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public enum EnumTargetType {
    STATIONARY,
    MOVING
  }

  public interface IRenderableXZTracker {
    EnumTargetType GetTargetType();

    //  Get the Yaw/XZ-Angle that the tracker would like to display
    //  Null if the tracker should not point in a particular direction
    float? GetXZAngleToPoint(BlockPos fromPos, ItemStack trackerStack);

    bool ShouldPointToTarget(BlockPos fromPos, ItemStack trackerStack);

    int GetDistanceToTarget(BlockPos fromPos, ItemStack trackerStack);

    //  The RotationOrigin of the first, top-level element should represent the point used to rotate the whole shape about the Y-axis.
    Shape GetNeedleShape(ICoreClientAPI capi);
  }
}

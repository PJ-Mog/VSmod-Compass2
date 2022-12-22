using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public enum EnumTargetType {
    STATIONARY,
    MOVING
  }

  public class XZTrackerProps {
    public AssetLocation NeedleShapeLocation;
    public int NeedleGlowLevel = 0;
    public int ShellGlowLevel = 0;
  }

  public interface IRenderableXZTracker : IDisplayableCollectible {
    EnumTargetType TargetType { get; }
    XZTrackerProps Props { get; }

    //  Get the Yaw/XZ-Angle that the tracker would like to display
    //  Null if the tracker should not point in a particular direction
    float? GetXZAngleToPoint(BlockPos fromPos, ItemStack trackerStack);

    bool ShouldPointToTarget(BlockPos fromPos, ItemStack trackerStack);

    int GetDistanceToTarget(BlockPos fromPos, ItemStack trackerStack);

    //  The RotationOrigin of the mesh, in 'block format' (i.e. 0.5f is half of a block's width)
    MeshData GenNeedleMesh(ICoreClientAPI capi, out Vec3f blockRotationOrigin);
  }
}

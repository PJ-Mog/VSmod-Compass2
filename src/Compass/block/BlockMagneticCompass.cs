using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Compass {
  public class BlockMagneticCompass : BlockCompass {
    protected override float? GetXZAngleToTargetRadians(BlockPos fromPos, ItemStack compass) {
      return 0f;
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return IsCrafted(compassStack);
    }

    protected override void SetCompassEntityPos(ItemStack compassStack, BlockPos entityPos) { }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      float trackerOrientation = 0;
      switch (renderTarget) {
        case EnumItemRenderTarget.Gui:
        case EnumItemRenderTarget.HandFp:
          trackerOrientation = (capi.World.Player as ClientPlayer).CameraYaw;
          break;
        case EnumItemRenderTarget.HandTp:
        case EnumItemRenderTarget.HandTpOff:
          trackerOrientation = GetCompassEntityYaw(compassStack);
          break;
      }

      float angle = GetXZAngleToPoint(null, compassStack) ?? GetWildSpinAngleRadians(capi);
      renderinfo.ModelRef = GetBestMeshRef(capi, angle, trackerOrientation);
    }
  }
}

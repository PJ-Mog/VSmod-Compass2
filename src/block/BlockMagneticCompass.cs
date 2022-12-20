using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockMagneticCompass : BlockCompass {
    protected override float? GetXZAngleToTargetRadians(BlockPos fromPos, ItemStack compass) {
      return 0f;
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return IsCrafted(compassStack);
    }

    protected override void SetCompassEntityPos(ItemStack compassStack, BlockPos entityPos) { }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      float yawCorrection = 0;
      switch (renderTarget) {
        case EnumItemRenderTarget.Gui:
        case EnumItemRenderTarget.HandFp:
          yawCorrection = capi.World.Player.Entity.Pos.Yaw;
          break;
        case EnumItemRenderTarget.HandTp:
        case EnumItemRenderTarget.HandTpOff:
          yawCorrection = GetCompassEntityYaw(compassStack);
          break;
      }

      float angle = GetXZAngleToPoint(null, compassStack) ?? GetWildSpinAngleRadians(capi);
      var mesh = GetFullMesh(capi, angle, yawCorrection);
      capi.Render.UpdateMesh(renderinfo.ModelRef, mesh);
    }
  }
}

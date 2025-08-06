using Compass.ConfigSystem;
using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public class BlockMagneticCompass : BlockCompass {
    protected CompassClientConfig ClientSettings;

    protected override void LoadClientSettings(ICoreClientAPI capi) {
      base.LoadClientSettings(capi);
      ClientSettings = capi.ModLoader.GetModSystem<CompassConfigurationSystem>()?.ClientSettings;
      if (ClientSettings.EnableColorTippedMagneticCompass.Value) {
        Textures["needle"] = Textures["alt_needle"];
      }
    }

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
          trackerOrientation = capi.World.Player.CameraYaw;
          break;
        case EnumItemRenderTarget.HandTp:
        case EnumItemRenderTarget.HandTpOff:
          trackerOrientation = GetCompassEntityYaw(compassStack);
          break;
      }

      float? desiredAngle = GetXZAngleToPoint(null, compassStack);
      float renderedAngle;
      if (desiredAngle == null) {
        renderedAngle = GetWildSpinAngleRadians(capi);
      }
      else {
        renderedAngle = (float)desiredAngle + GetAngleDistortion();
      }
      renderinfo.ModelRef.meshrefs[0] = GetBestMeshRef(capi, renderedAngle, trackerOrientation);
    }

    protected override float GetActiveStormInterference() {
      return CompassMath.GetFastSpinAngleRadians(api);
    }

    protected override float GetApproachingStormInterference(float daysUntilNextStorm) {
      return CompassMath.GetFastSpinAngleRadians(api) * (1 - daysUntilNextStorm / DaysBeforeStormToApplyInterference);
    }
  }
}

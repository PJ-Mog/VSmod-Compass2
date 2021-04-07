using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Compass {
  class BlockMagneticCompass : BlockBaseCompass {
    public override double? GetCompassAngleRadians(ICoreClientAPI capi, ItemStack itemstack) {
      return -capi.World.Player.CameraYaw;
    }
    public override Shape GetNeedleShape() {
      return api.Assets.TryGet("compass:shapes/block/compass-needle-magnetic.json")?.ToObject<Shape>();
    }
  }
}
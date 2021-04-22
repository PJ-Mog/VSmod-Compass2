using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class ItemOriginCompass : ItemBaseCompass {
    public override Shape GetNeedleShape() {
      return api.Assets.TryGet("compass:shapes/item/compass-needle-origin.json")?.ToObject<Shape>();
    }
    public override double? GetCompassAngleRadians(ICoreClientAPI capi, ItemStack itemstack) {
      var playerPos = capi.World.Player.Entity.Pos.AsBlockPos;
      var originPos = capi.World.DefaultSpawnPosition.AsBlockPos;

      var dX = playerPos.X - originPos.X;
      var dZ = playerPos.Z - originPos.Z;
      if (dX * dX + dZ * dZ < 2 * 2) { return null; }

      return Math.Atan2(dX, dZ) - capi.World.Player.CameraYaw;
    }
  }
}
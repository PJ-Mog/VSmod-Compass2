using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockPlayerCompass : BlockCompass {
    public override float GetNeedleYawToTargetRadians(BlockPos fromPos, ItemStack compassStack) {
      return GetYawRadians(fromPos, GetTargetPos(compassStack));
    }

    public override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCachedPos(GetCraftedByPlayerUID(compassStack));
    }

    public BlockPos GetCachedPos(string playerUid) {
      var compassMod = api.ModLoader.GetModSystem<CompassMod>() as CompassMod;
      return compassMod?.PlayerPosHandler.GetPlayerPos(playerUid);
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return GetTargetPos(compassStack) != null && base.ShouldPointToTarget(fromPos, compassStack);
    }
  }
}

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockPlayerCompass : BlockCompass {
    public override EnumTargetType GetTargetType() => EnumTargetType.MOVING;

    protected override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCachedPos(GetCraftedByPlayerUID(compassStack));
    }

    public BlockPos GetCachedPos(string playerUid) {
      var compassMod = api.ModLoader.GetModSystem<CompassMod>() as CompassMod;
      return compassMod?.PlayerPosHandler.GetPlayerPos(playerUid);
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return base.ShouldPointToTarget(fromPos, compassStack)
             && GetTargetPos(compassStack) != null;
    }
  }
}

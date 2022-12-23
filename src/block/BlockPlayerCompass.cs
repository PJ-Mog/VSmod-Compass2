using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  class BlockPlayerCompass : BlockCompass {
    public override EnumTargetType TargetType { get; protected set; } = EnumTargetType.Moving;

    protected override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCachedPos(GetCraftedByPlayerUID(compassStack));
    }

    public BlockPos GetCachedPos(string playerUid) {
      var compassMod = api.ModLoader.GetModSystem<CompassMod>() as CompassMod;
      return compassMod?.PlayerPosHandler.GetPlayerPos(api as ICoreClientAPI, playerUid);
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return base.ShouldPointToTarget(fromPos, compassStack)
             && GetTargetPos(compassStack) != null;
    }
  }
}

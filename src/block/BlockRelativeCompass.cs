using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    public override float GetNeedleYawToTargetRadians(BlockPos fromPos, ItemStack compassStack) {
      return GetYawRadians(fromPos, GetTargetPos(compassStack));
    }

    public override void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer player, ItemSlot slot) {
      SetTargetPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
    }
  }
}

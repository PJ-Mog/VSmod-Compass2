using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    public override float GetNeedle2DAngleRadians(BlockPos fromPos, ItemStack compassStack) {
      return Get2DAngleRadians(fromPos, GetTargetPos(compassStack));
    }

    public override void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer player, ItemSlot slot) {
      SetTargetPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
    }
  }
}

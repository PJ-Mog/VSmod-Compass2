using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    protected override void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer player, ItemSlot slot) {
      SetTargetPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
    }
  }
}

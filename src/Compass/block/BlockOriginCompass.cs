using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Compass {
  class BlockOriginCompass : BlockCompass {
    protected override void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer byPlayer, ItemSlot slot) {
      SetTargetPos(slot.Itemstack, world.DefaultSpawnPosition.AsBlockPos);
    }
  }
}

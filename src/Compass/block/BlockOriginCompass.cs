using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Compass {
  public class BlockOriginCompass : BlockCompass {
    protected override void OnSuccessfullyCrafted(IServerWorldAccessor world, IServerPlayer byPlayer, ItemSlot slot) {
      base.OnSuccessfullyCrafted(world, byPlayer, slot);
      SetTargetPos(slot.Itemstack, world.DefaultSpawnPosition.AsBlockPos);
    }
  }
}

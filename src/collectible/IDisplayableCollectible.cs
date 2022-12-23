using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public interface IDisplayableCollectible {
    IAdjustableItemStackRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos);
  }
}

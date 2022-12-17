using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public interface IDisplayableCollectible {
    IAdjustableRenderer CreateCustomRenderer(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos);
  }
}

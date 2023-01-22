using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rendering {
  public interface IContainedRenderer {
    IAdjustableItemStackRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos);
  }
}

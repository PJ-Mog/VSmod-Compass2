using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ContainedStackRenderer {
  public interface IContainedRenderer {
    IAdjustableItemStackRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos);
  }
}

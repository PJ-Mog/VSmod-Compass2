using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  public static class BlockEntityDisplayCaseExtension {
    public static void UpdateRenderer(this BlockEntityDisplayCase blockEntityDisplayCase, int index) {
      var renderers = blockEntityDisplayCase.GetRenderers();
      var itemStack = blockEntityDisplayCase.Inventory[index].Itemstack;
      var displayable = itemStack?.Collectible as IDisplayableCollectible;
      if (displayable == null) {
        renderers[index]?.Dispose();
        renderers[index] = null;
        return;
      }
      if (itemStack.GetHashCode(null) == renderers[index]?.ItemStackHashCode) {
        return;
      }

      renderers[index]?.Dispose();
      var newRenderer = displayable.CreateRendererFromStack(blockEntityDisplayCase.Api as ICoreClientAPI, itemStack, blockEntityDisplayCase.Pos);
      newRenderer.SetOffset(blockEntityDisplayCase.GetDisplayOffsetForSlot(index));
      newRenderer.SetScale(0.75f);
      renderers[index] = newRenderer;
    }

    public static Vec3f GetDisplayOffsetForSlot(this BlockEntityDisplayCase blockEntityDisplayCase, int index) {
      float x = index % 2 == 0 ? -0.1875f : 0.1875f;
      float y = 0.063125f;
      float z = index > 1 ? 0.1875f : -0.1875f;
      return new Vec3f(x, y, z);
    }
  }
}

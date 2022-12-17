using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  public static class BlockEntityDisplayCaseExtension {
    public static void UpdateRenderer(this BlockEntityDisplayCase blockEntityDisplayCase, int index) {
      var renderers = blockEntityDisplayCase.GetRenderers();
      renderers[index]?.Dispose();
      renderers[index] = null;

      var itemSlot = blockEntityDisplayCase.Inventory[index];
      var displayable = itemSlot.Itemstack?.Collectible as IDisplayableCollectible;
      if (displayable == null) { return; }

      var renderer = displayable.CreateCustomRenderer(blockEntityDisplayCase.Api as ICoreClientAPI, itemSlot.Itemstack, blockEntityDisplayCase.Pos);
      renderer?.SetOffset(blockEntityDisplayCase.GetDisplayOffsetForSlot(index));
      renderer?.SetScale(0.75f);
      renderers[index] = renderer;
    }

    public static Vec3f GetDisplayOffsetForSlot(this BlockEntityDisplayCase blockEntityDisplayCase, int index) {
      float x = index % 2 == 0 ? -0.1875f : 0.1875f;
      float y = 0.063125f;
      float z = index > 1 ? 0.1875f : -0.1875f;
      return new Vec3f(x, y, z);
    }
  }
}

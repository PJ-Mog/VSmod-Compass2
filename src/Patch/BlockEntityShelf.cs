using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  public static class BlockEntityShelfExtension {
    public static void UpdateRenderer(this BlockEntityShelf blockEntityShelf, int index) {
      var renderers = blockEntityShelf.GetRenderers();
      renderers[index]?.Dispose();
      renderers[index] = null;

      var itemSlot = blockEntityShelf.Inventory[index];
      var displayable = itemSlot.Itemstack?.Collectible as IDisplayableCollectible;
      if (displayable == null) { return; }

      var renderer = displayable.CreateCustomRenderer(blockEntityShelf.Api as ICoreClientAPI, itemSlot.Itemstack, blockEntityShelf.Pos);
      renderer?.SetOffset(blockEntityShelf.GetDisplayOffsetForSlot(index));
      renderers[index] = renderer;
    }

    public static Vec3f GetDisplayOffsetForSlot(this BlockEntityShelf blockEntityShelf, int index) {
      float x = index % 4 >= 2 ? 0.25f : -0.25f;
      float y = index >= 4 ? 0.625f : 0.125f;
      float z = index % 2 == 0 ? -0.25f : 0.125f;
      var mat = new Matrixf().RotateYDeg(blockEntityShelf.Block.Shape.rotateY);
      var offset = mat.TransformVector(new Vec4f(x, y, z, 0f)).XYZ;
      return offset;
    }
  }
}

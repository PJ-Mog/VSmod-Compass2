using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  public static class BlockEntityShelfExtension {
    public static void UpdateRenderer(this BlockEntityShelf blockEntityShelf, int index) {
      var renderers = blockEntityShelf.GetRenderers();
      var itemStack = blockEntityShelf.Inventory[index].Itemstack;
      var displayable = itemStack?.Collectible as IContainedRenderer;
      if (displayable == null) {
        renderers[index]?.Dispose();
        renderers[index] = null;
        return;
      }
      if (itemStack.GetHashCode(null) == renderers[index]?.ItemStackHashCode) {
        return;
      }

      renderers[index]?.Dispose();
      var newRenderer = displayable.CreateRendererFromStack(blockEntityShelf.Api as ICoreClientAPI, itemStack, blockEntityShelf.Pos);
      newRenderer.SetOffset(blockEntityShelf.GetDisplayOffsetForSlot(index));
      renderers[index] = newRenderer;
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

using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace ContainedStackRenderer {
  public interface IAdjustableItemStackRenderer : IRenderer {
    // Set this to the hash of the ItemStack used to generate the renderer.
    // It is used to determine when the renderer needs to be remade.
    int ItemStackHashCode { get; set; }
    Vec3f Offset { get; set; }
    float BlockRotationRadians { get; set; }
    float Scale { get; set; }
  }
}

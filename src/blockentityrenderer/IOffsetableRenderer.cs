using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Compass {
  public interface IOffsetableRenderer : IRenderer {
    void SetOffset(Vec3f rendererOffset);
  }
}

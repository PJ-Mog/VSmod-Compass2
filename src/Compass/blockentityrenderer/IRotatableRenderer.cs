using Vintagestory.API.Client;

namespace Compass {
  public interface IRotatableRenderer : IRenderer {
    void SetRotation(float blockRotation);
  }
}

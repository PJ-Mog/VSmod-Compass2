namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
    public string MaximumPreGeneratedMeshesDesc = "Maximum number of meshes to use for animating needle movement of held compasses. [Default: 120, Min: 8]";
    public int MaximumPreGeneratedMeshes = 120;
    internal int MaximumPreGeneratedMeshesMin = 8;
    public string ThirdPersonRenderUpdateTickIntervalMsDesc = "Milliseconds between updates to compasses rendered in another player's hand. Only updates on game ticks. [Default: 1, Min: 1]";
    public int ThirdPersonRenderUpdateTickIntervalMs = 1;
    internal int ThirdPersonRenderUpdateTickIntervalMsMin = 1;
  }
}

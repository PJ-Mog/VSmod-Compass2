namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
    public string MaximumPreGeneratedMeshesDesc = "Maximum number of meshes to use for animating needle movement.";
    public int MaximumPreGeneratedMeshes = 120;
    private int MaximumPreGeneratedMeshesMin = 8;
    public string ThirdPersonRenderUpdateTickIntervalMsDesc = "Number of milliseconds between updates to compasses rendered on other players. Will update on next game tick after this interval. [Default: 1, Min: 1]";
    public int ThirdPersonRenderUpdateTickIntervalMs = 1;
    private int ThirdPersonRenderUpdateTickIntervalMsMin = 1;
  }
}

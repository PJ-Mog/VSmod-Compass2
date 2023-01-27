namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
    public string MaximumPreGeneratedMeshesDesc = "Maximum number of meshes to use for animating needle movement of held compasses. [Default: 120, Min: 8]";
    public int MaximumPreGeneratedMeshes = 120;
    internal int MaximumPreGeneratedMeshesMin = 8;

    public string ThirdPersonRenderUpdateTickIntervalMsDesc = "Milliseconds between updates to compasses rendered in another player's hand. Only updates on game ticks. [Default: 1, Min: 1]";
    public int ThirdPersonRenderUpdateTickIntervalMs = 1;
    internal int ThirdPersonRenderUpdateTickIntervalMsMin = 1;

    public string PlacedCompassRenderUpdateTickIntervalMsDesc = "Milliseconds between updates to compasses which are placed as blocks or displayed inside another. Only affects compasses with moving targets and only updates on game ticks. [Default: 500, Min: 1]";
    public int PlacedCompassRenderUpdateTickIntervalMs = 500;
    internal int PlacedCompassRenderUpdateTickIntervalMsMin = 1;
  }
}

namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
    public string MaximumPreGeneratedMeshesDesc = "Maximum number of meshes to use for animating needle movement.";
    public int MaximumPreGeneratedMeshes = 120;
    private int MaximumPreGeneratedMeshesMin = 8;
  }
}

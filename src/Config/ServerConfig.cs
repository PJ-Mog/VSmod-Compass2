namespace Compass.ConfigSystem {
  public class ServerConfig : Config {
    public string EnableMagneticRecipeDesc = "Enable crafting a Magnetic Compass with a Magnetite Nugget.";
    public bool EnableMagneticRecipe = true;
    public string EnableScrapRecipeDesc = "Enable additional recipe for the Magnetic Compass. Uses Metal Scraps instead of Magnetite.";
    public bool EnableScrapRecipe = true;
    public string EnableOriginRecipeDesc = "Allow the Origin Compass to be crafted. <REQUIRED TO CRAFT THE RELATIVE COMPASS>";
    public bool EnableOriginRecipe = true;
    public string EnableRelativeRecipeDesc = "Allow the Relative Compass to be crafted.";
    public bool EnableRelativeRecipe = true;
    public string OriginCompassGearsDesc = $"Number of Temporal Gears required to craft the Origin Compass. Min: 1, Max: 8";
    public int OriginCompassGears = 2;
    internal int OriginCompassGearsMin = 1;
    internal int OriginCompassGearsMax = 8;
    public string RelativeCompassGearsDesc = "Number of Temporal Gears required to craft the Relative Compass. Min: 1, Max: 8";
    public int RelativeCompassGears = 2;
    internal int RelativeCompassGearsMin = 1;
    internal int RelativeCompassGearsMax = 8;
    public string AllowCompassesInOffhandDesc = "Allow a player to place a compass in their offhand slot.";
    public bool AllowCompassesInOffhand = true;
  }
}

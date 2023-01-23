namespace Compass.ConfigSystem {
  public class ServerConfig : Config {
    public string EnableMagneticRecipeDesc = "Allow crafting a Magnetic Compass with a Magnetite Nugget. [Default: true]";
    public bool EnableMagneticRecipe = true;
    public string EnableScrapRecipeDesc = "Allow crafting a Magnetic Compass with a Metal Scraps. [Default: true]";
    public bool EnableScrapRecipe = true;
    public string EnableOriginRecipeDesc = "Allow crafting an Origin Compass. [Default: true]";
    public bool EnableOriginRecipe = true;
    public string EnableRelativeRecipeDesc = "Allow crafting a Relative Compass. [Default: true]";
    public bool EnableRelativeRecipe = true;
    public string OriginCompassGearsDesc = "Number of Temporal Gears required to craft an Origin Compass. [Default: 2, Min: 1, Max: 8]";
    public int OriginCompassGears = 2;
    internal int OriginCompassGearsMin = 1;
    internal int OriginCompassGearsMax = 8;
    public string RelativeCompassGearsDesc = "Number of Temporal Gears required to craft a Relative Compass. [Default: 2, Min: 1, Max: 8]";
    public int RelativeCompassGears = 2;
    internal int RelativeCompassGearsMin = 1;
    internal int RelativeCompassGearsMax = 8;
    public string AllowCompassesInOffhandDesc = "Allow compasses to be placed in the offhand slot. [Default: true]";
    public bool AllowCompassesInOffhand = true;
  }
}

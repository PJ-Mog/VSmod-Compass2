using ProtoBuf;

namespace Compass.ConfigSystem {
  [ProtoContract]
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

    public string ActiveTemporalStormsAffectCompassesDesc = "During active temporal storms, compasses will be distorted. [Default: true]";
    [ProtoMember(1)]
    public bool ActiveTemporalStormsAffectCompasses = true;
    public bool ShouldSerializeActiveTemporalStormsAffectCompasses() => true;

    public string ApproachingTemporalStormsAffectCompassesDesc = "When a temporal storm is approaching, compasses will be distorted. [Default: false]";
    [ProtoMember(2)]
    public bool ApproachingTemporalStormsAffectCompasses = false;
    public bool ShouldSerializeApproachingTemporalStormsAffectCompasses() => true;

    public string ApproachingTemporalStormInterferenceBeginsDaysDesc = "Number of days before a storm that compasses will be affected by an approaching temporal storm. [Default: 0.35, Min: 0.1]";
    [ProtoMember(3)]
    public float ApproachingTemporalStormInterferenceBeginsDays = 0.35f;
    internal float ApproachingTemporalStormInterferenceBeginsDaysMin = 0.1f;
    public bool ShouldSerializeApproachingTemporalStormInterferenceBeginsDays() => true;
  }
}

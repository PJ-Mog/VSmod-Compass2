using Newtonsoft.Json;
using ProtoBuf;

namespace Compass.ConfigSystem {
  [ProtoContract]
  public class ServerConfig : Config {
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Allow crafting a Magnetic Compass with a Magnetite Nugget.")]
    public Setting<bool> EnableMagneticRecipe { get; set; } = new Setting<bool> {
      Default = true
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Allow crafting a Magnetic Compass with a Metal Scraps.")]
    public Setting<bool> EnableScrapRecipe { get; set; } = new Setting<bool> {
      Default = true
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Allow crafting a Relative Compass.")]
    public Setting<bool> EnableOriginRecipe { get; set; } = new Setting<bool> {
      Default = true
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Allow crafting a Relative Compass.")]
    public Setting<bool> EnableRelativeRecipe { get; set; } = new Setting<bool> {
      Default = true
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    [SettingDescription("Number of Temporal Gears required to craft an Origin Compass.")]
    public Setting<int> OriginCompassGears { get; set; } = new Setting<int> {
      Default = 2,
      Min = 1,
      Max = 8
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    [SettingDescription("Number of Temporal Gears required to craft a Relative Compass.")]
    public Setting<int> RelativeCompassGears { get; set; } = new Setting<int> {
      Default = 2,
      Min = 1,
      Max = 8
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Allow compasses to be placed in the offhand slot.")]
    public Setting<bool> AllowCompassesInOffhand { get; set; } = new Setting<bool> {
      Default = true
    };

    [ProtoMember(1)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("During active temporal storms, compasses will be distorted.")]
    public Setting<bool> ActiveTemporalStormsAffectCompasses { get; set; } = new Setting<bool> {
      Default = true
    };
    private bool ShouldSerializeActiveTemporalStormsAffectCompasses() => true;

    [ProtoMember(2)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("When a temporal storm is approaching, compasses will be distorted.")]
    public Setting<bool> ApproachingTemporalStormsAffectCompasses { get; set; } = new Setting<bool> {
      Default = false
    };
    private bool ShouldSerializeApproachingTemporalStormsAffectCompasses() => true;

    [ProtoMember(3)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<float>))]
    [SettingDescription("Number of days before a storm that compasses will be affected by an approaching temporal storm.")]
    public Setting<float> ApproachingTemporalStormInterferenceBeginsDays { get; set; } = new Setting<float> {
      Default = 0.35f,
      Min = 0.1f
    };
    private bool ShouldSerializeApproachingTemporalStormInterferenceBeginsDays() => true;

    [ProtoMember(4)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    [SettingDescription("Prevent crafting a Relative Compass based on temporal stability. Must be enabled for `AllowRelativeCompassCraftingBelowStability` to have any effect.")]
    public Setting<bool> RestrictRelativeCompassCraftingByStability { get; set; } = new Setting<bool> {
      Default = true
    };
    private bool ShouldSerializeRestrictRelativeCompassCraftingByStability() => true;

    [ProtoMember(5)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<float>))]
    [SettingDescription("Temporal stability at or above this value (as measured at sea level) will prevent the crafting of a Relative Compass. NOTES: Vanilla stability values range from 0 to 1.5 (2 if temporal stability is disabled). Stability values below 1 cause a reduction in player stability.")]
    public Setting<float> AllowRelativeCompassCraftingBelowStability { get; set; } = new Setting<float> {
      Default = 0.9f,
      Min = 0.1f
    };
  }
}

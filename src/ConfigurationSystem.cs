using Newtonsoft.Json;
using ProtoBuf;
using RiceConfig;

namespace Compass.ConfigSystem {
  public class CompassConfigurationSystem : ConfigurationSystem<CompassServerConfig, CompassClientConfig> {
    public override string ChannelName => "japanhasrice.compass2config";

    public override string ServerConfigFilename => "Compass2_ServerConfig.json";

    public override string ClientConfigFilename => "Compass2_ClientConfig.json";
  }

  [ProtoContract]
  public class CompassServerConfig : ServerConfig {
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableMagneticRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow crafting a Magnetic Compass with a Magnetite Nugget."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableScrapRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow crafting a Magnetic Compass with a Metal Scraps."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableOriginRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow crafting an Origin Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableRelativeRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow crafting a Relative Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> OriginCompassGears { get; set; } = new Setting<int> {
      Default = 2,
      Min = 0,
      Description = "Number of Temporal Gears required to craft an Origin Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> RelativeCompassGears { get; set; } = new Setting<int> {
      Default = 2,
      Min = 0,
      Description = "Number of Temporal Gears required to craft a Relative Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> AllowCompassesInOffhand { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow compasses to be placed in the offhand slot."
    };

    [ProtoMember(1)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> ActiveTemporalStormsAffectCompasses { get; set; } = new Setting<bool> {
      Default = true,
      Description = "During active temporal storms, compasses will be distorted."
    };
    private bool ShouldSerializeActiveTemporalStormsAffectCompasses() => true;

    [ProtoMember(2)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> ApproachingTemporalStormsAffectCompasses { get; set; } = new Setting<bool> {
      Default = false,
      Description = "When a temporal storm is approaching, compasses will be distorted."
    };
    private bool ShouldSerializeApproachingTemporalStormsAffectCompasses() => true;

    [ProtoMember(3)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<float>))]
    public Setting<float> ApproachingTemporalStormInterferenceBeginsDays { get; set; } = new Setting<float> {
      Default = 0.35f,
      Min = 0.01f,
      Description = "Number of days before a storm that compasses will be affected by an approaching temporal storm."
    };
    private bool ShouldSerializeApproachingTemporalStormInterferenceBeginsDays() => true;

    [ProtoMember(4)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> RestrictRelativeCompassCraftingByStability { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Prevent crafting a Relative Compass based on temporal stability. Must be enabled for `AllowRelativeCompassCraftingBelowStability` to have any effect."
    };
    private bool ShouldSerializeRestrictRelativeCompassCraftingByStability() => true;

    [ProtoMember(5)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<float>))]
    public Setting<float> AllowRelativeCompassCraftingBelowStability { get; set; } = new Setting<float> {
      Default = 0.9f,
      Min = 0.1f,
      Description = "Temporal stability at or above this value (as measured at sea level) will prevent the crafting of a Relative Compass. NOTES: Vanilla stability values range from 0 to 1.5 (2 if temporal stability is disabled). Stability values below 1 cause a reduction in player stability."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableSeraphRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow crafting a Seraph Compass."
    };

    [ProtoMember(6)]
    [JsonProperty, JsonConverter(typeof(SettingConverter<float>))]
    public Setting<float> DamageTakenToCraftSeraphCompass { get; set; } = new Setting<float> {
      Default = 0.5f,
      Description = "How much damage you will take to craft a Seraph Compass.",
      Min = 0.0f
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableReattuneSeraphRecipe { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow reattuning a Seraph Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<bool>))]
    public Setting<bool> EnableReattuneRelativeCompass { get; set; } = new Setting<bool> {
      Default = true,
      Description = "Allow reattuning a Relative Compass."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> RelativeCompassGearsToReattune { get; set; } = new Setting<int> {
      Default = 1,
      Min = 0,
      Description = "Number of Temporal Gears required to reattune a Relative Compass to a new location."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> SeraphCompassGearsToReattune { get; set; } = new Setting<int> {
      Default = 0,
      Min = 0,
      Description = "Number of Temporal Gears required to reattune a Seraph Compass to a new seraph."
    };
  }

  public class CompassClientConfig : ClientConfig {
    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> MaximumPreGeneratedMeshes { get; set; } = new Setting<int> {
      Default = 120,
      Min = 8,
      Description = "Maximum number of meshes to use for animating needle movement of held compasses."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> ThirdPersonRenderUpdateTickIntervalMs { get; set; } = new Setting<int> {
      Default = 1,
      Min = 1,
      Description = "Milliseconds between updates to compasses rendered in another player's hand. Only updates on game ticks."
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    public Setting<int> PlacedCompassRenderUpdateTickIntervalMs { get; set; } = new Setting<int> {
      Default = 500,
      Min = 1,
      Description = "Milliseconds between updates to compasses which are placed as blocks or displayed inside another. Only affects compasses with moving targets and only updates on game ticks."
    };
  }
}

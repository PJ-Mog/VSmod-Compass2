using Newtonsoft.Json;

namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    [SettingDescription("Maximum number of meshes to use for animating needle movement of held compasses.")]
    public Setting<int> MaximumPreGeneratedMeshes { get; set; } = new Setting<int> {
      Default = 120,
      Min = 8
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    [SettingDescription("Milliseconds between updates to compasses rendered in another player's hand. Only updates on game ticks.")]
    public Setting<int> ThirdPersonRenderUpdateTickIntervalMs { get; set; } = new Setting<int> {
      Default = 1,
      Min = 1
    };

    [JsonProperty, JsonConverter(typeof(SettingConverter<int>))]
    [SettingDescription("Milliseconds between updates to compasses which are placed as blocks or displayed inside another. Only affects compasses with moving targets and only updates on game ticks.")]
    public Setting<int> PlacedCompassRenderUpdateTickIntervalMs { get; set; } = new Setting<int> {
      Default = 500,
      Min = 1
    };
  }
}

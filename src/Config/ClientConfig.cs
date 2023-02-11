using Newtonsoft.Json;

namespace Compass.ConfigSystem {
  public class ClientConfig : Config {
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

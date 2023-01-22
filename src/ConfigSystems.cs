using Vintagestory.API.Common;

namespace Compass.ConfigSystem {
  public class CompassConfigClient : ModSystem {
    public ClientConfig Settings;
    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Client;
    }

    public override void StartPre(ICoreAPI api) {
      base.StartPre(api);
      Settings = Config.LoadOrCreateDefault<ClientConfig>(api, "Compass2_ClientConfig.json");
    }
  }

  public class CompassConfigServer : ModSystem {
    public ServerConfig Settings;
    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Server;
    }

    public override void StartPre(ICoreAPI api) {
      base.StartPre(api);
      Settings = Config.LoadOrCreateDefault<ServerConfig>(api, "Compass2_ServerConfig.json");
    }
  }
}

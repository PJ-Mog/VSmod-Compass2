using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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
    public static readonly string ChannelName = "japanhasrice.compass2";
    private IServerNetworkChannel ServerChannel;
    private IClientNetworkChannel ClientChannel;
    public delegate void ServerSettingsDelegate(ServerConfig serverSettings);
    public event ServerSettingsDelegate SettingsReceived;
    public ServerConfig Settings;

    public override bool ShouldLoad(EnumAppSide forSide) {
      return true;
    }

    public override void StartPre(ICoreAPI api) {
      base.StartPre(api);
      if (api.Side == EnumAppSide.Server) {
        Settings = Config.LoadOrCreateDefault<ServerConfig>(api, "Compass2_ServerConfig.json");
      }
    }

    public override void StartServerSide(ICoreServerAPI api) {
      base.StartServerSide(api);
      ServerChannel = api.Network.RegisterChannel(ChannelName).RegisterMessageType<ServerConfig>();
      api.Event.PlayerJoin += OnPlayerJoin;
    }

    private void OnPlayerJoin(IServerPlayer player) {
      ServerChannel.SendPacket(Settings, player);
    }

    public override void StartClientSide(ICoreClientAPI api) {
      base.StartClientSide(api);
      ClientChannel = api.Network.RegisterChannel(ChannelName).RegisterMessageType<ServerConfig>();
      ClientChannel.SetMessageHandler<ServerConfig>(OnReceivedServerSettings);
    }

    private void OnReceivedServerSettings(ServerConfig settings) {
      Settings = settings;
      SettingsReceived?.Invoke(Settings);
    }
  }
}

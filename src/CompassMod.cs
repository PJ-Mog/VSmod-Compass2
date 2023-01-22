using Compass.Client;
using Compass.Common;
using Compass.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Compass {
  public class CompassMod : ModSystem {
    public static readonly string HarmonyId = "compass2.japanhasrice";
    public ClientConfig ClientConfig;
    public ServerConfig ServerConfig;
    private ICoreClientAPI Capi;

    public override void StartPre(ICoreAPI api) {
      base.StartPre(api);
      ClientConfig = Config.LoadOrCreateDefault<ClientConfig>(api, "Compass2 Client Config.json");
      ServerConfig = Config.LoadOrCreateDefault<ServerConfig>(api, "Compass2 Server Config.json");
    }

    public override void Start(ICoreAPI api) {
      base.Start(api);

      RegisterModClasses(api);
    }

    public void RegisterModClasses(ICoreAPI api) {
      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));
      api.RegisterBlockClass("BlockPlayerCompass", typeof(BlockPlayerCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityXZTracker));
    }

    public override void StartClientSide(ICoreClientAPI capi) {
      base.StartClientSide(capi);
      Capi = capi;
      capi.World.RegisterGameTickListener(ThirdPersonCompassHandlingTick, 1, 5000);
    }

    protected void ThirdPersonCompassHandlingTick(float dt) {
      var onlinePlayers = Capi.World.AllOnlinePlayers;
      if (onlinePlayers.Length < 2) { return; }

      foreach (var player in onlinePlayers) {
        var playerEntity = player.Entity;
        if (playerEntity == null) { continue; }
        var stack = player.InventoryManager?.ActiveHotbarSlot?.Itemstack;
        (stack?.Collectible as BlockCompass)?.SetHoldingEntityData(stack, playerEntity);
      }
    }
  }
}

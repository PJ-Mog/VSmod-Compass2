using System.Reflection;
using Compass.ConfigSystem;
using HarmonyLib;
using Vintagestory.API.Common;

namespace Compass.Rendering {
  public class CompassRenderingSystem : ModSystem {
    public static readonly string HarmonyId = "contained-stack-renderer-mod";

    private ICoreAPI api;

    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Client;
    }

    public override void Start(ICoreAPI api) {
      base.Start(api);

      ApplyHarmonyPatches();

      var thirdPersonRenderUpdateTickInterval = api.ModLoader.GetModSystem<CompassConfigClient>().Settings.ThirdPersonRenderUpdateTickIntervalMs;
      api.World.RegisterGameTickListener(ThirdPersonCompassHandlingTick, thirdPersonRenderUpdateTickInterval, 5000);
    }

    public override void Dispose() {
      base.Dispose();

      RemoveHarmonyPatches();
    }

    private void ApplyHarmonyPatches() {
      new Harmony(HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
    }

    public void RemoveHarmonyPatches() {
      new Harmony(HarmonyId).UnpatchAll(HarmonyId);
    }

    protected void ThirdPersonCompassHandlingTick(float dt) {
      var onlinePlayers = api.World.AllOnlinePlayers;
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

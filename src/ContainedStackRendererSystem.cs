using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;

namespace ContainedStackRenderer {
  public class ContainedStackRendererMod : ModSystem {
    public static readonly string HarmonyId = "contained-stack-renderer-mod";

    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Client;
    }

    public override void Start(ICoreAPI api) {
      base.Start(api);

      ApplyHarmonyPatches();
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
  }
}

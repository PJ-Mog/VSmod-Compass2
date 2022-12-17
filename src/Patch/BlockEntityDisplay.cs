using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  [HarmonyPatch(typeof(BlockEntityDisplay))]
  public static class BlockEntityDisplayPatch {
    [HarmonyPostfix]
    [HarmonyPatch("updateMesh")]
    public static void UpdateRenderer(BlockEntityDisplay __instance, int index) {
      if (__instance?.Api?.Side == EnumAppSide.Server) { return; }
      (__instance as BlockEntityDisplayCase)?.UpdateRenderer(index);
      (__instance as BlockEntityShelf)?.UpdateRenderer(index);
    }
  }

  public static class BlockEntityDisplayExtension {
    private static Dictionary<BlockPos, IRenderer[]> allRenderers = new Dictionary<BlockPos, IRenderer[]>();

    public static IRenderer[] GetRenderers(this BlockEntityDisplay blockEntityDisplay) {
      if (!allRenderers.TryGetValue(blockEntityDisplay.Pos, out IRenderer[] beRenderers)) {
        beRenderers = new IRenderer[blockEntityDisplay.Inventory.Count];
        allRenderers.Add(blockEntityDisplay.Pos, beRenderers);
      }
      return beRenderers;
    }

    public static void DisposeRenderers(this BlockEntityDisplay blockEntityDisplay) {
      var renderers = blockEntityDisplay.GetRenderers();
      for (int i = 0; i < renderers.Length; i++) {
        renderers[i]?.Dispose();
        renderers[i] = null;
      }
    }
  }
}

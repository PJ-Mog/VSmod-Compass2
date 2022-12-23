using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
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
    public static IAdjustableRenderer[] GetRenderers(this BlockEntityDisplay blockEntityDisplay) {
      var key = GetKeyFor(blockEntityDisplay.Pos);
      return ObjectCacheUtil.GetOrCreate(blockEntityDisplay.Api, key, () => {
        return new IAdjustableRenderer[blockEntityDisplay.Inventory.Count];
      });
    }

    public static void DisposeRenderers(this BlockEntityDisplay blockEntityDisplay) {
      var renderers = blockEntityDisplay.GetRenderers();
      for (int i = 0; i < renderers.Length; i++) {
        renderers[i]?.Dispose();
        renderers[i] = null;
      }
      var key = GetKeyFor(blockEntityDisplay.Pos);
      ObjectCacheUtil.Delete(blockEntityDisplay.Api, key);
    }

    private static string GetKeyFor(BlockPos pos) {
      return "blockentitydisplay-renderers-" + pos;
    }
  }
}

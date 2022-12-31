using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ContainedStackRenderer.Patch {
  //  Cannot patch BlockEntityDisplay, BlockEntityGroundStorage, etc.
  //  The subclasses do not override the base functions,
  //  causing Harmony to fail to find the methods
  [HarmonyPatch(typeof(BlockEntity))]
  public static class BlockEntityPatch {
    [HarmonyPrefix]
    [HarmonyPatch("OnBlockRemoved")]
    public static void Removed(BlockEntity __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Server) { return; }
      (__instance as BlockEntityDisplay)?.DisposeRenderers();
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnBlockUnloaded")]
    public static void Unloaded(BlockEntity __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Server) { return; }
      (__instance as BlockEntityDisplay)?.DisposeRenderers();
    }
  }
}

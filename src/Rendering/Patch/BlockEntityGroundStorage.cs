using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Rendering.Patch {
  [HarmonyPatch(typeof(BlockEntityGroundStorage))]
  public static class BlockEntityGroundStoragePatch {
    [HarmonyPostfix()]
    [HarmonyPatch("updateMeshes")]
    public static void UpdateRenderers(BlockEntityGroundStorage __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Server) { return; }

      lock (__instance.inventoryLock) {
        var renderers = __instance.GetRenderers();
        for (int i = 0; i < renderers.Length; i++) {
          __instance.UpdateRenderer(renderers, i);
        }
      }
    }
  }

  public static class BlockEntityGroundStorageExtension {
    public static void UpdateRenderer(this BlockEntityGroundStorage blockEntityGroundStorage, IAdjustableItemStackRenderer[] renderers, int index) {
      var itemStack = blockEntityGroundStorage.Inventory[index].Itemstack;
      if (itemStack?.Collectible is IContainedRenderer displayable) {
        var offset = blockEntityGroundStorage.GetDisplayOffsetForSlot(index);
        if (itemStack.GetHashCode(null) == renderers[index]?.ItemStackHashCode) {
          renderers[index].Offset = offset;
          return;
        }
        renderers[index]?.Dispose();
        var newRenderer = displayable.CreateRendererFromStack(blockEntityGroundStorage.Api as ICoreClientAPI, itemStack, blockEntityGroundStorage.Pos);
        newRenderer.Offset = offset;
        renderers[index] = newRenderer;
        return;
      }
      renderers[index]?.Dispose();
      renderers[index] = null;
    }

    public static Vec3f GetDisplayOffsetForSlot(this BlockEntityGroundStorage blockEntityGroundStorage, int index) {
      Vec3f offset;
      switch (blockEntityGroundStorage?.StorageProps?.Layout) {
        case EnumGroundStorageLayout.Halves:
        case EnumGroundStorageLayout.WallHalves:
          offset = GetHalvesDisplayOffsetForSlot(index);
          break;
        case EnumGroundStorageLayout.Quadrants:
          offset = GetQuadrantsDisplayOffsetForSlot(index);
          break;
        default:
          return Vec3f.Zero;
      }
      return Rotate(offset, 0f - blockEntityGroundStorage.MeshAngle);
    }

    public static Vec3f GetQuadrantsDisplayOffsetForSlot(int index) {
      float x = index < 2 ? -0.25f : 0.25f;
      float z = index % 2 == 0 ? -0.25f : 0.25f;
      return new Vec3f(x, 0f, z);
    }

    public static Vec3f GetHalvesDisplayOffsetForSlot(int index) {
      var x = index == 0 ? -0.25f : 0.25f;
      return new Vec3f(x, 0f, 0f);
    }

    public static Vec3f Rotate(Vec3f offset, float rotateY) {
      return new Matrixf().Identity()
                          .RotateY(rotateY)
                          .TransformVector(new Vec4f(offset.X, offset.Y, offset.Z, 0f))
                          .XYZ;
    }
  }
}

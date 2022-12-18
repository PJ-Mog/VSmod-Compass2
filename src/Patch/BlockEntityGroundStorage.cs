using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  [HarmonyPatch(typeof(BlockEntityGroundStorage))]
  public static class BlockEntityGroundStoragePatch {
    [HarmonyPostfix()]
    [HarmonyPatch("updateMeshes")]
    public static void UpdateRenderers(BlockEntityGroundStorage __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Server) { return; }

      lock (__instance.inventoryLock) {
        var renderers = __instance.GetRenderers();
        for (int i = 0; i < renderers.Length; i++) {
          renderers[i]?.Dispose();
          renderers[i] = null;

          var itemStack = __instance.Inventory[i]?.Itemstack;
          var displayable = itemStack?.Collectible as IDisplayableCollectible;
          if (displayable == null) { continue; }

          var renderer = displayable.CreateRendererFromStack(__instance.Api as ICoreClientAPI, itemStack, __instance.Pos);
          renderer?.SetOffset(__instance.GetDisplayOffsetForSlot(i));
          renderers[i] = renderer;
        }
      }
    }
  }

  public static class BlockEntityGroundStorageExtension {
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

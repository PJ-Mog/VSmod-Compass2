using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Compass.Patch {
  [HarmonyPatch(typeof(BlockEntityGroundStorage))]
  public static class BlockEntityGroundStoragePatch {
    const bool runOriginalMethod = true;
    const bool skipOriginalMethod = false;

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

    [HarmonyPrefix()]
    [HarmonyPatch("OnPlayerInteractStart")]
    public static bool BeforeOnPlayerInteractStart(BlockEntityGroundStorage __instance, IPlayer player, BlockSelection bs, ref ItemSlot ___isUsingSlot, ref bool __result) {
      const bool interactionSuccessful = true;
      const bool interactionFailed = false;

      var isSneaking = player.Entity.Controls.Sneak;

      if (!isSneaking) {
        return runOriginalMethod;
      }

      var layout = __instance?.StorageProps?.Layout;
      if (layout == null || layout == EnumGroundStorageLayout.Stacking) {
        return runOriginalMethod;
      }

      MethodInfo rotatedOffset = typeof(BlockEntityGroundStorage).GetMethod("rotatedOffset", BindingFlags.Instance | BindingFlags.NonPublic);
      Vec3f hitPos = rotatedOffset.Invoke(__instance, new object[] { bs.HitPosition.ToVec3f(), __instance.MeshAngle }) as Vec3f;
      int inventoryIndex = 0;
      switch (layout) {
        case EnumGroundStorageLayout.Halves:
        case EnumGroundStorageLayout.WallHalves:
          inventoryIndex = hitPos.X <= 0.5 ? 0 : 1;
          break;
        case EnumGroundStorageLayout.Quadrants:
          inventoryIndex = (hitPos.X > 0.5 ? 2 : 0) + (hitPos.Z > 0.5 ? 1 : 0);
          break;
      }
      var storedCollectible = __instance.Inventory[inventoryIndex].Itemstack?.Collectible;
      if (storedCollectible == null) {
        return runOriginalMethod;
      }


      var interactable = storedCollectible?.GetBehavior<CollectibleBehaviorContainedInteractable>();
      if (interactable == null) {
        __result = interactionFailed;
        return skipOriginalMethod;
      }

      ___isUsingSlot = __instance.Inventory[inventoryIndex];
      __result = interactable.OnContainedInteractStart(__instance, ___isUsingSlot, player, bs);
      return skipOriginalMethod;
    }

    [HarmonyPrefix()]
    [HarmonyPatch("OnPlayerInteractStep")]
    public static bool BeforeOnPlayerInteractStep(BlockEntityGroundStorage __instance, float secondsUsed, IPlayer byPlayer, BlockSelection blockSel, ref ItemSlot ___isUsingSlot, ref bool __result) {
      bool allowOriginalMethod = true;
      var interactable = ___isUsingSlot?.Itemstack?.Collectible.GetBehavior<CollectibleBehaviorContainedInteractable>();
      if (interactable?.IsInteracting ?? false) {
        __result = interactable.OnContainedInteractStep(secondsUsed, __instance, ___isUsingSlot, byPlayer, blockSel);
        allowOriginalMethod = false;
      }
      return allowOriginalMethod;
    }

    [HarmonyPrefix()]
    [HarmonyPatch("OnPlayerInteractStop")]
    public static bool BeforeOnPlayerInteractStop(BlockEntityGroundStorage __instance, float secondsUsed, IPlayer byPlayer, BlockSelection blockSel, ref ItemSlot ___isUsingSlot) {
      bool allowOriginalMethod = true;
      var interactable = ___isUsingSlot?.Itemstack?.Collectible.GetBehavior<CollectibleBehaviorContainedInteractable>();
      if (interactable?.IsInteracting ?? false) {
        interactable.OnContainedInteractStop(secondsUsed, __instance, ___isUsingSlot, byPlayer, blockSel);
        allowOriginalMethod = false;
      }
      return allowOriginalMethod;
    }
  }

  public static class BlockEntityGroundStorageExtension {
    public static void UpdateRenderer(this BlockEntityGroundStorage blockEntityGroundStorage, IAdjustableItemStackRenderer[] renderers, int index) {
      var itemStack = blockEntityGroundStorage.Inventory[index].Itemstack;
      if (itemStack?.Collectible is IContainedRenderer displayable) {
        var offset = blockEntityGroundStorage.GetDisplayOffsetForSlot(index);
        if (itemStack.GetHashCode(null) == renderers[index]?.ItemStackHashCode) {
          renderers[index].SetOffset(offset);
          return;
        }
        renderers[index]?.Dispose();
        var newRenderer = displayable.CreateRendererFromStack(blockEntityGroundStorage.Api as ICoreClientAPI, itemStack, blockEntityGroundStorage.Pos);
        newRenderer.SetOffset(offset);
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

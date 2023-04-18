using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Compass.Rendering {
  [HarmonyPatch]
  public static class Patch {
    //  Cannot patch BlockEntityDisplay, BlockEntityGroundStorage, etc.
    //  The subclasses do not override the base functions,
    //  causing Harmony to fail to find the methods
    [HarmonyPatch(typeof(BlockEntity), "OnBlockRemoved")]
    [HarmonyPrefix]
    public static void BeforeOnBlockRemoved(BlockEntity __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Client && __instance is BlockEntityDisplay blockEntityDisplay) {
        blockEntityDisplay.DisposeRenderers();
      }
    }

    [HarmonyPatch(typeof(BlockEntity), "OnBlockUnloaded")]
    [HarmonyPrefix]
    public static void BeforeOnBlockUnloaded(BlockEntity __instance) {
      if (__instance?.Api?.Side == EnumAppSide.Client && __instance is BlockEntityDisplay blockEntityDisplay) {
        blockEntityDisplay.DisposeRenderers();
      }
    }

    [HarmonyPatch(typeof(BlockEntityDisplay), "updateMesh")]
    [HarmonyPostfix]
    public static void UpdateRenderer(BlockEntityDisplay __instance, int index) {
      if (__instance?.Api?.Side != EnumAppSide.Client) { return; }
      __instance.UpdateRenderer(index);
    }
  }

  public static class BlockEntityDisplayExtension {
    public static void UpdateRenderer(this BlockEntityDisplay blockEntityDisplay, int forSlotIndex) {
      var renderers = blockEntityDisplay.GetRenderers();
      var itemStack = blockEntityDisplay.Inventory[forSlotIndex].Itemstack;
      var displayable = itemStack?.Collectible as IContainedRenderer;
      if (displayable == null) {
        renderers[forSlotIndex]?.Dispose();
        renderers[forSlotIndex] = null;
        return;
      }

      if (itemStack.GetHashCode(null) != renderers[forSlotIndex]?.ItemStackHashCode) {
        renderers[forSlotIndex]?.Dispose();
        renderers[forSlotIndex] = displayable.CreateRendererFromStack(blockEntityDisplay.Api as ICoreClientAPI, itemStack, blockEntityDisplay.Pos);
      }

      renderers[forSlotIndex].Offset = blockEntityDisplay.GetOffset(forSlotIndex);
      renderers[forSlotIndex].Scale = blockEntityDisplay.GetScale();
    }

    public static IAdjustableItemStackRenderer[] GetRenderers(this BlockEntityDisplay blockEntityDisplay) {
      var key = GetKeyFor(blockEntityDisplay.Pos);
      return ObjectCacheUtil.GetOrCreate(blockEntityDisplay.Api, key, () => {
        return new IAdjustableItemStackRenderer[blockEntityDisplay.Inventory.Count];
      });
    }

    public static void DisposeRenderers(this BlockEntityDisplay blockEntityDisplay) {
      var renderers = blockEntityDisplay.GetRenderers();
      for (int i = 0; i < renderers.Length; i++) {
        renderers[i]?.Dispose();
        renderers[i] = null;
      }
      ObjectCacheUtil.Delete(blockEntityDisplay.Api, GetKeyFor(blockEntityDisplay.Pos));
    }

    private static string GetKeyFor(BlockPos pos) {
      return "blockentitydisplay-renderers-" + pos;
    }

    // Having issues with the `tfMatrices` field in `BlockEntityDisplay`. It is accessible with reflection,
    // but the values are not as expected. Until the problem is figured out, continuing with this implementation
    // where the offsets are pre-calculated per blocktype
    public static Vec3f GetOffset(this BlockEntityDisplay blockEntityDisplay, int forSlotIndex) {
      switch (blockEntityDisplay) {
        case BlockEntityDisplayCase displayCase:
          return displayCase.GetOffset(forSlotIndex);
        case BlockEntityShelf shelf:
          return shelf.GetOffset(forSlotIndex);
        case BlockEntityGroundStorage groundStorage:
          return groundStorage.GetOffset(forSlotIndex);
        default:
          return Vec3f.Zero;
      }
    }

    public static float GetScale(this BlockEntityDisplay blockEntityDisplay) {
      if (blockEntityDisplay is BlockEntityDisplayCase) {
        return 0.75f;
      }
      return 1f;
    }
  }

  public static class BlockEntityDisplayCaseExtension {
    private static Vec3f[] Offsets = new Vec3f[] {
      new Vec3f(-0.1875f, 0.063125f, -0.1875f),
      new Vec3f(0.1875f, 0.063125f, -0.1875f),
      new Vec3f(-0.1875f, 0.063125f, 0.1875f),
      new Vec3f(0.1875f, 0.063125f, 0.1875f)
    };

    private static Vec3f CenterOffset = new Vec3f(0f, 0.063125f, 0f);

    public static Vec3f GetOffset(this BlockEntityDisplayCase blockEntityDisplayCase, int forSlotIndex) {
      if (blockEntityDisplayCase.GetType().GetField("haveCenterPlacement", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(blockEntityDisplayCase) as bool? == true) {
        return CenterOffset;
      }
      return Offsets[forSlotIndex];
    }
  }

  public static class BlockEntityShelfExtension {
    private static Vec3f[] Offsets = new Vec3f[] {
      new Vec3f(-0.25f, 0.125f, -0.25f),
      new Vec3f(-0.25f, 0.125f, 0.125f),
      new Vec3f(0.25f, 0.125f, -0.25f),
      new Vec3f(0.25f, 0.125f, 0.125f),
      new Vec3f(-0.25f, 0.625f, -0.25f),
      new Vec3f(-0.25f, 0.625f, 0.125f),
      new Vec3f(0.25f, 0.625f, -0.25f),
      new Vec3f(0.25f, 0.625f, 0.125f)
    };

    public static Vec3f GetOffset(this BlockEntityShelf blockEntityShelf, int forSlotIndex) {
      return Rotate(Offsets[forSlotIndex], blockEntityShelf.Block.Shape.rotateY);
    }

    private static Vec3f Rotate(Vec3f offset, float rotateYDeg) {
      return new Matrixf().Identity()
                          .RotateYDeg(rotateYDeg)
                          .TransformVector(offset.ToVec4f(0f)).XYZ;
    }
  }

  public static class BlockEntityGroundStorageExtension {
    private static Dictionary<EnumGroundStorageLayout, Vec3f[]> Offsets = new Dictionary<EnumGroundStorageLayout, Vec3f[]> {
      { EnumGroundStorageLayout.Halves, new Vec3f[] {
        new Vec3f(-0.25f, 0f, 0f),
        new Vec3f(0.25f, 0f, 0f)
      }},
      { EnumGroundStorageLayout.WallHalves, new Vec3f[] {
        new Vec3f(-0.25f, 0f, 0f),
        new Vec3f(0.25f, 0f, 0f)
      }},
      { EnumGroundStorageLayout.Quadrants, new Vec3f[] {
        new Vec3f(-0.25f, 0f, -0.25f),
        new Vec3f(-0.25f, 0f, 0.25f),
        new Vec3f(0.25f, 0f, -0.25f),
        new Vec3f(0.25f, 0f, 0.25f)
      }}
    };

    public static Vec3f GetOffset(this BlockEntityGroundStorage blockEntityGroundStorage, int forSlotIndex) {
      Vec3f result;
      if (Offsets.TryGetValue(blockEntityGroundStorage?.StorageProps?.Layout ?? EnumGroundStorageLayout.SingleCenter, out Vec3f[] offsets)) {
        result = offsets[forSlotIndex];
      }
      else {
        result = Vec3f.Zero;
      }
      return Rotate(result, 0f - blockEntityGroundStorage.MeshAngle);
    }

    private static Vec3f Rotate(Vec3f offset, float rotateYRad) {
      return new Matrixf().Identity()
                          .RotateY(rotateYRad)
                          .TransformVector(new Vec4f(offset.X, offset.Y, offset.Z, 0f))
                          .XYZ;
    }
  }
}

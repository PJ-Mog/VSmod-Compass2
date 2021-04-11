using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {
  abstract class BlockCompass : Block {
    int MAX_ANGLED_MESHES = 60;
    MeshRef[] meshrefs;
    MeshData BaseMesh;
    MeshData NeedleMesh;
    public override void OnLoaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        OnLoadedClientSide(api as ICoreClientAPI);
      }
    }
    private void OnLoadedClientSide(ICoreClientAPI capi) {
      meshrefs = new MeshRef[MAX_ANGLED_MESHES];

      string key = Code.ToString() + "-meshes";

      var baseShape = capi.Assets.TryGet("compass:shapes/block/compass/base.json")?.ToObject<Shape>();
      var needleShape = capi.Assets.TryGet("compass:shapes/block/compass/needle.json")?.ToObject<Shape>();

      capi.Tesselator.TesselateShape(this, baseShape, out MeshData compassBaseMeshData, new Vec3f(0, 0, 0));

      meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, () => {
        for (var angleIndex = 0; angleIndex < MAX_ANGLED_MESHES; angleIndex += 1) {

          float angle = (float)((double)angleIndex / MAX_ANGLED_MESHES * 360);
          capi.Tesselator.TesselateShape(this, needleShape, out MeshData meshData, new Vec3f(0, angle, 0));

          meshData.AddMeshData(compassBaseMeshData);

          meshrefs[angleIndex] = capi.Render.UploadMesh(meshData);
        }
        return meshrefs;
      });
      // handle weird bug in VS where GUI shapes are drawn as mirror images: https://github.com/anegostudios/VintageStory-Issues/issues/839
      GuiTransform.Scale = -2.75f;
      GuiTransform.Rotate = false;
      GuiTransform.Translation.Add(-2f, 0f, 0f);
      GuiTransform.Rotation.Add(0f, 0f, 5f);
    }

    public override void OnUnloaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        for (var meshIndex = 0; meshIndex < MAX_ANGLED_MESHES; meshIndex += 1) {
          meshrefs[meshIndex]?.Dispose();
          meshrefs[meshIndex] = null;
        }
      }
    }

    public abstract float GetNeedleAngleRadians(BlockPos fromPos);

    protected static float GetAngleRadians(BlockPos fromPos, BlockPos toPos) {
      return (float)Math.Atan2(fromPos.X - toPos.X, fromPos.Z - toPos.Z);
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      if (world.Side == EnumAppSide.Server) {
        if (!HasPosSet(slot.Itemstack)) {
          var player = (slot.Inventory as InventoryBasePlayer)?.Player;
          if (player != null) {
            SetCompassCraftedPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
          }
        }
      }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      ItemStack stack = base.OnPickBlock(world, pos);

      BlockEntityCompass bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCompass;

      if (bec != null)
      {
        SetCompassCraftedPos(stack, bec.compassCraftedPos);
      }

      return stack;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    public static void SetCompassCraftedPos(ItemStack compassStack, BlockPos pos) {
      var attrs = compassStack.Attributes;
      attrs.SetInt("compass-crafted-x", pos.X);
      attrs.SetInt("compass-crafted-y", pos.Y);
      attrs.SetInt("compass-crafted-z", pos.Z);
    }

    public static BlockPos GetCompassCraftedPos(ItemStack compassStack) {
      var attrs = compassStack.Attributes;
      var x = attrs.GetInt("compass-crafted-x");
      var y = attrs.GetInt("compass-crafted-y");
      var z = attrs.GetInt("compass-crafted-z");

      return new BlockPos(x, y, z);
    }

    public static bool HasPosSet(ItemStack compassStack) {
      return compassStack.Attributes.HasAttribute("compass-crafted-z");
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
      float? angle = null;
      if (target == EnumItemRenderTarget.Gui || target == EnumItemRenderTarget.HandFp) {
        if (HasPosSet(itemstack)) {
          var player = capi.World.Player;
          angle = GetNeedleAngleRadians(player.Entity.Pos.AsBlockPos) - player.CameraYaw;
        }
        else {
          angle = null; // e.g. compass is being rendered in Handbook
        }
      }
      else {
        // TODO: think of a good solution for Ground and HandTp
        angle = null;
      }
      double milli = capi.World.ElapsedMilliseconds;
      float resolvedAngle = angle ?? ((float)(milli / 500) + ((float)Math.Sin(milli / 150)) + ((float)Math.Sin(milli / 432)) * 3);
      var bestMeshrefIndex = (int)GameMath.Mod(resolvedAngle / (Math.PI * 2) * MAX_ANGLED_MESHES + 0.5, MAX_ANGLED_MESHES);
      renderinfo.ModelRef = meshrefs[bestMeshrefIndex];
    }
  }
}

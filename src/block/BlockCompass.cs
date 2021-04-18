using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {
  public enum EnumTargetType : int {
    Stationary = 0,
    MovingFrequentUpdate = 500,
    Moving = 1000,
    MovingInfrequentUpdate = 5000
  }

  public static class EnumTargetTypeExtensions {
    public static bool IsDynamicTarget(this EnumTargetType targetType) { return targetType != EnumTargetType.Stationary; }
    public static int GetUpdateFrequencyMillis(this EnumTargetType targetType) { return (int)targetType; }
  }

  abstract class BlockCompass : Block {
    int MAX_ANGLED_MESHES = 60;
    protected static string ATTR_INT_CRAFTED_POS_X = "compass-crafted-x";
    protected static string ATTR_INT_CRAFTED_POS_Y = "compass-crafted-y";
    protected static string ATTR_INT_CRAFTED_POS_Z = "compass-crafted-z";
    MeshRef[] meshrefs;

    protected virtual float MIN_DISTANCE_TO_SHOW_DIRECTION {
      get { return 2; }
    }

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

    public abstract float GetNeedle2DAngleRadians(BlockPos fromPos, ItemStack compass);

    protected static float Get2DAngleRadians(BlockPos fromPos, BlockPos toPos) {
      return (float)Math.Atan2(fromPos.X - toPos.X, fromPos.Z - toPos.Z);
    }

    public virtual float Get2DDistanceToTarget(BlockPos fromPos, ItemStack compassStack) {
      var targetPos = GetTargetPos(compassStack);
      if (targetPos == null) return float.MaxValue;
      var dX = fromPos.X - targetPos.X;
      var dZ = fromPos.Z - targetPos.Z;
      return (float)Math.Sqrt(dX * dX + dZ * dZ);
    }

    // Should return null if either the target is not set or if the target is not a discrete position.
    public abstract BlockPos GetTargetPos(ItemStack compassStack);

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      if (world.Side == EnumAppSide.Server) {
        if (!IsCrafted(slot.Itemstack)) {
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
      attrs.SetInt(ATTR_INT_CRAFTED_POS_X, pos.X);
      attrs.SetInt(ATTR_INT_CRAFTED_POS_Y, pos.Y);
      attrs.SetInt(ATTR_INT_CRAFTED_POS_Z, pos.Z);
    }

    public static BlockPos GetCompassCraftedPos(ItemStack compassStack) {
      var attrs = compassStack.Attributes;
      var x = attrs.TryGetInt(ATTR_INT_CRAFTED_POS_X);
      var y = attrs.TryGetInt(ATTR_INT_CRAFTED_POS_Y);
      var z = attrs.TryGetInt(ATTR_INT_CRAFTED_POS_Z);
      if (x == null || y == null || z == null) return null;
      return new BlockPos((int)x, (int)y, (int)z);
    }

    public static bool IsCrafted(ItemStack compassStack) {
      return compassStack.Attributes.HasAttribute(ATTR_INT_CRAFTED_POS_X);
    }

    public virtual bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return IsCrafted(compassStack)
             && Get2DDistanceToTarget(fromPos, compassStack) >= MIN_DISTANCE_TO_SHOW_DIRECTION;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      float angle;
      var player = capi.World.Player;
      if ((renderTarget == EnumItemRenderTarget.Gui || renderTarget == EnumItemRenderTarget.HandFp) && ShouldPointToTarget(player.Entity.Pos.AsBlockPos, compassStack)) {
        angle = GetNeedle2DAngleRadians(player.Entity.Pos.AsBlockPos, compassStack) - player.CameraYaw;
      }
      else {
        // TODO: think of a good solution for Ground and HandTp
        angle = GetWildSpinAngle(capi);
      }
      var bestMeshrefIndex = (int)GameMath.Mod(angle / (Math.PI * 2) * MAX_ANGLED_MESHES + 0.5, MAX_ANGLED_MESHES);
      renderinfo.ModelRef = meshrefs[bestMeshrefIndex];
    }

    public static float GetWildSpinAngle(ICoreAPI api) {
      double milli = api.World.ElapsedMilliseconds;
      float angle = (float)((milli / 500) + (Math.Sin(milli / 150)) + (Math.Sin(milli / 432)) * 3);
      return angle;
    }
  }
}

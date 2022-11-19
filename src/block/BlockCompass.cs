using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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
    public static readonly string ATTR_BOOL_CRAFTED = "compass-is-crafted";
    public static readonly string ATTR_STR_CRAFTED_BY_PLAYER_UID = "compass-crafted-by-player-uid";
    public static readonly string ATTR_INT_TARGET_POS_X = "compass-target-x";
    public static readonly string ATTR_INT_TARGET_POS_Y = "compass-target-y";
    public static readonly string ATTR_INT_TARGET_POS_Z = "compass-target-z";
    public static readonly string ATTR_INT_ENTITY_POS_X = "compass-entity-x";
    public static readonly string ATTR_INT_ENTITY_POS_Y = "compass-entity-y";
    public static readonly string ATTR_INT_ENTITY_POS_Z = "compass-entity-z";
    public static readonly string ATTR_FLOAT_ENTITY_YAW = "compass-entity-yaw";

    internal static readonly string UNKNOWN_PLAYER_UID = "UNKNOWN";
    MeshRef[] meshrefs;

    protected virtual float MIN_DISTANCE_SQUARED_TO_SHOW_DIRECTION {
      get { return 4; }
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

    public abstract float GetNeedle2DAngleRadians(BlockPos fromPos, ItemStack compassStack);

    protected static float Get2DAngleRadians(BlockPos fromPos, BlockPos toPos) {
      return (float)Math.Atan2(fromPos.X - toPos.X, fromPos.Z - toPos.Z);
    }

    public virtual float Get2DDistanceSquaredToTarget(BlockPos fromPos, ItemStack compassStack) {
      // if the compass's target is not a discrete location, distance is max value
      var targetPos = GetTargetPos(compassStack);
      if (targetPos == null) return float.MaxValue;

      var dX = fromPos.X - targetPos.X;
      var dZ = fromPos.Z - targetPos.Z;
      return (float)(dX * dX + dZ * dZ);
    }

    // Should return null if either the target is not set or if the target is not a discrete position.
    public virtual BlockPos GetTargetPos(ItemStack compassStack) {
      if (compassStack == null) return null;
      var attrs = compassStack.Attributes;
      var x = attrs.TryGetInt(ATTR_INT_TARGET_POS_X);
      var y = attrs.TryGetInt(ATTR_INT_TARGET_POS_Y);
      var z = attrs.TryGetInt(ATTR_INT_TARGET_POS_Z);
      if (x == null || y == null || z == null) return null;
      return new BlockPos((int)x, (int)y, (int)z);
    }

    public virtual void SetTargetPos(ItemStack compassStack, BlockPos targetPos) {
      if (compassStack == null) return;
      if (targetPos == null) {
        UnsetTargetPos(compassStack);
      }
      else {
        var attrs = compassStack.Attributes;
        attrs.SetInt(ATTR_INT_TARGET_POS_X, targetPos.X);
        attrs.SetInt(ATTR_INT_TARGET_POS_Y, targetPos.Y);
        attrs.SetInt(ATTR_INT_TARGET_POS_Z, targetPos.Z);
      }
    }

    public virtual void UnsetTargetPos(ItemStack compassStack) {
      if (compassStack == null) return;
      var attrs = compassStack.Attributes;
      attrs.RemoveAttribute(ATTR_INT_TARGET_POS_X);
      attrs.RemoveAttribute(ATTR_INT_TARGET_POS_Y);
      attrs.RemoveAttribute(ATTR_INT_TARGET_POS_Z);
    }

    public virtual BlockPos GetEntityPos(ItemStack compassStack) {
      if (compassStack == null) return null;
      var attrs = compassStack.Attributes;
      var x = attrs.TryGetInt(ATTR_INT_ENTITY_POS_X);
      var y = attrs.TryGetInt(ATTR_INT_ENTITY_POS_Y);
      var z = attrs.TryGetInt(ATTR_INT_ENTITY_POS_Z);
      if (x == null || y == null || z == null) return null;
      return new BlockPos((int)x, (int)y, (int)z);
    }

    public virtual void SetEntityPos(ItemStack compassStack, BlockPos entityPos) {
      if (compassStack == null) return;
      if (entityPos == null) {
        UnsetEntityPos(compassStack);
      }
      else {
        var attrs = compassStack.Attributes;
        attrs.SetInt(ATTR_INT_ENTITY_POS_X, entityPos.X);
        attrs.SetInt(ATTR_INT_ENTITY_POS_Y, entityPos.Y);
        attrs.SetInt(ATTR_INT_ENTITY_POS_Z, entityPos.Z);
      }
    }

    public virtual void UnsetEntityPos(ItemStack compassStack) {
      if (compassStack == null) return;
      var attrs = compassStack.Attributes;
      attrs.RemoveAttribute(ATTR_INT_ENTITY_POS_X);
      attrs.RemoveAttribute(ATTR_INT_ENTITY_POS_Y);
      attrs.RemoveAttribute(ATTR_INT_ENTITY_POS_Z);
    }

    public virtual float GetEntityYaw(ItemStack compassStack) {
      return compassStack?.Attributes.TryGetFloat(ATTR_FLOAT_ENTITY_YAW) ?? 0;
    }

    public virtual void SetEntityYaw(ItemStack compassStack, float? entityYaw) {
      if (compassStack == null) return;
      if (entityYaw == null) {
        UnsetEntityYaw(compassStack);
      }
      else {
        compassStack.Attributes.SetFloat(ATTR_FLOAT_ENTITY_YAW, (float)entityYaw);
      }
    }

    public virtual void UnsetEntityYaw(ItemStack compassStack) {
      if (compassStack == null) return;
      compassStack.Attributes.RemoveAttribute(ATTR_FLOAT_ENTITY_YAW);
    }

    // Sealed to ensure inheriting classes always call certain functions to properly detect when a compass is created and placed in a player's inventory.
    // Override #OnBeforeModifiedInInventorySlot, #OnSuccessfullyCrafted, and/or #OnAfterModifiedInInventorySlot instead.
    sealed public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      OnBeforeModifiedInInventorySlot(world, slot, extractedStack);
      var player = (slot.Inventory as InventoryBasePlayer)?.Player;
      if (world.Side == EnumAppSide.Server && !IsCrafted(slot.Itemstack) && player != null) {
        SetIsCrafted(slot.Itemstack, true);
        SetCraftedByPlayerUID(slot.Itemstack, player.PlayerUID);
        OnSuccessfullyCrafted(world as IServerWorldAccessor, player, slot);
      }
      OnAfterModifiedInInventorySlot(world, slot, extractedStack);
    }

    public virtual void OnBeforeModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }
    public virtual void OnAfterModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      ItemStack compassStack = base.OnPickBlock(world, pos);

      BlockEntityCompass bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCompass;

      if (bec != null) {
        SetIsCrafted(compassStack, bec.IsCrafted);
        SetCraftedByPlayerUID(compassStack, bec.CraftedByPlayerUID);
        SetTargetPos(compassStack, bec.TargetPos);
      }

      return compassStack;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    // Called from OnModifiedInInventorySlot (server side) when the compass is first placed into a player's inventory and successfully marked as crafted
    public virtual void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer byPlayer, ItemSlot slot) { }

    internal static bool SetIsCrafted(ItemStack compassStack, bool isCrafted) {
      if (compassStack == null) return false;
      compassStack.Attributes.SetBool(ATTR_BOOL_CRAFTED, isCrafted);
      return true;
    }

    public static bool IsCrafted(ItemStack compassStack) {
      return compassStack?.Attributes.GetBool(ATTR_BOOL_CRAFTED) ?? false;
    }

    internal static bool SetCraftedByPlayerUID(ItemStack compassStack, string craftedByPlayerUID) {
      if (compassStack == null || craftedByPlayerUID == null || craftedByPlayerUID.Length == 0) return false;
      compassStack.Attributes.SetString(ATTR_STR_CRAFTED_BY_PLAYER_UID, craftedByPlayerUID);
      return true;
    }

    public static string GetCraftedByPlayerUID(ItemStack compassStack) {
      return compassStack?.Attributes.GetString(ATTR_STR_CRAFTED_BY_PLAYER_UID);
    }

    public virtual bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return IsCrafted(compassStack)
             && Get2DDistanceSquaredToTarget(fromPos, compassStack) >= MIN_DISTANCE_SQUARED_TO_SHOW_DIRECTION;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      float angle;
      var viewingPlayer = capi.World.Player;
      BlockPos fromPos = null;
      float yawCorrection = 0;
      switch (renderTarget) {
        case EnumItemRenderTarget.Gui:
        case EnumItemRenderTarget.HandFp:
          fromPos = viewingPlayer.Entity.Pos.AsBlockPos;
          yawCorrection = viewingPlayer.CameraYaw;
          break;
        case EnumItemRenderTarget.HandTp:
          fromPos = GetEntityPos(compassStack);
          yawCorrection = GetEntityYaw(compassStack);
          break;
        case EnumItemRenderTarget.Ground:
          fromPos = GetEntityPos(compassStack);
          renderinfo.Transform.Rotate = false; // this item will always drop to the ground in the same orientation
          break;
      }
      if (ShouldPointToTarget(fromPos, compassStack)) {
        angle = GetNeedle2DAngleRadians(fromPos, compassStack) - yawCorrection;
      }
      else {
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

    public override void OnGroundIdle(EntityItem entityItem) {
      base.OnGroundIdle(entityItem);
      SetEntityPos(entityItem.Itemstack, entityItem.Pos.AsBlockPos);
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity) {
      base.OnHeldIdle(slot, byEntity);
      SetEntityPos(slot.Itemstack, byEntity.Pos.AsBlockPos);
      SetEntityYaw(slot.Itemstack, byEntity.BodyYaw);
    }
  }
}

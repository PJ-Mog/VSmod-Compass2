using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Compass {
  abstract class BlockCompass : Block {
    const int MAX_ANGLED_MESHES = 60;
    public static readonly string ATTR_STR_CRAFTED_BY_PLAYER_UID = "compass-crafted-by-player-uid";
    public static readonly string ATTR_BYTES_TARGET_POS = "compass-target-pos";
    public static readonly string TEMP_ATTR_BYTES_ENTITY_POS = "compass-entity-pos";
    public static readonly string TEMP_ATTR_FLOAT_ENTITY_YAW = "compass-entity-yaw";
    public virtual AssetLocation baseLoc => new AssetLocation("compass", "block/compass/base");
    public virtual AssetLocation needleLoc => new AssetLocation("compass", "block/compass/needle");
    protected MeshRef[] meshrefs;

    protected virtual int MIN_DISTANCE_TO_SHOW_DIRECTION {
      get { return 3; }
    }

    public enum EnumTargetType {
      STATIONARY,
      MOVING
    }

    public virtual EnumTargetType TargetType => EnumTargetType.STATIONARY;

    public override void OnLoaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        PreGenerateMeshRefs(api as ICoreClientAPI);
      }
    }

    public override void OnUnloaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        DisposeMeshRefs(api as ICoreClientAPI);
      }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      return (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCompass)?.CompassStack ?? base.OnPickBlock(world, pos);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    #region Rendering

    protected virtual void PreGenerateMeshRefs(ICoreClientAPI capi) {
      if (meshrefs == null) {
        meshrefs = new MeshRef[MAX_ANGLED_MESHES];
      }

      string key = Code.ToString() + "-meshes";

      var compassBaseMeshData = GenBaseMesh(capi);

      meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, () => {
        var needleShape = GetShape(capi, needleLoc);
        for (var angleIndex = 0; angleIndex < MAX_ANGLED_MESHES; angleIndex += 1) {

          float angle = ((float)angleIndex / MAX_ANGLED_MESHES * 360);
          var needleMeshData = GenMesh(capi, needleShape, new Vec3f(0, angle, 0));

          needleMeshData.AddMeshData(compassBaseMeshData);

          meshrefs[angleIndex] = capi.Render.UploadMesh(needleMeshData);
        }
        return meshrefs;
      });
    }

    protected virtual void DisposeMeshRefs(ICoreClientAPI capi) {
      for (var meshIndex = 0; meshIndex < MAX_ANGLED_MESHES; meshIndex += 1) {
        meshrefs[meshIndex]?.Dispose();
        meshrefs[meshIndex] = null;
      }
      meshrefs = null;
    }

    public virtual MeshData GenNeedleMesh(ICoreClientAPI capi, Vec3f rotationDeg = null) {
      var shape = GetShape(capi, needleLoc);
      return GenMesh(capi, shape, rotationDeg);
    }

    public virtual MeshData GenBaseMesh(ICoreClientAPI capi) {
      var shape = GetShape(capi, baseLoc);
      return GenMesh(capi, shape);
    }

    protected Shape GetShape(ICoreClientAPI capi, AssetLocation assetLocation) {
      var shape = Vintagestory.API.Common.Shape.TryGet(capi, assetLocation.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
      if (shape == null) {
        capi.Logger.Error("[CompassMod] Failed to find shape {0} for {1}", assetLocation, Code);
      }
      return shape;
    }

    protected virtual MeshData GenMesh(ICoreClientAPI capi, Shape shape, Vec3f rotationDeg = null) {
      if (shape == null) {
        return new MeshData();
      }
      capi.Tesselator.TesselateShape(this, shape, out MeshData mesh, rotationDeg);
      return mesh;
    }

    protected int GetBestMatchMeshRefIndex(float angleTowardsTarget, float yawOfCompass = 0) {
      return (int)GameMath.Mod((angleTowardsTarget - yawOfCompass) / GameMath.TWOPI * MAX_ANGLED_MESHES + 0.5, MAX_ANGLED_MESHES);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      var viewingPlayer = capi.World.Player;
      BlockPos fromPos = null;
      float yawCorrection = 0;
      switch (renderTarget) {
        case EnumItemRenderTarget.Gui:
        case EnumItemRenderTarget.HandFp:
          fromPos = viewingPlayer.Entity.Pos.AsBlockPos;
          yawCorrection = viewingPlayer.Entity.Pos.Yaw;
          break;
        case EnumItemRenderTarget.HandTp:
        case EnumItemRenderTarget.HandTpOff:
          fromPos = GetCompassEntityPos(compassStack);
          yawCorrection = GetCompassEntityYaw(compassStack);
          break;
        case EnumItemRenderTarget.Ground:
          fromPos = GetCompassEntityPos(compassStack);
          break;
      }
      float angle = GetNeedleYawRadians(fromPos, compassStack) ?? GetWildSpinAngleRadians(capi);
      renderinfo.ModelRef = meshrefs[GetBestMatchMeshRefIndex(angle, yawCorrection)];
    }

    #endregion
    #region NeedleLogic

    //  The XZ-plane angle in radians that the compass should point.
    //  Null if the angle cannot be calucated or if the direction should be hidden.
    public float? GetNeedleYawRadians(BlockPos fromPos, ItemStack compassStack) {
      if (ShouldPointToTarget(fromPos, compassStack)) {
        return GetYawToTargetRadians(fromPos, compassStack);
      }
      return null;
    }

    //  The XZ-plane angle in radians to the compass's target.
    //  Null if the angle cannot be calculated.
    protected virtual float? GetYawToTargetRadians(BlockPos fromPos, ItemStack compassStack) {
      return CompassMath.YawRadians(fromPos, GetTargetPos(compassStack));
    }

    public virtual bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return fromPos != null
             && IsCrafted(compassStack)
             && GetDistanceToTarget(fromPos, compassStack) >= MIN_DISTANCE_TO_SHOW_DIRECTION;
    }

    public virtual int GetDistanceToTarget(BlockPos fromPos, ItemStack compassStack) {
      return CompassMath.XZManhattanDistance(fromPos, GetTargetPos(compassStack)) ?? MIN_DISTANCE_TO_SHOW_DIRECTION;
    }

    //  Sealed, override #OnBeforeModifiedInInventorySlot, #OnSuccessfullyCrafted, and/or #OnAfterModifiedInInventorySlot instead.
    sealed public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      OnBeforeModifiedInInventorySlot(world, slot, extractedStack);
      base.OnModifiedInInventorySlot(world, slot, extractedStack);
      var player = (slot.Inventory as InventoryBasePlayer)?.Player;
      if (world.Side == EnumAppSide.Server && player != null && !IsCrafted(slot.Itemstack)) {
        SetCraftedByPlayerUID(slot.Itemstack, player.PlayerUID);
        OnSuccessfullyCrafted(world as IServerWorldAccessor, player, slot);
      }
      OnAfterModifiedInInventorySlot(world, slot, extractedStack);
    }

    protected virtual void OnBeforeModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }

    //  Called server side when the compass is first placed into a player's inventory after being marked as crafted for the first time.
    protected virtual void OnSuccessfullyCrafted(IServerWorldAccessor world, IPlayer byPlayer, ItemSlot slot) { }

    protected virtual void OnAfterModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }

    public virtual float GetWildSpinAngleRadians(ICoreAPI api) {
      return CompassMath.GetWildSpinAngleRadians(api);
    }

    public override void OnGroundIdle(EntityItem entityItem) {
      base.OnGroundIdle(entityItem);
      SetCompassEntityPos(entityItem.Itemstack, entityItem.Pos.AsBlockPos);
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity) {
      base.OnHeldIdle(slot, byEntity);
      SetCompassEntityPos(slot.Itemstack, byEntity.Pos.AsBlockPos);
      SetCompassEntityYaw(slot.Itemstack, byEntity.BodyYaw);
    }

    #endregion
    #region GetAndSetAttributes

    protected virtual void SetCraftedByPlayerUID(ItemStack compassStack, string craftedByPlayerUID) {
      if (craftedByPlayerUID == null) { return; }
      compassStack?.Attributes.SetString(ATTR_STR_CRAFTED_BY_PLAYER_UID, craftedByPlayerUID);
    }

    protected virtual string GetCraftedByPlayerUID(ItemStack compassStack) {
      return compassStack?.Attributes.GetString(ATTR_STR_CRAFTED_BY_PLAYER_UID);
    }

    public virtual bool IsCrafted(ItemStack compassStack) {
      return compassStack?.Attributes.HasAttribute(ATTR_STR_CRAFTED_BY_PLAYER_UID) ?? false;
    }

    protected virtual void SetTargetPos(ItemStack compassStack, BlockPos targetPos) {
      if (targetPos == null) {
        compassStack?.Attributes.RemoveAttribute(ATTR_BYTES_TARGET_POS);
      }
      else {
        compassStack?.Attributes.SetBytes(ATTR_BYTES_TARGET_POS, SerializerUtil.Serialize(targetPos));
      }
    }

    //  The position of the compass's target.
    //  Null if the compass has not had its target set, the target cannot be found, or the target is not a discrete position.
    protected virtual BlockPos GetTargetPos(ItemStack compassStack) {
      var bytes = compassStack?.Attributes.GetBytes(ATTR_BYTES_TARGET_POS);
      if (bytes == null) { return null; }
      return SerializerUtil.Deserialize<BlockPos>(bytes);
    }

    protected virtual void SetCompassEntityPos(ItemStack compassStack, BlockPos entityPos) {
      if (entityPos == null) { return; }
      compassStack?.TempAttributes.SetBytes(TEMP_ATTR_BYTES_ENTITY_POS, SerializerUtil.Serialize(entityPos));
    }

    protected virtual BlockPos GetCompassEntityPos(ItemStack compassStack) {
      var bytes = compassStack?.TempAttributes.GetBytes(TEMP_ATTR_BYTES_ENTITY_POS);
      if (bytes == null) { return null; }
      return SerializerUtil.Deserialize<BlockPos>(bytes);
    }

    protected virtual void SetCompassEntityYaw(ItemStack compassStack, float entityYaw) {
      compassStack?.TempAttributes.SetFloat(TEMP_ATTR_FLOAT_ENTITY_YAW, entityYaw);
    }

    protected virtual float GetCompassEntityYaw(ItemStack compassStack) {
      return compassStack?.TempAttributes.GetFloat(TEMP_ATTR_FLOAT_ENTITY_YAW) ?? 0;
    }

    #endregion
  }
}

using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Compass {
  abstract class BlockCompass : Block, IRenderableXZTracker, IDisplayableCollectible {
    protected static readonly string ATTR_STR_CRAFTED_BY_PLAYER_UID = "compass-crafted-by-player-uid";
    protected static readonly string ATTR_BYTES_TARGET_POS = "compass-target-pos";
    protected static readonly string TEMP_ATTR_BYTES_ENTITY_POS = "compass-entity-pos";
    protected static readonly string TEMP_ATTR_FLOAT_ENTITY_YAW = "compass-entity-yaw";
    public virtual AssetLocation baseLoc => new AssetLocation("compass", "block/compass/base");
    public virtual AssetLocation needleLoc => new AssetLocation("compass", "block/compass/needle");

    protected virtual int MIN_DISTANCE_TO_SHOW_DIRECTION {
      get { return 3; }
    }

    public override void OnUnloaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        DisposeMeshData(api as ICoreClientAPI);
      }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      return (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCompass)?.TrackerStack?.Clone() ?? base.OnPickBlock(world, pos);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    #region Rendering

    protected virtual void DisposeMeshData(ICoreClientAPI capi) {
      ObjectCacheUtil.Delete(capi, Code.ToString() + "-needleMesh");
      ObjectCacheUtil.Delete(capi, Code.ToString() + "-baseMesh");
    }

    public virtual MeshData GetFullMesh(ICoreClientAPI capi, float needleAngleRadians, float baseAngleRadians = 0f) {
      var needleMesh = GetNeedleMesh(capi, out Vec3f rotationOrigin);
      var baseMesh = GetBaseMesh(capi);
      var fullMesh = new MeshData(needleMesh.VerticesCount + baseMesh.VerticesCount, needleMesh.IndicesCount + baseMesh.IndicesCount);
      fullMesh.AddMeshData(needleMesh);
      fullMesh.Rotate(rotationOrigin, 0f, needleAngleRadians - baseAngleRadians, 0f);
      fullMesh.AddMeshData(baseMesh);
      return fullMesh;
    }

    public virtual MeshData GetNeedleMesh(ICoreClientAPI capi, out Vec3f rotationOrigin) {
      var element = GetNeedleShape(capi).Elements[0].RotationOrigin;
      rotationOrigin = new Vec3f((float)element[0] / 16f, (float)element[1] / 16f, (float)element[2] / 16f);
      var key = Code.ToString() + "-needleMesh";
      var cachedMesh = ObjectCacheUtil.GetOrCreate(capi, key, () => {
        var mesh = GenMesh((capi), GetNeedleShape(capi));
        mesh.CompactBuffers();
        return mesh;
      });

      return cachedMesh;
    }

    public virtual MeshData GetBaseMesh(ICoreClientAPI capi) {
      var key = Code.ToString() + "-baseMesh";
      var cachedMesh = ObjectCacheUtil.GetOrCreate(capi, key, () => {
        var mesh = GenMesh(capi, GetBaseShape(capi));
        mesh.CompactBuffers();
        return mesh;
      });

      return cachedMesh;
    }

    public virtual Shape GetNeedleShape(ICoreClientAPI capi) {
      return GetShape(capi, needleLoc);
    }

    public virtual Shape GetBaseShape(ICoreClientAPI capi) {
      return GetShape(capi, baseLoc);
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

      float angle = GetXZAngleToPoint(fromPos, compassStack) ?? GetWildSpinAngleRadians(capi);
      var mesh = GetFullMesh(capi, angle, yawCorrection);
      capi.Render.UpdateMesh(renderinfo.ModelRef, mesh);
    }

    public IAdjustableRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos) {
      var renderer = new XZTrackerNeedleRenderer(capi, blockPos, this);
      if (GetTargetType() == EnumTargetType.STATIONARY) {
        renderer.TrackerTargetAngle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
      }
      else {
        renderer.TickListenerId = capi.World.RegisterGameTickListener((dt) => {
          if (renderer == null) { return; }
          var angle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
          renderer.TrackerTargetAngle = angle;
        }, 500);
      }
      return renderer;
    }

    #endregion
    #region NeedleLogic

    public virtual EnumTargetType GetTargetType() => EnumTargetType.STATIONARY;

    //  The XZ-plane angle in radians that the compass should point.
    //  Null if the angle cannot be calucated or if the direction should be hidden.
    public float? GetXZAngleToPoint(BlockPos fromPos, ItemStack compassStack) {
      if (ShouldPointToTarget(fromPos, compassStack)) {
        return GetXZAngleToTargetRadians(fromPos, compassStack);
      }
      return null;
    }

    //  The XZ-plane angle in radians to the compass's target.
    //  Null if the angle cannot be calculated.
    protected virtual float? GetXZAngleToTargetRadians(BlockPos fromPos, ItemStack compassStack) {
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

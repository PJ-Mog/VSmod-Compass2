using Compass.Utility;
using ContainedStackRenderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Compass {
  public abstract class BlockCompass : Block, IRenderableXZTracker, IContainedRenderer {
    protected virtual string MeshRefsCacheKey { get; set; }
    protected static readonly string ATTR_STR_CRAFTED_BY_PLAYER_UID = "compass-crafted-by-player-uid";
    protected static readonly string ATTR_BYTES_TARGET_POS = "compass-target-pos";
    protected static readonly string TEMP_ATTR_BYTES_ENTITY_POS = "compass-entity-pos";
    protected static readonly string TEMP_ATTR_FLOAT_ENTITY_YAW = "compass-entity-yaw";
    protected static readonly AssetLocation DEFAULT_NEEDLE_SHAPE_LOC = new AssetLocation("compass:shapes/block/compass/needle.json");
    public virtual XZTrackerProps Props { get; protected set; }
    public virtual Shape NeedleShape { get; protected set; }
    public virtual Shape ShellShape { get; protected set; }
    public virtual EnumTargetType TargetType { get; protected set; } = EnumTargetType.Stationary;
    protected CompassMath.DistanceCalculator GetDistance;

    public override void OnLoaded(ICoreAPI api) {
      base.OnLoaded(api);
      if (api.Side == EnumAppSide.Client) {
        MeshRefsCacheKey = Code.ToString() + "-meshrefs";
        var capi = api as ICoreClientAPI;
        LoadProperties(capi);
        GetMeshRefs(capi);
      }
    }

    protected virtual void LoadProperties(ICoreClientAPI capi) {
      if (Attributes != null && Attributes["XZTrackerProps"].Exists) {
        Props = Attributes["XZTrackerProps"].AsObject<XZTrackerProps>(null, Code.Domain);
      }

      if (Props.NeedleShapeLocation == null) {
        Props.NeedleShapeLocation = DEFAULT_NEEDLE_SHAPE_LOC;
        capi.Logger.Warning("[CompassMod] Collectible {0} has no defined needle shape (JSON Path: attributes/XZTrackerProps/needleShapeLocation). Using {1}.", Code, Props.NeedleShapeLocation);
      }
      Props.NeedleShapeLocation = Props.NeedleShapeLocation.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
      NeedleShape = GetShape(capi, Props.NeedleShapeLocation);

      ShellShape = GetShape(capi, Shape.Base);

      GetDistance = Props.DistanceFormula;
    }

    public override void OnUnloaded(ICoreAPI api) {
      base.OnUnloaded(api);
      if (api.Side == EnumAppSide.Client) {
        DisposeMeshRefs(api as ICoreClientAPI);
      }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      return (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityXZTracker)?.TrackerStack?.Clone() ?? base.OnPickBlock(world, pos);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    #region Rendering

    protected virtual MeshRef[] GetMeshRefs(ICoreClientAPI capi) {
      return ObjectCacheUtil.GetOrCreate(capi, MeshRefsCacheKey, () => {
        var meshRefs = new MeshRef[Props.MaximumMeshes];
        for (var angleIndex = 0; angleIndex < Props.MaximumMeshes; angleIndex++) {
          float angleDegrees = ((float)angleIndex / Props.MaximumMeshes * 360);
          meshRefs[angleIndex] = capi.Render.UploadMesh(GenFullMesh(capi, angleDegrees));
        }
        return meshRefs;
      });
    }

    public virtual MeshRef GetBestMeshRef(ICoreClientAPI capi, float forAngleRadians, float angleOfTrackerRadians = 0f) {
      var index = (int)GameMath.Mod((forAngleRadians - angleOfTrackerRadians) / GameMath.TWOPI * Props.MaximumMeshes + 0.5, Props.MaximumMeshes);
      return GetMeshRefs(capi)[index];
    }

    protected virtual void DisposeMeshRefs(ICoreClientAPI capi) {
      var meshRefs = ObjectCacheUtil.TryGet<MeshRef[]>(capi, MeshRefsCacheKey);
      if (meshRefs != null) {
        for (int i = 0; i < meshRefs.Length; i++) {
          meshRefs[i]?.Dispose();
          meshRefs[i] = null;
        }
      }
      ObjectCacheUtil.Delete(capi, MeshRefsCacheKey);
    }

    protected virtual MeshData GenFullMesh(ICoreClientAPI capi, float needleAngleDegrees) {
      var mesh = GenNeedleMesh(capi, needleAngleDegrees);
      mesh.AddMeshData(GenShellMesh(capi));
      return mesh;
    }

    public virtual MeshData GenNeedleMesh(ICoreClientAPI capi, out Vec3f blockRotationOrigin) {
      try {
        var shapeRotationOrigin = NeedleShape.Elements[0].RotationOrigin;
        blockRotationOrigin = new Vec3f((float)shapeRotationOrigin[0] / 16f, (float)shapeRotationOrigin[1] / 16f, (float)shapeRotationOrigin[2] / 16f);
      }
      catch {
        blockRotationOrigin = Vec3f.Zero;
      }
      var mesh = GenMesh(capi, NeedleShape);
      SetGlowFlags(mesh, Props.NeedleGlowLevel);
      return mesh;
    }

    protected virtual MeshData GenNeedleMesh(ICoreClientAPI capi, float YRotationDegrees) {
      var mesh = GenMesh(capi, NeedleShape, new Vec3f(0f, YRotationDegrees, 0f));
      SetGlowFlags(mesh, Props.NeedleGlowLevel);
      return mesh;
    }

    protected virtual MeshData GenShellMesh(ICoreClientAPI capi) {
      var mesh = GenMesh(capi, ShellShape);
      SetGlowFlags(mesh, Props.ShellGlowLevel);
      return mesh;
    }

    protected virtual void SetGlowFlags(MeshData mesh, int glowLevel) {
      GameMath.Clamp(glowLevel, 0, 255);
      mesh.SetVertexFlags(glowLevel);
    }

    protected Shape GetShape(ICoreClientAPI capi, AssetLocation assetLocation) {
      var shape = Vintagestory.API.Common.Shape.TryGet(capi, assetLocation.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
      if (shape == null) {
        capi.Logger.Error("[CompassMod] {0} failed to find shape {1}", Code, assetLocation);
      }
      return shape;
    }

    protected virtual MeshData GenMesh(ICoreClientAPI capi, Shape shape, Vec3f rotationDeg = null) {
      if (shape == null) {
        return new MeshData(4, 3);
      }
      capi.Tesselator.TesselateShape(this, shape, out MeshData mesh, rotationDeg);
      return mesh;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack compassStack, EnumItemRenderTarget renderTarget, ref ItemRenderInfo renderinfo) {
      var viewingPlayer = capi.World.Player;
      BlockPos fromPos = null;
      float trackerOrientation = 0;
      switch (renderTarget) {
        case EnumItemRenderTarget.Gui:
        case EnumItemRenderTarget.HandFp:
          fromPos = viewingPlayer.Entity.Pos.AsBlockPos;
          trackerOrientation = viewingPlayer.Entity.Pos.Yaw;
          break;
        case EnumItemRenderTarget.HandTp:
        case EnumItemRenderTarget.HandTpOff:
          fromPos = GetCompassEntityPos(compassStack);
          trackerOrientation = GetCompassEntityYaw(compassStack);
          break;
        case EnumItemRenderTarget.Ground:
          fromPos = GetCompassEntityPos(compassStack);
          break;
      }

      float angle = GetXZAngleToPoint(fromPos, compassStack) ?? GetWildSpinAngleRadians(capi);
      renderinfo.ModelRef = GetBestMeshRef(capi, angle, trackerOrientation);
    }

    public IAdjustableItemStackRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos) {
      var renderer = new XZTrackerNeedleRenderer(capi, blockPos, this);
      if (TargetType == EnumTargetType.Stationary) {
        renderer.TrackerTargetAngle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
      }
      else {
        renderer.TickListenerId = capi.World.RegisterGameTickListener((dt) => {
          if (renderer == null) { return; }
          var angle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
          renderer.TrackerTargetAngle = angle;
        }, 500);
      }
      renderer.ItemStackHashCode = displayableStack.GetHashCode(null);
      return renderer;
    }

    #endregion
    #region NeedleLogic

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
             && GetDistanceToTarget(fromPos, compassStack) >= Props.MinTrackingDistance;
    }

    public virtual int GetDistanceToTarget(BlockPos fromPos, ItemStack compassStack) {
      return GetDistance(fromPos, GetTargetPos(compassStack)) ?? Props.MinTrackingDistance;
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

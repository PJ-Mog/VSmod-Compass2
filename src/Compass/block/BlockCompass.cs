using Compass.ConfigSystem;
using Compass.Rendering;
using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Compass {
  public abstract class BlockCompass : Block, IRenderableXZTracker, IContainedRenderer {
    protected static readonly string AttrBool_IsCrafted = "compass-is-crafted";
    protected static readonly string AttrStr_CraftedByPlayerUid = "compass-crafted-by-player-uid";
    protected static readonly string AttrStr_AttunedToPlayerUid = "compass-attuned-to-player-uid";
    protected static readonly string AttrInt_TargetPosX = "compass-target-pos-x";
    protected static readonly string AttrInt_TargetPosY = "compass-target-pos-y";
    protected static readonly string AttrInt_TargetPosZ = "compass-target-pos-z";
    protected static readonly string AttrTempInt_EntityPosX = "compass-entity-pos-x";
    protected static readonly string AttrTempInt_EntityPosY = "compass-entity-pos-y";
    protected static readonly string AttrTempInt_EntityPosZ = "compass-entity-pos-z";
    protected static readonly string AttrTempFloat_EntityYaw = "compass-entity-yaw";
    protected static readonly AssetLocation DefaultNeedleShapeLocation = new AssetLocation(CompassMod.Domain, "shapes/block/compass/needle.json");

    protected CompassMath.DistanceCalculator GetDistance { get; set; } = CompassMath.XZManhattanDistance;

    protected virtual string MeshRefsCacheKey => Code.ToString() + "-meshrefs";
    protected int SeaLevel => api.World.SeaLevel;
    protected bool AreTemporalStormsEnabled { get; set; } = false;
    protected bool ShouldDistortDuringActiveStorm { get; set; } = false;
    protected bool ShouldDistortWhileStormApproaches { get; set; } = false;
    protected float DaysBeforeStormToApplyInterference { get; set; } = 0.35f;
    protected bool IsCraftingRestrictedByStability { get; set; } = false;
    protected float AllowCraftingBelowStability { get; set; } = 2.1f;
    protected int PreGeneratedMeshCount { get; set; } = 8;
    protected int RendererUpdateIntervalMs { get; set; } = 500;
    public virtual EnumTargetType TargetType { get; protected set; } = EnumTargetType.Stationary;
    protected SystemTemporalStability TemporalStabilitySystem { get; set; }
    public virtual XZTrackerProps Props { get; protected set; }
    public virtual Shape NeedleShape { get; protected set; }
    public virtual Shape ShellShape { get; protected set; }

    public override void OnLoaded(ICoreAPI api) {
      base.OnLoaded(api);
      LoadExternalSystemsAndSettings(api);
      LoadProperties(api);
      LoadServerSettings(api);

      if (api.Side == EnumAppSide.Client) {
        var capi = api as ICoreClientAPI;
        LoadClientSettings(capi);
        GetMeshRefs(capi);
      }
      else {
        RegisterServerCommands(api);
      }
    }

    protected virtual void LoadExternalSystemsAndSettings(ICoreAPI api) {
      TemporalStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
      AreTemporalStormsEnabled = TemporalStabilitySystem != null && api.World.Config.GetString("temporalStorms") != "off";
    }

    protected virtual void LoadProperties(ICoreAPI api) {
      if (api.Side != EnumAppSide.Client) { return; }

      var capi = api as ICoreClientAPI;
      if (Attributes != null && Attributes["XZTrackerProps"].Exists) {
        Props = Attributes["XZTrackerProps"].AsObject<XZTrackerProps>(null, Code.Domain);
      }
      Props ??= new XZTrackerProps();

      if (Props.NeedleShapeLocation == null) {
        Props.NeedleShapeLocation = DefaultNeedleShapeLocation;
        api.Logger.ModWarning("Collectible {0} has no defined needle shape (JSON Path: attributes/XZTrackerProps/needleShapeLocation). Using {1}.", Code, Props.NeedleShapeLocation);
      }
      Props.NeedleShapeLocation = Props.NeedleShapeLocation.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
      NeedleShape = GetShape(capi, Props.NeedleShapeLocation);

      ShellShape = GetShape(capi, Shape.Base.Clone());

      GetDistance = Props.DistanceFormula;
    }

    protected virtual void LoadServerSettings(ICoreAPI api) {
      var serverConfigSystem = base.api.ModLoader.GetModSystem<CompassConfigurationSystem>();
      if (serverConfigSystem == null) {
        LoadServerSettings(new CompassServerConfig());
        api.Logger.ModError("The {0} ModSystem was not loaded. Using default settings.", nameof(CompassConfigurationSystem));
        return;
      }

      if (api.Side == EnumAppSide.Client) {
        serverConfigSystem.ServerSettingsReceived += LoadServerSettings;
      }

      if (serverConfigSystem.ServerSettings != null) {
        LoadServerSettings(serverConfigSystem.ServerSettings);
      }
    }

    protected virtual void LoadServerSettings(CompassServerConfig serverSettings) {
      ShouldDistortDuringActiveStorm = AreTemporalStormsEnabled && serverSettings.ActiveTemporalStormsAffectCompasses.Value;
      ShouldDistortWhileStormApproaches = AreTemporalStormsEnabled && serverSettings.ApproachingTemporalStormsAffectCompasses.Value;

      DaysBeforeStormToApplyInterference = serverSettings.ApproachingTemporalStormInterferenceBeginsDays.Value;
    }

    protected virtual void LoadClientSettings(ICoreClientAPI capi) {
      var clientSettings = capi.ModLoader.GetModSystem<CompassConfigurationSystem>()?.ClientSettings;
      if (clientSettings == null) {
        capi.Logger.ModError("The {0} ModSystem was not loaded. Using default settings.", nameof(CompassConfigurationSystem));
        clientSettings = new CompassClientConfig();
      }

      PreGeneratedMeshCount = clientSettings.MaximumPreGeneratedMeshes.Value;
      RendererUpdateIntervalMs = clientSettings.PlacedCompassRenderUpdateTickIntervalMs.Value;
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
        var meshRefs = new MeshRef[PreGeneratedMeshCount];
        for (var angleIndex = 0; angleIndex < PreGeneratedMeshCount; angleIndex++) {
          float angleDegrees = (float)angleIndex / PreGeneratedMeshCount * 360;
          meshRefs[angleIndex] = capi.Render.UploadMesh(GenFullMesh(capi, angleDegrees));
        }
        return meshRefs;
      });
    }

    public virtual MeshRef GetBestMeshRef(ICoreClientAPI capi, float forAngleRadians, float angleOfTrackerRadians = 0f) {
      var index = (int)GameMath.Mod((forAngleRadians - angleOfTrackerRadians) / GameMath.TWOPI * PreGeneratedMeshCount + 0.5, PreGeneratedMeshCount);
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
        capi.Logger.ModError("{0} failed to find shape {1}", Code, assetLocation);
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
          fromPos = viewingPlayer.Entity.Pos.AsBlockPos;
          trackerOrientation = viewingPlayer.CameraYaw;
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

      float? desiredAngle = GetXZAngleToPoint(fromPos, compassStack);
      float renderedAngle;
      if (desiredAngle == null) {
        renderedAngle = GetWildSpinAngleRadians(capi);
      }
      else {
        renderedAngle = (float)desiredAngle + GetAngleDistortion();
      }
      renderinfo.ModelRef.meshrefs[0] = GetBestMeshRef(capi, renderedAngle, trackerOrientation);
    }

    public IAdjustableItemStackRenderer CreateRendererFromStack(ICoreClientAPI capi, ItemStack displayableStack, BlockPos blockPos) {
      var renderer = new XZTrackerNeedleRenderer(capi, blockPos, this);
      if (TargetType == EnumTargetType.Stationary) {
        renderer.TrackerTargetAngle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
      }
      else {
        void rendererUpdater(float dt) {
          if (renderer == null) { return; }
          renderer.TrackerTargetAngle = (displayableStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(blockPos, displayableStack);
        }
        renderer.TickListenerId = capi.World.RegisterGameTickListener(rendererUpdater, RendererUpdateIntervalMs);
      }
      renderer.AngleDistortionDelegate = GetAngleDistortion;
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

    protected virtual float GetAngleDistortion() {
      float distortion = GetTemporalInterference();
      distortion += GetIdleWobble();
      return distortion;
    }

    protected virtual float GetTemporalInterference() {
      if (!AreTemporalStormsEnabled) { return 0f; }
      var stormData = TemporalStabilitySystem.StormData;
      if (ShouldDistortDuringActiveStorm && stormData.nowStormActive) {
        return GetActiveStormInterference();
      }
      float daysUntilNextStorm = (float)(stormData.nextStormTotalDays - api.World.Calendar.TotalDays);
      if (ShouldDistortWhileStormApproaches && daysUntilNextStorm <= DaysBeforeStormToApplyInterference) {
        return GetApproachingStormInterference(daysUntilNextStorm);
      }
      return 0f;
    }

    protected virtual float GetActiveStormInterference() {
      return CompassMath.GetStormInterferenceRadians(api);
    }

    protected virtual float GetApproachingStormInterference(float daysUntilNextStorm) {
      return CompassMath.GetStormInterferenceRadians(api, 1 - (daysUntilNextStorm / DaysBeforeStormToApplyInterference));
    }

    protected virtual float GetIdleWobble() {
      return CompassMath.GetIdleWobble(api);
    }

    //  Sealed, override #OnBeforeModifiedInInventorySlot, #OnSuccessfullyCrafted, and/or #OnAfterModifiedInInventorySlot instead.
    sealed public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      OnBeforeModifiedInInventorySlot(world, slot, extractedStack);
      base.OnModifiedInInventorySlot(world, slot, extractedStack);
      var player = (slot.Inventory as InventoryBasePlayer)?.Player;
      if (world.Side == EnumAppSide.Server && player != null && !IsCrafted(slot.Itemstack)) {
        OnSuccessfullyCrafted(world as IServerWorldAccessor, player as IServerPlayer, slot);
      }
      OnAfterModifiedInInventorySlot(world, slot, extractedStack);
    }

    protected virtual void OnBeforeModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }

    //  Called server side when the compass is first placed into a player's inventory after being marked as crafted for the first time.
    protected virtual void OnSuccessfullyCrafted(IServerWorldAccessor world, IServerPlayer byPlayer, ItemSlot slot) {
      SetCraftedByPlayerUID(slot.Itemstack, byPlayer.PlayerUID);
    }

    protected virtual void OnAfterModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) { }

    public virtual float GetWildSpinAngleRadians(ICoreAPI api) {
      return CompassMath.GetWildSpinAngleRadians(api);
    }

    public override void OnGroundIdle(EntityItem entityItem) {
      base.OnGroundIdle(entityItem);
      if (api.Side != EnumAppSide.Client) { return; }
      SetCompassEntityPos(entityItem.Itemstack, entityItem.Pos.AsBlockPos);
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity) {
      base.OnHeldIdle(slot, byEntity);
      if (api.Side != EnumAppSide.Client) { return; }
      SetHoldingEntityData(slot.Itemstack, byEntity);
    }

    public virtual void SetHoldingEntityData(ItemStack compassStack, EntityAgent byEntity) {
      SetCompassEntityPos(compassStack, byEntity.Pos.AsBlockPos);
      SetCompassEntityYaw(compassStack, byEntity.BodyYaw - GameMath.PIHALF);
    }

    #endregion
    #region GetAndSetAttributes

    protected virtual void SetCraftedByPlayerUID(ItemStack compassStack, string craftedByPlayerUID) {
      if (craftedByPlayerUID == null) {
        compassStack?.Attributes.RemoveAttribute(AttrBool_IsCrafted);
        compassStack?.Attributes.RemoveAttribute(AttrStr_CraftedByPlayerUid);
      }
      else {
        compassStack?.Attributes.SetBool(AttrBool_IsCrafted, true);
        compassStack?.Attributes.SetString(AttrStr_CraftedByPlayerUid, craftedByPlayerUID);
      }
    }

    protected virtual string GetCraftedByPlayerUID(ItemStack compassStack) {
      return compassStack?.Attributes.GetString(AttrStr_CraftedByPlayerUid);
    }

    public virtual bool IsCrafted(ItemStack compassStack) {
      return compassStack?.Attributes.GetBool(AttrBool_IsCrafted) ?? false;
    }

    protected virtual void SetTargetPos(ItemStack compassStack, BlockPos targetPos) {
      if (targetPos == null) {
        compassStack?.Attributes.RemoveAttribute(AttrInt_TargetPosX);
        compassStack?.Attributes.RemoveAttribute(AttrInt_TargetPosY);
        compassStack?.Attributes.RemoveAttribute(AttrInt_TargetPosZ);
      }
      else {
        compassStack?.Attributes.SetInt(AttrInt_TargetPosX, targetPos.X);
        compassStack?.Attributes.SetInt(AttrInt_TargetPosY, targetPos.Y);
        compassStack?.Attributes.SetInt(AttrInt_TargetPosZ, targetPos.Z);
      }
    }

    //  The position of the compass's target.
    //  Null if the compass has not had its target set, the target cannot be found, or the target is not a discrete position.
    protected virtual BlockPos GetTargetPos(ItemStack compassStack) {
      var x = compassStack?.Attributes.TryGetInt(AttrInt_TargetPosX);
      var y = compassStack?.Attributes.TryGetInt(AttrInt_TargetPosY);
      var z = compassStack?.Attributes.TryGetInt(AttrInt_TargetPosZ);
      if (x == null || y == null || z == null) { return null; }
      return new BlockPos((int)x, (int)y, (int)z, Vintagestory.API.Config.Dimensions.NormalWorld);
    }

    protected virtual void SetCompassEntityPos(ItemStack compassStack, BlockPos entityPos) {
      if (entityPos == null) { return; }
      compassStack?.TempAttributes.SetInt(AttrTempInt_EntityPosX, entityPos.X);
      compassStack?.TempAttributes.SetInt(AttrTempInt_EntityPosY, entityPos.Y);
      compassStack?.TempAttributes.SetInt(AttrTempInt_EntityPosZ, entityPos.Z);
    }

    protected virtual BlockPos GetCompassEntityPos(ItemStack compassStack) {
      var x = compassStack?.TempAttributes.TryGetInt(AttrTempInt_EntityPosX);
      var y = compassStack?.TempAttributes.TryGetInt(AttrTempInt_EntityPosY);
      var z = compassStack?.TempAttributes.TryGetInt(AttrTempInt_EntityPosZ);
      if (x == null || y == null || z == null) { return null; }
      return new BlockPos((int)x, (int)y, (int)z, Vintagestory.API.Config.Dimensions.NormalWorld);
    }

    protected virtual void SetCompassEntityYaw(ItemStack compassStack, float entityYaw) {
      compassStack?.TempAttributes.SetFloat(AttrTempFloat_EntityYaw, entityYaw);
    }

    protected virtual float GetCompassEntityYaw(ItemStack compassStack) {
      return compassStack?.TempAttributes.GetFloat(AttrTempFloat_EntityYaw) ?? 0;
    }

    #endregion
    #region Commands

    protected static void RegisterServerCommands(ICoreAPI api) {
      var baseCommand = api.ChatCommands.GetOrCreate("compass2")
        .WithDescription("Commands for the Compass2 mod.")
        .RequiresPrivilege(Privilege.chat);

      baseCommand.BeginSubCommand("show")
        .WithDescription("View data for the currently held compass")
        .RequiresPrivilege(Privilege.controlserver)
        .HandleWith(OnShow)
        .EndSubCommand();

      baseCommand.BeginSubCommand("set")
        .WithDescription("Change data for the currently held compass.")
        .RequiresPrivilege(Privilege.controlserver)
        .BeginSubCommand("craftedBy")
          .WithDescription("Change who originally created the compass. Can use a player's Name or UID or a made-up value.")
          .WithArgs(api.ChatCommands.Parsers.Word("newCrafter"))
          .HandleWith(OnSetCraftedBy)
          .EndSubCommand()
        .BeginSubCommand("target")
          .WithDescription("Change the compass's targeted position to the provided coordinates (current position if not provided).")
          .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("pos"))
          .HandleWith(OnSetTarget)
          .EndSubCommand()
        .EndSubCommand();

      baseCommand.BeginSubCommand("reset")
        .WithDescription("Reset the currently held compass. The compass will act as if it had just been crafted.")
        .RequiresPrivilege(Privilege.controlserver)
        .HandleWith(OnReset)
        .EndSubCommand();

      baseCommand.BeginSubCommand("remove")
        .WithAlias("delete")
        .WithDescription("Delete specific data for the currently held compass.")
        .RequiresPrivilege(Privilege.controlserver)
        .BeginSubCommand("craftedBy")
          .WithDescription("Deletes the reference to the original crafter. Will immediately change to the current holder.")
          .HandleWith(OnRemoveCraftedBy)
          .EndSubCommand()
        .BeginSubCommand("target")
          .WithDescription("Deletes the target position.")
          .HandleWith(OnRemoveTarget)
          .EndSubCommand();
    }

    protected static TextCommandResult OnShow(TextCommandCallingArgs args) {
      var activeStack = args.Caller.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      var craftedByUid = compassBlock.GetCraftedByPlayerUID(activeStack);
      var craftedByName = (args.Caller.Entity.Api as ICoreServerAPI).PlayerData.GetPlayerDataByUid(craftedByUid)?.LastKnownPlayername;
      var targetPos = compassBlock.GetTargetPos(activeStack);
      return TextCommandResult.Success(Lang.Get("{0} held by {1} has a target pos of {2} and was crafted by '{3}' (UID: {4})", compassBlock.Code, args.Caller.Player.PlayerName, targetPos, craftedByName, craftedByUid));
    }

    protected static TextCommandResult OnSetCraftedBy(TextCommandCallingArgs args) {
      var activeSlot = args.Caller.Player?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      var sapi = args.Caller.Entity.Api as ICoreServerAPI;
      var newCrafter = args[0] as string;
      var playerData = sapi.PlayerData.GetPlayerDataByLastKnownName(newCrafter) ?? sapi.PlayerData.GetPlayerDataByUid(newCrafter);
      string newValueOutput;
      if (playerData != null) {
        newCrafter = playerData.PlayerUID;
        newValueOutput = $"'{playerData.LastKnownPlayername}' (UID: {newCrafter})";
      }
      else {
        newValueOutput = $"'{newCrafter}' (not a valid/known player on this server)";
      }

      compassBlock.SetCraftedByPlayerUID(activeStack, newCrafter);
      activeSlot.MarkDirty();
      args.Caller.Player.InventoryManager.BroadcastHotbarSlot();
      return TextCommandResult.Success(Lang.Get("Successfully set Crafter for {0} held by {1} to {2}.", compassBlock.Code, args.Caller.Player.PlayerName, newValueOutput));
    }

    protected static TextCommandResult OnSetTarget(TextCommandCallingArgs args) {
      var mapMiddle = args.Caller.Entity?.Api.World.DefaultSpawnPosition.XYZ.Clone();
      mapMiddle.Y = 0;
      var newPos = (args[0] as Vec3d)?.AsBlockPos;
      var activeSlot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
      var activeStack = activeSlot.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      compassBlock.SetTargetPos(activeStack, newPos);
      activeSlot.MarkDirty();
      args.Caller.Player.InventoryManager.BroadcastHotbarSlot();
      return TextCommandResult.Success(Lang.Get("Successfully set TargetPos for {0} held by {1} to {2}.", compassBlock.Code, args.Caller.Player.PlayerName, newPos));
    }

    protected static TextCommandResult OnReset(TextCommandCallingArgs args) {
      var activeSlot = args.Caller.Player.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      compassBlock.SetCraftedByPlayerUID(activeStack, null);
      compassBlock.SetTargetPos(activeStack, null);
      activeSlot.MarkDirty();
      return TextCommandResult.Success(Lang.Get("Successfully reset {0} held by {1}.", compassBlock.Code, args.Caller.Player.PlayerName));
    }

    protected static TextCommandResult OnRemoveCraftedBy(TextCommandCallingArgs args) {
      var activeSlot = args.Caller.Player?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      compassBlock.SetCraftedByPlayerUID(activeStack, null);
      activeSlot.MarkDirty();
      args.Caller.Player.InventoryManager.BroadcastHotbarSlot();
      return TextCommandResult.Success(Lang.Get("Successfully removed Crafter for {0} held by {1}.", compassBlock.Code, args.Caller.Player.PlayerName));
    }

    protected static TextCommandResult OnRemoveTarget(TextCommandCallingArgs args) {
      var activeSlot = args.Caller.Player?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      if (activeStack?.Collectible is not BlockCompass compassBlock) {
        return NotHoldingCompassError(args);
      }

      compassBlock.SetTargetPos(activeStack, null);
      activeSlot.MarkDirty();
      args.Caller.Player.InventoryManager.BroadcastHotbarSlot();
      return TextCommandResult.Success(Lang.Get("Successfully removed Target for {0} held by {1}.", compassBlock.Code, args.Caller.Player.PlayerName));
    }

    protected static TextCommandResult NotHoldingCompassError(TextCommandCallingArgs args) {
      return TextCommandResult.Error(Lang.Get("{0} needs to be holding a compass in their active hotbar slot.", args.Caller.Player.PlayerName));
    }

    #endregion
  }
}

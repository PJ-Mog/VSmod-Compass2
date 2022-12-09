using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    protected static readonly string ATTR_STACK = "compass-stack";
    public ItemStack CompassStack;
    protected float? NeedleAngleRad;
    protected IRenderer needleRenderer;

    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Client) {
        var capi = (ICoreClientAPI)api;
        InitializeNeedleRenderer(capi);
        InitializeNeedleAngleUpdater(capi);
      }
    }

    protected virtual void InitializeNeedleRenderer(ICoreClientAPI capi) {
      needleRenderer = new CompassNeedleRenderer(capi, Pos, GenNeedleMesh(capi), GetNeedleRenderAngle);
    }

    protected virtual void InitializeNeedleAngleUpdater(ICoreClientAPI capi) {
      if ((this.CompassStack?.Block as BlockCompass)?.TargetType == BlockCompass.EnumTargetType.MOVING) {
        capi.World.RegisterGameTickListener(UpdateNeedleAngle, 200);
      }
    }

    public virtual float? GetNeedleRenderAngle(ICoreClientAPI capi) => this.NeedleAngleRad;

    protected virtual MeshData GenNeedleMesh(ICoreClientAPI capi) {
      return (Block as BlockCompass)?.GenNeedleMesh(capi);
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      this.CompassStack = byItemStack;
      if (Api.Side == EnumAppSide.Client) {
        SetNeedleRenderAngle();
        InitializeNeedleAngleUpdater(Api as ICoreClientAPI);
      }
    }

    protected virtual void SetNeedleRenderAngle() {
      this.NeedleAngleRad = (this.CompassStack?.Block as BlockCompass)?.GetNeedleYawRadians(this.Pos, this.CompassStack);
    }

    public virtual void UpdateNeedleAngle(float deltaTime) {
      SetNeedleRenderAngle();
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.CompassStack = tree.GetItemstack(ATTR_STACK);
      this.CompassStack?.ResolveBlockOrItem(worldAccessForResolve);
      if (worldAccessForResolve.Side == EnumAppSide.Client) {
        SetNeedleRenderAngle();
      }
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetItemstack(ATTR_STACK, this.CompassStack);
    }

    public override void OnBlockRemoved() {
      base.OnBlockRemoved();

      needleRenderer?.Dispose();
      needleRenderer = null;
    }

    public override void OnBlockUnloaded() {
      base.OnBlockUnloaded();

      needleRenderer?.Dispose();
      needleRenderer = null;
    }
  }
}

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    protected static readonly string ATTR_STACK = "compass-stack";
    public ItemStack TrackerStack;
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
      if (needleRenderer == null && TrackerStack != null) {
        needleRenderer = new XZTrackerNeedleRenderer(capi, Pos, TrackerStack.Collectible as IRenderableXZTracker, GetNeedleRenderAngle);
      }
    }

    protected virtual void InitializeNeedleAngleUpdater(ICoreClientAPI capi) {
      if ((this.TrackerStack?.Collectible as IRenderableXZTracker)?.GetTargetType() == EnumTargetType.MOVING) {
        RegisterGameTickListener(UpdateNeedleAngle, 200);
      }
    }

    public virtual float? GetNeedleRenderAngle(ICoreClientAPI capi) => this.NeedleAngleRad;

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      if (byItemStack != null) {
        this.TrackerStack = byItemStack.Clone();
        this.TrackerStack.StackSize = 1;
      }
      if (Api.Side == EnumAppSide.Client) {
        SetNeedleRenderAngle();
        var capi = Api as ICoreClientAPI;
        InitializeNeedleRenderer(capi);
        InitializeNeedleAngleUpdater(capi);
      }
    }

    protected virtual void SetNeedleRenderAngle() {
      this.NeedleAngleRad = (this.TrackerStack?.Collectible as IRenderableXZTracker)?.GetXZAngleToPoint(this.Pos, this.TrackerStack);
    }

    public virtual void UpdateNeedleAngle(float deltaTime) {
      SetNeedleRenderAngle();
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.TrackerStack = tree.GetItemstack(ATTR_STACK);
      this.TrackerStack?.ResolveBlockOrItem(worldAccessForResolve);
      if (worldAccessForResolve.Side == EnumAppSide.Client) {
        SetNeedleRenderAngle();
      }
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetItemstack(ATTR_STACK, this.TrackerStack);
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

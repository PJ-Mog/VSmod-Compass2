using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    protected static readonly string ATTR_STACK = "tracker-stack";
    public ItemStack TrackerStack;
    protected float? NeedleAngleRad;
    protected IRenderer needleRenderer;

    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Client) {
        var capi = (ICoreClientAPI)api;
        InitializeNeedleRenderer(capi);
      }
    }

    protected virtual void InitializeNeedleRenderer(ICoreClientAPI capi) {
      if (needleRenderer == null && TrackerStack != null) {
        needleRenderer = (TrackerStack.Collectible as IRenderableXZTracker)?.CreateRendererFromStack(capi, TrackerStack, Pos);
      }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      if (byItemStack != null) {
        this.TrackerStack = byItemStack.Clone();
        this.TrackerStack.StackSize = 1;
      }
      if (Api.Side == EnumAppSide.Client) {
        var capi = Api as ICoreClientAPI;
        InitializeNeedleRenderer(capi);
      }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.TrackerStack = tree.GetItemstack(ATTR_STACK);
      this.TrackerStack?.ResolveBlockOrItem(worldAccessForResolve);
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

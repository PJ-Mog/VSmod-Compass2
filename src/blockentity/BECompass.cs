using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    public const string ATTR_STACK = "compass-stack";
    public ItemStack CompassStack;
    internal float? AngleRad;
    protected IRenderer needleRenderer;

    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Client) {
        InitializeNeedleRenderer(api as ICoreClientAPI);
      }
    }

    public virtual void InitializeNeedleRenderer(ICoreClientAPI capi) {
      needleRenderer = new CompassNeedleRenderer(capi, Pos, GenNeedleMesh(capi), GetNeedleRenderAngle);
    }

    public virtual float? GetNeedleRenderAngle(ICoreClientAPI capi) => this.AngleRad;

    protected virtual MeshData GenNeedleMesh(ICoreClientAPI capi) {
      if ((Block?.BlockId ?? 0) == 0) { return new MeshData(); }

      capi.Tesselator.TesselateShape(Block, Api.Assets.TryGet("compass:shapes/block/compass/needle.json").ToObject<Shape>(), out MeshData mesh);

      return mesh;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      BlockCompass blockCompass = byItemStack?.Block as BlockCompass;

      if (blockCompass != null) {
        this.CompassStack = byItemStack;
        SetNeedleRenderAngle();
      }
    }

    public void SetNeedleRenderAngle() {
      BlockCompass blockCompass = this.CompassStack?.Block as BlockCompass;
      if (blockCompass?.ShouldPointToTarget(Pos, CompassStack) ?? false) {
        this.AngleRad = blockCompass.GetNeedleYawToTargetRadians(this.Pos, this.CompassStack);
      }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.CompassStack = tree.GetItemstack(ATTR_STACK);
      this.CompassStack?.ResolveBlockOrItem(worldAccessForResolve);
      SetNeedleRenderAngle();
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

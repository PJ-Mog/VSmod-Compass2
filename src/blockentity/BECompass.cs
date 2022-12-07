using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    public string CraftedByPlayerUID;
    public BlockPos TargetPos;
    internal float? AngleRad;
    protected CompassNeedleRenderer renderer;

    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Client) {
        InitializeNeedleRenderer();
      }
    }

    public virtual void InitializeNeedleRenderer() {
      renderer = new CompassNeedleRenderer(Api as ICoreClientAPI, Pos, GenMesh("needle"), (Api) => { return AngleRad ?? BlockCompass.GetWildSpinAngle(Api); });
      (Api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      BlockCompass blockCompass = byItemStack?.Block as BlockCompass;

      if (blockCompass != null && byItemStack != null) {
        this.CraftedByPlayerUID = BlockCompass.GetCraftedByPlayerUID(byItemStack);
        this.TargetPos = blockCompass.GetTargetPos(byItemStack);
        if (blockCompass.ShouldPointToTarget(Pos, byItemStack)) {
          this.AngleRad = blockCompass.GetNeedleYawToTargetRadians(this.Pos, byItemStack);
        }
      }
    }

    public override void OnBlockRemoved() {
      base.OnBlockRemoved();

      renderer?.Dispose();
      renderer = null;
    }

    internal MeshData GenMesh(string type = "base") {
      if ((Block?.BlockId ?? 0) == 0) return null;
      MeshData mesh;
      ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

      mesher.TesselateShape(Block, Api.Assets.TryGet("compass:shapes/block/compass/" + type + ".json").ToObject<Shape>(), out mesh);

      return mesh;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.CraftedByPlayerUID = tree.GetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID);
      this.TargetPos = SerializerUtil.Deserialize<BlockPos>(tree.GetBytes(BlockCompass.ATTR_BYTES_TARGET_BLOCK_POS));
      this.AngleRad = tree.TryGetFloat("AngleRad");
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID, this.CraftedByPlayerUID);
      tree.SetBytes(BlockCompass.ATTR_BYTES_TARGET_BLOCK_POS, SerializerUtil.Serialize(this.TargetPos));
      if (this.AngleRad != null) tree.SetFloat("AngleRad", (float)this.AngleRad);
    }

    public override void OnBlockUnloaded() {
      base.OnBlockUnloaded();

      renderer?.Dispose();
    }
  }
}

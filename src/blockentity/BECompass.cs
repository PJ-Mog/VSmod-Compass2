using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    internal BlockCompass ownBlock;
    public bool IsCrafted;
    public string CraftedByPlayerUID;
    public BlockPos TargetPos;
    internal float? AngleRad;
    CompassNeedleRenderer renderer;

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      this.ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockCompass;

      if (api.Side == EnumAppSide.Client)
      {
        renderer = new CompassNeedleRenderer(api as ICoreClientAPI, Pos, GenMesh("needle"));
        renderer.AngleRad = AngleRad;
        (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");
      }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      BlockCompass blockCompass = byItemStack?.Block as BlockCompass;

      if (blockCompass != null && byItemStack != null) {
        this.IsCrafted = BlockCompass.IsCrafted(byItemStack);
        this.CraftedByPlayerUID = BlockCompass.GetCraftedByPlayerUID(byItemStack);
        this.TargetPos = blockCompass.GetTargetPos(byItemStack);
        if (blockCompass.ShouldPointToTarget(Pos, byItemStack)) {
          this.AngleRad = blockCompass.GetNeedle2DAngleRadians(this.Pos, byItemStack);
        }
      }
      if (Api.Side == EnumAppSide.Client) {
        this.renderer.AngleRad = this.AngleRad;
        if (compassBaseMesh == null) compassBaseMesh = GenMesh("base");
        if (compassNeedleMesh == null) compassNeedleMesh = GenMesh("needle");
        MarkDirty(true);
      }
    }

    public override void OnBlockRemoved() {
      base.OnBlockRemoved();

      renderer?.Dispose();
      renderer = null;
    }

    // public string Type { get { return Block.LastCodePart(); } }

    MeshData compassBaseMesh;
    MeshData compassNeedleMesh;

    internal MeshData GenMesh(string type = "base") {
        if (this.ownBlock == null) return null;
        if (this.ownBlock.BlockId == 0) return null;
        MeshData mesh;
        ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

        mesher.TesselateShape(ownBlock, Api.Assets.TryGet("compass:shapes/block/compass/" + type + ".json").ToObject<Shape>(), out mesh);

        return mesh;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      this.IsCrafted = tree.GetBool(BlockCompass.ATTR_BOOL_CRAFTED);
      this.CraftedByPlayerUID = tree.GetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID);
      var x = tree.TryGetInt(BlockCompass.ATTR_INT_TARGET_POS_X);
      var y = tree.TryGetInt(BlockCompass.ATTR_INT_TARGET_POS_Y);
      var z = tree.TryGetInt(BlockCompass.ATTR_INT_TARGET_POS_Z);
      if (x == null || y == null || z == null) {
        this.TargetPos = null;
      } else {
        this.TargetPos = new BlockPos((int)x, (int)y, (int)z);
      }
      this.AngleRad = tree.TryGetFloat("AngleRad");
      if (worldAccessForResolve.Api.Side == EnumAppSide.Client) {
        if (compassBaseMesh == null) {
          GenMesh("base");
          MarkDirty(true);
        }
        if (compassNeedleMesh == null) {
          GenMesh("needle");
          MarkDirty(true);
        }
      }
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetBool(BlockCompass.ATTR_BOOL_CRAFTED, this.IsCrafted);
      tree.SetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID, this.CraftedByPlayerUID);
      if (this.TargetPos != null) {
        tree.SetInt(BlockCompass.ATTR_INT_TARGET_POS_X, this.TargetPos.X);
        tree.SetInt(BlockCompass.ATTR_INT_TARGET_POS_Y, this.TargetPos.Y);
        tree.SetInt(BlockCompass.ATTR_INT_TARGET_POS_Z, this.TargetPos.Z);
      }
      if (this.AngleRad != null) tree.SetFloat("AngleRad", (float)this.AngleRad);
    }

    public override void OnBlockUnloaded() {
        base.OnBlockUnloaded();

        renderer?.Dispose();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
      if (compassBaseMesh == null) {
        compassBaseMesh = GenMesh("base");
      }
      if (compassNeedleMesh == null) {
        compassNeedleMesh = GenMesh("needle");
      }

      mesher.AddMeshData(compassBaseMesh);
      return true;
    }
  }
}
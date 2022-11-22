using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    public bool IsCrafted;
    public string CraftedByPlayerUID;
    public BlockPos TargetPos;
    internal float? AngleRad;
    CompassNeedleRenderer renderer;

    public string Type {
      get { return Block.LastCodePart(); }
    }

    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);

      if (api.Side == EnumAppSide.Client) {
        renderer = new CompassNeedleRenderer(api as ICoreClientAPI, Pos, GenMesh("needle"));
        renderer.AngleRad = AngleRad;
        (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");
        if (compassBaseMesh == null) compassBaseMesh = GenMesh("base");
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
        MarkDirty(true);
      }
    }

    public override void OnBlockRemoved() {
      base.OnBlockRemoved();

      renderer?.Dispose();
      renderer = null;
    }

    MeshData compassBaseMesh {
      get {
        object value;
        Api.ObjectCache.TryGetValue("compassbowlmesh", out value);
        return (MeshData)value;
      }
      set {
        Api.ObjectCache["compassbowlmesh"] = value;
      }
    }
    MeshData compassNeedleMesh {
      get {
        object value;
        Api.ObjectCache.TryGetValue($"compassneedlemesh-{Type}", out value);
        return (MeshData)value;
      }
      set {
        Api.ObjectCache[$"compassneedlemesh-{Type}"] = value;
      }
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
      this.IsCrafted = tree.GetBool(BlockCompass.ATTR_BOOL_CRAFTED);
      this.CraftedByPlayerUID = tree.GetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID);
      this.TargetPos = SerializerUtil.Deserialize<BlockPos>(tree.GetBytes(BlockCompass.ATTR_BYTES_TARGET_BLOCK_POS));
      this.AngleRad = tree.TryGetFloat("AngleRad");
      MarkDirty(true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetBool(BlockCompass.ATTR_BOOL_CRAFTED, this.IsCrafted);
      tree.SetString(BlockCompass.ATTR_STR_CRAFTED_BY_PLAYER_UID, this.CraftedByPlayerUID);
      tree.SetBytes(BlockCompass.ATTR_BYTES_TARGET_BLOCK_POS, SerializerUtil.Serialize(this.TargetPos));
      if (this.AngleRad != null) tree.SetFloat("AngleRad", (float)this.AngleRad);
    }

    public override void OnBlockUnloaded() {
      base.OnBlockUnloaded();

      renderer?.Dispose();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
      mesher.AddMeshData(compassBaseMesh);
      return true;
    }
  }
}

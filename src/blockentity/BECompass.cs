using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    internal BlockCompass ownBlock;
    internal BlockPos compassCraftedPos;
    internal float AngleRad;
    CompassNeedleRenderer renderer;

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      
      this.ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockCompass;

      if (api.Side == EnumAppSide.Client)
      {
        renderer = new CompassNeedleRenderer(api as ICoreClientAPI, Pos, GenMesh("needle"));
        renderer.AngleRad = this.AngleRad;
        (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");
      }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      BlockCompass blockCompass = byItemStack?.Block as BlockCompass;

      if (blockCompass != null) {
        this.compassCraftedPos = BlockCompass.GetCompassCraftedPos(byItemStack);
        this.AngleRad = blockCompass.GetNeedleAngleRadians(Pos);
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

    public string Type { get { return Block.LastCodePart(); } }

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
      var x = tree.GetInt("craftedX");
      var y = tree.GetInt("craftedY");
      var z = tree.GetInt("craftedZ");
      this.compassCraftedPos = new BlockPos(x, y, z);
      this.AngleRad = tree.GetFloat("AngleRad");
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
      tree.SetInt("craftedX", this.compassCraftedPos.X);
      tree.SetInt("craftedY", this.compassCraftedPos.Y);
      tree.SetInt("craftedZ", this.compassCraftedPos.Z);
      tree.SetFloat("AngleRad", this.AngleRad);
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
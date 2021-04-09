using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {
    internal BlockCompass ownBlock;
    CompassNeedleRenderer renderer;

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      
      this.ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockCompass;

      if (api.Side == EnumAppSide.Client)
      {
        renderer = new CompassNeedleRenderer(api as ICoreClientAPI, Pos, GenMesh("needle"));
        (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");
      }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      BlockCompass blockCompass = byItemStack?.Block as BlockCompass;

      if (Api.Side == EnumAppSide.Client) {
        if (blockCompass != null) {
          this.renderer.AngleRad = blockCompass.GetNeedleAngleRadians(Pos);
        }
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

    MeshData compassBaseMesh {
        get {
            object value;
            Api.ObjectCache.TryGetValue("compassbasemesh", out value);
            return (MeshData)value;
        }
        set { Api.ObjectCache["compassbasemesh"] = value; }
    }

    MeshData compassNeedleMesh {
        get {
            object value;
            Api.ObjectCache.TryGetValue("compassneedlemesh-" + Type, out value);
            return (MeshData)value;
        }
        set { Api.ObjectCache["compassneedlemesh-" + Type] = value; }
    }

    internal MeshData GenMesh(string type = "base") {
        Block block = Api.World.BlockAccessor.GetBlock(Pos);
        if (block.BlockId == 0) return null;

        MeshData mesh;
        ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

        mesher.TesselateShape(block, Api.Assets.TryGet("compass:shapes/block/compass/" + type + ".json").ToObject<Shape>(), out mesh);

        return mesh;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      if (Api?.Side == EnumAppSide.Client) {
        if (this.ownBlock != null) {
          this.renderer.AngleRad = ownBlock.GetNeedleAngleRadians(Pos);
          MarkDirty(true);
        }
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
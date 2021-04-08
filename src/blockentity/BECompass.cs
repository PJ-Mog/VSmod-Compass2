using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Compass {

  public class BlockEntityCompass : BlockEntity {

    internal float AngleRad;
    CompassNeedleRenderer renderer;

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
      
      if (api.Side == EnumAppSide.Client)
      {
        renderer = new CompassNeedleRenderer(api as ICoreClientAPI, Pos, GenMesh("needle"));


        BlockCompass compass = api.World.BlockAccessor.GetBlock(Pos) as BlockCompass;
        if (compass != null) {
          this.AngleRad = compass.GetNeedleAngleRadians(Pos);
          renderer.AngleRad = this.AngleRad;
        }

        (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "compass");

        if (compassBaseMesh == null)
        {
            compassBaseMesh = GenMesh("base");
        }
        if (compassNeedleMesh == null)
        {
            compassNeedleMesh = GenMesh("needle");
        }
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
        if (tree.HasAttribute("AngleRad")) AngleRad = tree.GetFloat("AngleRad");
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetFloat("AngleRad", AngleRad);
    }

    public override void OnBlockUnloaded() {
        base.OnBlockUnloaded();

        renderer?.Dispose();
    }
  }
}
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Compass {
  public class CompassNeedleRenderer : IRenderer {

    private ICoreClientAPI api;
    private BlockPos compassPos;
    MeshRef meshref;
    public Matrixf ModelMat = new Matrixf();
    public float? AngleRad = null;

    public CompassNeedleRenderer(ICoreClientAPI coreClientAPI, BlockPos compassPos, MeshData mesh) {
      this.api = coreClientAPI;
      this.compassPos = compassPos;
      this.meshref = api.Render.UploadMesh(mesh);
    }

    public double RenderOrder {
      get { return 0.5; }
    }

    public int RenderRange {
      get { return 24; }
    }

    public void Dispose() {
      api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

      meshref.Dispose();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
      if (meshref == null) return;

      IRenderAPI rpi = api.Render;
      Vec3d camPos = api.World.Player.Entity.CameraPos;

      rpi.GlDisableCullFace();
      rpi.GlToggleBlend(true);

      IStandardShaderProgram prog = rpi.PreparedStandardShader(compassPos.X, compassPos.Y, compassPos.Z);
      prog.Tex2D = api.BlockTextureAtlas.AtlasTextureIds[0];

      // TODO: Decouple from BlockCompass
      var renderAngle = AngleRad ?? BlockCompass.GetWildSpinAngle(api);

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(compassPos.X - camPos.X, compassPos.Y - camPos.Y, compassPos.Z - camPos.Z)
        .Translate(0.5f, 11f / 16f, 0.5f)
        .RotateY(renderAngle)
        .Translate(-0.5f, -11f / 16f, -0.5f)
        .Values
      ;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(meshref);
      prog.Stop();
    }
  }
}

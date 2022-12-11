using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Compass {
  public class CompassNeedleRenderer : IRenderer {

    private ICoreClientAPI api;
    private BlockPos compassPos;
    MeshRef meshref;
    public Matrixf ModelMat = new Matrixf();

    public delegate float? GetAngleHandler(ICoreClientAPI api);
    private GetAngleHandler GetAngle;

    public delegate float GetBackupAngle(ICoreClientAPI api);
    private GetBackupAngle backupAngleHandler;
    public GetBackupAngle BackupAngleHandler {
      get { return backupAngleHandler; }
      set { if (value != null) backupAngleHandler = value; }
    }

    public CompassNeedleRenderer(ICoreClientAPI capi, BlockPos compassPos, MeshData mesh, GetAngleHandler angleHandler) {
      this.api = capi;
      this.compassPos = compassPos;
      this.meshref = api.Render.UploadMesh(mesh);
      GetAngle = angleHandler;
      BackupAngleHandler = CompassMath.GetWildSpinAngleRadians;

      capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "compass-needle");
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

      var renderAngle = GetAngle?.Invoke(api) ?? BackupAngleHandler(api);

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(compassPos.X - camPos.X, compassPos.Y - camPos.Y, compassPos.Z - camPos.Z)
        .Translate(0.5f, 0f, 0.5f)
        .RotateY(renderAngle)
        .Translate(-0.5f, -0f, -0.5f)
        .Values
      ;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(meshref);
      prog.Stop();
    }
  }
}

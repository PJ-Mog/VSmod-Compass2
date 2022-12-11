using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public class XZTrackerNeedleRenderer : IRenderer {

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

    private double temporaryXOffset = 0.0;
    private double temporaryZOffset = 0.0;

    public XZTrackerNeedleRenderer(ICoreClientAPI capi, BlockPos compassPos, IRenderableXZTracker tracker, GetAngleHandler angleHandler) {
      this.api = capi;
      this.compassPos = compassPos;
      GetAngle = angleHandler;
      BackupAngleHandler = CompassMath.GetWildSpinAngleRadians;

      var needleShape = tracker?.GetNeedleShape(capi);
      var shapeElements = needleShape?.Elements;
      if (shapeElements != null && shapeElements.Length > 0) {
        temporaryXOffset = shapeElements[0].RotationOrigin[0] / 16;
        temporaryZOffset = shapeElements[0].RotationOrigin[2] / 16;
      }
      capi.Tesselator.TesselateShape(tracker as CollectibleObject, needleShape, out MeshData mesh);
      this.meshref = api.Render.UploadMesh(mesh);
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
        .Translate(temporaryXOffset, 0f, temporaryZOffset)
        .RotateY(renderAngle)
        .Translate(-temporaryXOffset, -0f, -temporaryZOffset)
        .Values
      ;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(meshref);
      prog.Stop();
    }
  }
}

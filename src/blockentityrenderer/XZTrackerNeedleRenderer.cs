using System;
using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public class XZTrackerNeedleRenderer : IAdjustableRenderer {

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
    private Vec3f offset = Vec3f.Zero;
    private float scale = 1f;

    private const float WOBBLE_FREQUENCY = 0.0025f;
    private const float MAX_WOBBLE_RADIANS = 0.03f;
    private const float MAX_VELOCITY = 6f;
    private const float MAX_SNAP_TO_TARGET_VELOCITY = 1f;
    private const float FRICTION = 0.5f; // Must be less than snap speed
    private const float ANGULAR_ACCELERATION = 5f;
    private float realAngle = 0f;
    private float angularFrequency = 0f;
    private int rotationDirection = 1;

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

    public void SetOffset(Vec3f offset) {
      if (offset == null) { return; }
      this.offset = offset;
    }

    public void SetRotation(float rotation) { }

    public void SetScale(float scale) {
      this.scale = scale;
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

      var targetAngle = GameMath.Mod(GetAngle?.Invoke(api) ?? BackupAngleHandler(api), GameMath.TWOPI);
      SimulateMovementTo(targetAngle, deltaTime);
      var renderedAngle = realAngle;
      if (realAngle == targetAngle) {
        renderedAngle += GetWobbleAdjustment();
      }

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(compassPos.X - camPos.X, compassPos.Y - camPos.Y, compassPos.Z - camPos.Z)
        .Translate(temporaryXOffset, 0f, temporaryZOffset)
        .Translate(offset.X, offset.Y, offset.Z)
        .Scale(scale, scale, scale)
        .RotateY(renderedAngle)
        .Translate(-temporaryXOffset, -0f, -temporaryZOffset)
        .Values
      ;

      prog.ViewMatrix = rpi.CameraMatrixOriginf;
      prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
      rpi.RenderMesh(meshref);
      prog.Stop();
    }

    private void SimulateMovementTo(float targetAngle, float deltaTime) {
      if (targetAngle == realAngle && angularFrequency == 0f) {
        return;
      }

      var normalizedAngularDistance = GameMath.Mod(targetAngle - realAngle, GameMath.TWOPI);
      var absoluteAngularDistance = Math.Min(GameMath.TWOPI - normalizedAngularDistance, normalizedAngularDistance);
      int directionOfAngularAcceleration = absoluteAngularDistance != normalizedAngularDistance ? -1 : 1;

      angularFrequency += directionOfAngularAcceleration * ANGULAR_ACCELERATION * deltaTime * rotationDirection;
      if (angularFrequency < 0) {
        angularFrequency = Math.Abs(angularFrequency);
        rotationDirection *= -1;
      }
      angularFrequency = GameMath.Min(angularFrequency, MAX_VELOCITY);

      var angularDisplacement = angularFrequency * deltaTime;
      if (angularDisplacement > absoluteAngularDistance) {
        if (angularFrequency <= MAX_SNAP_TO_TARGET_VELOCITY) {
          realAngle = targetAngle;
          angularFrequency = 0f;
          return;
        }
        angularFrequency -= FRICTION;
        angularDisplacement -= FRICTION * deltaTime;
      }
      realAngle += rotationDirection * angularDisplacement;
      realAngle = GameMath.Mod(realAngle, GameMath.TWOPI);
    }

    private float GetWobbleAdjustment() {
      return GameMath.FastSin(api.World.ElapsedMilliseconds * WOBBLE_FREQUENCY) * MAX_WOBBLE_RADIANS;
    }
  }
}

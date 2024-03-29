using System;
using Compass.Rendering;
using Compass.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Compass {
  public class XZTrackerNeedleRenderer : IAdjustableItemStackRenderer {

    private ICoreClientAPI api;
    private BlockPos trackerPos;
    private long? tickListenerId;
    public int ItemStackHashCode { get; set; }
    public long? TickListenerId {
      get { return tickListenerId; }
      set {
        UnregisterTickListener();
        tickListenerId = value;
      }
    }
    MeshRef meshref;
    public Matrixf ModelMat = new Matrixf();

    public delegate float GetBackupAngle(ICoreClientAPI api);
    private GetBackupAngle backupAngleHandler;
    public GetBackupAngle BackupAngleHandler {
      get { return this.backupAngleHandler; }
      set { if (value != null) this.backupAngleHandler = value; }
    }

    public Func<float> AngleDistortionDelegate { get; set; }

    private Vec3f rotationOrigin = Vec3f.Zero;
    private Vec3f offset = Vec3f.Zero;
    public Vec3f Offset {
      get { return this.offset; }
      set { if (value != null) this.offset = value; }
    }
    public float BlockRotationRadians { get; set; } = 0f;
    public float Scale { get; set; } = 1f;

    private const float MAX_VELOCITY = 6f;
    private const float MAX_SNAP_TO_TARGET_VELOCITY = 1f;
    private const float FRICTION = 0.5f; // Must be less than snap speed
    private const float ANGULAR_ACCELERATION = 5f;
    public float? TrackerTargetAngle;
    private float realAngle = 0f;
    private float angularFrequency = 0f;
    private int rotationDirection = 1;

    public XZTrackerNeedleRenderer(ICoreClientAPI capi, BlockPos trackerPos, IRenderableXZTracker tracker) {
      this.api = capi;
      this.trackerPos = trackerPos;
      this.realAngle = (float)api.World.Rand.NextDouble() * GameMath.TWOPI;
      BackupAngleHandler = CompassMath.GetWildSpinAngleRadians;

      var mesh = tracker.GenNeedleMesh(capi, out Vec3f blockRotationOrigin);
      rotationOrigin = blockRotationOrigin;
      this.meshref = api.Render.UploadMesh(mesh);
      capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "tracker-needle");
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
      UnregisterTickListener();
    }

    private void UnregisterTickListener() {
      if (tickListenerId != null) {
        api.World.UnregisterGameTickListener((long)tickListenerId);
      }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
      if (meshref == null) return;

      IRenderAPI rpi = api.Render;
      Vec3d camPos = api.World.Player.Entity.CameraPos;

      rpi.GlDisableCullFace();
      rpi.GlToggleBlend(true);

      IStandardShaderProgram prog = rpi.PreparedStandardShader(trackerPos.X, trackerPos.Y, trackerPos.Z);
      prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;

      var targetAngle = GameMath.Mod(TrackerTargetAngle ?? BackupAngleHandler(api), GameMath.TWOPI);
      SimulateMovementTo(targetAngle, deltaTime);
      var renderedAngle = realAngle + (AngleDistortionDelegate?.Invoke() ?? 0f);

      prog.ModelMatrix = ModelMat
        .Identity()
        .Translate(trackerPos.X - camPos.X, trackerPos.Y - camPos.Y, trackerPos.Z - camPos.Z)
        .Translate(rotationOrigin.X, rotationOrigin.Y, rotationOrigin.Z)
        .Translate(Offset.X, Offset.Y, Offset.Z)
        .Scale(Scale, Scale, Scale)
        .RotateY(renderedAngle)
        .Translate(-rotationOrigin.X, -rotationOrigin.Y, -rotationOrigin.Z)
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
  }
}

using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {
  abstract class ItemBaseCompass : Item {
    private int MAX_ANGLED_MESHES = 60;
    MeshRef[] meshrefs;
    public override void OnLoaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        OnLoadedClientSide(api as ICoreClientAPI);
      }
    }
    private void OnLoadedClientSide(ICoreClientAPI capi) {
      meshrefs = new MeshRef[MAX_ANGLED_MESHES];

      string key = Code.ToString() + "-meshes";

      var baseShape = capi.Assets.TryGet("compass:shapes/item/compass-base.json")?.ToObject<Shape>();
      var needleShape = GetNeedleShape();

      capi.Tesselator.TesselateShape(this, baseShape, out MeshData compassBaseMeshData, new Vec3f(0, 0, 0));

      meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, () => {
        for (var angleIndex = 0; angleIndex < MAX_ANGLED_MESHES; angleIndex += 1) {

          float angle = (float)((double)angleIndex / MAX_ANGLED_MESHES * 360);
          capi.Tesselator.TesselateShape(this, needleShape, out MeshData meshData, new Vec3f(0, angle, 0));

          meshData.AddMeshData(compassBaseMeshData);

          meshrefs[angleIndex] = capi.Render.UploadMesh(meshData);
        }
        return meshrefs;
      });

      // handle weird bug in VS where GUI shapes are drawn as mirror images: https://github.com/anegostudios/VintageStory-Issues/issues/839
      GuiTransform.ScaleXYZ.X *= -1;
    }

    public abstract Shape GetNeedleShape();

    public abstract double? GetCompassAngleRadians(ICoreClientAPI capi, ItemStack itemstack);

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
      double? angle = null;
      if (target == EnumItemRenderTarget.Gui || target == EnumItemRenderTarget.HandFp) {
        angle = GetCompassAngleRadians(capi, itemstack);
      }
      else {
        // TODO: think of a good solution for Ground and HandTp
        angle = null;
      }
      double resolvedAngle = angle ?? ((double)capi.World.ElapsedMilliseconds / 500);
      var bestMeshrefIndex = (int)GameMath.Mod(resolvedAngle / (Math.PI * 2) * MAX_ANGLED_MESHES + 0.5, MAX_ANGLED_MESHES);
      renderinfo.ModelRef = meshrefs[bestMeshrefIndex];
    }

  }
}
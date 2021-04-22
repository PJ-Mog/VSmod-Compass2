using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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

    public override void OnUnloaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        for (var meshIndex = 0; meshIndex < MAX_ANGLED_MESHES; meshIndex += 1) {
          meshrefs[meshIndex]?.Dispose();
          meshrefs[meshIndex] = null;
        }
      }
    }

    public abstract Shape GetNeedleShape();

    public abstract double? GetCompassAngleRadians(ICoreClientAPI capi, ItemStack itemstack);

    public virtual void OnNewPlayerCompass(IWorldAccessor world, ItemSlot slot, IPlayer player) {
      // pass
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      if (world.Side == EnumAppSide.Server) {
        var attrs = slot.Itemstack.Attributes;
        if (!attrs.HasAttribute("compass-owned")) {
          var player = (slot.Inventory as InventoryBasePlayer)?.Player;
          if (player != null) {
            attrs.SetBool("compass-owned", true);
            OnNewPlayerCompass(world, slot, player);
          }
        }
      }
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
      double? angle = null;
      if (target == EnumItemRenderTarget.Gui || target == EnumItemRenderTarget.HandFp) {
        if (itemstack.Attributes.HasAttribute("compass-owned")) {
          angle = GetCompassAngleRadians(capi, itemstack);
        }
        else {
          angle = null; // e.g. compass is being rendered in Handbook
        }
      }
      else {
        // TODO: think of a good solution for Ground and HandTp
        angle = null;
      }
      double milli = capi.World.ElapsedMilliseconds;
      double resolvedAngle = angle ?? (milli / 500 + Math.Sin(milli / 150) + Math.Sin(milli / 432) * 3);
      var bestMeshrefIndex = (int)GameMath.Mod(resolvedAngle / (Math.PI * 2) * MAX_ANGLED_MESHES + 0.5, MAX_ANGLED_MESHES);
      renderinfo.ModelRef = meshrefs[bestMeshrefIndex];
    }
    public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props) {
      ItemStack placeableCompass = props.TransitionedStack.ResolvedItemstack;
      if (!slot.Empty) {
        BlockCompass.SetIsCrafted(placeableCompass, slot.Itemstack.Attributes.HasAttribute("compass-owned"));
        BlockCompass.SetCraftedByPlayerUID(placeableCompass, BlockCompass.UNKNOWN_PLAYER_UID);
      }
      return placeableCompass;
    }
  }
}

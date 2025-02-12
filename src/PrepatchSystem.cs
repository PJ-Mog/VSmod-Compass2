using System;
using System.Collections.Generic;
using System.Text;
using Compass.ConfigSystem;
using JsonPatch.Operations;
using JsonPatch.Operations.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tavis;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods.NoObf;

namespace Compass.Prepatch {
  public class CompassPrepatchSystem : ModSystem {
    protected class JsonAsset {
      public EnumItemStorageFlags StorageFlags = EnumItemStorageFlags.General;
    }

    protected static readonly string CompassBlockPath = "blocktypes/compass.json";
    protected static readonly string MagneticRecipePath = "recipes/grid/compass-magnetic.json";
    protected static readonly string ScrapRecipePath = "recipes/grid/compass-magnetic-from-scrap.json";
    protected static readonly string OriginRecipePath = "recipes/grid/compass-origin.json";
    protected static readonly string RelativeRecipePath = "recipes/grid/compass-relative.json";

    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Server;
    }

    public override double ExecuteOrder() {
      return 0.04; // Before ModJsonPatchLoader (0.05)
    }

    public override void AssetsLoaded(ICoreAPI api) {
      var settings = api.ModLoader.GetModSystem<CompassConfigurationSystem>().ServerSettings;

      var patches = new List<Vintagestory.ServerMods.NoObf.JsonPatch>();

      patches.Add(GetMagneticRecipeEnabledPatch(settings.EnableMagneticRecipe.Value));
      patches.Add(GetScrapRecipeEnabledPatch(settings.EnableScrapRecipe.Value));
      patches.Add(GetOriginRecipeEnabledPatch(settings.EnableOriginRecipe.Value));
      patches.Add(GetRelativeRecipeEnabledPatch(settings.EnableRelativeRecipe.Value));
      patches.Add(GetCompassOffhandPatch(api, settings.AllowCompassesInOffhand.Value));

      if (settings.EnableOriginRecipe.Value) {
        patches.Add(GetOriginGearQuantityPatch(settings.OriginCompassGears.Value));
      }

      if (settings.EnableRelativeRecipe.Value) {
        patches.Add(GetRelativeGearQuantityPatch(settings.RelativeCompassGears.Value));
      }

      if (!settings.RestrictRelativeCompassCraftingByStability.Value || !api.World.Config.GetBool("temporalStability", true)) {
        patches.Add(GetRelativeCompassHandbookPatch());
      }

      int applied = 0;
      int notFound = 0;
      int errorCount = 0;
      var fakeSource = new AssetLocation(CompassMod.Domain, CompassMod.ModId + "-Prepatcher");
      for (int i = 0; i < patches.Count; i++) {
        ApplyPatch(api, i, fakeSource, patches[i], ref applied, ref notFound, ref errorCount);
      }
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetMagneticRecipeEnabledPatch(bool isEnabled) {
      return GetEnabledPatch(new AssetLocation(CompassMod.Domain, MagneticRecipePath), isEnabled);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetScrapRecipeEnabledPatch(bool isEnabled) {
      return GetEnabledPatch(new AssetLocation(CompassMod.Domain, ScrapRecipePath), isEnabled);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetOriginRecipeEnabledPatch(bool isEnabled) {
      return GetEnabledPatch(new AssetLocation(CompassMod.Domain, OriginRecipePath), isEnabled);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetRelativeRecipeEnabledPatch(bool isEnabled) {
      return GetEnabledPatch(new AssetLocation(CompassMod.Domain, RelativeRecipePath), isEnabled);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetEnabledPatch(AssetLocation assetToPatch, bool isEnabled) {
      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Replace,
        File = assetToPatch,
        Path = "/enabled",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(isEnabled))
      };
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetOriginGearQuantityPatch(int quantityGears) {
      return GetGearsPatch(new AssetLocation(CompassMod.Domain, OriginRecipePath), quantityGears);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetRelativeGearQuantityPatch(int quantityGears) {
      return GetGearsPatch(new AssetLocation(CompassMod.Domain, RelativeRecipePath), quantityGears);
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetGearsPatch(AssetLocation assetToPatch, int quantityGears) {
      string pattern = "C".PadRight(quantityGears + 1, 'G').PadRight(9, '_');
      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Replace,
        File = assetToPatch,
        Path = "/ingredientPattern",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(pattern))
      };
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetCompassOffhandPatch(ICoreAPI api, bool isEnabled) {
      var compassAssetLocation = new AssetLocation(CompassMod.Domain, CompassBlockPath);
      var compassAsset = api.Assets.TryGet(compassAssetLocation);
      var storageFlags = compassAsset.ToObject<JsonAsset>().StorageFlags;

      if (isEnabled) {
        storageFlags = storageFlags | EnumItemStorageFlags.Offhand;
      }
      else {
        storageFlags = ~(~storageFlags | EnumItemStorageFlags.Offhand);
      }

      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Replace,
        File = compassAssetLocation,
        Path = "/storageFlags",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(storageFlags))
      };
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetRelativeCompassHandbookPatch() {
      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Remove,
        File = new AssetLocation(CompassMod.Domain, CompassBlockPath),
        Path = "/attributes/handbookByType/*-relative/extraSections/1"
      };
    }

    // This function is wholesale copied from vanilla's ModJsonPatchLoader#ApplyPatch(), I couldn't get reflection to work to be able to call it externally (private field 'api' isn't set at this point)
    protected void ApplyPatch(ICoreAPI api, int patchIndex, AssetLocation patchSourcefile, Vintagestory.ServerMods.NoObf.JsonPatch jsonPatch, ref int applied, ref int notFound, ref int errorCount) {
      EnumAppSide targetSide = jsonPatch.Side == null ? jsonPatch.File.Category.SideType : (EnumAppSide)jsonPatch.Side;

      if (targetSide != EnumAppSide.Universal && jsonPatch.Side != api.Side) return;

      if (jsonPatch.File == null) {
        api.Logger.ModError("Patch {0} in {1} failed because it is missing the target file property", patchIndex, patchSourcefile);
        return;
      }

      var loc = jsonPatch.File.Clone();

      if (jsonPatch.File.Path.EndsWith("*")) {
        List<IAsset> assets = api.Assets.GetMany(jsonPatch.File.Path.TrimEnd('*'), jsonPatch.File.Domain, false);
        foreach (var val in assets) {
          jsonPatch.File = val.Location;
          ApplyPatch(api, patchIndex, patchSourcefile, jsonPatch, ref applied, ref notFound, ref errorCount);
        }

        jsonPatch.File = loc;

        return;
      }



      if (!loc.Path.EndsWith(".json")) loc.Path += ".json";

      var asset = api.Assets.TryGet(loc);
      if (asset == null) {
        if (jsonPatch.File.Category == null) {
          api.Logger.ModVerboseDebug("Patch {0} in {1}: File {2} not found. Wrong asset category", patchIndex, patchSourcefile, loc);
        }
        else {
          EnumAppSide catSide = jsonPatch.File.Category.SideType;
          if (catSide != EnumAppSide.Universal && api.Side != catSide) {
            api.Logger.ModVerboseDebug("Patch {0} in {1}: File {2} not found. Hint: This asset is usually only loaded {3} side", patchIndex, patchSourcefile, loc, catSide);
          }
          else {
            api.Logger.ModVerboseDebug("Patch {0} in {1}: File {2} not found", patchIndex, patchSourcefile, loc);
          }
        }


        notFound++;
        return;
      }

      Operation op = null;
      switch (jsonPatch.Op) {
        case EnumJsonPatchOp.Add:
          if (jsonPatch.Value == null) {
            api.Logger.ModError("Patch {0} in {1} failed probably because it is an add operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
            errorCount++;
            return;
          }
          op = new AddMergeOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path), Value = jsonPatch.Value.Token };
          break;
        case EnumJsonPatchOp.AddEach:
          if (jsonPatch.Value == null) {
            api.Logger.ModError("Patch {0} in {1} failed probably because it is an add each operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
            errorCount++;
            return;
          }
          op = new AddEachOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path), Value = jsonPatch.Value.Token };
          break;
        case EnumJsonPatchOp.Remove:
          op = new RemoveOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path) };
          break;
        case EnumJsonPatchOp.Replace:
          if (jsonPatch.Value == null) {
            api.Logger.ModError("Patch {0} in {1} failed probably because it is a replace operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
            errorCount++;
            return;
          }
          op = new ReplaceOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path), Value = jsonPatch.Value.Token };
          break;
        case EnumJsonPatchOp.Copy:
          op = new CopyOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path), FromPath = new JsonPointer(jsonPatch.FromPath) };
          break;
        case EnumJsonPatchOp.Move:
          op = new MoveOperation() { Path = new Tavis.JsonPointer(jsonPatch.Path), FromPath = new JsonPointer(jsonPatch.FromPath) };
          break;
      }

      PatchDocument patchdoc = new PatchDocument(op);
      JToken token;
      try {
        token = JToken.Parse(asset.ToText());
      }
      catch (Exception e) {
        api.Logger.ModError("Patch {0} (target: {3}) in {1} failed probably because the syntax of the value is broken: {2}", patchIndex, patchSourcefile, e, loc);
        errorCount++;
        return;
      }

      try {
        patchdoc.ApplyTo(token);
      }
      catch (PathNotFoundException p) {
        api.Logger.ModError("Patch {0} (target: {4}) in {1} failed because supplied path {2} is invalid: {3}", patchIndex, patchSourcefile, jsonPatch.Path, p.Message, loc);
        errorCount++;
        return;
      }
      catch (Exception e) {
        api.Logger.ModError("Patch {0} (target: {3}) in {1} failed, following Exception was thrown: {2}", patchIndex, patchSourcefile, e.Message, loc);
        errorCount++;
        return;
      }

      string text = token.ToString();
      asset.Data = Encoding.UTF8.GetBytes(text);

      applied++;
    }
  }
}

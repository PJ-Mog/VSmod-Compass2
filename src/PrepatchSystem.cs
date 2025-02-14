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
    protected static readonly AssetLocation CompassBlock = new(CompassMod.Domain, "blocktypes/compass.json");

    protected ICoreAPI Api;
    protected CompassServerConfig CompassModServerSettings => Api.ModLoader.GetModSystem<CompassConfigurationSystem>().ServerSettings;

    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Server;
    }

    public override double ExecuteOrder() {
      return 0.04; // Before ModJsonPatchLoader (0.05)
    }

    public override void AssetsLoaded(ICoreAPI api) {
      Api = api;

      int index = 0;
      int appliedCount = 0;
      int notFoundCount = 0;
      int errorCount = 0;
      var fakeSource = new AssetLocation(CompassMod.Domain, CompassMod.ModId + "-Prepatcher");
      foreach (var patch in GetJsonPatches()) {
        ApplyPatch(api, index, fakeSource, patch, ref appliedCount, ref notFoundCount, ref errorCount);
        index++;
      }
    }

    protected List<Vintagestory.ServerMods.NoObf.JsonPatch> GetJsonPatches() {
      List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches = new();

      AddGeneralPatches(jsonPatches);
      AddMagneticCompassPatches(jsonPatches);
      AddOriginCompassPatches(jsonPatches);
      AddRelativeCompassPatches(jsonPatches);
      AddSeraphCompassPatches(jsonPatches);

      return jsonPatches;
    }

    protected void AddGeneralPatches(List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      jsonPatches.Add(GenerateOffhandPatchFor(CompassBlock, CompassModServerSettings.AllowCompassesInOffhand.Value));
    }

    protected void AddMagneticCompassPatches(List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      var magnetiteRecipe = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-magnetic.json");
      var scrapRecipe = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-magnetic-from-scrap.json");

      AddEnablePatchFor(magnetiteRecipe, CompassModServerSettings.EnableMagneticRecipe.Value, jsonPatches);
      AddEnablePatchFor(scrapRecipe, CompassModServerSettings.EnableScrapRecipe.Value, jsonPatches);
    }

    protected void AddOriginCompassPatches(List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      var originRecipe = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-origin.json");

      AddEnablePatchFor(originRecipe, CompassModServerSettings.EnableOriginRecipe.Value, jsonPatches);
      if (CompassModServerSettings.EnableOriginRecipe.Value) {
        AddGearQuantityPatchesFor(originRecipe, CompassModServerSettings.OriginCompassGears.Value, jsonPatches);
      }
    }

    protected void AddRelativeCompassPatches(List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      var relativeRecipe = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-relative.json");
      var relativeRecipeReattune = new AssetLocation(CompassMod.Domain, "recipes/grid/reattunement/compass-relative.json");

      AddEnablePatchFor(relativeRecipe, CompassModServerSettings.EnableRelativeRecipe.Value, jsonPatches);
      if (CompassModServerSettings.EnableRelativeRecipe.Value) {
        AddGearQuantityPatchesFor(relativeRecipe, CompassModServerSettings.RelativeCompassGears.Value, jsonPatches);
      }
      if (!CompassModServerSettings.RestrictRelativeCompassCraftingByStability.Value || !Api.World.Config.GetBool("temporalStability", true)) {
        jsonPatches.Add(GetRelativeCompassHandbookPatch());
      }

      AddEnablePatchFor(relativeRecipeReattune, CompassModServerSettings.EnableReattuneRelativeCompass.Value, jsonPatches);
      if (CompassModServerSettings.EnableReattuneRelativeCompass.Value) {
        AddGearQuantityPatchesFor(relativeRecipeReattune, CompassModServerSettings.ReattuneRelativeCompassGears.Value, jsonPatches);
      }
    }

    protected void AddSeraphCompassPatches(List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      var seraphRecipeFromOrigin = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-player-from-origin.json");
      var seraphRecipeFromRelative = new AssetLocation(CompassMod.Domain, "recipes/grid/compass-player-from-relative.json");
      var seraphRecipeReattune = new AssetLocation(CompassMod.Domain, "recipes/grid/reattunement/compass-player.json");

      AddEnablePatchFor(seraphRecipeFromOrigin, CompassModServerSettings.EnableSeraphRecipe.Value, jsonPatches);
      AddEnablePatchFor(seraphRecipeFromRelative, CompassModServerSettings.EnableSeraphRecipe.Value, jsonPatches);
      AddEnablePatchFor(seraphRecipeReattune, CompassModServerSettings.EnableReattuneSeraphRecipe.Value, jsonPatches);
      if (CompassModServerSettings.DamageTakenToCraftSeraphCompass.Value <= 0.0f) {
        jsonPatches.Add(GetSeraphCompassHandbookPatch());
      }
    }

    protected void AddEnablePatchFor(AssetLocation assetToPatch, bool isEnabled, List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      jsonPatches.Add(new() {
        Op = EnumJsonPatchOp.Replace,
        File = assetToPatch,
        Path = "/enabled",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(isEnabled))
      });
    }

    protected class AssetWithIngredientPattern {
      public string IngredientPattern = "";
    }

    // Assumes a grid recipe of max size (3x3), shapeless, with 'G' as the ingredient key for temporal gears and has an explicit quantity for those gears.
    protected void AddGearQuantityPatchesFor(AssetLocation assetLocation, int gearQuantity, List<Vintagestory.ServerMods.NoObf.JsonPatch> jsonPatches) {
      // Extract current ingredient pattern and erase all 'G' and '_' from it
      var asset = Api.Assets.TryGet(assetLocation);
      var ingredientPattern = asset.ToObject<AssetWithIngredientPattern>().IngredientPattern.Replace("G", "").Replace("_", "");
      if (ingredientPattern.Length >= 9) {
        Api.Logger.ModError("Could not modify temporal gears in {0}. Not enough space in the pattern '{1}'. Skipping.", assetLocation, ingredientPattern);
        return;
      }

      // For general user acceptance purposes, temporal gears in the recipes are spread out, 1 per slot, unless the configured quantity is not possible without stacking.
      // Shapeless recipes that require multiple of the same item do not care if those items are stacked or spread out.
      // A recipe with 9 of the same item in a single stack and a recipe with 9 of the same item, 1 in each slot of the grid are the same,
      // and each one can be crafted with the other's arrangement.
      // A user configuring the required gears beyond what can fit into the grid without stacking is assumed to have external modifications that allow this.
      if (ingredientPattern.Length + gearQuantity > 9) {
        ingredientPattern = (ingredientPattern + "G").PadRight(9, '_');
        jsonPatches.Add(new() {
          Op = EnumJsonPatchOp.Replace,
          File = assetLocation,
          Path = "/ingredients/G/quantity",
          Value = JsonObject.FromJson(JsonConvert.SerializeObject(gearQuantity))
        });
      }
      else {
        ingredientPattern = (ingredientPattern + "".PadRight(gearQuantity, 'G')).PadRight(9, '_');
      }

      jsonPatches.Add(new() {
        Op = EnumJsonPatchOp.Replace,
        File = assetLocation,
        Path = "/ingredientPattern",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(ingredientPattern))
      });
    }

    protected class AssetWithStorageFlags {
      public EnumItemStorageFlags StorageFlags = EnumItemStorageFlags.General;
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GenerateOffhandPatchFor(AssetLocation assetLocation, bool isEnabled) {
      // Retrieve the asset as it was initially loaded from JSON.
      // Convert it to a simple object to extract the current StorageFlags so that the offhand flag can be manipulated without affecting other settings.
      var asset = Api.Assets.TryGet(assetLocation);
      var storageFlags = asset.ToObject<AssetWithStorageFlags>().StorageFlags;

      if (isEnabled) {
        storageFlags |= EnumItemStorageFlags.Offhand;
      }
      else {
        storageFlags = ~(~storageFlags | EnumItemStorageFlags.Offhand);
      }

      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Replace,
        File = assetLocation,
        Path = "/storageFlags",
        Value = JsonObject.FromJson(JsonConvert.SerializeObject(storageFlags))
      };
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetRelativeCompassHandbookPatch() {
      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Remove,
        File = CompassBlock,
        Path = "/attributes/handbookByType/*-relative/extraSections/1"
      };
    }

    protected Vintagestory.ServerMods.NoObf.JsonPatch GetSeraphCompassHandbookPatch() {
      return new Vintagestory.ServerMods.NoObf.JsonPatch() {
        Op = EnumJsonPatchOp.Remove,
        File = CompassBlock,
        Path = "/attributes/handbookByType/*-player/extraSections/1"
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

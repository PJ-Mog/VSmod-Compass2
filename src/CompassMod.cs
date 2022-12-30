using System.Reflection;
using Compass.Common;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Compass {
  public class CompassMod : ModSystem {
    public const string NETWORK_CHANNEL = "compass2";
    private Config config;
    public PlayerPosHandler PlayerPosHandler;

    public override void Start(ICoreAPI api) {
      base.Start(api);

      config = Config.LoadOrCreateDefault(api);
      PlayerPosHandler = new PlayerPosHandler(api);

      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));
      api.RegisterBlockClass("BlockPlayerCompass", typeof(BlockPlayerCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityXZTracker));

      var harmony = new Harmony("japanhasrice.compass2");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void Dispose() {
      base.Dispose();
      new Harmony("japanhasrice.compass2").UnpatchAll("japanhasrice.compass2");
    }

    public override void AssetsFinalize(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) { return; }

      var sapi = (ICoreServerAPI)api;
      var scrapRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-magnetic-from-scrap.json");
      var originRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-origin.json");
      var relativeRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-relative.json");
      var compassModGridRecipes = sapi.World.GridRecipes.FindAll(gr => gr.Name.Domain == "compass");
      var scrap = compassModGridRecipes.Find(gr => gr.Name.Path == scrapRecipeAssetLoc.Path);
      var origin = compassModGridRecipes.Find(gr => gr.Name.Path == originRecipeAssetLoc.Path);
      var relative = compassModGridRecipes.Find(gr => gr.Name.Path == relativeRecipeAssetLoc.Path);

      if (!config.EnableScrapRecipe) {
        sapi.World.GridRecipes.Remove(scrap);
      }

      if (!config.EnableOriginRecipe) {
        sapi.World.GridRecipes.Remove(origin);
      }
      else {
        origin.IngredientPattern = "C".PadRight(config.OriginCompassGears + 1, 'G').PadRight(9, '_');
        origin.ResolveIngredients(sapi.World);
      }

      if (!config.EnableRelativeRecipe) {
        sapi.World.GridRecipes.Remove(relative);
      }
      else {
        relative.IngredientPattern = "C".PadRight(config.RelativeCompassGears + 1, 'G').PadRight(9, '_');
        relative.ResolveIngredients(sapi.World);
      }

      if (config.AllowCompassesInOffhand) {
        var allCompassModBlockAssetLocs = sapi.World.Collectibles.FindAll(c => c.Code.Domain.Equals("compass") && c.Code.ToShortString().Contains("compass"));
        foreach (var collectible in allCompassModBlockAssetLocs) {
          collectible.StorageFlags = collectible.StorageFlags | EnumItemStorageFlags.Offhand;
        }
      }
    }
  }
}

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[assembly: ModInfo("Compass")]

namespace Compass {
  public class CompassMod : ModSystem {
    private ModConfig config;

    private AssetLocation scrapRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-magnetic-from-scrap.json");
    private AssetLocation originRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-origin.json");
    private AssetLocation relativeRecipeAssetLoc = new AssetLocation("compass", "recipes/grid/compass-relative.json");

    public override void Start(ICoreAPI api) {
      api.Logger.Debug("[Compass] Start");
      base.Start(api);
      
      config = ModConfig.Load(api);

      api.RegisterItemClass("ItemMagneticCompass", typeof(ItemMagneticCompass));
      api.RegisterItemClass("ItemRelativeCompass", typeof(ItemRelativeCompass));
      api.RegisterItemClass("ItemOriginCompass", typeof(ItemOriginCompass));

      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityCompass));

      if (api.Side == EnumAppSide.Server) {
        var sapi = (ICoreServerAPI)api;
        sapi.Event.ServerRunPhase(EnumServerRunPhase.GameReady, () => {
          var compassModGridRecipes = sapi.World.GridRecipes.FindAll(gr => gr.Name.Domain == "compass");
          var scrap = compassModGridRecipes.Find(gr => gr.Name.Path == scrapRecipeAssetLoc.Path);
          var origin = compassModGridRecipes.Find(gr => gr.Name.Path == originRecipeAssetLoc.Path);
          var relative = compassModGridRecipes.Find(gr => gr.Name.Path == relativeRecipeAssetLoc.Path);

          if (!config.EnableScrapRecipe) sapi.World.GridRecipes.Remove(scrap);

          if (!config.EnableOriginRecipe) sapi.World.GridRecipes.Remove(origin);
          else {
            origin.IngredientPattern = "C".PadRight(GameMath.Clamp(config.OriginCompassGears, 1, 8) + 1, 'G').PadRight(9, '_');
            origin.ResolveIngredients(sapi.World);
          }

          if (!config.EnableRelativeRecipe) sapi.World.GridRecipes.Remove(relative);
          else {
            relative.IngredientPattern = "C".PadRight(GameMath.Clamp(config.RelativeCompassGears, 1, 8) + 1, 'G').PadRight(9, '_');
            relative.ResolveIngredients(sapi.World);
          }
        });
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

[assembly: ModInfo("Compass")]

namespace Compass {
  public class CompassMod : ModSystem {
    private ModConfig config;
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
        ((ICoreServerAPI)api).Event.ServerRunPhase(EnumServerRunPhase.GameReady, () => {
          var compassRecipes = ((ICoreServerAPI)api).World.GridRecipes.FindAll(r => r.Name.Domain == "compass");
          var originRecipe = compassRecipes.Find(r => r.Name.ToShortString().Contains("origin"));
          var relativeRecipe = compassRecipes.Find(r => r.Name.ToShortString().Contains("relative"));
          var scrapRecipe = compassRecipes.Find(r => r.Name.ToShortString().Contains("scrap"));

          originRecipe.IngredientPattern = "C".PadRight(config.OriginCompassGears + 1, 'G').PadRight(9, '_');
          originRecipe.ResolveIngredients(api.World);
          originRecipe.Enabled = config.EnableOriginRecipe;

          relativeRecipe.IngredientPattern = "C".PadRight(config.RelativeCompassGears + 1, 'G').PadRight(9, '_');
          relativeRecipe.ResolveIngredients(api.World);
          relativeRecipe.Enabled = config.EnableRelativeRecipe;

          scrapRecipe.Enabled = config.EnableScrapRecipe;
        });
      }
    }
  }
}

using Compass.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Compass {
  public class CompassMod : ModSystem {
    public static readonly string HarmonyId = "compass2.japanhasrice";
    private Config config;
    private ICoreClientAPI Capi;

    public override void Start(ICoreAPI api) {
      base.Start(api);

      config = Config.LoadOrCreateDefault(api);

      RegisterModClasses(api);
    }

    public void RegisterModClasses(ICoreAPI api) {
      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));
      api.RegisterBlockClass("BlockPlayerCompass", typeof(BlockPlayerCompass));

      api.RegisterBlockEntityClass("BlockEntityCompass", typeof(BlockEntityXZTracker));
    }

    public override void StartClientSide(ICoreClientAPI capi) {
      base.StartClientSide(capi);
      Capi = capi;
      capi.World.RegisterGameTickListener(ThirdPersonCompassHandlingTick, 1, 5000);
    }

    protected void ThirdPersonCompassHandlingTick(float dt) {
      var onlinePlayers = Capi.World.AllOnlinePlayers;
      if (onlinePlayers.Length < 2) { return; }

      foreach (var player in onlinePlayers) {
        var playerEntity = player.Entity;
        if (playerEntity == null) { continue; }
        var stack = player.InventoryManager?.ActiveHotbarSlot?.Itemstack;
        (stack?.Collectible as BlockCompass)?.SetHoldingEntityData(stack, playerEntity);
      }
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

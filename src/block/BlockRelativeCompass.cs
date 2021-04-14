using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  class BlockRelativeCompass : BlockCompass {
    private static AssetLocation update = new AssetLocation("compass:recipes/grid/update.json");
    public override float GetNeedleAngleRadians(BlockPos fromPos, ItemStack compass) {
      return GetAngleRadians(fromPos, GetCompassCraftedPos(compass));
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
      if (byRecipe.Name.Equals(update) && this.api.Side == EnumAppSide.Server) {
        foreach (var slot in allInputslots) {
          if (!slot.Empty) {
            var x = slot.Itemstack?.Attributes.GetInt("compass-target-x");
            var z = slot.Itemstack?.Attributes.GetInt("compass-target-z");
            if (x == null || z == null) return;
            var targetPos = new BlockPos((int)x, 0, (int)z);
            var y = ((ICoreServerAPI)api).World.BlockAccessor.GetTerrainMapheightAt(targetPos);
            targetPos.Y = y;
            SetCompassCraftedPos(outputSlot.Itemstack, targetPos);
          }
        }
      }
    }
  }
}
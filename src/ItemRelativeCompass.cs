using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Compass {
  class ItemRelativeCompass : ItemBaseCompass {
    // public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
    //   outputSlot.Itemstack.Attributes.SetDouble("x", outputSlot.Inventory);
    // }
    // public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity) {
    //   base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
    // }
    public override Shape GetNeedleShape() {
      return api.Assets.TryGet("compass:shapes/item/compass-needle-relative.json")?.ToObject<Shape>();
    }

    public override void OnNewPlayerCompass(IWorldAccessor world, ItemSlot slot, IPlayer player) {
      var attrs = slot.Itemstack.Attributes;
      var blockPos = player.Entity.Pos.AsBlockPos;
      attrs.SetInt("compass-target-x", blockPos.X);
      attrs.SetInt("compass-target-z", blockPos.Z);
    }

    public override double? GetCompassAngleRadians(ICoreClientAPI capi, ItemStack itemstack) {
      var playerPos = capi.World.Player.Entity.Pos.AsBlockPos;
      var targetX = itemstack.Attributes.GetInt("compass-target-x");
      var targetZ = itemstack.Attributes.GetInt("compass-target-z");

      var dX = playerPos.X - targetX;
      var dZ = playerPos.Z - targetZ;
      if (dX * dX + dZ * dZ < 2 * 2) { return null; }

      return Math.Atan2(dX, dZ) - capi.World.Player.CameraYaw;
    }
  }
}
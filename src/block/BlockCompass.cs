using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Compass {
  abstract class BlockCompass : Block {
    public override void OnLoaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        OnLoadedClientSide(api as ICoreClientAPI);
      }
    }
    private void OnLoadedClientSide(ICoreClientAPI capi) {
      // handle weird bug in VS where GUI shapes are drawn as mirror images: https://github.com/anegostudios/VintageStory-Issues/issues/839
      GuiTransform.Scale = -2.75f;
      GuiTransform.Rotate = false;
      GuiTransform.Translation.Add(-2f, 0f, 0f);
      GuiTransform.Rotation.Add(0f, 0f, 5f);
    }

    public abstract float GetNeedleAngleRadians(BlockPos fromPos);

    protected static float GetAngleRadians(BlockPos fromPos, BlockPos toPos) {
      return (float)Math.Atan2(fromPos.X - toPos.X, fromPos.Z - toPos.Z);
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
      if (world.Side == EnumAppSide.Server) {
        if (!HasPosSet(slot.Itemstack)) {
          var player = (slot.Inventory as InventoryBasePlayer)?.Player;
          if (player != null) {
            SetCompassCraftedPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
          }
        }
      }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
      ItemStack stack = base.OnPickBlock(world, pos);

      BlockEntityCompass bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCompass;

      if (bec != null)
      {
        SetCompassCraftedPos(stack, bec.compassCraftedPos);
      }

      return stack;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      return new ItemStack[] { OnPickBlock(world, pos) };
    }

    public static void SetCompassCraftedPos(ItemStack compassStack, BlockPos pos) {
      var attrs = compassStack.Attributes;
      attrs.SetInt("compass-crafted-x", pos.X);
      attrs.SetInt("compass-crafted-y", pos.Y);
      attrs.SetInt("compass-crafted-z", pos.Z);
    }

    public static BlockPos GetCompassCraftedPos(ItemStack compassStack) {
      var attrs = compassStack.Attributes;
      var x = attrs.GetInt("compass-crafted-x");
      var y = attrs.GetInt("compass-crafted-y");
      var z = attrs.GetInt("compass-crafted-z");

      return new BlockPos(x, y, z);
    }

    public static bool HasPosSet(ItemStack compassStack) {
      return compassStack.Attributes.HasAttribute("compass-crafted-z");
    }
  }
}
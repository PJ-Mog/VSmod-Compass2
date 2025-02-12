using System.Collections.Generic;
using Compass.ConfigSystem;
using Compass.PlayerPos;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  public class BlockPlayerCompass : BlockCompass {
    protected static readonly string NotificationWillTakeDamage = CompassMod.Domain + "-will-take-damage";

    protected float DamageTakenToCraft { get; set; } = 0.0f;

    public override EnumTargetType TargetType { get; protected set; } = EnumTargetType.Moving;

    protected override void LoadServerSettings(CompassServerConfig serverSettings) {
      base.LoadServerSettings(serverSettings);

      DamageTakenToCraft = serverSettings.DamageTakenToCraftSeraphCompass.Value;
    }

    protected override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCachedPos(GetCraftedByPlayerUID(compassStack));
    }

    public BlockPos GetCachedPos(string playerUid) {
      var playerPosSystem = api.ModLoader.GetModSystem<PlayerPosSystem>();
      return playerPosSystem?.GetPlayerPos(api as ICoreClientAPI, playerUid) ?? api.World.PlayerByUid(playerUid)?.Entity?.Pos?.AsBlockPos;
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
      var inventory = outputSlot.Inventory;
      if (inventory == null) {
        // Player is viewing an item in the handbook which is a crafting ingredient for this.
        SetCraftedByPlayerUID(outputSlot.Itemstack, "handbook");
        return;
      }

      if (DamageTakenToCraft > 0.0f) {
        var playerUid = inventory.InventoryID.Replace(GlobalConstants.craftingInvClassName + "-", "");
        var player = api.World.PlayerByUid(playerUid);
        (player as IServerPlayer)?.SendIngameError(NotificationWillTakeDamage, langparams: DamageTakenToCraft);
      }
    }

    protected override void OnSuccessfullyCrafted(IServerWorldAccessor world, IServerPlayer byPlayer, ItemSlot slot) {
      base.OnSuccessfullyCrafted(world, byPlayer, slot);

      if (DamageTakenToCraft > 0.0f) {
        var damageSource = new DamageSource() {
          Source = EnumDamageSource.Internal,
          Type = EnumDamageType.Injury,
          KnockbackStrength = 0f,
          YDirKnockbackDiv = 0f
        };
        byPlayer.Entity.ReceiveDamage(damageSource, DamageTakenToCraft);
      }
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return base.ShouldPointToTarget(fromPos, compassStack)
             && GetTargetPos(compassStack) != null;
    }

    public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi) {
      var list = base.GetHandBookStacks(capi);
      foreach (var compassStack in list) {
        SetCraftedByPlayerUID(compassStack, "handbook");
      }
      return list;
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer) {
      var list = base.GetDropsForHandbook(handbookStack, forPlayer);
      foreach (var compassDrop in list) {
        SetCraftedByPlayerUID(compassDrop.ResolvedItemstack, "handbook");
      }
      return list;
    }
  }
}

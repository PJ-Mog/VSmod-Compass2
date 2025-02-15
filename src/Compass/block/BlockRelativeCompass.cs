using System.Text;
using Compass.ConfigSystem;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass {
  public class BlockRelativeCompass : BlockCompass {
    protected static readonly string ErrorTemporalStabilityTooHigh = CompassMod.Domain + "-stability-too-high";

    protected bool IsTemporalStabilityEnabled { get; set; } = false;

    protected override void LoadExternalSystemsAndSettings(ICoreAPI api) {
      base.LoadExternalSystemsAndSettings(api);
      IsTemporalStabilityEnabled = TemporalStabilitySystem != null && api.World.Config.GetBool("temporalStability", true);
    }

    protected override void LoadServerSettings(CompassServerConfig serverSettings) {
      base.LoadServerSettings(serverSettings);

      IsCraftingRestrictedByStability = IsTemporalStabilityEnabled && serverSettings.RestrictRelativeCompassCraftingByStability.Value;
      AllowCraftingBelowStability = serverSettings.AllowRelativeCompassCraftingBelowStability.Value;
    }

    protected override void OnSuccessfullyCrafted(IServerWorldAccessor world, IServerPlayer player, ItemSlot slot) {
      if (!IsCraftingRestrictedByStability || IsStabilityLowEnough(player?.Entity?.Pos.AsBlockPos.ToVec3d())) {
        base.OnSuccessfullyCrafted(world, player, slot);
        SetTargetPos(slot.Itemstack, player.Entity.Pos.AsBlockPos);
      }
      else {
        player.SendIngameError(ErrorTemporalStabilityTooHigh);
      }
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
      if (!IsCraftingRestrictedByStability) { return; }

      var inventory = outputSlot.Inventory;
      if (inventory == null) {
        // Probably a handbook/creative instance.
        return;
      }

      var playerUid = inventory?.InventoryID.Replace(GlobalConstants.craftingInvClassName + "-", "");
      var player = api.World.PlayerByUid(playerUid);
      var pos = player?.Entity?.Pos?.AsBlockPos?.ToVec3d();

      if (IsStabilityLowEnough(pos)) { return; }

      outputSlot.TakeOutWhole();
      outputSlot.MarkDirty();
      (player as IServerPlayer)?.SendIngameError(ErrorTemporalStabilityTooHigh);
    }

    protected bool IsStabilityLowEnough(Vec3d pos) {
      if (pos == null) { return true; }
      pos.Y = SeaLevel;
      var stability = TemporalStabilitySystem.GetTemporalStability(pos);
      return stability < AllowCraftingBelowStability;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
      base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

      if (inSlot.Inventory?.ClassName == "dummy" || inSlot.Inventory?.ClassName == "not") {
        // Probably a handbook instance.
        return;
      }

      if (GetTargetPos(inSlot.Itemstack) == null) {
        dsc.AppendLine(Lang.Get(CompassMod.Domain + ":not-attuned-to-location"));
        return;
      }
    }
  }
}

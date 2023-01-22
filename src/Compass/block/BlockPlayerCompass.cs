using Compass.PlayerPos;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Compass {
  public class BlockPlayerCompass : BlockCompass {
    public override EnumTargetType TargetType { get; protected set; } = EnumTargetType.Moving;

    protected override BlockPos GetTargetPos(ItemStack compassStack) {
      return GetCachedPos(GetCraftedByPlayerUID(compassStack));
    }

    public BlockPos GetCachedPos(string playerUid) {
      var playerPosSystem = api.ModLoader.GetModSystem<PlayerPosSystem>();
      return playerPosSystem?.GetPlayerPos(api as ICoreClientAPI, playerUid) ?? api.World.PlayerByUid(playerUid)?.Entity?.Pos?.AsBlockPos;
    }

    public override bool ShouldPointToTarget(BlockPos fromPos, ItemStack compassStack) {
      return base.ShouldPointToTarget(fromPos, compassStack)
             && GetTargetPos(compassStack) != null;
    }
  }
}

using Compass;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CompassAdmin {
  public class CompassAdminSystem : ModSystem {
    protected ICoreServerAPI sapi;

    public override void StartServerSide(ICoreServerAPI api) {
      base.StartServerSide(api);
      sapi = api;
      sapi.RegisterCommand("compass", "For viewing or manipulating compasses from the Compass mod or its extensions.", "[for|show|set|help]", OnCompassCommand, Privilege.commandplayer);
    }

    public void OnCompassCommand(IServerPlayer callingPlayer, int groupId, CmdArgs args) {
      string commandBase = "/compass";
      switch (args.PopWord()?.ToLowerInvariant()) {
        case "for":
          OnFor(commandBase + " for", callingPlayer, groupId, args);
          break;
        case "show":
          OnShow(commandBase + " show", callingPlayer, groupId, args, callingPlayer);
          break;
        case "set":
          OnSet(commandBase + " set", callingPlayer, groupId, args, callingPlayer);
          break;
        case "reset":
          OnReset(commandBase + " reset", callingPlayer, groupId, args, callingPlayer);
          break;
        case "remove":
          OnRemove(commandBase + " remove", callingPlayer, groupId, args, callingPlayer);
          break;
        case "help":
          callingPlayer.SendMessage(groupId, commandBase + " [for|show|set|reset|help]", EnumChatType.CommandError);
          callingPlayer.SendMessage(groupId, "for: Perform a command on a different player's compass in their active hotbar slot. Can reference the player by their UID or current Name.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "show: View data for the currently held compass.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "set: Change data for the currently held compass.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "reset: Reset data for the currently held compass. The compass will act as if it had just been crafted.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "remove: Delete data for the currently held compass.", EnumChatType.CommandSuccess);
          break;
        default:
          callingPlayer.SendMessage(groupId, commandBase + " [for|show|set|reset|remove|help]", EnumChatType.CommandError);
          break;
      }
    }

    public void OnCompassCommand(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      switch (args.PopWord()?.ToLowerInvariant()) {
        case "show":
          OnShow(commandString + " show", callingPlayer, groupId, args, compassHolder);
          break;
        case "set":
          OnSet(commandString + " set", callingPlayer, groupId, args, compassHolder);
          break;
        case "reset":
          OnReset(commandString + " reset", callingPlayer, groupId, args, compassHolder);
          break;
        case "remove":
          OnRemove(commandString + " remove", callingPlayer, groupId, args, compassHolder);
          break;
        case "help":
          callingPlayer.SendMessage(groupId, commandString + " [show|set|reset|help]", EnumChatType.CommandError);
          callingPlayer.SendMessage(groupId, "show: View data for the currently held compass.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "set: Change data for the currently held compass.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "reset: Reset data for the currently held compass. The compass will act as if it had just been crafted.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "remove: Delete data for the currently held compass.", EnumChatType.CommandSuccess);
          break;
        default:
          callingPlayer.SendMessage(groupId, commandString + " [show|set|reset|remove|help]", EnumChatType.CommandError);
          break;
      }
    }

    public void OnFor(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args) {
      var forPlayerString = args.PopWord();
      if (forPlayerString == null) {
        callingPlayer.SendMessage(groupId, commandString + " <player_name_or_uid>", EnumChatType.CommandError);
        return;
      }

      IServerPlayer compassHolder = null;
      foreach (IServerPlayer onlinePlayer in sapi.World.AllOnlinePlayers) {
        if ((onlinePlayer.PlayerName == forPlayerString || onlinePlayer.PlayerUID == forPlayerString) && onlinePlayer?.ConnectionState == EnumClientState.Playing) {
          compassHolder = onlinePlayer;
          break;
        }
      }

      if (compassHolder == null) {
        callingPlayer.SendMessage(groupId, Lang.Get("Player {0} is not online or does not exist.", forPlayerString), EnumChatType.CommandError);
        return;
      }

      OnCompassCommand(commandString + " " + forPlayerString, callingPlayer, groupId, args, compassHolder);
    }

    public void OnShow(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var activeStack = compassHolder?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      var craftedByUid = activeCompass.AdminGetCraftedByPlayerUID(activeStack);
      var craftedByName = sapi.PlayerData.GetPlayerDataByUid(craftedByUid)?.LastKnownPlayername;
      var targetPos = activeCompass.AdminGetTargetPos(activeStack);
      callingPlayer.SendMessage(groupId, Lang.Get("{0} held by {1} has a target pos of {2} and was crafted by '{3}' (UID: {4})", activeCompass.Code, compassHolder.PlayerName, targetPos, craftedByName, craftedByUid), EnumChatType.CommandSuccess);
    }

    public void OnSet(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      switch (args.PopWord()?.ToLowerInvariant()) {
        case "craftedby":
          OnSetCraftedBy(commandString + " craftedby", callingPlayer, groupId, args, compassHolder);
          break;
        case "target":
          OnSetTarget(commandString + " target", callingPlayer, groupId, args, compassHolder);
          break;
        case "help":
          callingPlayer.SendMessage(groupId, commandString + " [craftedBy|target|help]", EnumChatType.CommandError);
          callingPlayer.SendMessage(groupId, "craftedBy: Change who originally created the compass. Can use a player's Name or UID or a made-up value.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "target: Change the compass's targeted position to the provided coordinates, formatted ~X ~Y ~Z, =X =Y =Z, or X Y Z", EnumChatType.CommandSuccess);
          break;
        default:
          callingPlayer.SendMessage(groupId, commandString + " [craftedBy|target|help]", EnumChatType.CommandError);
          break;
      }
    }

    public void OnSetCraftedBy(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var newCrafterString = args.PopAll();
      if (newCrafterString.Length == 0) {
        callingPlayer.SendMessage(groupId, commandString + " <player_name|player_uid|other>", EnumChatType.CommandError);
        return;
      }

      var activeSlot = compassHolder?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      string newCraftedByName = null;
      string newCraftedByUid = newCrafterString;

      var playerData = sapi.PlayerData.GetPlayerDataByLastKnownName(newCrafterString) ?? sapi.PlayerData.GetPlayerDataByUid(newCrafterString);
      if (playerData != null) {
        newCraftedByName = playerData.LastKnownPlayername;
        newCraftedByUid = playerData.PlayerUID;
      }

      activeCompass.AdminSetCraftedByPlayerUID(activeStack, newCraftedByUid);
      activeSlot.MarkDirty();
      compassHolder.InventoryManager.BroadcastHotbarSlot();
      string newValueOutput = newCraftedByName == null ? $"'{newCraftedByUid}'" : $"'{newCraftedByName}' (UID: {newCraftedByUid})";
      callingPlayer.SendMessage(groupId, Lang.Get("Successfully set Crafter for {0} held by {1} to {2}.", activeCompass.Code, compassHolder.PlayerName, newValueOutput), EnumChatType.CommandSuccess);
    }

    public void OnSetTarget(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var mapMiddle = sapi.World.DefaultSpawnPosition.XYZ.Clone();
      mapMiddle.Y = 0;
      var newPos = args.PopFlexiblePos(compassHolder.Entity?.Pos.XYZ, mapMiddle)?.AsBlockPos;
      if (newPos == null) {
        callingPlayer.SendMessage(groupId, commandString + " X Y Z", EnumChatType.CommandError);
        return;
      }

      var activeSlot = compassHolder?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      activeCompass.AdminSetTargetPos(activeStack, newPos);
      activeSlot.MarkDirty();
      compassHolder.InventoryManager.BroadcastHotbarSlot();
      callingPlayer.SendMessage(groupId, Lang.Get("Successfully set TargetPos for {0} held by {1} to {2}.", activeCompass.Code, compassHolder.PlayerName, newPos), EnumChatType.CommandSuccess);
    }

    public void OnReset(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var activeSlot = compassHolder.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      activeCompass.AdminReset(activeStack);
      activeSlot.MarkDirty();
      callingPlayer.SendMessage(groupId, Lang.Get("Successfully reset {0} held by {1}.", activeCompass.Code, compassHolder.PlayerName), EnumChatType.CommandSuccess);
    }

    public void OnRemove(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      switch (args.PopWord()?.ToLowerInvariant()) {
        case "craftedby":
          OnRemoveCraftedBy(commandString + " craftedby", callingPlayer, groupId, args, compassHolder);
          break;
        case "target":
          OnRemoveTarget(commandString + " target", callingPlayer, groupId, args, compassHolder);
          break;
        case "help":
          callingPlayer.SendMessage(groupId, commandString + " [craftedBy|target|help]", EnumChatType.CommandError);
          callingPlayer.SendMessage(groupId, "craftedBy: Deletes the reference to the original crafter. Will immediately change to the current holder.", EnumChatType.CommandSuccess);
          callingPlayer.SendMessage(groupId, "target: Deletes the target position.", EnumChatType.CommandSuccess);
          break;
        default:
          callingPlayer.SendMessage(groupId, commandString + " [craftedBy|target|help]", EnumChatType.CommandError);
          break;
      }
    }

    public void OnRemoveCraftedBy(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var activeSlot = compassHolder?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      activeCompass.AdminSetCraftedByPlayerUID(activeStack, null);
      activeSlot.MarkDirty();
      compassHolder.InventoryManager.BroadcastHotbarSlot();
      callingPlayer.SendMessage(groupId, Lang.Get("Successfully removed Crafter for {0} held by {1}.", activeCompass.Code, compassHolder.PlayerName), EnumChatType.CommandSuccess);
    }

    public void OnRemoveTarget(string commandString, IServerPlayer callingPlayer, int groupId, CmdArgs args, IServerPlayer compassHolder) {
      var activeSlot = compassHolder?.InventoryManager?.ActiveHotbarSlot;
      var activeStack = activeSlot?.Itemstack;
      var activeCompass = activeStack?.Collectible as BlockCompass;
      if (activeCompass == null) {
        OnNotHoldingCompassError(callingPlayer, groupId, compassHolder);
        return;
      }

      activeCompass.AdminSetTargetPos(activeStack, null);
      activeSlot.MarkDirty();
      compassHolder.InventoryManager.BroadcastHotbarSlot();
      callingPlayer.SendMessage(groupId, Lang.Get("Successfully removed Target for {0} held by {1}.", activeCompass.Code, compassHolder.PlayerName), EnumChatType.CommandSuccess);
    }

    public void OnNotHoldingCompassError(IServerPlayer callingPlayer, int groupId, IServerPlayer compassHolder) {
      callingPlayer.SendMessage(groupId, Lang.Get("{0} needs to be holding a compass in their active hotbar slot.", compassHolder.PlayerName), EnumChatType.CommandError);
      compassHolder.SendIngameError("Hold a compass in your active hotbar slot while the admin runs the commands");
    }
  }
}

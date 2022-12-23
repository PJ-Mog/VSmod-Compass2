using System.Collections.Generic;
using Compass.Common.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass.Common {
  public class PlayerPosHandler {
    private class PlayerPosData {
      public string PlayerUid = "";
      public BlockPos LastKnownPos;
      public long LastUpdatedAt = 0;
      public bool IsAwaitingUpdate = false;
      public bool HasReceivedUpdate = false;
      public long LastRequestedServerDataAt = 0;

      public PlayerPosData(string playerUid) {
        PlayerUid = playerUid;
      }
    }

    private Dictionary<string, PlayerPosData> posCache = new Dictionary<string, PlayerPosData>();

    public PlayerPosHandler(ICoreAPI api) {
      api.Network.RegisterChannel(CompassMod.NETWORK_CHANNEL)
                 .RegisterMessageType(typeof(RequestPosMessage))
                 .RegisterMessageType(typeof(PosDataMessage));

      if (api.Side == EnumAppSide.Server) {
        (api as ICoreServerAPI).Network.GetChannel(CompassMod.NETWORK_CHANNEL).SetMessageHandler<RequestPosMessage>(OnReceivedPosRequest);
      }
      else {
        (api as ICoreClientAPI).Network.GetChannel(CompassMod.NETWORK_CHANNEL).SetMessageHandler<PosDataMessage>(OnReceivedPosUpdate);
      }
    }

    public BlockPos GetPlayerPos(ICoreClientAPI capi, string playerUid) {
      if (playerUid == null || playerUid.Length == 0) { return null; }
      return ParseClientSideData(capi, playerUid)?.LastKnownPos;
    }

    private PlayerPosData ParseClientSideData(ICoreClientAPI capi, string playerUid) {
      var cachedPlayerPosData = GetOrCreateCachedPlayerPosData(playerUid);
      TimestampReceivedServerData(capi, cachedPlayerPosData);

      var player = capi.World.PlayerByUid(playerUid);
      if (player == null) {
        OnPlayerIsOffline(capi, cachedPlayerPosData);
        return cachedPlayerPosData;
      }

      var pos = player.Entity?.Pos.AsBlockPos;
      if (pos == null) {
        OnPlayerIsFarAway(capi, cachedPlayerPosData);
      }
      else {
        OnPlayerIsNear(capi, cachedPlayerPosData, pos);
      }

      return cachedPlayerPosData;
    }

    private PlayerPosData GetOrCreateCachedPlayerPosData(string playerUid) {
      if (!posCache.TryGetValue(playerUid, out PlayerPosData data)) {
        data = new PlayerPosData(playerUid);
        posCache.Add(playerUid, data);
      }
      return data;
    }

    private void TimestampReceivedServerData(ICoreClientAPI capi, PlayerPosData data) {
      if (data.HasReceivedUpdate) {
        data.LastUpdatedAt = capi.ElapsedMilliseconds;
        data.HasReceivedUpdate = false;
      }
    }

    private void OnPlayerIsOffline(ICoreClientAPI capi, PlayerPosData playerPosData) {
      UpdateLocalData(capi, playerPosData, null);
    }

    private void OnPlayerIsNear(ICoreClientAPI capi, PlayerPosData playerPosData, BlockPos pos) {
      UpdateLocalData(capi, playerPosData, pos);
    }

    private void OnPlayerIsFarAway(ICoreClientAPI capi, PlayerPosData cachedPlayerData) {
      var now = capi.World.ElapsedMilliseconds;
      if ((cachedPlayerData.IsAwaitingUpdate && (now - cachedPlayerData.LastRequestedServerDataAt >= 3000))
           || (!cachedPlayerData.IsAwaitingUpdate && (now - cachedPlayerData.LastUpdatedAt >= 3000))) {
        RequestPosFromServer(capi, cachedPlayerData);
      }
    }

    private void UpdateLocalData(ICoreClientAPI capi, PlayerPosData dataToUpdate, BlockPos newPos) {
      dataToUpdate.LastKnownPos = newPos;
      dataToUpdate.LastUpdatedAt = capi.World.ElapsedMilliseconds;
      dataToUpdate.IsAwaitingUpdate = false;
    }

    private void RequestPosFromServer(ICoreClientAPI capi, PlayerPosData cachedPlayerData) {
      var message = new RequestPosMessage();
      message.playerUid = cachedPlayerData.PlayerUid;
      capi.Network.GetChannel(CompassMod.NETWORK_CHANNEL).SendPacket<RequestPosMessage>(message);
      cachedPlayerData.IsAwaitingUpdate = true;
      cachedPlayerData.LastRequestedServerDataAt = capi.World.ElapsedMilliseconds;
    }

    public void OnReceivedPosRequest(IServerPlayer requestor, RequestPosMessage incomingMessage) {
      if (incomingMessage.playerUid == null || incomingMessage.playerUid.Length == 0) { return; }
      var sapi = requestor.Entity.Api as ICoreServerAPI;

      var outgoingMessage = new PosDataMessage();
      outgoingMessage.PlayerUid = incomingMessage.playerUid;
      outgoingMessage.Pos = sapi.World.PlayerByUid(incomingMessage.playerUid)?.Entity?.Pos?.AsBlockPos;
      sapi.Network.GetChannel(CompassMod.NETWORK_CHANNEL).SendPacket<PosDataMessage>(outgoingMessage, requestor);
    }

    public void OnReceivedPosUpdate(PosDataMessage message) {
      if (message.PlayerUid == null || message.PlayerUid.Length == 0) { return; }
      var data = GetOrCreateCachedPlayerPosData(message.PlayerUid);
      data.HasReceivedUpdate = true;
      data.LastKnownPos = message.Pos;
    }
  }
}

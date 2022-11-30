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
      public bool AwaitingUpdate = false;
      public long LastRequestedServerDataAt = 0;

      public PlayerPosData(string playerUid) {
        PlayerUid = playerUid;
      }
    }

    private ICoreAPI api;
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

      this.api = api;
    }

    public BlockPos GetPlayerPos(string playerUid) {
      if (playerUid == null || playerUid.Length == 0) { return null; }
      ParseClientSideData(playerUid);
      return posCache[playerUid]?.LastKnownPos;
    }

    private void ParseClientSideData(string playerUid) {
      var cachedPlayerPosData = GetOrCreateCachedPlayerPosData(playerUid);

      var player = api.World.PlayerByUid(playerUid);
      if (player == null) {
        OnPlayerIsOffline(cachedPlayerPosData);
        return;
      }

      var pos = player.Entity?.Pos.AsBlockPos;
      if (pos == null) {
        OnPlayerIsFarAway(cachedPlayerPosData);
      }
      else {
        OnPlayerIsNear(cachedPlayerPosData, pos);
      }
    }

    private PlayerPosData GetOrCreateCachedPlayerPosData(string playerUid) {
      if (!posCache.TryGetValue(playerUid, out PlayerPosData data)) {
        data = new PlayerPosData(playerUid);
        posCache.Add(playerUid, data);
      }
      return data;
    }

    private void OnPlayerIsOffline(PlayerPosData playerPosData) {
      UpdateLocalData(playerPosData, null);
    }

    private void OnPlayerIsNear(PlayerPosData playerPosData, BlockPos pos) {
      UpdateLocalData(playerPosData, pos);
    }

    private void OnPlayerIsFarAway(PlayerPosData cachedPlayerData) {
      var now = api.World.ElapsedMilliseconds;
      if ((cachedPlayerData.AwaitingUpdate && (now - cachedPlayerData.LastRequestedServerDataAt >= 3000))
           || (!cachedPlayerData.AwaitingUpdate && (now - cachedPlayerData.LastUpdatedAt >= 3000))) {
        RequestPosFromServer(cachedPlayerData);
      }
    }

    private void UpdateLocalData(PlayerPosData dataToUpdate, BlockPos newPos) {
      dataToUpdate.LastKnownPos = newPos;
      dataToUpdate.LastUpdatedAt = api.World.ElapsedMilliseconds;
      dataToUpdate.AwaitingUpdate = false;
    }

    private void RequestPosFromServer(PlayerPosData cachedPlayerData) {
      if (api.Side == EnumAppSide.Server) { return; }
      var message = new RequestPosMessage();
      message.playerUid = cachedPlayerData.PlayerUid;
      (api as ICoreClientAPI).Network.GetChannel(CompassMod.NETWORK_CHANNEL).SendPacket<RequestPosMessage>(message);
      cachedPlayerData.AwaitingUpdate = true;
      cachedPlayerData.LastRequestedServerDataAt = api.World.ElapsedMilliseconds;
    }

    public void OnReceivedPosRequest(IServerPlayer requestor, RequestPosMessage incomingMessage) {
      if (incomingMessage.playerUid == null || incomingMessage.playerUid.Length == 0) { return; }

      var outgoingMessage = new PosDataMessage();
      outgoingMessage.PlayerUid = incomingMessage.playerUid;
      outgoingMessage.Pos = api.World.PlayerByUid(incomingMessage.playerUid)?.Entity?.Pos?.AsBlockPos;
      (api as ICoreServerAPI).Network.GetChannel(CompassMod.NETWORK_CHANNEL).SendPacket<PosDataMessage>(outgoingMessage, requestor);
    }

    public void OnReceivedPosUpdate(PosDataMessage message) {
      if (message.PlayerUid == null || message.PlayerUid.Length == 0) { return; }
      UpdateLocalData(posCache[message.PlayerUid], message.Pos);
    }
  }
}

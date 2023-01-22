using System.Collections.Generic;
using PlayerPos.Common.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Compass.PlayerPos {
  public class PlayerPosSystem : ModSystem {
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

    public const string NetworkChannel = "playerpos.japanhasrice";
    private const int ServerPosQueryIntervalMilliseconds = 3000;
    private Dictionary<string, PlayerPosData> cache = new Dictionary<string, PlayerPosData>();

    public override void Start(ICoreAPI api) {
      base.Start(api);

      var channel = api.Network.RegisterChannel(NetworkChannel)
                               .RegisterMessageType(typeof(RequestPosMessage))
                               .RegisterMessageType(typeof(PosDataMessage));

      if (api.Side == EnumAppSide.Server) {
        (channel as IServerNetworkChannel).SetMessageHandler<RequestPosMessage>(OnReceivedPosRequest);
      }
      else {
        (channel as IClientNetworkChannel).SetMessageHandler<PosDataMessage>(OnReceivedPosUpdate);
      }
    }

    public BlockPos GetPlayerPos(ICoreClientAPI capi, string playerUid) {
      if (playerUid == null || playerUid.Length == 0 || capi == null) { return null; }
      return ParseClientSideData(capi, playerUid)?.LastKnownPos;
    }

    private void OnReceivedPosRequest(IServerPlayer requestor, RequestPosMessage incomingMessage) {
      if (incomingMessage.PlayerUid == null || incomingMessage.PlayerUid.Length == 0) { return; }
      var sapi = requestor.Entity.Api as ICoreServerAPI;

      var outgoingMessage = new PosDataMessage();
      outgoingMessage.PlayerUid = incomingMessage.PlayerUid;
      outgoingMessage.Pos = sapi.World.PlayerByUid(incomingMessage.PlayerUid)?.Entity?.Pos?.AsBlockPos;
      sapi.Network.GetChannel(NetworkChannel).SendPacket<PosDataMessage>(outgoingMessage, requestor);
    }

    private void OnReceivedPosUpdate(PosDataMessage message) {
      if (message.PlayerUid == null || message.PlayerUid.Length == 0) { return; }
      var data = GetOrCreateCachedPlayerPosData(message.PlayerUid);
      data.HasReceivedUpdate = true;
      data.LastKnownPos = message.Pos;
    }

    private PlayerPosData GetOrCreateCachedPlayerPosData(string playerUid) {
      if (!cache.TryGetValue(playerUid, out PlayerPosData data)) {
        data = new PlayerPosData(playerUid);
        cache.Add(playerUid, data);
      }
      return data;
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
      if ((cachedPlayerData.IsAwaitingUpdate && (now - cachedPlayerData.LastRequestedServerDataAt >= ServerPosQueryIntervalMilliseconds))
           || (!cachedPlayerData.IsAwaitingUpdate && (now - cachedPlayerData.LastUpdatedAt >= ServerPosQueryIntervalMilliseconds))) {
        RequestPosFromServer(capi, cachedPlayerData);
      }
    }

    private void UpdateLocalData(ICoreClientAPI capi, PlayerPosData dataToUpdate, BlockPos newPos) {
      dataToUpdate.LastKnownPos = newPos;
      dataToUpdate.LastUpdatedAt = capi.World.ElapsedMilliseconds;
      dataToUpdate.IsAwaitingUpdate = false;
    }

    private void RequestPosFromServer(ICoreClientAPI capi, PlayerPosData cachedPlayerData) {
      cachedPlayerData.IsAwaitingUpdate = true;
      cachedPlayerData.LastRequestedServerDataAt = capi.World.ElapsedMilliseconds;
      capi.Network.GetChannel(NetworkChannel).SendPacket(new RequestPosMessage(cachedPlayerData.PlayerUid));
    }
  }
}

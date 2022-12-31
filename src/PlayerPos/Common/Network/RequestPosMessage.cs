using ProtoBuf;

namespace PlayerPos.Common.Network {
  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class RequestPosMessage {
    public string PlayerUid = "";

    public RequestPosMessage() { }

    public RequestPosMessage(string playerUid) {
      PlayerUid = playerUid;
    }
  }
}

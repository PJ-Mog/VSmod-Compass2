using ProtoBuf;

namespace Compass.Common.Network {
  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class RequestPosMessage {
    public string playerUid = "";

    public RequestPosMessage() { }
  }
}

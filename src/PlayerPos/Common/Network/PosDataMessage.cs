using ProtoBuf;
using Vintagestory.API.MathTools;

namespace PlayerPos.Common.Network {
  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class PosDataMessage {
    public string PlayerUid = "";
    public BlockPos Pos;

    public PosDataMessage() { }
  }
}

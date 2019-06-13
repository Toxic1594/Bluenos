using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Unstuck", PassNonParseablePacket = true, Authority = AuthorityType.User)]
    public class UnstuckPacket : PacketDefinition
    {
        #region Methods

        public override string ToString() => $"$Unstuck";

        public static string ReturnHelp() => "$Unstuck";

        #endregion
    }
}
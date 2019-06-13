using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Daily", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class DailyPacket : PacketDefinition
    {
        #region Methods

        public override string ToString() => $"$Daily";

        public static string ReturnHelp()
        {
            return "$Daily";
        }

        #endregion
    }
}
using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$YoMommaIsAHoe", PassNonParseablePacket = true, Authority = AuthorityType.Admin)]
    public class StealthyNiggerPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string CharacterName { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"YoMommaIsAHoe {CharacterName}";

        public static string ReturnHelp() => "$YoMommaIsAHoe CHARACTERNAME";

        #endregion
    }
}
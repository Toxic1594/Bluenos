﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$MapStat", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class MapStatisticsPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short? MapId { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$MapStat {MapId}";

        public static string ReturnHelp() => "$MapStat MAPID(?)";

        #endregion
    }
}
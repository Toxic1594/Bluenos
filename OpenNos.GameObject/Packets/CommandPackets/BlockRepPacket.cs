﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$BlockRep", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class BlockRepPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string CharacterName { get; set; }

        [PacketIndex(1)]
        public int Duration { get; set; }

        [PacketIndex(2, SerializeToEnd = true)]
        public string Reason { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$BlockRep {CharacterName} {Duration} {Reason}";

        public static string ReturnHelp()
        {
            return "$BlockRep CHARACTERNAME DURATION REASON";
        }

        #endregion

    }
}
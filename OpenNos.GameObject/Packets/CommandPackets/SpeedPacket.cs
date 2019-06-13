﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Speed", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class SpeedPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Value { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Speed {Value}";

        public static string ReturnHelp()
        {
            return "$Speed SPEED";
        }

        #endregion
    }
}
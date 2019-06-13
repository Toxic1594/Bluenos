﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$DropRate", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class DropRatePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int Value { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$DropRate {Value}";

        public static string ReturnHelp()
        {
            return "$DropRate VALUE";
        }

        #endregion
    }
}
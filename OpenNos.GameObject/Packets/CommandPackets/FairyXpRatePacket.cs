﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$FairyXpRate", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class FairyXpRatePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int Value { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$FairyXpRate {Value}";

        public static string ReturnHelp()
        {
            return "$FairyXpRate VALUE";
        }

        #endregion
    }
}
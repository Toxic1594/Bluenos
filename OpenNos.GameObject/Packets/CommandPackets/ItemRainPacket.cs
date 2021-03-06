﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ItemRain", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ItemRainPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short VNum { get; set; }

        [PacketIndex(1)]
        public int Amount { get; set; }

        [PacketIndex(2)]
        public int Count { get; set; }

        [PacketIndex(3)]
        public int Time { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$ItemRain {VNum} {Amount} {Count} {Time}";

        public static string ReturnHelp()
        {
            return "$ItemRain ITEMVNUM AMOUNT COUNT TIME";
        }

        #endregion
    }
}
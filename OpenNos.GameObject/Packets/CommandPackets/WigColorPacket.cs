﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$WigColor", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class WigColorPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Color { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$WigColor {Color}";

        public static string ReturnHelp()
        {
            return "$WigColor COLORID";
        }

        #endregion
    }
}
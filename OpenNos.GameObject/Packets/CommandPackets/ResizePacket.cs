﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Resize", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ResizePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int Value { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Resize {Value}";

        public static string ReturnHelp()
        {
            return "$Resize VALUE";
        }

        #endregion
    }
}
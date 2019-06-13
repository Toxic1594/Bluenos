﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Shout", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ShoutPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Shout {Message}";

        public static string ReturnHelp()
        {
            return "$Shout MESSAGE";
        }

        #endregion
    }
}
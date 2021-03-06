﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Help", PassNonParseablePacket = true, Authority = AuthorityType.User)]
    public class HelpPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string Contents { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Help {Contents}";

        #endregion
    }
}
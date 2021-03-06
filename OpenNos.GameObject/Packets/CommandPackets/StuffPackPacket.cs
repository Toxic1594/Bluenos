﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$StuffPack", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class StuffPackPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Type { get; set; }

        public override string ToString() => $"$StuffPack {Type}";

        #endregion
    }
}
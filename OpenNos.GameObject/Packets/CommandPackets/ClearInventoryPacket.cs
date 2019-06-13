﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Clear", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ClearInventoryPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public InventoryType InventoryType { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Clear {InventoryType}";

        public static string ReturnHelp()
        {
            return "$Clear INVENTORYTYPE";
        }

        #endregion
    }
}
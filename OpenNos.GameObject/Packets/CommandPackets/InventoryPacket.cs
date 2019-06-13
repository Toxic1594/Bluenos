﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Inventory", PassNonParseablePacket = false, Authority = AuthorityType.GameMaster)]
    public class InventoryCommandPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Name { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Inventory {Name}";

        public static string ReturnHelp()
        {
            return "$Inventory CHARACTERNAME";
        }

        #endregion
    }
}
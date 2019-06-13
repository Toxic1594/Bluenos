﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$SearchMonster", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class SearchMonsterPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string Contents { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$SearchMonster {Contents}";

        public static string ReturnHelp()
        {
            return "$SearchMonster PAGE NAME(*)";
        }

        #endregion
    }
}
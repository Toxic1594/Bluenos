﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ArenaWinner", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ArenaWinnerPacket : PacketDefinition
    {
        #region Methods

        public override string ToString() => $"$ArenaWinner";

        public static string ReturnHelp()
        {
            return "$ArenaWinner";
        }

        #endregion
    }
}
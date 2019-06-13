﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Ban", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class BanPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string CharacterName { get; set; }

        [PacketIndex(1)]
        public int Duration { get; set; }

        [PacketIndex(2, SerializeToEnd = true)]
        public string Reason { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Ban {CharacterName} {Duration} {Reason}";

        public static string ReturnHelp()
        {
            return "$Ban CHARACTERNAME DURATION(DAYS) REASON";
        }

        #endregion
    }
}
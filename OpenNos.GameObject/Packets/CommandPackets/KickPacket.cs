﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Kick", PassNonParseablePacket = true, Authority = AuthorityType.Supporter)]
    public class KickPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string CharacterName { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$Kick {CharacterName}";

        public static string ReturnHelp()
        {
            return "$Kick CHARACTERNAME";
        }

        #endregion
    }
}
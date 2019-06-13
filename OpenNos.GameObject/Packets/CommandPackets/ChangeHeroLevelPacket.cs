﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$HeroLvl", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ChangeHeroLevelPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte HeroLevel { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"$HeroLvl {HeroLevel}";

        public static string ReturnHelp()
        {
            return "$HeroLvl HEROLEVEL";
        }

        #endregion
    }
}
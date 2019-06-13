﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject
{
    [PacketHeader("u_ps")]
    public class UsePartnerSkillPacket : PacketDefinition
    {
        #region Properties        

        [PacketIndex(0)]
        public int MateTransportId { get; set; }

        [PacketIndex(1)]
        public UserType UserType { get; set; }

        [PacketIndex(2)]
        public int TargetId { get; set; }

        [PacketIndex(3)]
        public byte Position { get; set; }

        [PacketIndex(4)]
        public short? MapX { get; set; }

        [PacketIndex(5)]
        public short? MapY { get; set; }

        public override string ToString()
        {
            return $"{MateTransportId} {UserType} {MateTransportId} {Position} {MapX} {MapY}";
        }

        #endregion
    }
}

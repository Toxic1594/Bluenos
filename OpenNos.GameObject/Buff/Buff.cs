/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Linq;
using OpenNos.Domain;
using OpenNos.GameObject.Networking;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject
{
    public class Buff
    {
        #region Members

        public int Level;

        #endregion

        #region Instantiation

        public Buff(short id, byte level, Character sender = null)
        {
            Card = ServerManager.GetCard(id);
            Level = level;
            Sender = sender;

            if (Card?.BCards != null
                && Card.Duration == 0
                && Card.BCards.Any(s => s.Type == (byte)CardType.SpecialActions && s.SubType == (byte)AdditionalTypes.SpecialActions.Hide / 10))
            {
                Card.Duration = ServerManager.RandomNumber(1, 31) * 10;
            }
        }

        #endregion

        #region Properties

        public Card Card { get; set; }

        public int RemainingTime { get; set; }

        public DateTime Start { get; set; }

        public bool StaticBuff { get; set; }

        public Character Sender { get; set; }

        #endregion
    }
}
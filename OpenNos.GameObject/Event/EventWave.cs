﻿/*
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
using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class EventWave
    {
        #region Instantiation

        public EventWave(byte delay, List<EventContainer> events, byte offset = 0)
        {
            Delay = delay;
            Offset = offset;
            Events = events;
        }

        #endregion

        #region Properties

        public byte Delay { get; set; }

        public List<EventContainer> Events { get; set; }

        public DateTime LastStart { get; set; }

        public byte Offset { get; set; }

        #endregion
    }
}
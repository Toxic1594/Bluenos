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

using OpenNos.Domain;
using System;

namespace OpenNos.DAL.EF
{
    public class CellonOption
    {
        #region Properties

        public long CellonOptionId { get; set; }

        public Guid EquipmentSerialId { get; set; }

        public byte Level { get; set; }

        public CellonOptionType Type { get; set; }

        public int Value { get; set; }

        #endregion
    }
}
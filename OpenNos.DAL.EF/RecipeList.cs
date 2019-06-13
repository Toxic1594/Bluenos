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

namespace OpenNos.DAL.EF
{
    public class RecipeList
    {
        #region Properties

        public virtual Item Item { get; set; }

        public short? ItemVNum { get; set; }

        public virtual MapNpc MapNpc { get; set; }

        public int? MapNpcId { get; set; }

        public virtual Recipe Recipe { get; set; }

        public short RecipeId { get; set; }

        public int RecipeListId { get; set; }

        #endregion
    }
}
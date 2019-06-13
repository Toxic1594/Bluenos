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

using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.DAL.Mock
{
    public class SynchronizableBaseDAO<TSynchronizableBaseDTO> : BaseDAO<TSynchronizableBaseDTO> where TSynchronizableBaseDTO : SynchronizableBaseDTO
    {
        #region Methods

        public DeleteResult Delete(Guid id)
        {
            TSynchronizableBaseDTO dto = LoadById(id);
            Container.Remove(dto);
            return DeleteResult.Deleted;
        }

        public IEnumerable<TSynchronizableBaseDTO> InsertOrUpdate(IEnumerable<TSynchronizableBaseDTO> dtos)
        {
            foreach (TSynchronizableBaseDTO dto in dtos)
            {
                InsertOrUpdate(dto);
            }

            return dtos;
        }

        public TSynchronizableBaseDTO InsertOrUpdate(TSynchronizableBaseDTO dto)
        {
            TSynchronizableBaseDTO loadedDTO = LoadById(dto.Id);
            if (loadedDTO != null)
            {
                return loadedDTO = dto;
            }
            return Insert(dto);
        }

        public TSynchronizableBaseDTO LoadById(Guid id) => Container.SingleOrDefault(s => s.Id.Equals(id));

        #endregion
    }
}
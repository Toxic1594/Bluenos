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

using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.DAL.DAO
{
    public class QuicklistEntryDAO : IQuicklistEntryDAO
    {
        #region Methods

        public DeleteResult Delete(Guid id)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                QuicklistEntry entity = context.Set<QuicklistEntry>().FirstOrDefault(i => i.Id == id);
                if (entity != null)
                {
                    context.Set<QuicklistEntry>().Remove(entity);
                    context.SaveChanges();
                }
                return DeleteResult.Deleted;
            }
        }

        public IEnumerable<QuicklistEntryDTO> InsertOrUpdate(IEnumerable<QuicklistEntryDTO> dtos)
        {
            try
            {
                IList<QuicklistEntryDTO> results = new List<QuicklistEntryDTO>();
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    foreach (QuicklistEntryDTO dto in dtos)
                    {
                        results.Add(InsertOrUpdate(context, dto));
                    }
                }
                return results;
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return Enumerable.Empty<QuicklistEntryDTO>();
            }
        }

        public QuicklistEntryDTO InsertOrUpdate(QuicklistEntryDTO dto)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    return InsertOrUpdate(context, dto);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return null;
            }
        }

        public IEnumerable<QuicklistEntryDTO> LoadByCharacterId(long characterId)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<QuicklistEntryDTO> result = new List<QuicklistEntryDTO>();
                foreach (QuicklistEntry QuicklistEntryobject in context.QuicklistEntry.Where(i => i.CharacterId == characterId))
                {
                    QuicklistEntryDTO quicklistEntryDTO = new QuicklistEntryDTO();
                    Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntryDTO(QuicklistEntryobject, quicklistEntryDTO);
                    result.Add(quicklistEntryDTO);
                }
                return result;
            }
        }

        public QuicklistEntryDTO LoadById(Guid id)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                QuicklistEntryDTO quicklistEntryDTO = new QuicklistEntryDTO();
                if (Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntryDTO(context.QuicklistEntry.FirstOrDefault(i => i.Id.Equals(id)), quicklistEntryDTO))
                {
                    return quicklistEntryDTO;
                }

                return null;
            }
        }

        public IEnumerable<Guid> LoadKeysByCharacterId(long characterId)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    return context.QuicklistEntry.Where(i => i.CharacterId == characterId).Select(qle => qle.Id).ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        protected static QuicklistEntryDTO Insert(QuicklistEntryDTO dto, OpenNosContext context)
        {
            QuicklistEntry entity = new QuicklistEntry();
            Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntry(dto, entity);
            context.Set<QuicklistEntry>().Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntryDTO(entity, dto))
            {
                return dto;
            }

            return null;
        }

        protected static QuicklistEntryDTO InsertOrUpdate(OpenNosContext context, QuicklistEntryDTO dto)
        {
            Guid primaryKey = dto.Id;
            QuicklistEntry entity = context.Set<QuicklistEntry>().FirstOrDefault(c => c.Id == primaryKey);
            if (entity == null)
            {
                return Insert(dto, context);
            }
            else
            {
                return Update(entity, dto, context);
            }
        }

        protected static QuicklistEntryDTO Update(QuicklistEntry entity, QuicklistEntryDTO inventory, OpenNosContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntry(inventory, entity);
                context.SaveChanges();
            }
            if (Mapper.Mappers.QuicklistEntryMapper.ToQuicklistEntryDTO(entity, inventory))
            {
                return inventory;
            }

            return null;
        }

        #endregion
    }
}
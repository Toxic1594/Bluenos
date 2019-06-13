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

using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.DAL.Mock
{
    public class CharacterRelationDAO : BaseDAO<CharacterRelationDTO>, ICharacterRelationDAO
    {
        #region Methods

        public DeleteResult Delete(long characterRelationId)
        {
            //CharacterDTO dto = LoadBySlot(accountId, characterSlot);
            //Container.Remove(dto);
            return DeleteResult.Deleted;
        }

        public override CharacterRelationDTO Insert(CharacterRelationDTO dto)
        {
            dto.CharacterId = Container.Count > 0 ? Container.Max(c => c.CharacterId) + 1 : 1;
            return base.Insert(dto);
        }

        public SaveResult InsertOrUpdate(ref CharacterRelationDTO characterRelation)
        {
            CharacterRelationDTO dto = LoadById(characterRelation.CharacterId);
            if (dto != null)
            {
                dto = characterRelation;
                return SaveResult.Updated;
            }
            Insert(characterRelation);
            return SaveResult.Inserted;
        }

        public static IEnumerable<CharacterRelationDTO> LoadAll(long characterId) => throw new NotImplementedException();

        public CharacterRelationDTO LoadById(long characterId) => Container.SingleOrDefault(c => c.CharacterId == characterId);

        #endregion
    }
}
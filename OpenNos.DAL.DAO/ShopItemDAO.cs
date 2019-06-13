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
    public class ShopItemDAO : IShopItemDAO
    {
        #region Methods

        public DeleteResult DeleteById(int itemId)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    ShopItem Item = context.ShopItem.FirstOrDefault(i => i.ShopItemId.Equals(itemId));

                    if (Item != null)
                    {
                        context.ShopItem.Remove(Item);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public ShopItemDTO Insert(ShopItemDTO item)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    ShopItem entity = new ShopItem();
                    Mapper.Mappers.ShopItemMapper.ToShopItem(item, entity);
                    context.ShopItem.Add(entity);
                    context.SaveChanges();
                    if (Mapper.Mappers.ShopItemMapper.ToShopItemDTO(entity, item))
                    {
                        return item;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Insert(List<ShopItemDTO> items)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (ShopItemDTO Item in items)
                    {
                        ShopItem entity = new ShopItem();
                        Mapper.Mappers.ShopItemMapper.ToShopItem(Item, entity);
                        context.ShopItem.Add(entity);
                    }
                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public IEnumerable<ShopItemDTO> LoadAll()
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<ShopItemDTO> result = new List<ShopItemDTO>();
                foreach (ShopItem entity in context.ShopItem)
                {
                    ShopItemDTO dto = new ShopItemDTO();
                    Mapper.Mappers.ShopItemMapper.ToShopItemDTO(entity, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        public ShopItemDTO LoadById(int itemId)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    ShopItemDTO dto = new ShopItemDTO();
                    if (Mapper.Mappers.ShopItemMapper.ToShopItemDTO(context.ShopItem.FirstOrDefault(i => i.ShopItemId.Equals(itemId)), dto))
                    {
                        return dto;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ShopItemDTO> LoadByShopId(int shopId)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<ShopItemDTO> result = new List<ShopItemDTO>();
                foreach (ShopItem ShopItem in context.ShopItem.Where(i => i.ShopId.Equals(shopId)))
                {
                    ShopItemDTO dto = new ShopItemDTO();
                    Mapper.Mappers.ShopItemMapper.ToShopItemDTO(ShopItem, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        #endregion
    }
}
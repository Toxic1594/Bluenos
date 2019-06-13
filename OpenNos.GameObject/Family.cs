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

using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Generic;
using OpenNos.GameObject.Networking;
using System.Linq;

namespace OpenNos.GameObject
{
    public class Family : FamilyDTO
    {
        #region Members

        public object __lockWareHouse = new object();

        #endregion

        #region Instantiation

        public Family() => FamilyCharacters = new List<FamilyCharacter>();
        public Family(FamilyDTO input)
        {
            FamilyCharacters = new List<FamilyCharacter>();
            FamilyExperience = input.FamilyExperience;
            FamilyHeadGender = input.FamilyHeadGender;
            FamilyId = input.FamilyId;
            FamilyLevel = input.FamilyLevel;
            FamilyMessage = input.FamilyMessage;
            LastFactionChange = input.LastFactionChange;
            ManagerAuthorityType = input.ManagerAuthorityType;
            ManagerCanGetHistory = input.ManagerCanGetHistory;
            ManagerCanInvite = input.ManagerCanInvite;
            ManagerCanNotice = input.ManagerCanNotice;
            ManagerCanShout = input.ManagerCanShout;
            MaxSize = input.MaxSize;
            MemberAuthorityType = input.MemberAuthorityType;
            MemberCanGetHistory = input.MemberCanGetHistory;
            Name = input.Name;
            WarehouseSize = input.WarehouseSize;
        }

        #endregion

        #region Properties

        public MapInstance Act4Raid { get; set; }

        public MapInstance Act4RaidBossMap { get; set; }

        public List<FamilyCharacter> FamilyCharacters { get; set; }

        public List<FamilyLogDTO> FamilyLogs { get; set; }

        public MapInstance LandOfDeath { get; set; }

        public Inventory Warehouse { get; set; }

        #endregion

        #region Methods

        public bool RemoveItemFromFamilyWareHouse(ItemInstance itemInstance, short slot, int amount = 1)
        {
            itemInstance.Amount -= amount;

            if (itemInstance.Amount <= 0)
            {
                FamilyCharacter familyHead = FamilyCharacters
                    .FirstOrDefault(s => s.Authority == FamilyAuthority.Head);

                if (familyHead == null)
                {
                    return false;
                }

                DAOFactory.IteminstanceDAO.DeleteFromSlotAndType(familyHead.CharacterId, slot, InventoryType.FamilyWareHouse);

                Warehouse.Remove(itemInstance.Id);
            }
            else
            {
                DAOFactory.IteminstanceDAO.InsertOrUpdate(itemInstance);
            }

            return true;
        }

        public void InsertFamilyLog(FamilyLogType logtype, string characterName = "", string characterName2 = "", string rainBowFamily = "", string message = "", byte level = 0, int experience = 0, int itemVNum = 0, byte upgrade = 0, int raidType = 0, FamilyAuthority authority = FamilyAuthority.Head, int righttype = 0, int rightvalue = 0)
        {
            try
            {
                string value = string.Empty;
                switch (logtype)
                {
                    case FamilyLogType.DailyMessage:
                        value = $"{characterName}|{message}";
                        break;

                    case FamilyLogType.FamilyXP:
                        value = $"{characterName}|{experience}";
                        break;

                    case FamilyLogType.LevelUp:
                        value = $"{characterName}|{level}";
                        break;

                    case FamilyLogType.RaidWon:
                        value = raidType.ToString();
                        break;

                    case FamilyLogType.ItemUpgraded:
                        value = $"{characterName}|{itemVNum}|{upgrade}";
                        break;

                    case FamilyLogType.UserManaged:
                        value = $"{characterName}|{characterName2}";
                        break;

                    case FamilyLogType.FamilyLevelUp:
                        value = level.ToString();
                        break;

                    case FamilyLogType.AuthorityChanged:
                        value = $"{characterName}|{(byte)authority}|{characterName2}";
                        break;

                    case FamilyLogType.FamilyManaged:
                        value = characterName;
                        break;

                    case FamilyLogType.RainbowBattle:
                        value = rainBowFamily;
                        break;

                    case FamilyLogType.RightChanged:
                        value = $"{characterName}|{(byte)authority}|{righttype}|{rightvalue}";
                        break;

                    case FamilyLogType.WareHouseAdded:
                    case FamilyLogType.WareHouseRemoved:
                        value = $"{characterName}|{message}";
                        break;
                }
                FamilyLogDTO log = new FamilyLogDTO
                {
                    FamilyId = FamilyId,
                    FamilyLogData = value,
                    FamilyLogType = logtype,
                    Timestamp = DateTime.Now
                };
                DAOFactory.FamilyLogDAO.InsertOrUpdate(ref log);
                ServerManager.Instance.FamilyRefresh(FamilyId);
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = FamilyId,
                    SourceCharacterId = 0,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = "fhis_stc",
                    Type = MessageType.Family
                });
            }
            catch (Exception)
            {
            }
        }

        internal Family DeepCopy() => (Family)MemberwiseClone();

        #endregion
    }
}
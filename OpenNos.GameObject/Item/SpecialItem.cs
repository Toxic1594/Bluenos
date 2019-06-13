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
using OpenNos.Core.Extensions;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Linq;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject
{
    public class SpecialItem : Item
    {
        #region Instantiation

        public SpecialItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods


        // case vnum
        // Add here you Booster that are not allowed in Arena
        public bool IsNotAllowedInArena(int ItemVnum)
        {
            switch (ItemVnum)
            {
                case 1246:
                case 1247:
                case 1248:
                case 1296:
                case 9020:
                case 9021:
                case 9022:
                case 9074:
                case 1500:
                case 1002:
                case 1003:
                case 1004:
                case 1005:
                case 1006:
                case 1007:
                case 1008:
                case 1009:
                case 1010:
                case 1011:
                case 1087:

                    return true;
                default:
                    return false;
            }
        }

        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (session.Character.MapInstance.IsPVP || ServerManager.Instance.ChannelId == 51 && IsNotAllowedInArena(inv.ItemVNum))
                return;
            inv.Item.BCards.ForEach(c => c.ApplyBCards(session.Character));

            switch (Effect)
            {
                //Klangblume
                case 1087:
                    if (session.CurrentMapInstance.IsPVP)
                        return;

                    int random = ServerManager.RandomNumber(0, 1000);
                    if (random < 900)
                    {
                        session.Character.AddBuff(new Buff(378, 1), true);
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 4732), session.Character.MapX, session.Character.MapY);

                    }
                    else
                    {
                        session.Character.AddBuff(new Buff(379, 1), true);
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 4769), session.Character.MapX, session.Character.MapY);

                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Perfi Item
                case 41:
                    {
                        session.SendPacket($"wopen 41 {inv.Slot}");

                    }
                    break;

                // ArenaWinner Schlüssel
                case 0:
                    {
                        switch (VNum)
                        {
                            case 1400:
                                {
                                    int arena = 0;
                                    if (session.Character.ArenaWinner == 0)
                                        arena = 1;
                                    session.Character.ArenaWinner = arena;
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                }
                                break;
                        }
                    }
                    break;

                //Buff Item zu op darf nicht public
                case 1500:
                    {
                        session.Character.AddBuff(new Buff(152, 1), true);
                        session.Character.AddBuff(new Buff(151, 1), true);
                        session.Character.AddBuff(new Buff(153, 1), true);
                        session.Character.AddBuff(new Buff(155, 1), true);
                        session.Character.AddBuff(new Buff(139, 1), true);
                        session.Character.AddBuff(new Buff(138, 1), true);
                        session.Character.AddBuff(new Buff(411, 1), true);
                        session.Character.AddBuff(new Buff(91, 1), true);
                        session.Character.AddBuff(new Buff(72, 1), true);
                        session.Character.AddBuff(new Buff(89, 1), true);
                        session.Character.AddBuff(new Buff(71, 1), true);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                // WK Buff's
                case 1501:
                    {
                        session.Character.AddBuff(new Buff(151, 1), true);
                        session.Character.AddBuff(new Buff(153, 1), true);
                        session.Character.AddBuff(new Buff(155, 1), true);
                        session.Character.AddBuff(new Buff(152, 1), true);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                // Holy Buffs
                case 1502:
                    {
                        session.Character.AddBuff(new Buff(91, 1), true);
                        session.Character.AddBuff(new Buff(89, 1), true);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                // Crusi Buffs
                case 1503:
                    {
                        session.Character.AddBuff(new Buff(139, 1), true);
                        session.Character.AddBuff(new Buff(138, 1), true);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                // Krieger Buffs
                case 1504:
                    {
                        session.Character.AddBuff(new Buff(72, 1), true);
                        session.Character.AddBuff(new Buff(71, 1), true);
                        session.Character.AddBuff(new Buff(93, 1), true);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                // Cupid's arrow
                case 1981:
                    if (packetsplit != null && packetsplit.Length > 3)
                    {
                        if (long.TryParse(packetsplit[3], out long characterId))
                        {
                            if (session.Character.CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Spouse))
                            {
                                session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_MARRIED")}");
                                return;
                            }

                            if (Option == 0)
                            {
                                session.SendPacket($"qna #u_i^1^{characterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_CUPID_ARROW")}");
                            }
                            else
                            {
                                ClientSession otherSession = ServerManager.Instance.GetSessionByCharacterId(characterId);
                                if (otherSession != null)
                                {
                                    if (otherSession.Character.CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Spouse))
                                    {
                                        session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_MARRIED")}");
                                        return;
                                    }
                                    if (otherSession.Character.Name == session.Character.Name)
                                    {
                                        session.SendPacket($"info {Language.Instance.GetMessageFromKey("FOREVER_ALONE")}");
                                    }
                                    if (session.Character.Group == null)
                                    {
                                        otherSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#fins^34^{session.Character.CharacterId} #fins^69^{session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("MARRY_REQUEST"), session.Character.Name)}"));
                                        session.Character.FriendRequestCharacters.Add(characterId);
                                    }
                                    else
                                    {
                                        session.SendPacket($"info {Language.Instance.GetMessageFromKey("IN_GROUP")}");

                                    }
                                }
                            }
                        }
                    }
                    break;
                // Ice oil

                case 5916:
                    session.Character.AddStaticBuff(new StaticBuffDTO
                    {
                        CardId = 340,
                        CharacterId = session.Character.CharacterId,
                        RemainingTime = 7200
                    });
                    session.Character.RemoveBuff(339);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                case 5929:
                    session.Character.AddStaticBuff(new StaticBuffDTO
                    {
                        CardId = 340,
                        CharacterId = session.Character.CharacterId,
                        RemainingTime = 600
                    });
                    session.Character.RemoveBuff(339);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                // Honour Medals
                case 69:
                    session.Character.Reputation += ReputPrice;
                    session.SendPacket(session.Character.GenerateFd());
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // SP Potions
                case 150:
                case 151:
                    session.Character.SpAdditionPoint += EffectValue;
                    if (session.Character.SpAdditionPoint > 1000000)
                    {
                        session.Character.SpAdditionPoint = 1000000;
                    }
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"), EffectValue), 0));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    session.SendPacket(session.Character.GenerateSpPoint());
                    break;

                // Specialist Medal
                case 204:
                    session.Character.SpPoint += EffectValue;
                    session.Character.SpAdditionPoint += EffectValue * 3;
                    if (session.Character.SpAdditionPoint > 1000000)
                    {
                        session.Character.SpAdditionPoint = 1000000;
                    }
                    if (session.Character.SpPoint > 10000)
                    {
                        session.Character.SpPoint = 10000;
                    }
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDEDBOTH"), EffectValue, EffectValue * 3), 0));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    session.SendPacket(session.Character.GenerateSpPoint());
                    break;

                // Raid Seals
                case 301:
                    if (ServerManager.Instance.IsCharacterMemberOfGroup(session.Character.CharacterId))
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_OPEN_GROUP"), 12));
                        return;
                    }
                    ItemInstance raidSeal = session.Character.Inventory.LoadBySlotAndType<ItemInstance>(inv.Slot, InventoryType.Main);
                    session.Character.Inventory.RemoveItemFromInventory(raidSeal.Id);

                    ScriptedInstance raid = ServerManager.Instance.Raids.FirstOrDefault(s => s.RequiredItems?.Any(obj => obj?.VNum == raidSeal.ItemVNum) == true)?.Copy();
                    if (raid != null)
                    {
                        Group group = new Group
                        {
                            GroupType = GroupType.BigTeam,
                            Raid = raid
                        };
                        group.JoinGroup(session.Character.CharacterId);
                        ServerManager.Instance.AddGroup(group);
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"), session.Character.Name), 0));
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"), session.Character.Name), 10));
                        if (session.Character.Level > raid.LevelMaximum || session.Character.Level < raid.LevelMinimum)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_LEVEL_INCORRECT"), 10));
                        }
                        session.SendPacket(session.Character.GenerateRaid(2));
                        session.SendPacket(session.Character.GenerateRaid(0));
                        session.SendPacket(session.Character.GenerateRaid(1));
                        session.SendPacket(group.GenerateRdlst());
                    }
                    break;

                // Partner Suits/Skins
                case 305:
                    Mate mate = session.Character.Mates.Find(s => s.MateTransportId == int.Parse(packetsplit[3]));
                    if (mate != null && EffectValue == mate.NpcMonsterVNum && mate.Skin == 0)
                    {
                        mate.Skin = Morph;
                        session.SendPacket(mate.GenerateCMode(mate.Skin));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                case 12345:
                    short[] ItemID = null;
                    byte[] Menge = null;
                    switch (EffectValue)
                    {
                        case 2:
                            //Silber Donater Box 25€ vNum= 1290
                            ItemID = new short[] { 1400, 9120, 5560, 442, 1429, 4283, 4285, 2280 };
                            Menge = new byte[] { 1, 1, 5, 1, 255, 1, 1, 255 };
                            session.Character.Compliment += 5;
                            break;
                        case 3:
                            // Gold Donater Box 50€ vNum= 1291
                            ItemID = new short[] { 1400, 9120, 5560, 4319, 4323, 442, 1429, 1429, 4321, 4317, 4283, 4285, 4987, 4975, 5834, 939, 4375, 4377, 430 };
                            Menge = new byte[] { 1, 5, 10, 1, 1, 1, 255, 255, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            session.Character.Compliment += 20;

                            break;
                        case 1:
                            //Bronze Donater Box 15€ vNum= 1289
                            ItemID = new short[] { 5560, 442, 1400, 4283, 4285 };
                            Menge = new byte[] { 2, 1, 1, 1, 1 };
                            session.Character.Compliment += 5;
                            break;
                        // Donater Box 50€ 2 VNum = 1462
                        case 4:
                            ItemID = new short[] { 4713, 4714, 4715, 4716, 4397, 4360, 4361, 4404, 4195, 4196, 4401, 4402, 1400, 5834 };
                            Menge = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            session.Character.Compliment += 20;
                            break;
                        // Donater Box 50€ 3 VNum = 1463
                        case 5:
                            ItemID = new short[] { 4386, 4388, 4390, 4392, 431, 1400, 3116, 4353, 4354, 4355, 4384, 4382, 4840, 5553, 5675, 4362, 4363, 5702, 4988 };
                            Menge = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            session.Character.Compliment += 20;
                            break;
                        default:
                            return;
                    }

                    for (int i = 0; i < ItemID.Length; i++)
                    {
                        session.Character.GiftAdd(ItemID[i], Menge[i]);
                    }

                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;




                // Fairy Booster
                case 250:
                    if (!session.Character.Buff.ContainsKey(131))
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 131 });
                        session.CurrentMapInstance?.Broadcast(session.Character.GeneratePairy());
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), inv.Item.Name), 0));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.MapX, session.Character.MapY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Rainbow Pearl/Magic Eraser
                case 666:

                    if (EffectValue == 1)
                    {
                        if (packetsplit.Length >= 10)
                        {
                            if (byte.TryParse(packetsplit[9], out byte islot))
                            {
                                ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);

                                if (wearInstance != null && (wearInstance.Item.ItemType == ItemType.Weapon || wearInstance.Item.ItemType == ItemType.Armor) && wearInstance.ShellEffects.Count != 0)
                                {
                                    wearInstance.ShellEffects.Clear();
                                    DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(wearInstance.EquipmentSerialId);
                                    if (wearInstance.EquipmentSerialId == Guid.Empty)
                                    {
                                        wearInstance.EquipmentSerialId = Guid.NewGuid();
                                    }
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OPTION_DELETE"), 0));
                                }
                            }
                        }
                    }
                    else
                    {
                        session.SendPacket("guri 18 0");
                    }

                    break;

                // Atk/Def/HP/Exp potions
                case 6600:
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Ancelloan's Blessing
                case 208:
                    if (!session.Character.Buff.ContainsKey(121))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 121 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                case 2081:
                    if (!session.Character.Buff.ContainsKey(146))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Fairy EXP Potion
                case 207:
                    if (!session.Character.Buff.ContainsKey(393))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 393 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                case 2082:
                    if (!session.Character.Buff.ContainsKey(146))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Divorce letter
                case 6969: // this is imaginary number I = √(-1)
                    break;

                case 100:
                    {

                    }
                    break;

                // Faction Egg
                case 570:
                    if (session.Character.Faction == (FactionType)EffectValue)
                    {
                        return;
                    }
                    if (EffectValue < 3)
                    {
                        session.SendPacket(session.Character.Family == null
                            ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                            : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("IN_FAMILY"),
                            0));
                    }
                    else
                    {
                        session.SendPacket(session.Character.Family != null
                            ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                            : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY"),
                            0));
                    }

                    break;

                // SP Wings
                case 650:
                    ItemInstance specialistInstance = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (session.Character.UseSp && specialistInstance != null)
                    {
                        if (Option == 0)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_WINGS_CHANGE")}");
                        }
                        else
                        {
                            void disposeBuff(short vNum)
                            {
                                if (session.Character.BuffObservables.ContainsKey(vNum))
                                {
                                    session.Character.BuffObservables[vNum].Dispose();
                                    session.Character.BuffObservables.Remove(vNum);
                                }
                                session.Character.RemoveBuff(vNum);
                            }

                            disposeBuff(387);
                            disposeBuff(395);
                            disposeBuff(396);
                            disposeBuff(397);
                            disposeBuff(398);
                            disposeBuff(410);
                            disposeBuff(411);
                            disposeBuff(444);

                            specialistInstance.Design = (byte)EffectValue;

                            session.Character.MorphUpgrade2 = EffectValue;
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                            session.SendPacket(session.Character.GenerateStat());
                            session.SendPacket(session.Character.GenerateStatChar());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"), 0));
                    }
                    break;

                // Self-Introduction
                case 203:
                    if (!session.Character.IsVehicled && Option == 0)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 2, session.Character.CharacterId, 1));
                    }
                    break;

                // Magic Lamp
                case 651:
                    if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                    {
                        if (Option == 0)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_USE")}");
                        }
                        else
                        {
                            session.Character.ChangeSex();
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                    }
                    break;

                // Vehicles
                case 1000:
                    if (EffectValue != 0 || ServerManager.Instance.ChannelId == 51 || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.EventGameInstance)
                    {
                        return;
                    }
                    if (Morph > 0)
                    {
                        if (Option == 0 && !session.Character.IsVehicled)
                        {
                            if (session.Character.IsSitting)
                            {
                                session.Character.IsSitting = false;
                                session.CurrentMapInstance?.Broadcast(session.Character.GenerateRest());
                            }
                            session.Character.LastDelay = DateTime.Now;
                            session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 3, $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2"));
                        }
                        else
                        {
                            if (!session.Character.IsVehicled && Option != 0)
                            {
                                DateTime delay = DateTime.Now.AddSeconds(-4);
                                if (session.Character.LastDelay > delay && session.Character.LastDelay < delay.AddSeconds(2))
                                {
                                    session.Character.Speed = Speed;
                                    session.Character.IsVehicled = true;
                                    session.Character.VehicleSpeed = Speed;
                                    session.Character.MorphUpgrade = 0;
                                    session.Character.MorphUpgrade2 = 0;
                                    session.Character.Morph = Morph + (byte)session.Character.Gender;
                                    session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 196), session.Character.MapX, session.Character.MapY);
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                    session.SendPacket(session.Character.GenerateCond());
                                    session.Character.LastSpeedChange = DateTime.Now;
                                }
                            }
                            else if (session.Character.IsVehicled)
                            {
                                session.Character.RemoveVehicle();
                            }
                        }
                    }
                    break;

                // Sealed Vessel
                case 1002:
                    if (EffectValue == 69)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1000);
                        if (rnd < 5)
                        {
                            short[] vnums =
                            {
                                5560, 5591, 4099, 907, 1160, 4705, 4706, 4707, 4708, 4709, 4710, 4711, 4712, 4713, 4714,
                                4715, 4716
                            };
                            byte[] counts = { 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            int item = ServerManager.RandomNumber(0, 17);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                        }
                        else if (rnd < 30)
                        {
                            short[] vnums = { 361, 362, 363, 366, 367, 368, 371, 372, 373 };
                            session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 9)], 1);
                        }
                        else
                        {
                            short[] vnums =
                            {
                                1161, 2282, 1030, 1244, 1218, 5369, 1012, 1363, 1364, 2160, 2173, 5959, 5983, 2514,
                                2515, 2516, 2517, 2518, 2519, 2520, 2521, 1685, 1686, 5087, 5203, 2418, 2310, 2303,
                                2169, 2280, 5892, 5893, 5894, 5895, 5896, 5897, 5898, 5899, 5332, 5105, 2161, 2162
                            };
                            byte[] counts =
                            {
                                10, 10, 20, 5, 1, 1, 99, 1, 1, 5, 5, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 5, 20,
                                20, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
                            };
                            int item = ServerManager.RandomNumber(0, 42);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                        }
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else if (session.HasCurrentMapInstance && session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && (session.Character.LastVessel.AddSeconds(1) <= DateTime.Now || session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.FastVessels)))
                    {
                        short[] vnums = { 1386, 1387, 1388, 1389, 1390, 1391, 1392, 1393, 1394, 1395, 1396, 1397, 1398, 1399, 1400, 1401, 1402, 1403, 1404, 1405 };
                        short vnum = vnums[ServerManager.RandomNumber(0, 20)];

                        NpcMonster npcmonster = ServerManager.GetNpc(vnum);
                        if (npcmonster == null)
                        {
                            return;
                        }
                        MapMonster monster = new MapMonster
                        {
                            MonsterVNum = vnum,
                            MapY = session.Character.MapY,
                            MapX = session.Character.MapX,
                            MapId = session.Character.MapInstance.Map.MapId,
                            Position = session.Character.Direction,
                            IsMoving = true,
                            MapMonsterId = session.CurrentMapInstance.GetNextMonsterId(),
                            ShouldRespawn = false
                        };
                        monster.Initialize(session.CurrentMapInstance);
                        session.CurrentMapInstance.AddMonster(monster);
                        session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.LastVessel = DateTime.Now;
                    }
                    break;

                // Golden Bazaar Medal
                case 1003:
                    if (!session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BazaarMedalGold
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Silver Bazaar Medal
                case 1004:
                    if (!session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalGold))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BazaarMedalSilver
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Pet Slot Expansion
                case 1006:
                    if (Option == 0)
                    {
                        session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2 {Language.Instance.GetMessageFromKey("ASK_PET_MAX")}");
                    }
                    else if (session.Character.MaxMateCount < 30)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GET_PET_PLACES"), 10));
                        session.SendPacket(session.Character.GenerateScpStc());
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                // Pet Basket
                case 1007:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBasket))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.PetBasket
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket("ib 1278 1");
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Partner's Backpack
                case 1008:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.PetBackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Backpack Expansion
                case 1009:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Sealed Tarot Card
                case 1005:
                    session.Character.GiftAdd((short)(VNum - Effect), 1);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Tarot Card Game
                case 1894:
                    if (EffectValue == 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            session.Character.GiftAdd((short)(Effect + ServerManager.RandomNumber(0, 10)), 1);
                        }
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                // Sealed Tarot Card
                case 2152:
                    session.Character.GiftAdd((short)(VNum + Effect), 1);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                case 998:
                    {
                        session.Character.AddBuff(new Buff(885, 1));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                case 2102:
                    if (EffectValue == 27)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                4012,4015,4018,349,353,356,355,352,351
                            };
                            byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 9);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                // Shellsystembox

                case 2402:
                    if (EffectValue == 15)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                576,573,585,588
                            };
                            byte[] counts = { 1, 1, 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 4);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;


                // Klangblume

                case 4048:
                    session.Character.AddBuff(new Buff(378, 3));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Truhe De Lvl 95-96
                case 2101:
                    if (EffectValue == 28)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            //#Check if he gets really the VNUM's that are called in the piece of code
                            short[] vnums =
                            {
                                4904,4913,4922,4901,4919,4910,4907,4925,4916
                            };
                            byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 9);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                // Truhe de Lvl 92-93

                case 2100:
                    if (EffectValue == 26)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            //#Check if he gets really the VNUM's that are called in the piece of code
                            short[] vnums =
                            {
                                4900,4909,4918,4903,4909,4921,4906,4915,4924
                            };
                            byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 9);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;


                // Dies ist die lvl40-70 Lvl Map / EQ Truhe

                case 2099:
                    if (EffectValue == 25)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                404,410,405,264,409,761,407,411,408
                            };
                            byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 9);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                case 9998:
                    if (EffectValue == 12)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                573,572
                            };
                            byte[] counts = { 1, 1 };
                            int rare = ServerManager.RandomNumber(0, 7);
                            int item = ServerManager.RandomNumber(0, 2); // sry bin bhd XD
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                // For the Beginners

                case 388:
                    ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg("{0} hat erfolgreich das Beginner-Packet geöffnet!", 0));
                    session.Character.GiftAdd(1, 1, 7, 10, 0, false, 0);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Feen erfahrungstrank

                case 6235:
                    if (!session.Character.Buff.ContainsKey(393))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 393 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;


                //Buff de Abenteurer
                case 6234:
                    session.Character.AddBuff(new Buff(155, 3));
                    session.Character.AddBuff(new Buff(156, 3));
                    break;

                // 200 Segi // 5370 buff keine ahnung alter
                case 209:
                    if (!session.Character.Buff.ContainsKey(393) && !session.Character.Buff.ContainsKey(4000))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 393 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                //Segen des Schutzengels

                case 1285:
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 122 });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                //Sp Blessing

                case 1362:
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                // 5204 Vollmond

                case 9010:
                    if (EffectValue == 9010)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                1030, 1246, 1247, 1248
                            };
                            byte[] counts = { 100, 1, 1, 1 };
                            int item = ServerManager.RandomNumber(0, 4);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                case 9011:
                    if (EffectValue == 9011)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                2282, 1246, 1247, 1248
                            };
                            byte[] counts = { 100, 1, 1, 1 };
                            int item = ServerManager.RandomNumber(0, 4);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                case 9012:
                    if (EffectValue == 9012)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                5560, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246, 1247, 1248, 1246
                            };
                            byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, };
                            int item = ServerManager.RandomNumber(0, 50);
                            //session.Character.Reputation += 20000;
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;
                case 13371://arenawinner item
                    {
                        session.Character.ArenaWinner = session.Character.ArenaWinner == 0 ? 1 : 0;
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("u can use this item again to turn on/off the wings"), 10));
                        break;
                    }
                case 13372://level 90-99 item
                    {
                        if (session.Character.Level >= 90 && session.Character.Level < 99)
                        {
                            session.Character.Level++;
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6), session.Character.PositionX, session.Character.PositionY);
                            session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("You Reached Level: " + session.Character.Level), 0);
                            session.SendPacket(session.Character.GenerateLev());
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        }
                        if (session.Character.HeroLevel < 1)
                        {
                            session.Character.HeroLevel++;
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("NOT_REQUIRED_LEVEL"), session.Character.Name), 0));
                        }

                        break;


                    }

                case 13373://sealed treasure chest
                    {
                        int rnd = ServerManager.RandomNumber(0, 1000);

                        short[] vnums = null;
                        if (rnd < 970 && rnd > 5)
                        {
                            vnums = new short[] { 1012, 1244, 105, 1010, 1246, 1247, 1247 };
                            byte[] counts = { 1, 5, 10, 5, 10, 3, 3, 3 };
                            int item = ServerManager.RandomNumber(0, 7);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else if (rnd <= 1)
                        {
                            short[] vnums3 = null;
                            vnums3 = new short[] { 429, 4304, 5827 };
                            byte[] counts3 = { 1, 1, 1 };
                            int item3 = ServerManager.RandomNumber(0, 3);
                            session.Character.GiftAdd(vnums[item3], counts3[item3]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            short[] vnums2 = null;
                            vnums2 = new short[] { 2440, 2441, 2442, 2443, 1286, 1296, 5370, 5931, 5914, 1965, 2604, 2434, 2392, 5911};
                            byte[] counts2 = { 4, 4, 4, 4, 3, 5, 2, 1, 1, 1, 10 , 10, 10, 10};
                            int item2 = ServerManager.RandomNumber(0, 14);
                            session.Character.GiftAdd(vnums[item2], counts2[item2]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }

                        break;
                    }

                case 13374: // Fairy Costume Set
                    {
                        session.Character.GiftAdd(4439, 1);
                        session.Character.GiftAdd(4441, 1);
                        session.Character.GiftAdd(4443, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13375: // Fire Devil Costume
                    {
                        session.Character.GiftAdd(4409, 1);
                        session.Character.GiftAdd(4411, 1);
                        session.Character.GiftAdd(4435, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13376: // White Tiger Costume Set
                    {
                        session.Character.GiftAdd(4248, 1);
                        session.Character.GiftAdd(4256, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13377: // Football Set
                    {
                        session.Character.GiftAdd(4195, 1);
                        session.Character.GiftAdd(4196, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13379: // Police Costume Set
                    {
                        session.Character.GiftAdd(4282, 1);
                        session.Character.GiftAdd(4285, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13378: // Cuddly Tiger Costume Set
                    {
                        session.Character.GiftAdd(4244, 1);
                        session.Character.GiftAdd(4252, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13380: // Magic Jaguar Box
                    {
                        session.Character.GiftAdd(5834, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13381: // Mother Nature Rune Packet
                    {
                        session.Character.GiftAdd(4356, 1);
                        session.Character.GiftAdd(4357, 1);
                        session.Character.GiftAdd(4358, 1);
                        session.Character.GiftAdd(4359, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13382: // Groovy Beach Costume Set
                    {
                        session.Character.GiftAdd(4266, 1);
                        session.Character.GiftAdd(4268, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13399: // Portier Uniform-Set
                    {
                        session.Character.GiftAdd(4287, 1);
                        session.Character.GiftAdd(4289, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13383: // Magic Donkey Pinata Box
                    {
                        session.Character.GiftAdd(5743, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13301: // Wedding Box
                    {
                        session.Character.GiftAdd(1981, 1);
                        session.Character.GiftAdd(982, 1);
                        session.Character.GiftAdd(982, 1);
                        session.Character.GiftAdd(984, 1);
                        session.Character.GiftAdd(984, 1);
                        session.Character.GiftAdd(1984, 10);
                        session.Character.GiftAdd(1986, 10);
                        session.Character.GiftAdd(1988, 10);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13302: // Oto-Fox Costume Set
                    {
                        session.Character.GiftAdd(4177, 1);
                        session.Character.GiftAdd(4179, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13303: // Aqua Bushtail Costume Set
                    {
                        session.Character.GiftAdd(4064, 1);
                        session.Character.GiftAdd(4065, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13304: // Illusionist Costume Set
                    {
                        session.Character.GiftAdd(4258, 1);
                        session.Character.GiftAdd(4260, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13305: // Viking Costume Set
                    {
                        session.Character.GiftAdd(4301, 1);
                        session.Character.GiftAdd(4303, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13384://herolevel 75 item
                    {
                        byte Hlvl = session.Character.HeroLevel;
                        if (Hlvl >= 50)
                        {
                            if (Hlvl < 75)
                            {
                                session.Character.HeroLevel += 1;
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6), session.Character.PositionX, session.Character.PositionY);
                                session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 198), session.Character.PositionX, session.Character.PositionY);
                                session.SendPacket(session.Character.GenerateLev());
                                session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                                session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            else
                            {
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("You are already Hero Lvl 75!"), 0);
                            }

                        }
                        else
                        {
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("You need atleast Hero Lvl 50 to use this!"), 0);
                        }
                        break;
                    }
                case 13385://donator 550% feen box
                    {
                        session.Character.GiftAdd(4713, 1, 0, 0, 0, false, 0, 200);
                        session.Character.GiftAdd(4714, 1, 0, 0, 0, false, 0, 200);
                        session.Character.GiftAdd(4715, 1, 0, 0, 0, false, 0, 200);
                        session.Character.GiftAdd(4716, 1, 0, 0, 0, false, 0, 200);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }
                case 13386: // Fernon Mystery Box
                    {
                        int rnd = ServerManager.RandomNumber(0, 1000);
                        short[] vnums = null;
                        vnums = new short[] { 5560, 5553, 442, 5950, 4315, 5831, 5841, 5604, 429, 4425, 9675, 5837, 5702, 2096 };
                        byte[] counts = { 1, 1, 1, 5, 1, 1, 1, 1, 1, 1, 5, 1, 1, 2 };
                        int item = ServerManager.RandomNumber(0, 13);
                        session.Character.GiftAdd(vnums[item], counts[item]);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                        break;
                    }


                case 13387: //cali box
                    {
                        short[] vnums = null;
                        vnums = new short[] { 2282, 1030, 1296, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 1429, 4487, 4488, 4489, 5572 };
                        byte[] counts = { 30, 30, 5, 1, 1, 1, 1 , 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 10, 1, 1, 1, 1};
                        int item = ServerManager.RandomNumber(0, 32);
                        session.Character.GiftAdd(vnums[item], counts[item]);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }

                case 13388: // laurena box
                    {
                        short[] vnums = null;
                        vnums = new short[] { 4699, 5835, 5836, 5837, 2282, 1030, 1296, 4491, 4492, 4493, 5604 };
                        byte[] counts = {1, 1, 1, 1, 50, 50, 5, 1, 1, 1, 1};
                        int item = ServerManager.RandomNumber(0, 11);
                        session.Character.GiftAdd(vnums[item], counts[item]);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                        
                    }
                    

                case 4040: // Mother Nature Rune Packet
                    if (EffectValue == 5050)
                    {
                        int rnd = ServerManager.RandomNumber(0, 1);
                        if (rnd < 1)
                        {
                            short[] vnums =
                            {
                                4713, 4714, 4715, 4716
                            };
                            byte[] counts = { 1, 1, 1, 1 };
                            int item = ServerManager.RandomNumber(0, 4);
                            session.Character.GiftAdd(vnums[item], counts[item]);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                default:
                    switch (VNum)
                    {
                        case 5841:
                            int rnd = ServerManager.RandomNumber(0, 1000);
                            short[] vnums = null;
                            if (rnd < 900)
                            {
                                vnums = new short[] { 4356, 4357, 4358, 4359 };
                            }
                            else
                            {
                                vnums = new short[] { 4360, 4361, 4362, 4363 };
                            }
                            session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 4)], 1);
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            break;

                        default:
                            Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType(), VNum, Effect, EffectValue));
                            break;
                    }
                    break;
            }
        }

        #endregion
    }
}